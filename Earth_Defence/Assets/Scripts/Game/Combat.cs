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

        /// <summary>공격력 = 기지 레벨 + 장비 · 칩 (판이 시작할 때 Battle 이 계산해 둠)</summary>
        public static double Atk => Battle.I != null && Battle.I.atk > 0 ? Battle.I.atk : SaveData.Cur != null ? SaveData.Cur.Atk : Tables.C("Atk_Base", 100);

        /// <summary>2차 조건 피해 칩 (일반 브래킷에 더함 — 기획서 6-3 · 9-3)</summary>
        static double CondBonus(TurretInst t, Enemy e)
        {
            var b = Battle.I; if (b == null || b.lo == null || b.lo.cond.Count == 0) return 0;
            var lo = b.lo; double v = lo.C("dmg") + lo.C(ElKey(t.row.elem));
            if (lo.C("hp70") > 0 && e.hp >= e.maxHp * 0.7) v += lo.C("hp70");
            if (lo.C("elite_dmg") > 0 && (e.row.IsElite || e.row.IsBoss)) v += lo.C("elite_dmg");
            if (lo.C("early") > 0 && b.time < 120f) v += lo.C("early");
            if (lo.C("low_hp") > 0 && b.baseHp < b.baseMax * 0.3) v += lo.C("low_hp");
            if (lo.C("debuff") > 0 && e.Debuffed) v += lo.C("debuff");
            if (lo.C("near") > 0 && e.Pos.y < Tables.Cf("Pos_Near_Y")) v += lo.C("near");
            if (lo.C("boss_on") > 0 && b.BossOnField) v += lo.C("boss_on");
            v += lo.C("per_turret") * (b.run.turrets.Count - 1) + lo.C("per_card") * b.picks;
            return v / 100.0;
        }

        /// <summary>속성 피해 회로 키 — 맞을 때마다 "el_" + 속성 글자를 새로 만들던 것을 고정 글자로 (11-17)</summary>
        static string ElKey(string elem) => elem switch { "phys" => "el_phys", "light" => "el_light", "grav" => "el_grav", "fire" => "el_fire", "bolt" => "el_bolt", _ => "el_" + elem };

        public static double Calc(TurretInst t, Enemy e, double ratePct, out bool crit)
        {
            double ordinary = t.Ordinary + CondBonus(t, e);
            if (e.Paralyzed) ordinary += t.S("dmg_vs_para") / 100.0;
            if (t.Has("dmg_by_slow")) ordinary += t.S("dmg_by_slow") / 100.0 * Mathf.Clamp01(e.slow / Mathf.Max(0.01f, Tables.Cf("Slow_Max", 0.5f)));
            double late = t.late;   // 3차: 스킨 Final DMG (가지고 있으면)
            crit = R < t.CritRate;
            double critMul = crit ? Tables.C("Crit_Dmg", 1.25) + (Battle.I?.lo?.critDmg ?? 0) / 100.0 : 1;
            // 밸런스 2차: 스테이지 약점(Stage 표 Weak) — 이 스테이지 적은 모두(보스 포함) 그 속성에 약함. 적 자기 약점과 같은 값(Weak_Mul), 겹치진 않음
            var bs = Battle.I; bool stageWeak = bs != null && bs.stage != null && bs.IsStage && !string.IsNullOrEmpty(bs.stage.weak) && bs.stage.weak == t.row.elem;
            double weak = e.row.weak == t.row.elem || stageWeak ? Tables.C("Weak_Mul", 1.5) : e.row.resist == t.row.elem ? Tables.C("Resist_Mul", 0.6) : 1;
            float y = e.Pos.y;
            double pos = y < Tables.Cf("Pos_Near_Y") ? Tables.C("Pos_Near", 1.1) : y > Tables.Cf("Pos_Far_Y") ? Tables.C("Pos_Far", 0.9) : 1;
            double d = Atk * ratePct / 100.0 * (1 + ordinary) * (1 + late) * critMul * weak * pos * t.Penalty * (1 + e.vuln) * (1 - e.row.armor);
            // 칩 "치명 때 적 최대 체력 N% 더" (상한 = 원래 피해 x Crit_Hp_Cap)
            double ch = Battle.I?.lo?.B("crit_hp") ?? 0;
            if (crit && ch > 0) d += Math.Min(e.maxHp * ch / 100.0, d * (Tables.C("Crit_Hp_Cap", 4) - 1));
            return d;
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
            if (src != null) src.dmgDone += d;
            if (number) Audio.Play(Audio.Sfx.Hit);
            if (number && (SaveData.Cur == null || SaveData.Cur.nums)) Fx.Number(e.Pos + Vector2.up * e.radius, d, crit, e);
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
            // 2차: 밀어내기 카드(쾅쾅 대포가 아닌 터렛 — 대포는 자기 폭발에서 밀어냄)
            if (!killed && t.Has("knock") && t.row.kind != "cannon") e.Knock(Tables.Cf("Knock_Base", 0.6f) * (float)t.S("knock") / 100f);
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
            if (t.Has("kill_chain") && R < t.S("kill_chain")) Chain(t, e, 2, Tables.C("Chain_Rate", 50));
            if (t.Has("kill_explode") && R < t.S("kill_explode")) Explode(t, at, Tables.Cf("Explode_Radius", 0.9f) * 1.3f, Tables.C("Explode_Rate", 60) * 2);
            // 11-16 저녁: 벌도 적이 죽은 자리 대신 벌집에서 날아감 (사용자 "벌집에서 나가게")
            if (t.Has("kill_bee") && R < t.S("kill_bee")) Battle.I?.Add(new Bee(t, HiveHead(at), 3, Tables.TurretById["hive"].rate * 0.5));
            if (t.row.kind == "hive" && t.Has("echo")) Battle.I?.Add(new Bee(t, HiveHead(at), 3 + (int)t.S("echo_hits"), t.row.rate * 0.6, echo: true));
        }

        /// <summary>둥근 폭발 (곁가지 — 효과 없음)</summary>
        public static void Explode(TurretInst t, Vector2 at, float radius, double ratePct, Color? c = null)
        {
            Fx.Burst(at, radius, c);
            Audio.Play(Audio.Sfx.Boom, Mathf.Clamp(radius / 1.2f, 0.4f, 1f));
            var list = Battle.I?.enemies;
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var e = list[i];
                if (!e.dead && Vector2.Distance(e.Pos, at) <= radius + e.radius) Hit(t, e, ratePct, false, false);
            }
        }

        static readonly System.Collections.Generic.HashSet<Enemy> chainHit = new();
        /// <summary>연쇄 번개: 맞은 적에서 가까운 적으로 n 번 튐</summary>
        public static void Chain(TurretInst t, Enemy from, int n, double ratePct)
        {
            var b = Battle.I; if (b == null) return;
            Vector2 p = from.Pos;
            var hit = chainHit; hit.Clear(); hit.Add(from);     // 11-17: 연쇄마다 새로 만들던 목록을 다시 씀 (연쇄 안의 공격은 효과를 다시 일으키지 않아 겹쳐 불리지 않음)
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
