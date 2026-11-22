using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EarthDef
{
    // =====================================================================================
    // 11-22 새 타워 6종의 공격 물체 (타워_전체_정리.html #13~#18 · 원본 타워 추가 컨셉.xlsx)
    //  별빛 부메랑 Boomerang · 서리 눈꽃포 Frost.Blast · 자기장 톱날 SawBlade · 유성 박격포 MeteorFall · 곁가지 DelayBlast
    //  용암 화염방사기는 Beam(flame) + Zone · 별빛 증폭탑은 Battle.PrismPulse (Battle12.cs)
    // =====================================================================================

    /// <summary>별빛 부메랑: 사거리 끝까지 날아가며 길 위의 적을 모두 꿰뚫고, 되돌아오며 한 번 더 (돌아올 땐 맞은 적 목록을 비움).
    /// 정점 회전(돌파 A) = 돌아설 곳에서 잠깐 돌며 틱 피해 · 쌍부메랑(돌파 B) = 좌우 곡선(curve) · 별가루 궤적 = 길에 늦게 터지는 파편</summary>
    public class Boomerang : Attack
    {
        Vector2 home, dir, pos, side; float speed, dist, r, t, curve, apexT, apexTick, trailT; double rate;
        int phase;   // 0 = 나감 · 1 = 정점 회전 · 2 = 돌아옴
        readonly HashSet<Enemy> hitSet = new();
        public Action onHome;
        public static int LastReturns, LastApexTicks;   // 시험용
        readonly TurretUnit owner;

        public Boomerang(TurretInst tr, Vector2 from, Vector2 d, float speed, float dist, float radius, double rate, float curve = 0, float scale = 1f)
        {
            src = tr; owner = tr.unit; home = from; pos = from; dir = d.normalized; this.speed = speed; this.dist = dist; r = radius; this.rate = rate; this.curve = curve;
            side = new Vector2(-dir.y, dir.x);
            MakeSprite("boomerang", Art.Fx("boomerang"), from, 31);
            go.transform.localScale = Vector3.one * (radius / 0.55f) * 0.9f * scale;
            var g = new GameObject("glow"); g.transform.SetParent(go.transform, false);
            var gs = g.AddComponent<SpriteRenderer>(); gs.sprite = Art.Fx("glow"); gs.sortingOrder = 30; Art.Additive(gs);
            gs.color = new Color(1f, 0.86f, 0.45f, 0.45f); g.transform.localScale = Vector3.one * 1.3f;
        }

        /// <summary>나가는 길(0~1)의 자리 — 곡선이면 옆으로 둥글게</summary>
        Vector2 Path(float k) => home + dir * dist * k + side * curve * Mathf.Sin(k * Mathf.PI);

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt;
            go.transform.rotation = Quaternion.Euler(0, 0, -t * 900f);
            if (owner) home = owner.Head;
            float travel = dist / Mathf.Max(0.1f, speed);
            if (phase == 0)
            {
                float k = Mathf.Clamp01(t / travel);
                // 끝에 가까울수록 느려짐 (던진 부메랑처럼)
                pos = Path(1 - (1 - k) * (1 - k));
                if (k >= 1)
                {
                    hitSet.Clear();
                    if (src.Has("hover_apex")) { phase = 1; apexT = Tables.Cf("Boomerang_Apex_Time", 1.2f) * (1 + (float)src.S("apex_dur") / 100f); }
                    else phase = 2;
                    t = 0;
                }
            }
            else if (phase == 1)
            {
                apexT -= dt; apexTick -= dt;
                go.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 1600f);
                if (apexTick <= 0)
                {
                    apexTick = 0.25f; LastApexTicks++;
                    double ar = Tables.C("Boomerang_Apex_Rate", 120) * (1 + src.S("apex_dmg") / 100.0) * (rate / Mathf.Max(1f, src.row.rate));
                    foreach (var e in Battle.I.enemies) if (!e.dead && Vector2.Distance(e.Pos, pos) <= r * 1.3f + e.radius) Combat.Hit(src, e, ar, false, true);
                    Fx.Ring(pos, r * 1.3f, Art.Hex("#ffe082"));
                }
                if (apexT <= 0) { phase = 2; t = 0; hitSet.Clear(); }
            }
            else
            {
                var to = home - pos; float step = speed * 1.15f * dt;
                if (to.magnitude <= step + 0.2f) { LastReturns++; onHome?.Invoke(); Kill(); return; }
                pos += to.normalized * step;
            }
            go.transform.position = pos;
            if (src.Has("trail_dmg") && (trailT -= dt) <= 0 && phase != 1)
            {
                trailT = 0.3f;
                Battle.I.Add(new DelayBlast(src, pos, 0.5f, 0.8f, src.S("trail_dmg"), Art.Hex("#ffe9a6")));
            }
            double hr = phase == 2 ? rate * (1 + src.S("return_dmg") / 100.0) : rate;
            var list = Battle.I.enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.dead || hitSet.Contains(e) || (e.Pos - pos).sqrMagnitude > (e.radius + r) * (e.radius + r)) continue;
                hitSet.Add(e);
                Combat.Hit(src, e, hr, true, true);
                Fx.Sparks(e.Pos, Art.Hex("#ffe08a"), 3, 4f, 0.8f);
            }
        }
    }

    /// <summary>늦게 터지는 작은 폭발 (부메랑 별가루 · 유성 파편) — 곁가지</summary>
    public class DelayBlast : Attack
    {
        Vector2 pos; float delay, r; double rate; readonly Color col;
        public DelayBlast(TurretInst tr, Vector2 p, float delay, float radius, double rate, Color c, string sprite = "dot", float scale = 0.35f)
        {
            src = tr; pos = p; this.delay = delay; r = radius; this.rate = rate; col = c;
            MakeSprite("delay", Art.Fx(sprite), p, 30);
            Art.Additive(sr); sr.color = new Color(c.r, c.g, c.b, 0.85f); go.transform.localScale = Vector3.one * scale;
        }
        public override void Step(float dt)
        {
            if (dead) return;
            delay -= dt;
            go.transform.localScale *= 1 + dt * 0.8f;
            if (delay > 0) return;
            Combat.Explode(src, pos, r, rate, col);
            Kill();
        }
    }

    /// <summary>서리 눈꽃포 폭발 (포탄이 맞은 자리): 반경 안 적 모두 피해(효과 있음) + 둔화 · 얼음 파편 · 서리 지대</summary>
    public static class Frost
    {
        public static int LastHits;   // 시험용
        public static void Blast(TurretInst t, Vector2 at, double rate, float radius, bool sub = false)
        {
            var b = Battle.I; if (b == null) return;
            var ice = Art.Hex("#9fe8ff");
            Fx.Burst(at, radius, ice); Fx.Ring(at, radius, Color.Lerp(ice, Color.white, 0.4f));
            Audio.Play(Audio.Sfx.Boom, 0.7f);
            float slowMax = Tables.Cf("Slow_Max", 0.5f) + (float)t.S("slow_max") / 100f;
            int n = 0;
            for (int i = b.enemies.Count - 1; i >= 0; i--)
            {
                if (i >= b.enemies.Count) continue;
                var e = b.enemies[i];
                if (e.dead || Vector2.Distance(e.Pos, at) > radius + e.radius) continue;
                e.AddSlow(Tables.Cf("Frost_Slow", 0.3f), t.row.dur > 0 ? t.row.dur : 2.5f, slowMax);
                Combat.Hit(t, e, rate, !sub, true); n++;
            }
            LastHits = Mathf.Max(LastHits, n);
            if (sub) return;
            int shards = (int)t.S("ice_shard");
            for (int i = 0; i < shards; i++)
            {
                float a = i * Mathf.PI * 2 / shards + 0.4f;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var pr = new Projectile(t, "frost_ball", at + d * 0.3f, d, 7f, rate * 0.45, 0, 0.45f) { life = 0.6f, hitR = 0.14f };
                pr.onHit = (p, en) => { Combat.Hit(t, en, p.rate, false); en.AddSlow(0.25f, 1.5f, slowMax); };
                b.Add(pr);
            }
            if (t.Has("frost_zone"))
            {
                float zr = radius * (1 + (float)t.S("zone_area") / 100f), zt = 3f * (1 + (float)t.S("zone_dur") / 100f);
                var z = new Zone(t, at, zr, zt, 0.5f, Tables.C("Frost_Zone_Rate", 50), ice, 0.35f) { alpha = 0.5f };
                if (t.Has("zone_detonate")) z.onEnd = () => Combat.Explode(t, at, 1.6f * (zr / Mathf.Max(0.1f, t.row.radius)), 220, ice);
                b.Add(z);
            }
        }
    }

    /// <summary>자기장 톱날: 맞힌 자리에 놓여 돌며 틱마다 둘레 피해 (장갑 무시는 Combat). 추적(돌파 A) · 끝날 때 분열(돌파 B)</summary>
    public class SawBlade : Attack
    {
        Vector2 pos; float r, life, t, tick, tickT; double rate; readonly bool mini, track; readonly float pull;
        public static int LastTicks; public static float LastMoved;   // 시험용
        Vector2 start;

        public SawBlade(TurretInst tr, Vector2 p, float radius, float life, float tickSec, double rate, bool mini = false, float pull = 0)
        {
            src = tr; pos = start = p; r = radius; this.life = life; tick = tickSec; this.rate = rate; this.mini = mini; this.pull = pull;
            track = !mini && tr.Has("saw_track");
            MakeSprite("saw", Art.Fx("saw"), p, 6);   // 적 아래 (톱날이 적을 가리지 않게)
            sr.color = new Color(1f, 1f, 1f, 0.9f);
            go.transform.localScale = Vector3.one * (r / 0.95f);
            var g = new GameObject("glow"); g.transform.SetParent(go.transform, false);
            var gs = g.AddComponent<SpriteRenderer>(); gs.sprite = Art.Fx("glow"); gs.sortingOrder = 25; Art.Additive(gs);
            gs.color = new Color(1f, 0.92f, 0.4f, 0.4f); g.transform.localScale = Vector3.one * 1.5f; gs.sortingOrder = 5;
        }

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; life -= dt; tickT -= dt;
            go.transform.rotation = Quaternion.Euler(0, 0, -t * 1100f);
            float a = Mathf.Clamp01(Mathf.Min(t / 0.12f, life / 0.25f));
            go.transform.localScale = Vector3.one * (r / 0.95f) * (0.6f + 0.4f * a);
            if (track)
            {   // 엘리트 · 보스 먼저, 없으면 가까운 적
                Enemy tg = null; float bd = 99f;
                foreach (var e in Battle.I.enemies) if (!e.dead && (e.row.IsBoss || e.row.IsElite)) { float d = Vector2.Distance(e.Pos, pos); if (d < bd) { bd = d; tg = e; } }
                tg ??= Battle.I.Nearest(pos, 6f);
                if (tg != null) pos = Vector2.MoveTowards(pos, tg.Pos, Tables.Cf("Saw_Track_Speed", 2.2f) * dt);
                go.transform.position = pos;
                LastMoved = Mathf.Max(LastMoved, Vector2.Distance(start, pos));
            }
            if (pull > 0) foreach (var e in Battle.I.enemies) if (!e.dead && Vector2.Distance(e.Pos, pos) < r * 1.8f + e.radius) e.Pull(pos, pull * dt);
            if (Random.value < 0.3f) Fx.Sparks(pos + Random.insideUnitCircle * r * 0.8f, Art.Hex("#ffe98a"), 1, 3f, 0.7f);
            if (tickT <= 0)
            {
                tickT += tick;
                LastTicks++;
                foreach (var e in Battle.I.enemies)
                    if (!e.dead && Vector2.Distance(e.Pos, pos) <= r + e.radius) Combat.Hit(src, e, rate, !mini, false);
            }
            if (life <= 0) End();
        }

        void End()
        {
            if (!mini && src.Has("saw_split"))
            {
                Combat.Explode(src, pos, 1.3f, 200 * rate / Math.Max(1.0, Tables.C("Saw_Tick_Rate", 48)), Art.Hex("#ffe27a"));
                for (int i = 0; i < 2; i++)
                {
                    var d = new Vector2(i == 0 ? -1 : 1, 0.3f) * 0.9f;
                    Battle.I.Add(new SawBlade(src, pos + d, r * 0.55f, 1.5f, tick, rate * 0.5, true));
                }
            }
            Fx.Puff(pos, 0.5f);
            Kill();
        }
    }

    /// <summary>유성 박격포 유성: 하늘에서 비스듬히 떨어져 맞은 자리에서 둥글게 폭발 (맞은 적 모두 효과 있음 — 마비 · 취약).
    /// big = 거대 혜성 · 충돌구 · 파편 · 벼락 운석</summary>
    public class MeteorFall : Attack
    {
        Vector2 pos, to, vel; float r, speed; double rate; readonly bool big;
        public static int LastImpacts;   // 시험용

        public MeteorFall(TurretInst tr, Vector2 target, float speed, float radius, double rate, bool big = false, float scale = 1f)
        {
            src = tr; to = target; this.speed = speed; r = radius; this.rate = rate; this.big = big;
            float top = World.I ? World.I.Top + 0.6f : World.SpawnY + 1f;
            pos = new Vector2(target.x + 2.2f, top);
            vel = (to - pos).normalized * speed;
            MakeSprite("meteor", Art.Fx("meteor"), pos, 31);
            go.transform.localScale = Vector3.one * (big ? 1.9f : 1.3f) * scale;   // 11-22 캡처로 보니 작아서 1.3배
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg);
        }

        public override void Step(float dt)
        {
            if (dead) return;
            var step = vel * dt;
            if ((to - pos).magnitude <= step.magnitude) { Impact(); return; }
            pos += step; go.transform.position = pos;
            if (Random.value < 0.6f) Fx.Sparks(pos - vel.normalized * 0.3f, Color.Lerp(Art.Hex("#ff7a2a"), Art.Hex("#ffe08a"), Random.value), 1, 1.5f, 0.7f, 40f, Mathf.Atan2(-vel.y, -vel.x) * Mathf.Rad2Deg);
        }

        void Impact()
        {
            var b = Battle.I; LastImpacts++;
            Fx.Burst(to, r, Art.Hex("#ff9a54")); Fx.Ring(to, r * 1.1f, Art.Hex("#ffcf6b"));
            Audio.Play(Audio.Sfx.Boom, big ? 1f : 0.75f);
            for (int i = b.enemies.Count - 1; i >= 0; i--)
            {
                if (i >= b.enemies.Count) continue;
                var e = b.enemies[i];
                if (!e.dead && Vector2.Distance(e.Pos, to) <= r + e.radius) Combat.Hit(src, e, rate, true, true);
            }
            if (src.Has("hit_bolt") && Combat.R < src.S("hit_bolt")) b.Add(new Strike(src, to.x, 0.5f, 0.3f, 0.3f, Tables.C("Mini_Bolt_Rate", 120), mini: true, y: to.y));
            if (big && src.Has("crater_burn")) b.Add(new Zone(src, to, 1.6f, 3f, 0.5f, 35, Art.Hex("#ff7a3a")) { alpha = 0.55f });
            int sh = (int)src.S("shrapnel");
            for (int i = 0; i < sh; i++)
                b.Add(new DelayBlast(src, to + Random.insideUnitCircle * 1.5f, 0.25f + i * 0.08f, 0.5f, rate * 0.45, Art.Hex("#ffb35c")));
            Kill();
        }
    }

    /// <summary>유성 연발 (3발 + 유성우 포화) — 간격마다 적이 몰린 곳(가까운 적)으로 하나씩</summary>
    public class MeteorVolley : Attack
    {
        readonly Enemy first; int left, idx; float gap, t; readonly double rate, mul; readonly int baseN;
        public MeteorVolley(TurretInst tr, Enemy target, double mul)
        {
            src = tr; first = target; this.mul = mul; rate = tr.row.rate * mul;
            baseN = Mathf.Max(1, tr.row.count); left = baseN + (int)tr.S("meteor_extra");
            gap = Tables.Cf("Meteor_Gap", 0.35f);
        }
        public override void Step(float dt)
        {
            if (dead) return;
            t -= dt;
            if (t > 0) return;
            t = gap;
            var b = Battle.I;
            var tg = idx == 0 && first != null && !first.dead ? first : (b.Nearest(src.unit ? src.unit.Head : World.BaseCenter, src.Range) ?? b.RandomEnemy());
            if (tg == null) { Kill(); return; }
            idx++;
            bool giant = idx == 3 && src.Has("giant_meteor");
            bool extra = idx > baseN;
            float spd = src.row.speed * (1 + (float)src.S("speed") / 100f);
            float r = src.row.radius * src.Area * (giant ? 1.6f : 1f);
            double rr = rate * (giant ? 2 : 1) * (extra ? 0.8 : 1);
            var aim = tg.Pos + tg.Dir * 0.5f;   // 조금 앞을 노림
            b.Add(new MeteorFall(src, aim, spd, r, rr, giant, extra ? 0.8f : (float)Math.Max(0.75, mul)));
            if (src.unit) Fx.Muzzle(src.unit.Head, 80f, Art.Hex("#ff9a54"), 0.9f);
            if (--left <= 0) Kill();
        }
    }
}
