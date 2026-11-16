using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>전장에 잠깐 있는 공격 물체 (탄 · 레이저 · 장판 · 낙뢰 · 벌 · 불바다). Battle 이 고정 간격으로 Step 을 부름.</summary>
    public abstract class Attack
    {
        public bool dead;
        protected GameObject go;
        protected SpriteRenderer sr;
        public TurretInst src;
        public abstract void Step(float dt);
        public virtual void Kill() { if (dead) return; dead = true; if (go) UnityEngine.Object.Destroy(go); }

        protected SpriteRenderer MakeSprite(string name, Sprite s, Vector2 pos, int order = 30)
        {
            go = new GameObject(name);
            if (Battle.I) go.transform.SetParent(Battle.I.transform, false);
            go.transform.position = pos;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
            return sr;
        }

        protected static bool Visible(Vector2 p) => p.y < (World.I ? World.I.Top + 0.5f : World.SpawnY + 3f) && p.y > World.Bottom - 1f && Mathf.Abs(p.x) < World.HalfW + 1f;
    }

    /// <summary>날아가는 탄 (총알 · 관통 포탄 · 미사일). 유도면 목표를 향해 꺾임. 관통 수만큼 적을 지나감.</summary>
    public class Projectile : Attack
    {
        public Vector2 pos, dir;
        public float speed, hitR = 0.18f, life = 3f, turn;
        public Enemy target;
        public int pierce;
        public double rate;
        public Action<Projectile, Enemy> onHit;
        public Action<Projectile> onEnd;
        readonly HashSet<Enemy> hitSet = new();
        // 11-16 네온: 탄마다 빛 색 · 뒤에 빛 번짐(빛 더하기) · 맞으면 불꽃 튐 · 미사일은 날며 불티
        readonly Color glowCol;
        readonly bool exhaust;
        float emberT;

        public static Color ShotColor(string sprite) => sprite switch
        {
            "bullet" => Art.Hex("#ffd45e"),
            "bullet_charge" => Art.Hex("#ffc04a"),
            "shell" => Art.Hex("#ffa860"),     // 11-16 저녁: 대장 탄(금빛)과 헷갈리지 않게 주황
            "missile" or "mini_missile" => Art.Hex("#ff9a5a"),
            "cannonball" => Art.Hex("#ffcf6b"),
            _ => Color.white,
        };

        public Projectile(TurretInst t, string sprite, Vector2 p, Vector2 d, float speed, double rate, int pierce = 0, float scale = 1f)
        {
            src = t; pos = p; dir = d.normalized; this.speed = speed; this.rate = rate; this.pierce = pierce;
            MakeSprite("shot_" + sprite, Art.Shot(sprite), p, 31);
            go.transform.localScale = Vector3.one * scale;
            glowCol = ShotColor(sprite);
            exhaust = sprite == "missile" || sprite == "mini_missile" || sprite == "cannonball";
            // 머리 둘레 빛 번짐 (자식 → 같이 움직임)
            var g = new GameObject("glow");
            g.transform.SetParent(go.transform, false);
            var gs = g.AddComponent<SpriteRenderer>();
            gs.sprite = Art.Fx("glow"); gs.sortingOrder = 30; Art.Additive(gs);
            var gc = glowCol; gc.a = 0.45f; gs.color = gc;
            float gk = sprite == "bullet_charge" ? 0.85f : sprite == "bullet" ? 0.38f : sprite == "cannonball" ? 0.75f : exhaust ? 0.6f : sprite == "shell" ? 0.9f : 0.55f;
            g.transform.localScale = Vector3.one * gk;
            if (exhaust && sprite != "cannonball") g.transform.localPosition = new Vector3(-0.32f * (sprite == "missile" ? 1f : 0.6f), 0, 0);
            Face();
        }

        void Spark(Enemy e)
        {
            var at = Vector2.Lerp(pos, e.Pos, 0.4f);
            var hc = glowCol; hc.a = 0.6f;
            Fx.Glow(at, 0.22f, hc, 0.1f);
            Fx.Sparks(at, Color.Lerp(glowCol, Color.white, 0.35f), 3, 4.5f, 0.85f, 120f, Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg);
        }

        void Face() => go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        public override void Step(float dt)
        {
            if (dead) return;
            life -= dt;
            if (turn > 0)
            {
                if (target == null || target.dead) target = Battle.I.Nearest(pos, 30f, hitSet);
                if (target != null)
                {
                    var want = (target.Pos - pos).normalized;
                    dir = Vector3.RotateTowards(dir, want, turn * dt, 0).normalized;
                }
            }
            pos += dir * speed * dt;
            go.transform.position = pos; Face();
            if (exhaust && (emberT -= dt) <= 0)
            {
                emberT = 0.05f;
                Fx.Sparks(pos - dir * 0.35f * go.transform.localScale.x, glowCol, 1, 1.2f, 0.7f, 50f, Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg);
            }
            var list = Battle.I.enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e.dead || hitSet.Contains(e)) continue;
                if ((e.Pos - pos).sqrMagnitude > (e.radius + hitR) * (e.radius + hitR)) continue;
                hitSet.Add(e);
                Spark(e);
                onHit?.Invoke(this, e);
                if (dead) return;
                if (--pierce < 0) { onEnd?.Invoke(this); Kill(); return; }
            }
            if (life <= 0 || !Visible(pos)) { onEnd?.Invoke(this); Kill(); }
        }
    }

    /// <summary>레이저 (쏘는 동안 틱마다 선 위의 모든 적). 휘두르기 = 좌우로 흔듦. mini = 맞힐 때 나오는 작은 레이저</summary>
    public class Beam : Attack
    {
        Vector2 origin; float angle, baseAngle, dur, t, tick, tickT, width, length = 22f, sweep;
        double rate; bool mini;
        readonly Color col;
        // 11-16 네온: 색 번짐 띠(빛 더하기) + 흰 심 + 쏘는 곳 빛 + 맞는 적마다 불꽃
        SpriteRenderer core, flare;
        public static Vector2? LastMiniOrigin;   // 시험용: 마지막 작은 레이저가 나간 곳

        public Beam(TurretInst tr, Vector2 o, float angleDeg, float duration, float tickSec, float width, double rate, float sweepDeg = 0, bool mini = false, float len = 0)
        {
            src = tr; origin = o; angle = baseAngle = angleDeg; dur = duration; tick = tickSec; this.width = width; this.rate = rate; sweep = sweepDeg; this.mini = mini;
            if (mini) { length = len > 0 ? len : 4.5f; LastMiniOrigin = o; }
            col = Elem.Color("light");
            MakeSprite("beam", Art.Fx("beam", new Vector4(40, 8, 40, 8)), o, 28);
            sr.drawMode = SpriteDrawMode.Sliced;
            Art.Additive(sr);
            var gc0 = col; gc0.a = 0.85f; sr.color = gc0;
            core = Child("core", Art.Fx("beam_core", new Vector4(20, 8, 20, 8)), 29);
            core.drawMode = SpriteDrawMode.Sliced;
            core.color = Color.Lerp(col, Color.white, 0.5f);
            flare = Child("flare", Art.Fx("glow"), 29);
            Art.Additive(flare);
            var fc = Color.Lerp(col, Color.white, 0.3f); fc.a = 0.7f; flare.color = fc;
            Layout(1);
        }

        SpriteRenderer Child(string n, Sprite s, int order)
        {
            var g = new GameObject(n);
            g.transform.SetParent(go.transform, false);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = s; r.sortingOrder = order;
            return r;
        }

        void Layout(float a)
        {
            var d = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            go.transform.position = origin + d * length / 2;
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
            float w = Mathf.Max(0.16f, width) * a * (1 + 0.08f * Mathf.Sin(t * 55f));   // 살짝 떨림
            sr.size = new Vector2(length + w, w * 1.7f);
            core.size = new Vector2(length, Mathf.Max(0.04f, w * 0.3f));
            flare.transform.localPosition = new Vector3(-length / 2, 0, 0);
            flare.transform.localScale = Vector3.one * (w * 2.0f / 0.64f) * (1 + 0.15f * Mathf.Sin(t * 40f));
        }

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; tickT -= dt;
            if (sweep > 0) angle = baseAngle + Mathf.Sin(t / dur * Mathf.PI * 2) * sweep;
            float fade = Mathf.Clamp01(Mathf.Min(t / 0.08f, (dur - t) / 0.12f));
            Layout(0.4f + 0.6f * fade);
            { var c = sr.color; c.a = 0.85f * fade; sr.color = c; var cc = core.color; cc.a = fade; core.color = cc; }
            if (tickT <= 0)
            {
                tickT += tick;
                var d = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                var list = Battle.I.enemies;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e.dead) continue;
                    var rel = e.Pos - origin;
                    float along = Vector2.Dot(rel, d);
                    if (along < -e.radius || along > length + e.radius) continue;
                    float side = Mathf.Abs(rel.x * d.y - rel.y * d.x);
                    if (side <= width / 2 + e.radius)
                    {
                        Combat.Hit(src, e, rate, !mini, true);
                        if (UnityEngine.Random.value < 0.6f) Fx.Sparks(origin + d * along, Color.Lerp(col, Color.white, 0.5f), 2, 3.5f, 0.8f);
                    }
                }
            }
            if (t >= dur) Kill();
        }
    }

    /// <summary>끈끈이 드론 장판: 화면 폭 가로 띠. 틱마다 피해 + 둔화(쌓임). 끌어당김 · 사라질 때 폭발</summary>
    public class Field : Attack
    {
        float y, half, dur, t, tick, tickT;
        double rate;
        readonly HashSet<Enemy> inside = new();

        public Field(TurretInst tr, float y, float halfH, float duration, float tickSec, double rate)
        {
            src = tr; this.y = y; half = halfH; dur = duration; tick = tickSec; this.rate = rate;
            MakeSprite("field", Art.Fx("field", new Vector4(28, 24, 28, 24)), new Vector2(0, y), 8);
            sr.drawMode = SpriteDrawMode.Sliced;      // 장판은 보통 그리기 (빛 더하기면 겹칠 때 하얗게 탐 — 11-16 캡처로 확인)
            sr.color = Elem.Color("grav");
            sr.size = new Vector2(World.HalfW * 2 + 0.4f, half * 2);
        }

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; tickT -= dt;
            var c = sr.color; c.a = Mathf.Clamp01(Mathf.Min(t / 0.2f, (dur - t) / 0.4f)); sr.color = c;
            var list = Battle.I.enemies;
            if (src.Has("pull"))
                foreach (var e in list) if (!e.dead && Mathf.Abs(e.Pos.y - y) < half + e.radius + 0.6f) e.Pull(new Vector2(e.Pos.x, y), 0.9f * dt);
            if (tickT <= 0)
            {
                tickT += tick;
                float slowMax = Tables.Cf("Slow_Max", 0.5f) + (float)src.S("slow_max") / 100f;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e.dead || Mathf.Abs(e.Pos.y - y) > half + e.radius) continue;
                    e.AddSlow(0.1f, tick + 0.2f, slowMax);
                    if (src.Has("paralyze") && Combat.R < src.S("paralyze")) e.Paralyze(Tables.Cf("Para_Time", 1f));
                    Combat.Hit(src, e, rate, true, false);
                }
            }
            if (t >= dur)
            {
                if (src.Has("field_explode"))
                {
                    for (float x = -4.5f; x <= 4.6f; x += 1.5f) Fx.Burst(new Vector2(x, y), half * 1.2f, Elem.Color("grav"));
                    foreach (var e in list) if (!e.dead && Mathf.Abs(e.Pos.y - y) <= half + e.radius) Combat.Hit(src, e, 300, false);
                }
                Kill();
            }
        }
    }

    /// <summary>벼락탑 낙뢰: x 에 세로줄. 틱마다 줄 위 모든 적. mini = 처치 때 나오는 작은 낙뢰</summary>
    public class Strike : Attack
    {
        float x, width, dur, t, tick, tickT;
        double rate; bool mini;

        float miniY;

        public Strike(TurretInst tr, float x, float width, float duration, float tickSec, double rate, bool mini = false, float y = 0)
        {
            miniY = y;
            src = tr; this.x = x; this.width = width; dur = duration; tick = tickSec; this.rate = rate; this.mini = mini;
            float top = mini ? 3.5f : (World.I ? World.I.Top + 0.5f : World.SpawnY + 2f), bottom = World.ShieldY;
            MakeSprite("bolt", Art.Fx("bolt"), new Vector2(x, (top + bottom) / 2), 29);
            Art.Additive(sr);
            // 그림 160x600 @PPU200 = 0.8 x 3칸 (11-16 네온: 번짐 포함이라 넓어짐)
            float h = mini ? 3.5f : top - bottom;
            go.transform.localScale = new Vector3(width / 0.8f * 1.5f, h / 3f, 1);
            if (mini) go.transform.position = new Vector3(x, y + 1.2f, 0);
            Fx.Glow(new Vector2(x, mini ? y : bottom + 0.2f), mini ? 0.5f : 0.9f, Art.Hex("#ffd84a"), 0.25f);
        }

        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; tickT -= dt;
            int fr = (int)(t * 20);
            sr.flipX = fr % 2 == 0;
            // 낙뢰 여러 줄이 겹치면 하얗게 타서 0.75 로
            sr.sprite = Art.Fx(fr / 2 % 2 == 0 ? "bolt" : "bolt2");      // 두 모양을 번갈아 → 지직거림
            var c = sr.color; c.a = 0.95f * Mathf.Clamp01((dur - t) / 0.15f); sr.color = c;   // 11-16 밤: 셰이더가 투명도를 못 받던 때 확인받은 밝기(≈1)에 맞춤
            if (tickT <= 0)
            {
                tickT += tick;
                var list = Battle.I.enemies;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    if (e.dead || Mathf.Abs(e.Pos.x - x) > width / 2 + e.radius) continue;
                    if (mini && Mathf.Abs(e.Pos.y - miniY) > 1.8f) continue;
                    Combat.Hit(src, e, rate, !mini);
                    Fx.Sparks(e.Pos, Art.Hex("#ffe98a"), 2, 4f, 0.8f);
                }
            }
            if (t >= dur) Kill();
        }
    }

    /// <summary>벌집의 꼬마 벌: 적을 쫓아가 hits 번 쏨. 펄스 · 사라질 때 펄스 · 메아리 벌</summary>
    public class Bee : Attack
    {
        Vector2 pos, vel;
        int hits; bool firstHit = true, echo;
        double rate; float cool, life = 14f;
        Enemy target;

        public Bee(TurretInst tr, Vector2 p, int hits, double rate, bool echo = false)
        {
            src = tr; pos = p; this.hits = hits; this.rate = rate; this.echo = echo;
            vel = new Vector2(UnityEngine.Random.Range(-2f, 2f), 3f);
            MakeSprite("bee", Art.Fx("bee"), p, 32);   // 그림에 금빛 번짐 들어 있음 (11-16)
            if (echo) { sr.color = new Color(1, 0.9f, 0.6f); go.transform.localScale = Vector3.one * 0.8f; }
        }

        public override void Step(float dt)
        {
            if (dead) return;
            life -= dt; cool -= dt;
            if (target == null || target.dead) target = Battle.I.Nearest(pos, 14f);
            float spd = src.row.speed > 0 ? src.row.speed : 7f;
            if (target != null && cool <= 0)
            {
                var want = (target.Pos - pos).normalized * spd;
                vel = Vector2.Lerp(vel, want, 6f * dt);
                if (Vector2.Distance(pos, target.Pos) < target.radius + 0.25f)
                {
                    int shots = 1 + (int)src.S("shots_free");
                    for (int i = 0; i < shots && !target.dead; i++) Combat.Hit(src, target, rate);
                    Fx.Hit(target.Pos, 0.7f);
                    if (firstHit && src.Has("pulse") && !echo) Pulse();
                    firstHit = false;
                    hits--; cool = 0.35f;
                    vel = new Vector2(UnityEngine.Random.Range(-3f, 3f), 3.5f);
                    if (hits <= 0) { End(); return; }
                }
            }
            else vel = Vector2.Lerp(vel, new Vector2(Mathf.Sin(life * 3) * 2, 0.5f), 2f * dt);
            pos += vel * dt;
            pos.x = Mathf.Clamp(pos.x, -World.HalfW, World.HalfW);
            pos.y = Mathf.Clamp(pos.y, World.ShieldY - 1.5f, World.SpawnY);
            go.transform.position = pos;
            sr.flipX = vel.x < 0;
            if (life <= 0) End();
        }

        void Pulse()
        {
            float r = 0.9f * (1 + (float)src.S("pulse_area") / 100f);
            Fx.Ring(pos, r, Elem.Color("grav"));
            foreach (var e in Battle.I.enemies) if (!e.dead && Vector2.Distance(e.Pos, pos) <= r + e.radius) Combat.Hit(src, e, 80, false, false);
            if (src.Has("slow")) foreach (var e in Battle.I.enemies) if (!e.dead && Vector2.Distance(e.Pos, pos) <= r + e.radius) e.AddSlow((float)src.S("slow") / 100f, 2f, Tables.Cf("Slow_Max", 0.5f));
        }

        void End()
        {
            if (src.Has("pulse_end") && !echo) Pulse();
            Kill();
        }
    }

    /// <summary>불바다 · 둥근 장판: 틱마다 원 안의 적</summary>
    public class Zone : Attack
    {
        Vector2 pos; float r, dur, t, tick, tickT, slow, boltT; double rate; bool bolts;
        /// <summary>가장 진할 때 투명도 (불바퀴 자국처럼 많이 깔리는 것은 옅게)</summary>
        public float alpha = 0.8f;
        /// <summary>slow = 틱마다 둔화 (2차 전기 덫) · bolts = 0.5초마다 작은 낙뢰 (덫 낙뢰)</summary>
        public Zone(TurretInst tr, Vector2 p, float radius, float duration, float tickSec, double rate, Color c, float slow = 0, bool bolts = false)
        {
            src = tr; pos = p; r = radius; dur = duration; tick = tickSec; this.rate = rate; this.slow = slow; this.bolts = bolts;
            MakeSprite("zone", Art.Fx("zone"), p, 7);
            Art.Additive(sr);
            sr.color = c;
            go.transform.localScale = Vector3.one * (r / 0.4f);
        }
        public override void Step(float dt)
        {
            if (dead) return;
            t += dt; tickT -= dt;
            var c = sr.color; c.a = alpha * Mathf.Clamp01(Mathf.Min(t / 0.2f, (dur - t) / 0.4f)); sr.color = c;
            if (tickT <= 0)
            {
                tickT += tick;
                foreach (var e in Battle.I.enemies)
                {
                    if (e.dead || Vector2.Distance(e.Pos, pos) > r + e.radius) continue;
                    if (slow > 0) e.AddSlow(slow, tick + 0.2f, Tables.Cf("Slow_Max", 0.5f));
                    Combat.Hit(src, e, rate, false, false);
                }
            }
            if (bolts && (boltT -= dt) <= 0)
            {
                boltT = 0.5f;
                Battle.I.Add(new Strike(src, pos.x + UnityEngine.Random.Range(-r, r) * 0.6f, 0.4f, 0.25f, 0.25f, Tables.C("Mini_Bolt_Rate", 120) * 0.5, mini: true, y: pos.y));
            }
            if (t >= dur) Kill();
        }
    }

    /// <summary>연쇄 번개 선 (보이기만)</summary>
    public class Zap : Attack
    {
        float t;
        public Zap(Vector2 a, Vector2 b)
        {
            MakeSprite("zap", Art.Fx("bolt2"), (a + b) / 2, 33);
            Art.Additive(sr);
            float len = Vector2.Distance(a, b);
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg - 90);
            go.transform.localScale = new Vector3(0.65f, len / 3f, 1);
            var zc = sr.color; zc.a = 0.95f; sr.color = zc;
            Fx.Sparks(b, Art.Hex("#ffe98a"), 3, 4f, 0.8f);
        }
        public override void Step(float dt)
        {
            t += dt;
            var c = sr.color; c.a = 0.95f * (1 - t / 0.2f); sr.color = c;
            if (t >= 0.2f) Kill();
        }
    }
}
