using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    // =====================================================================================
    // 2차 터렛 5종의 공격 물체 (기획서 4-4 쾅쾅 대포 · 4-5 집중 빔 · 4-8 소용돌이포 · 4-10 불바퀴 · 4-12 찌릿 코일)
    // =====================================================================================

    /// <summary>부유 기뢰 (쾅쾅 대포 돌파 B): 떠 있다가 적이 닿으면 폭발</summary>
    public class Mine : Attack
    {
        Vector2 pos, drift; float life, t; double rate; bool knock;
        public Mine(TurretInst tr, Vector2 p, Vector2 v, double rate, bool knock)
        {
            src = tr; pos = p; drift = v; this.rate = rate; this.knock = knock;
            life = Tables.Cf("Mine_Life", 8f);
            MakeSprite("mine", Art.Fx("mine"), p, 26);
            go.transform.localScale = Vector3.one * 0.85f;
        }
        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; life -= dt;
            pos += drift * dt; drift *= Mathf.Max(0, 1 - 3f * dt);
            go.transform.position = pos + new Vector2(0, Mathf.Sin(t * 3f) * 0.05f);
            go.transform.rotation = Quaternion.Euler(0, 0, t * 40f);
            var c = sr.color; c.a = Mathf.Clamp01(life / 0.5f); sr.color = c;
            foreach (var e in Battle.I.enemies)
            {
                if (e.dead || Vector2.Distance(e.Pos, pos) > e.radius + 0.3f) continue;
                Boom(); return;
            }
            if (life <= 0) Kill();
        }
        void Boom()
        {
            float r = 0.8f;
            Combat.Explode(src, pos, r, rate, Art.Hex("#ffcf6b"));
            if (knock) foreach (var e in Battle.I.enemies) if (!e.dead && Vector2.Distance(e.Pos, pos) <= r + e.radius) e.Knock(Tables.Cf("Knock_Base", 0.6f));
            Kill();
        }
    }

    /// <summary>집중 빔: 머리에서 한 적까지 곧은 빔. 틱마다 그 적만 · 죽으면 가까운 적으로 옮김.
    /// 집중 누적(돌파 A) = 같은 적에 붙어 있을수록 피해가 오름 · 굴절(돌파 B) = 맞은 적에서 가까운 적들에게 굴절 빔</summary>
    public class FocusBeam : Attack
    {
        Enemy target; readonly Enemy avoid;
        float dur, t, tickT, onTarget;
        readonly float width;
        readonly Color col = Art.Hex("#9ff0e0");
        SpriteRenderer core, flare;
        readonly List<SpriteRenderer> refr = new();
        readonly List<Enemy> refrT = new();
        public static int LastRefract;      // 시험용
        public static float LastRamp;       // 시험용: 지금 누적 배율

        float sizzleT, flick = 1f;
        Vector2 lastHit;

        public FocusBeam(TurretInst tr, Enemy tg, float duration, float width, Enemy avoid = null)
        {
            // 11-16 저녁 사용자 "너무 단순함 · 두께는 1/2 · 쏘는 동안 레이저로 지글지글 지지는 연출"
            //   → 굵기 절반 · 빛이 빠르게 깜빡 · 맞는 곳에 불꽃이 계속 튀고 빛이 일렁 · 가끔 연기
            src = tr; target = tg; dur = duration; this.width = width * 0.5f; this.avoid = avoid;
            MakeSprite("focus", Art.Fx("beam", new Vector4(40, 8, 40, 8)), tr.unit ? tr.unit.Head : Vector2.zero, 28);
            sr.drawMode = SpriteDrawMode.Sliced; Art.Additive(sr);
            var g = col; g.a = 0.85f; sr.color = g;
            core = Child("core", Art.Fx("beam_core", new Vector4(20, 8, 20, 8)), 29); core.drawMode = SpriteDrawMode.Sliced; core.color = Color.Lerp(col, Color.white, 0.6f);
            flare = Child("flare", Art.Fx("glow"), 29); Art.Additive(flare); var fc = col; fc.a = 0.8f; flare.color = fc;
        }

        SpriteRenderer Child(string n, Sprite s, int order, Transform parent = null)
        {
            var g = new GameObject(n); g.transform.SetParent(parent ? parent : go.transform, false);
            var r = g.AddComponent<SpriteRenderer>(); r.sprite = s; r.sortingOrder = order; return r;
        }

        static void Line(SpriteRenderer r, Vector2 a, Vector2 b, float w)
        {
            var d = b - a; float len = Mathf.Max(0.05f, d.magnitude);
            r.transform.position = (a + b) / 2;
            r.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            r.size = new Vector2(len + w, w * 1.7f);
        }

        float Ramp => Mathf.Clamp01(onTarget / Mathf.Max(0.1f, Tables.Cf("Focus_Ramp_Sec", 3f))) * (float)src.S("focus_ramp") / 100f;

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; tickT -= dt;
            var head = src.unit ? src.unit.Head : (Vector2)go.transform.position;
            if (target == null || target.dead)
            {
                var nt = Battle.I.Nearest(head, src.Range);
                if (nt == avoid && avoid != null) { var skip = new HashSet<Enemy> { avoid }; nt = Battle.I.Nearest(head, src.Range, skip); }
                if (nt != target) { target = nt; if (!src.Has("focus_keep")) onTarget = 0; }
                if (target == null) { if (t > 0.2f) Kill(); return; }
            }
            onTarget += dt;
            float fade = Mathf.Clamp01(Mathf.Min(t / 0.08f, (dur - t) / 0.15f));
            if ((sizzleT -= dt) <= 0)
            {
                sizzleT = 0.035f;
                flick = Random.Range(0.65f, 1.15f);
                var at0 = target.Pos + Random.insideUnitCircle * target.radius * 0.25f;
                var dir = (head - target.Pos).normalized;
                Fx.Sparks(at0, Color.Lerp(col, Color.white, Random.value * 0.6f), 2, Random.Range(2.5f, 5f), 0.7f, 110f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                Fx.Glow(at0, Random.Range(0.18f, 0.3f) * (1 + Ramp * 0.15f), new Color(0.75f, 1f, 0.95f, 0.75f), 0.08f);
                if (Random.value < 0.12f) Fx.Puff(at0 + Vector2.up * 0.1f, 0.35f);
                lastHit = at0;
            }
            float w = Mathf.Max(0.07f, width) * (0.5f + 0.5f * fade) * (1 + 0.5f * Ramp / 4f) * flick;
            Line(sr, head, target.Pos, w);
            core.transform.localScale = Vector3.one; core.size = new Vector2(sr.size.x - w, Mathf.Max(0.04f, w * 0.32f));
            flare.transform.position = head; flare.transform.localScale = Vector3.one * (w * 2.2f / 0.64f);
            { var c = sr.color; c.a = 0.85f * fade; sr.color = c; var cc = core.color; cc.a = fade; core.color = cc; }
            LastRamp = Ramp;
            if (tickT <= 0)
            {
                tickT += src.Tick > 0 ? src.Tick : 0.2f;
                double rate = src.row.rate * (1 + Ramp);
                Vector2 at = target.Pos;
                Combat.Hit(src, target, rate, true, true);
                if (Random.value < 0.5f) Fx.Sparks(at, Color.Lerp(col, Color.white, 0.4f), 2, 3.5f, 0.8f);
                // 굴절: 맞은 적 근처 적 N 마리
                int n = (int)src.S("refract");
                refrT.Clear();
                if (n > 0)
                {
                    var skip = new HashSet<Enemy> { target };
                    for (int i = 0; i < n; i++) { var e = Battle.I.Nearest(at, 3.2f, skip); if (e == null) break; skip.Add(e); refrT.Add(e); }
                    double rr = rate * Tables.C("Refract_Rate", 0.5) * (1 + src.S("refract_dmg") / 100.0);
                    foreach (var e in refrT) Combat.Hit(src, e, rr, false, true);
                    LastRefract = refrT.Count;
                }
            }
            // 굴절 빔 그림
            while (refr.Count < refrT.Count) { var r = Child("refract", Art.Fx("beam", new Vector4(40, 8, 40, 8)), 28, Battle.I.transform); r.drawMode = SpriteDrawMode.Sliced; Art.Additive(r); refr.Add(r); }
            for (int i = 0; i < refr.Count; i++)
            {
                bool on = i < refrT.Count && target != null && refrT[i] != null && !refrT[i].dead;
                refr[i].enabled = on;
                if (on) { Line(refr[i], target.Pos, refrT[i].Pos, w * 0.55f); var c = col; c.a = 0.7f * fade; refr[i].color = c; }
            }
            if (t >= dur) Kill();
        }

        public override void Kill()
        {
            foreach (var r in refr) if (r) Object.Destroy(r.gameObject);
            base.Kill();
        }
    }

    /// <summary>소용돌이 (소용돌이포): 천천히 날며 둘레 적을 가운데로 끌어당기고 틱마다 피해.
    /// hole = 블랙홀(돌파 A, 제자리 · 5초). 끝날 때 중력 폭발(돌파 B)</summary>
    public class Vortex : Attack
    {
        Vector2 pos, dir; float speed, life, t, tickT, r, pull, suckT; double rate; readonly bool hole;
        public static int LastPulled;   // 시험용

        public Vortex(TurretInst tr, Vector2 p, Vector2 d, float speed, float life, float radius, double rate, float pull, bool hole = false)
        {
            src = tr; pos = p; dir = d.normalized; this.speed = speed; this.life = life; r = radius; this.rate = rate; this.pull = pull; this.hole = hole;
            MakeSprite(hole ? "blackhole" : "vortex", Art.Fx("vortex"), p, 9);
            // 11-16 저녁 "희미해 보여서 조금 더 선명하고 끌어들이는 효과를 더 강하게": 진한 보라 + 둘레에서 가운데로 빨려드는 빛 알갱이
            sr.color = hole ? Art.Hex("#7d62d8") : Art.Hex("#a68bff");
            go.transform.localScale = Vector3.one * (r / 0.9f);
        }

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; tickT -= dt;
            pos += dir * speed * dt;
            if (pos.y > World.SpawnY - 0.5f) { pos.y = World.SpawnY - 0.5f; speed = 0; }
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, -t * (hole ? 260f : 360f));
            float a = Mathf.Clamp01(Mathf.Min(t / 0.2f, life / 0.3f));
            var c = sr.color; c.a = 0.9f * a; sr.color = c;
            go.transform.localScale = Vector3.one * (r / 0.9f) * (0.6f + 0.4f * a);
            if ((suckT -= dt) <= 0)
            {
                suckT = 0.03f;
                float a0 = Random.Range(0f, Mathf.PI * 2), rr = r * Random.Range(1.3f, 1.8f);
                var p0 = pos + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * rr;
                float toC = Mathf.Atan2(pos.y - p0.y, pos.x - p0.x) * Mathf.Rad2Deg + 35f;   // 살짝 비틀려 소용돌이처럼
                Fx.Sparks(p0, Color.Lerp(Art.Hex("#c8b6ff"), Color.white, Random.value * 0.5f), 1, rr / 0.32f, 0.9f, 8f, toC);
            }
            int pulled = 0;
            foreach (var e in Battle.I.enemies)
            {
                if (e.dead) continue;
                float d = Vector2.Distance(e.Pos, pos);
                if (d < r * 1.7f + e.radius) { e.Pull(pos, pull * dt); pulled++; }
            }
            LastPulled = Mathf.Max(LastPulled, pulled);
            if (tickT <= 0)
            {
                tickT += src.Tick > 0 ? src.Tick : 0.2f;
                foreach (var e in Battle.I.enemies)
                    if (!e.dead && Vector2.Distance(e.Pos, pos) <= r + e.radius) Combat.Hit(src, e, rate, !hole, false);
            }
            life -= dt;
            if (life <= 0) End();
        }

        void End()
        {
            if (!hole && src.Has("blackhole"))
            {
                float k = 1 + (float)src.S("bh_area") / 100f;
                Battle.I.Add(new Vortex(src, pos, Vector2.up, 0, Tables.Cf("Blackhole_Time", 5f), r * 0.8f * k, src.row.rate * 0.5 * (1 + src.S("bh_dmg") / 100.0), pull * 0.8f * k, true));
            }
            if (!hole && src.Has("vortex_burst"))
                Combat.Explode(src, pos, r * 1.5f * (1 + (float)src.S("burst_area") / 100f), Tables.C("Vortex_Burst_Rate", 300) * (1 + src.S("burst_dmg") / 100.0), Elem.Color("grav"));
            Kill();
        }
    }

    /// <summary>불바퀴: 벽(좌우 · 위 · 방어막)에 튕기며 굴러다님. 틱마다 둘레 적을 맞히고, 맞힌 수만큼 횟수가 줄어 0이면 사라짐.
    /// 불꽃 자국(돌파 A) · 꼬마 바퀴(돌파 B)</summary>
    public class FireWheel : Attack
    {
        Vector2 pos, vel; float r, life, t, tickT, trailT, miniT; int hits; double rate; readonly bool mini;
        public int Bounces;
        public static int LastBounces;   // 시험용

        public FireWheel(TurretInst tr, Vector2 p, Vector2 v, float radius, int hits, float life, double rate, bool mini = false)
        {
            src = tr; pos = p; vel = v; r = radius; this.hits = hits; this.life = life; this.rate = rate; this.mini = mini;
            MakeSprite("wheel", Art.Fx("wheel"), p, 27);
            go.transform.localScale = Vector3.one * (r / 0.45f);
            miniT = Tables.Cf("Mini_Wheel_Gap", 3f);
            var g = new GameObject("glow"); g.transform.SetParent(go.transform, false);
            var gs = g.AddComponent<SpriteRenderer>(); gs.sprite = Art.Fx("glow"); gs.sortingOrder = 26; Art.Additive(gs);
            gs.color = new Color(1f, 0.55f, 0.3f, 0.5f); g.transform.localScale = Vector3.one * 1.6f;
            glowR = gs;
            // 11-16 저녁 사용자 "바퀴 주변에 불꽃이 이글이글": 테두리에 불꽃 8개 (바퀴와 따로 돌며 크기가 빠르게 일렁 · 빛 더하기)
            int nf = mini ? 5 : 8;
            flames = new SpriteRenderer[nf]; fph = new float[nf];
            for (int i = 0; i < nf; i++)
            {
                var f = new GameObject("flame"); f.transform.SetParent(go.transform.parent, false);
                var fs = f.AddComponent<SpriteRenderer>(); fs.sprite = Art.Fx("flame"); fs.sortingOrder = 28; Art.Additive(fs);
                flames[i] = fs; fph[i] = Random.Range(0f, 10f);
            }
        }

        SpriteRenderer glowR;
        SpriteRenderer[] flames; float[] fph; float fAng;

        void Flames(float dt)
        {
            fAng += dt * 140f;
            float k = r / 0.45f;
            for (int i = 0; i < flames.Length; i++)
            {
                float a = (fAng + i * 360f / flames.Length) * Mathf.Deg2Rad;
                float n = 0.75f + 0.35f * Mathf.Sin(t * 23f + fph[i]) * Mathf.Sin(t * 9f + fph[i] * 2f);   // 이글이글
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var ft = flames[i].transform;
                ft.position = pos + d * r * 0.92f;
                ft.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90f + Mathf.Sin(t * 17f + fph[i]) * 12f);   // 바깥쪽을 봄 + 흔들
                ft.localScale = new Vector3(0.55f * k * (0.8f + 0.2f * n), 0.75f * k * n, 1);
                flames[i].color = new Color(1f, Mathf.Lerp(0.45f, 0.75f, n - 0.4f), 0.25f, 0.85f);
            }
            if (glowR) { var c = glowR.color; c.a = 0.4f + 0.2f * Mathf.Sin(t * 19f); glowR.color = c; }
        }

        public override void Kill()
        {
            if (flames != null) foreach (var f in flames) if (f) Object.Destroy(f.gameObject);
            base.Kill();
        }

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; life -= dt; tickT -= dt;
            pos += vel * dt;
            float top = World.SpawnY - 0.6f, bot = World.GroundY + r + 0.1f;   // 11-18: 벽 = 방어선(왼쪽) · 오른쪽 끝 · 위 · 달 바닥
            if (pos.x < World.ShieldX + r) { pos.x = World.ShieldX + r; vel.x = Mathf.Abs(vel.x); Bounce(); }
            if (pos.x > World.HalfW - r) { pos.x = World.HalfW - r; vel.x = -Mathf.Abs(vel.x); Bounce(); }
            if (pos.y > top) { pos.y = top; vel.y = -Mathf.Abs(vel.y); Bounce(); }
            if (pos.y < bot) { pos.y = bot; vel.y = Mathf.Abs(vel.y); Bounce(); }
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, -t * 720f * Mathf.Sign(vel.x == 0 ? 1 : vel.x));
            Flames(dt);
            if ((trailT -= dt) <= 0 && !mini)
            {
                trailT = 0.12f;
                Fx.Sparks(pos, Art.Hex("#ffb35c"), 1, 1.5f, 0.8f);
                if (src.Has("wheel_trail"))
                {
                    trailT = 0.3f;
                    // 11-16: 속성 색(분홍빛)이면 밤하늘에서 공처럼 보여서 옅은 주황 불길로 (캡처로 확인)
                    Battle.I.Add(new Zone(src, pos, 0.4f, 2f * (1 + (float)src.S("trail_dur") / 100f), 0.4f,
                        Tables.C("Wheel_Trail_Rate", 25) * (1 + src.S("trail_dmg") / 100.0), Art.Hex("#ff9440")) { alpha = 0.42f });
                }
            }
            if (!mini && src.Has("mini_wheel") && (miniT -= dt) <= 0)
            {
                miniT = Tables.Cf("Mini_Wheel_Gap", 3f);
                int n = (int)src.S("mini_wheel");
                for (int i = 0; i < n; i++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2);
                    Battle.I.Add(new FireWheel(src, pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * vel.magnitude * 1.1f, r * 0.55f, 15, 5f,
                        rate * 0.45 * (1 + src.S("mini_dmg") / 100.0), true));
                }
            }
            if (tickT <= 0)
            {
                tickT += src.Tick > 0 ? src.Tick : 0.2f;
                foreach (var e in Battle.I.enemies)
                {
                    if (e.dead || Vector2.Distance(e.Pos, pos) > r + e.radius) continue;
                    Combat.Hit(src, e, rate, !mini, true);
                    if (--hits <= 0) break;
                }
            }
            if (hits <= 0 || life <= 0) { Fx.Puff(pos, 0.6f); Kill(); }
        }

        void Bounce() { Bounces++; LastBounces = Mathf.Max(LastBounces, Bounces); }
    }

    /// <summary>찌릿 코일 번개: 충전(머리에 빛이 커짐) → 첫 적에게 번개 → 맞은 적마다 폭발 → 방금 맞은 적이 아닌 무작위 적으로 튐 (bounces 번).
    /// 전기 덫(돌파 A) · 과충전(돌파 B: 충전 +0.5초 · 폭발 범위 +40%) · 확장 충격파</summary>
    public class TeslaBolt : Attack
    {
        Enemy target; Vector2 from; float charge, t, gap; int left; double rate; float radius;
        SpriteRenderer orb;
        public static int LastJumps;   // 시험용
        int jumps;

        public TeslaBolt(TurretInst tr, Enemy tg, Vector2 head, float chargeSec, int bounces, double rate, float radius)
        {
            src = tr; target = tg; from = head; charge = chargeSec; left = 1 + bounces; this.rate = rate; this.radius = radius;
            MakeSprite("tesla_charge", Art.Fx("glow"), head, 33);
            Art.Additive(sr); sr.color = new Color(1f, 0.9f, 0.45f, 0.9f);
            go.transform.localScale = Vector3.one * 0.2f;
            var g = new GameObject("orb"); g.transform.SetParent(go.transform, false);
            orb = g.AddComponent<SpriteRenderer>(); orb.sprite = Art.Fx("dot"); orb.sortingOrder = 34; orb.color = new Color(1, 1, 0.85f);
            g.transform.localScale = Vector3.one * 0.5f;
        }

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt;
            if (t < charge)
            {
                float k = t / charge;
                go.transform.position = src.unit ? src.unit.Head : from;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.75f, k) * (1 + 0.15f * Mathf.Sin(t * 60f));
                if (Random.value < 0.35f) Fx.Sparks(go.transform.position, Art.Hex("#ffe98a"), 1, 2.5f, 0.7f);
                return;
            }
            sr.enabled = false; orb.enabled = false;
            if ((gap -= dt) > 0) return;
            gap = 0.07f;
            if (jumps == 0) from = src.unit ? src.unit.Head : from;
            if (target == null || target.dead) target = Battle.I.Nearest(from, 6f);
            if (target == null || left <= 0) { Kill(); return; }
            var at = target.Pos;
            Battle.I.Add(new Zap(from, at));
            Audio.Play(Audio.Sfx.Zap);
            Blast(at);
            jumps++; LastJumps = Mathf.Max(LastJumps, jumps);
            left--;
            var prev = target;
            target = Battle.I.RandomEnemyNear(at, 5f, prev);
            from = at;
            if (left <= 0) Kill();
        }

        void Blast(Vector2 at)
        {
            Fx.Burst(at, radius, Art.Hex("#ffe27a"));
            Fx.Glow(at, radius * 0.9f, new Color(1f, 0.93f, 0.5f, 0.7f), 0.2f);
            var list = Battle.I.enemies;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var e = list[i];
                if (e.dead) continue;
                float d = Vector2.Distance(e.Pos, at);
                if (d <= radius + e.radius) Combat.Hit(src, e, rate, true, true);
                else if (src.Has("shockwave") && d <= radius * 1.6f + e.radius) Combat.Hit(src, e, rate * 0.5, false, false);
            }
            if (src.Has("shockwave")) Fx.Ring(at, radius * 1.6f, Art.Hex("#ffe27a"));
            if (src.Has("trap"))
                Battle.I.Add(new Zone(src, at, radius * 0.9f, Tables.Cf("Trap_Time", 2f) * (1 + (float)src.S("trap_dur") / 100f), 0.4f,
                    Tables.C("Trap_Rate", 40) * (1 + src.S("trap_dmg") / 100.0), Art.Hex("#ffe27a"), 0.25f, src.Has("trap_bolt")));
        }
    }
}
