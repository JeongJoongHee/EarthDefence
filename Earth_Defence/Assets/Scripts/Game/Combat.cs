using System;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 피해 공식 (기획서 6-3): ATK x Rate x (1 + 일반 합) x (1 + 후반 합) x 치명 x 약점 x 거리 — 여기에 탄 +1 감소 · 취약 · 장갑.
    /// 맞힐 때 붙는 효과(둔화 · 마비 · 화상 · 취약 · 폭발 · 작은 레이저 · 연쇄)와 처치 때 효과(작은 낙뢰 · 벌 · 폭발 · 메아리 벌).
    /// 폭발·연쇄 같은 곁가지 공격은 procs=false 로 다시 효과를 일으키지 않음 (끝없이 이어지지 않게).
    /// </summary>
    public static class Combat
    {
        static System.Random rnd = new(7);
        public static void Seed(int s) => rnd = new System.Random(s);
        public static double R => rnd.NextDouble();

        public static double Atk => SaveData.Cur != null ? SaveData.Cur.Atk : Tables.C("Atk_Base", 100);

        public static double Calc(TurretInst t, Enemy e, double ratePct, out bool crit)
        {
            double ordinary = t.Ordinary;
            if (e.Paralyzed) ordinary += t.S("dmg_vs_para") / 100.0;
            if (t.Has("dmg_by_slow")) ordinary += t.S("dmg_by_slow") / 100.0 * Mathf.Clamp01(e.slow / Mathf.Max(0.01f, Tables.Cf("Slow_Max", 0.5f)));
            double late = 0;   // 2차: S1 칩 · 스킨 Final DMG 자리
            crit = R < t.CritRate;
            double critMul = crit ? Tables.C("Crit_Dmg", 1.25) : 1;
            double weak = e.row.weak == t.row.elem ? Tables.C("Weak_Mul", 1.5) : e.row.resist == t.row.elem ? Tables.C("Resist_Mul", 0.6) : 1;
            float y = e.Pos.y;
            double pos = y < Tables.Cf("Pos_Near_Y") ? Tables.C("Pos_Near", 1.1) : y > Tables.Cf("Pos_Far_Y") ? Tables.C("Pos_Far", 0.9) : 1;
            return Atk * ratePct / 100.0 * (1 + ordinary) * (1 + late) * critMul * weak * pos * t.Penalty * (1 + e.vuln) * (1 - e.row.armor);
        }

        /// <summary>터렛 공격 1번 → 피해 + 효과. 죽였으면 true</summary>
        public static bool Hit(TurretInst t, Enemy e, double ratePct, bool procs = true, bool number = true)
        {
            if (e == null || e.dead || t == null) return false;
            double d = Calc(t, e, ratePct, out bool crit);
            if (procs && t.Has("fire_add")) d *= 1 + t.S("fire_add") / 100.0;
            Vector2 at = e.Pos;
            bool killed = Raw(e, d, crit, t, number);
            if (procs) OnHit(t, e, at, killed);
            if (killed && procs) OnKill(t, e, at);
            return killed;
        }

        /// <summary>피해만 (화상 등). 죽으면 Battle 에 알림</summary>
        public static bool Raw(Enemy e, double d, bool crit, TurretInst src, bool number = true)
        {
            if (e == null || e.dead) return false;
            bool killed = e.Damage(d);
            if (number) Fx.Number(e.Pos + Vector2.up * e.radius, d, crit, e);
            if (killed) Battle.I?.OnKill(e, src);
            return killed;
        }

        static void OnHit(TurretInst t, Enemy e, Vector2 at, bool killed)
        {
            if (!killed)
            {
                if (t.Has("slow")) e.AddSlow((float)t.S("slow") / 100f, 2f, Tables.Cf("Slow_Max", 0.5f));
                if (t.Has("paralyze") && t.row.kind != "field" && R < t.S("paralyze"))
                    e.Paralyze(Tables.Cf("Para_Time", 1f) * (1 + (float)t.S("para_time") / 100f));
                if (t.Has("burn")) e.Burn(Atk * Tables.C("Burn_Rate", 40) / 100.0 * (1 + t.Ordinary) * (1 + t.S("burn_amp") / 100.0), Tables.Cf("Burn_Time", 3f), t);
                if (t.Has("vuln")) e.Vulnerable(t.S("vuln") / 100.0, Tables.Cf("Vuln_Time", 4f));
            }
            if (t.Has("explode") && R < t.S("explode"))
                Explode(t, at, Tables.Cf("Explode_Radius", 0.9f) * (1 + (float)t.S("explode_area") / 100f), Tables.C("Explode_Rate", 60) * (1 + t.S("explode_dmg") / 100.0));
            if (t.Has("hit_laser") && R < t.S("hit_laser"))
            {
                // 11-16: 맞은 자리에서 아무 방향으로 나가던 작은 레이저 → 레이저 포에서 그 적을 향해 (사용자 "타워에서 나갈 수 있도록")
                var lu = Battle.I?.run.Get("laser")?.unit;
                Vector2 o = lu ? lu.Head : (t.unit ? t.unit.Head : at + Vector2.down * 2f);
                var dv = at - o;
                Battle.I?.Add(new Beam(t, o, Mathf.Atan2(dv.y, dv.x) * Mathf.Rad2Deg, 0.28f, 0.28f, 0.2f, Tables.C("Mini_Laser_Rate", 80), mini: true, len: dv.magnitude + 0.6f));
            }
            if (t.Has("chain")) Chain(t, e, (int)t.S("chain"), Tables.C("Chain_Rate", 50) * (1 + t.S("chain_dmg") / 100.0));
            if (t.Has("hit_chain") && R < t.S("hit_chain")) Chain(t, e, 2, Tables.C("Chain_Rate", 50));
        }

        static Vector2 HiveHead(Vector2 fallback)
        {
            var hu = Battle.I?.run.Get("hive")?.unit;
            return hu ? hu.Head + new Vector2(UnityEngine.Random.Range(-0.3f, 0.3f), 0) : fallback;
        }

        static void OnKill(TurretInst t, Enemy e, Vector2 at)
        {
            if (t.Has("kill_bolt") && R < t.S("kill_bolt")) Battle.I?.Add(new Strike(t, at.x, 0.5f, 0.3f, 0.3f, Tables.C("Mini_Bolt_Rate", 120), mini: true, y: at.y));
            if (t.Has("kill_explode") && R < t.S("kill_explode")) Explode(t, at, Tables.Cf("Explode_Radius", 0.9f) * 1.3f, Tables.C("Explode_Rate", 60) * 2);
            // 11-16 저녁: 벌도 적이 죽은 자리 대신 벌집에서 날아감 (사용자 "벌집에서 나가게")
            if (t.Has("kill_bee") && R < t.S("kill_bee")) Battle.I?.Add(new Bee(t, HiveHead(at), 3, Tables.TurretById["hive"].rate * 0.5));
            if (t.row.kind == "hive" && t.Has("echo")) Battle.I?.Add(new Bee(t, HiveHead(at), 3 + (int)t.S("echo_hits"), t.row.rate * 0.6, echo: true));
        }

        /// <summary>둥근 폭발 (곁가지 — 효과 없음)</summary>
        public static void Explode(TurretInst t, Vector2 at, float radius, double ratePct, Color? c = null)
        {
            Fx.Burst(at, radius, c);
            var list = Battle.I?.enemies;
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var e = list[i];
                if (!e.dead && Vector2.Distance(e.Pos, at) <= radius + e.radius) Hit(t, e, ratePct, false, false);
            }
        }

        /// <summary>연쇄 번개: 맞은 적에서 가까운 적으로 n 번 튐</summary>
        public static void Chain(TurretInst t, Enemy from, int n, double ratePct)
        {
            var b = Battle.I; if (b == null) return;
            Vector2 p = from.Pos;
            var hit = new System.Collections.Generic.HashSet<Enemy> { from };
            float range = Tables.Cf("Chain_Range", 2.4f);
            for (int k = 0; k < n; k++)
            {
                Enemy best = null; float bd = range;
                foreach (var e in b.enemies)
                {
                    if (e.dead || hit.Contains(e)) continue;
                    float d = Vector2.Distance(p, e.Pos);
                    if (d < bd) { bd = d; best = e; }
                }
                if (best == null) break;
                hit.Add(best);
                b.Add(new Zap(p, best.Pos));
                p = best.Pos;
                Hit(t, best, ratePct, false);
            }
        }
    }
}
