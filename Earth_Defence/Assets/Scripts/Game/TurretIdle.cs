using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 11-16 밤 터렛 대기 효과 (사용자 "효과를 애니처럼 돌려 작동하고 있는 느낌" — 1단계 벼락탑 · 레이저 포 → 2단계 나머지 4종).
    /// 터렛 그림을 받침(_base) + 움직일 조각으로 나눠(tools/earth_export.py export_turret) 코드로 움직임.
    ///   레이저 포: 수정이 둥실 · 수정 빛 숨쉬기 · 수정 둘레 빛 고리 + 고리를 따라 도는 빛 점 2개(앞쪽은 수정 앞, 뒤쪽은 뒤)
    ///   벼락탑: 구슬 빛이 불규칙하게 떨림 · 구슬에서 작은 번개가 치직치직(0.05~0.12초마다 모양·방향이 바뀜) · 가끔 불꽃
    ///   관통포: 바퀴가 돎(쏠 때가 가까울수록 빨리, 11-16 0.5배) · 포신 끝 · 바퀴 가운데 빛
    ///   끈끈이 드론: 몸이 둥실 · 프로펠러가 돎(관통포 바퀴와 같은 빠르기) · 눈동자가 가까운 적을 봄 · 가끔 깜빡
    ///   벌집: 작은 벌 3마리가 벌집 둘레를 빙빙(앞쪽은 벌집 앞, 뒤쪽은 뒤) · 입구가 따뜻하게 숨쉬기
    ///   유도 미사일: 쏘면 위 미사일 2개가 상자 속으로 내려갔다가 다시 올라와 장전 · 등 깜빡
    /// 세기 = 쉬는 동안 옅게(약 40%) → 쏠 때가 가까워질수록 세게(충전) → 쏘는 순간 번쩍 (Config Idle_Fx 배율, 0 = 끄기).
    /// </summary>
    public class TurretIdle : MonoBehaviour
    {
        public static bool Has(string art) => art is "laser" or "bolt" or "rail" or "drone" or "hive" or "missile"
                                                  or "cannon" or "focus" or "vortex" or "wheel" or "tesla";
        /// <summary>받침 그림 (관통포는 11-18 에 포신을 머리로 나눠 받침 = rail_stand)</summary>
        public static string BaseSprite(string art) => art == "rail" ? "rail_stand" : art + "_base";

        // ---------- 11-18 사령탑: 쏘는 머리만 45° 오른쪽 위로 (사용자 "탑이 아래에서 위로 쏘는 느낌 → 45도 · 머리만") ----------
        // 머리 = 회전 중심(pivot)에 둔 Head 아래 HeadIn(-pivot) — 조각들은 예전 좌표 그대로 HeadIn 에 붙음 → Head 를 돌리면 머리만 돎
        readonly System.Collections.Generic.List<Transform> heads = new(), inners = new();
        readonly System.Collections.Generic.List<Vector2> tips = new();
        public bool HasHead => heads.Count > 0;
        Transform NewHead(float px, float py, float tx, float ty)
        {
            var h = new GameObject("Head").transform; h.SetParent(transform, false);
            var pv = L2U(px, py); h.localPosition = pv;
            var inner = new GameObject("HeadIn").transform; inner.SetParent(h, false); inner.localPosition = -pv;
            heads.Add(h); inners.Add(inner); tips.Add(L2U(tx, ty));
            return inner;
        }
        /// <summary>머리 방향 (도, 0 = 오른쪽 · 90 = 위)</summary>
        public void SetAim(float deg) { var q = Quaternion.Euler(0, 0, deg - 90f); foreach (var h in heads) h.localRotation = q; }
        /// <summary>쏘는 곳 (머리 끝, 미사일은 두 개 가운데)</summary>
        public Vector2 TipWorld
        {
            get
            {
                Vector2 s = Vector2.zero;
                for (int i = 0; i < inners.Count; i++) s += (Vector2)inners[i].TransformPoint(tips[i]);
                return s / Mathf.Max(1, inners.Count);
            }
        }
        public int Parts { get; private set; }
        public float Intensity { get; private set; }

        TurretUnit u;
        string art;
        float t, flash, k = 1f;
        Color col;
        // 레이저
        SpriteRenderer crystal, crystalGlow, ringB, ringF;
        SpriteRenderer[] dots;
        float ang;
        // 벼락
        SpriteRenderer orb, orbGlow;
        SpriteRenderer[] arcs;
        float[] arcT;
        float flick = 1f, flickT;
        // 관통포
        SpriteRenderer wheel, tipGlow, hubGlow;
        // 드론
        Transform hover;
        SpriteRenderer[] props;
        SpriteRenderer pupil, lid;
        Vector2 look, lookWant; float lookT, blinkT = 2f, propSpin;
        // 벌집
        SpriteRenderer doorGlow;
        SpriteRenderer[] bees;
        // 미사일
        SpriteRenderer[] mis, misGlow;
        // 2차 5종: 도는 조각 · 빛 · 포신
        SpriteRenderer spin, spinGlow, barrel, fuse;
        float spinAng, recoil;

        // 그림 좌표(로컬 200x222, 받침 바닥 y 213.8) → 터렛 발에서의 칸 : x (px-100)·1.56/200 , y (213.8-py)·1.56/200
        static Vector2 L2U(float x, float y) => new((x - 100f) * 1.56f / 200f, (213.8f - y) * 1.56f / 200f);

        public void Setup(TurretUnit unit, string turretArt)
        {
            u = unit; art = turretArt;
            k = Tables.Cf("Idle_Fx", 1f);
            col = Elem.Color(u.t.row.elem);
            if (art == "laser")
            {
                ringB = Part("RingBack", Art.Turret("laser_ring_back"), 4, true);
                var hd = NewHead(100, 92, 100, 26);
                crystal = Part("Crystal", Art.Turret("laser_crystal"), 5, false, hd);
                crystalGlow = Part("CrystalGlow", Art.Fx("glow"), 5, true, hd);
                crystalGlow.transform.localPosition = L2U(100, 62);
                ringF = Part("RingFront", Art.Turret("laser_ring_front"), 6, true);
                dots = new[] { Part("Dot0", Art.Fx("glow"), 6, true), Part("Dot1", Art.Fx("glow"), 6, true) };
                ringB.color = ringF.color = col;
            }
            else if (art == "rail")
            {
                var hd = NewHead(100, 150, 100, 28);
                Part("Barrel", Art.Turret("rail_barrel"), 3, false, hd);
                wheel = Part("Wheel", Art.Get("Turret/rail_wheel", Art.PPU), 4, false, hd);
                wheel.transform.localPosition = L2U(100, 138);
                Part("WheelHl", Art.Turret("rail_hl"), 5, false, hd);
                tipGlow = Part("TipGlow", Art.Fx("glow"), 5, true, hd); tipGlow.transform.localPosition = L2U(100, 30);
                hubGlow = Part("HubGlow", Art.Fx("glow"), 5, true, hd); hubGlow.transform.localPosition = L2U(100, 138);
            }
            else if (art == "drone")
            {
                hover = new GameObject("Hover").transform; hover.SetParent(transform, false);
                Part("Body", Art.Turret("drone_body"), 4, false, hover);
                props = new[] { Part("Prop0", Art.Get("Turret/drone_prop", Art.PPU), 5, false, hover), Part("Prop1", Art.Get("Turret/drone_prop", Art.PPU), 5, false, hover) };
                props[0].transform.localPosition = L2U(36, 58); props[1].transform.localPosition = L2U(164, 58);
                pupil = Part("Pupil", Art.Get("Turret/drone_pupil", Art.PPU), 5, false, hover);
                lid = Part("Lid", Art.Get("Turret/drone_lid", Art.PPU), 6, false, hover);
                lid.transform.localPosition = L2U(100, 86); lid.enabled = false;
                blinkT = Random.Range(1.5f, 3.5f);
            }
            else if (art == "hive")
            {
                doorGlow = Part("DoorGlow", Art.Fx("glow"), 4, true); doorGlow.transform.localPosition = L2U(100, 148);
                bees = new SpriteRenderer[3];
                for (int i = 0; i < 3; i++) bees[i] = Part("Bee" + i, Art.Fx("bee"), 6, false);
            }
            else if (art == "missile")
            {
                var hl = NewHead(78, 112, 78, 32); var hr = NewHead(122, 104, 122, 24);
                mis = new[] { Part("MisL", Art.Turret("missile_l"), 2, false, hl), Part("MisR", Art.Turret("missile_r"), 2, false, hr) };   // 상자(받침) 뒤
                misGlow = new[] { Part("Light0", Art.Fx("glow"), 4, true), Part("Light1", Art.Fx("glow"), 4, true) };
                misGlow[0].transform.localPosition = L2U(76, 136); misGlow[1].transform.localPosition = L2U(124, 136);
            }
            else if (art == "cannon")
            {
                var hd = NewHead(100, 120, 100, 28);
                barrel = Part("Barrel", Art.Turret("cannon_barrel"), 4, false, hd);
                fuse = Part("Fuse", Art.Fx("glow"), 5, true, hd); fuse.transform.localPosition = L2U(100, 40);
            }
            else if (art is "focus" or "vortex" or "wheel")
            {
                Vector2 c0 = art == "focus" ? L2U(100, 70) : art == "vortex" ? L2U(100, 74) : L2U(100, 76);
                spinGlow = Part("SpinGlow", Art.Fx("glow"), 4, true); spinGlow.transform.localPosition = c0;   // 빛은 조각 뒤 (앞이면 하얗게 탐)
                spin = Part("Spin", Art.Get("Turret/" + art + "_spin", Art.PPU), 5, false); spin.transform.localPosition = c0;
            }
            else if (art == "tesla")
            {
                orbGlow = Part("OrbGlow", Art.Fx("glow"), 4, true);
                orb = Part("Orb", Art.Turret("tesla_orb"), 5, false);
                orbGlow.transform.localPosition = L2U(100, 46);
                arcs = new SpriteRenderer[4]; arcT = new float[4];
                for (int i = 0; i < arcs.Length; i++)
                {
                    arcs[i] = Part("Arc" + i, Art.Get("Turret/spark_arc_" + (i % 3), Art.PPU, new Vector2(0.02f, 0.5f)), 6, true);
                    arcs[i].enabled = false; arcT[i] = Random.Range(0f, 0.2f);
                }
            }
            else
            {
                orbGlow = Part("OrbGlow", Art.Fx("glow"), 4, true);            // 구슬 뒤 (앞에 두면 구슬이 하얗게 탐 — 촬영으로 확인)
                orb = Part("Orb", Art.Turret("bolt_orb"), 5, false);
                orbGlow.transform.localPosition = L2U(100, 52);
                arcs = new SpriteRenderer[4]; arcT = new float[4];
                for (int i = 0; i < arcs.Length; i++)
                {
                    arcs[i] = Part("Arc" + i, Art.Get("Turret/spark_arc_" + (i % 3), Art.PPU, new Vector2(0.02f, 0.5f)), 6, true);
                    arcs[i].enabled = false; arcT[i] = Random.Range(0f, 0.2f);
                }
            }
        }

        public void RestAim() { if (HasHead) SetAim(Tables.Cf("Head_Rest_Deg", 45f)); }

        SpriteRenderer Part(string name, Sprite s, int order, bool add, Transform parent = null)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent ? parent : transform, false);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = s; r.sortingOrder = order;
            if (add) Art.Additive(r);
            Parts++;
            return r;
        }

        public void OnFire() { flash = 1f; recoil = 1f; }
        /// <summary>3차 스킨: 속성 색 대신 돌린 색으로 빛남</summary>
        public void Tint(Color c) { col = c; if (ringB) ringB.color = ringF.color = c; }

        void Update()
        {
            if (u == null) return;
            float dt = Time.deltaTime;
            t += dt;
            flash = Mathf.Max(0, flash - dt * 3f);
            float ch = u.Charge01;
            Intensity = Mathf.Max(0.4f + 0.45f * ch * ch, flash);
            float a = Intensity * k;
            switch (art)
            {
                case "laser": Laser(dt, a); break;
                case "bolt": Bolt(dt, a); break;
                case "rail": Rail(dt, a); break;
                case "drone": Drone(dt, a); break;
                case "hive": Hive(dt, a); break;
                case "missile": Missile(dt, a, ch); break;
                case "cannon": Cannon(dt, a); break;
                case "focus": case "vortex": case "wheel": Spin(dt, a); break;
                case "tesla": Bolt(dt, a, 46f); break;
            }
        }

        static Color A(Color c, float al) { c.a = al; return c; }

        /// <summary>관통포 바퀴 · 드론 프로펠러 도는 빠르기(도/초). 11-16 밤 사용자 "0.5배로 느리게" · "프로펠러도 관통포랑 비슷하게" → 처음 35+420a² 의 절반</summary>
        static float SpinDeg(float a) => 17.5f + 210f * a * a;

        void Rail(float dt, float a)
        {
            wheel.transform.localRotation = Quaternion.Euler(0, 0, wheel.transform.localEulerAngles.z - SpinDeg(a) * dt);   // 충전될수록 빨리
            var lc = Color.Lerp(col, Color.white, 0.5f);
            tipGlow.color = A(lc, 0.12f + 0.55f * a); tipGlow.transform.localScale = Vector3.one * (0.3f + 0.25f * a);
            hubGlow.color = A(lc, 0.1f + 0.4f * a * (0.85f + 0.15f * Mathf.Sin(t * 6f))); hubGlow.transform.localScale = Vector3.one * (0.35f + 0.2f * a);
        }

        void Drone(float dt, float a)
        {
            hover.localPosition = new Vector3(0, Mathf.Sin(t * 2.4f) * 0.035f, 0);                                       // 둥실
            propSpin += SpinDeg(a) * Mathf.Deg2Rad * dt;                                                              // 관통포 바퀴와 같은 빠르기
            for (int i = 0; i < 2; i++)
            {
                float w = Mathf.Abs(Mathf.Cos(propSpin + i * 0.8f));
                props[i].transform.localScale = new Vector3(0.25f + 0.75f * w, 1, 1);                                   // 빨리 도는 날개
            }
            if ((lookT -= dt) <= 0)
            {
                lookT = 0.2f;
                var e = Battle.I ? Battle.I.Nearest(transform.position, 30f) : null;
                lookWant = e != null ? (e.Pos - (Vector2)pupil.transform.position).normalized : Vector2.zero;
            }
            look = Vector2.Lerp(look, lookWant, Mathf.Min(1, 8f * dt));
            pupil.transform.localPosition = L2U(100, 88) + new Vector2(look.x * 0.045f, look.y * 0.03f);              // 눈동자가 적을 봄
            if ((blinkT -= dt) <= 0)
            {
                lid.enabled = blinkT > -0.12f;
                if (blinkT <= -0.12f) { lid.enabled = false; blinkT = Random.Range(2.5f, 4.5f); }
            }
        }

        void Hive(float dt, float a)
        {
            doorGlow.color = A(new Color(1f, 0.62f, 0.25f), 0.08f + 0.28f * a * (0.8f + 0.2f * Mathf.Sin(t * 3f)));   // 하얗게 안 타게 옅은 주황 (촬영으로 확인)
            doorGlow.transform.localScale = Vector3.one * (0.4f + 0.15f * a);
            Vector2 c0 = L2U(100, 110);
            for (int i = 0; i < bees.Length; i++)
            {
                float sp = (55f + i * 22f) * (0.8f + 0.7f * a);
                float ph = (t * sp + i * 120f) * Mathf.Deg2Rad;
                float sn = Mathf.Sin(ph), cs = Mathf.Cos(ph);
                float rx = 0.62f + i * 0.05f, ry = 0.14f, h = (i - 1) * 0.28f + Mathf.Sin(t * 3f + i) * 0.03f;
                bees[i].transform.localPosition = new Vector3(c0.x + cs * rx, c0.y + h - sn * ry, 0);
                bees[i].sortingOrder = sn > 0 ? 6 : 2;                                                                 // 앞쪽 = 벌집 앞
                bees[i].flipX = sn > 0;                                                                                // 가는 쪽을 봄
                bees[i].transform.localScale = Vector3.one * (0.8f + (sn > 0 ? 0.1f : 0f));                                // 작으면 안 보여서 (촬영으로 확인)
            }
        }

        void Missile(float dt, float a, float ch)
        {
            for (int i = 0; i < 2; i++)
            {
                float r = Mathf.Clamp01((ch - 0.05f - i * 0.12f) / 0.45f);
                r = r * r * (3 - 2 * r);                                                                                 // 부드럽게 올라옴
                mis[i].transform.localPosition = new Vector3(0, -0.62f * (1 - r), 0);                                   // 쏜 뒤 상자 속에서 다시 장전
                float blink = Mathf.Sin(t * (4f + 8f * a) + i * Mathf.PI) > 0 ? 1f : 0.35f;
                misGlow[i].color = A(new Color(1f, 0.6f, 0.45f), (0.15f + 0.5f * a) * blink);
                misGlow[i].transform.localScale = Vector3.one * 0.32f;
            }
        }

        void Laser(float dt, float a)
        {
            crystal.transform.localPosition = new Vector3(0, Mathf.Sin(t * 2.2f) * 0.022f, 0);           // 둥실
            var gc = Color.Lerp(col, Color.white, 0.4f); gc.a = 0.15f + 0.45f * a * (0.85f + 0.15f * Mathf.Sin(t * 5f));
            crystalGlow.color = gc;
            crystalGlow.transform.localPosition = (Vector3)L2U(100, 62) + crystal.transform.localPosition;
            crystalGlow.transform.localScale = Vector3.one * (0.75f + 0.3f * a);
            var rc = col; rc.a = 0.12f + 0.5f * a; ringB.color = ringF.color = rc;
            // 빛 점 2개가 고리를 따라 돎 (쏠 때가 가까울수록 빨리)
            ang += (70f + 260f * a) * dt;
            Vector2 c0 = L2U(100, 66); float rx = 46f * 1.56f / 200f, ry = 12f * 1.56f / 200f;
            for (int i = 0; i < 2; i++)
            {
                float r = (ang + i * 180f) * Mathf.Deg2Rad;
                float sn = Mathf.Sin(r);
                dots[i].transform.localPosition = new Vector3(c0.x + Mathf.Cos(r) * rx, c0.y - sn * ry, 0);
                dots[i].sortingOrder = sn > 0 ? 6 : 4;                                                    // 아래(앞) = 수정 앞
                dots[i].transform.localScale = Vector3.one * (0.22f + 0.12f * a) * (sn > 0 ? 1f : 0.8f);
                var dc = Color.Lerp(col, Color.white, 0.5f); dc.a = (0.35f + 0.65f * a) * (sn > 0 ? 1f : 0.6f);
                dots[i].color = dc;
            }
        }

        /// <summary>쾅쾅 대포: 쏘면 포신이 뒤로 밀렸다 돌아옴 · 심지 빛이 충전될수록 셈</summary>
        void Cannon(float dt, float a)
        {
            recoil = Mathf.Max(0, recoil - dt * 4f);
            barrel.transform.localPosition = new Vector3(0, -0.09f * recoil * recoil + Mathf.Sin(t * 2f) * 0.006f, 0);
            float fl = 0.8f + 0.2f * Mathf.Sin(t * 23f) * Mathf.Sin(t * 7f);
            fuse.color = A(new Color(1f, 0.7f, 0.35f), (0.12f + 0.6f * a) * fl);
            fuse.transform.localScale = Vector3.one * (0.22f + 0.2f * a) * fl;
            if (a > 0.6f && Random.value < dt * 5f * a) Fx.Sparks((Vector2)transform.TransformPoint(L2U(100, 40)), new Color(1f, 0.75f, 0.4f), 1, 1.6f, 0.6f, 80f, 90f);
        }

        /// <summary>집중 빔 렌즈 · 소용돌이 원반 · 불바퀴: 가운데 조각이 돎(충전될수록 빨리) + 뒤 빛 숨쉬기</summary>
        void Spin(float dt, float a)
        {
            float sp = art == "focus" ? SpinDeg(a) * 0.6f : art == "vortex" ? 60f + 420f * a * a : SpinDeg(a) * 1.4f;
            spinAng -= sp * dt;
            spin.transform.localRotation = Quaternion.Euler(0, 0, spinAng);
            if (art == "focus") spin.transform.localPosition = (Vector3)L2U(100, 70) + new Vector3(0, Mathf.Sin(t * 2.2f) * 0.02f, 0);
            var gc = art == "wheel" ? new Color(1f, 0.55f, 0.3f) : Color.Lerp(col, Color.white, 0.3f);
            float fl = art == "wheel" ? 0.8f + 0.2f * Mathf.Sin(t * 19f) : 0.88f + 0.12f * Mathf.Sin(t * 4f);
            spinGlow.color = A(gc, (0.12f + 0.45f * a) * fl);
            spinGlow.transform.localPosition = spin.transform.localPosition;
            spinGlow.transform.localScale = Vector3.one * (0.7f + 0.35f * a);
            if (art == "wheel" && Random.value < dt * (2f + 6f * a)) Fx.Sparks((Vector2)transform.TransformPoint(spin.transform.localPosition), new Color(1f, 0.7f, 0.35f), 1, 1.8f, 0.6f, 120f, 90f);
        }

        void Bolt(float dt, float a, float orbY = 52f)
        {
            if ((flickT -= dt) <= 0) { flickT = Random.Range(0.04f, 0.09f); flick = Random.Range(0.6f, 1f); }   // 불규칙 떨림
            var oc = Color.Lerp(col, Color.white, 0.2f); oc.a = (0.25f + 0.5f * a) * flick;
            orbGlow.color = oc;
            orbGlow.transform.localScale = Vector3.one * (1.0f + 0.45f * a) * (0.9f + 0.1f * flick);
            Vector2 c0 = L2U(100, orbY); float rr = 22f * 1.56f / 200f;
            for (int i = 0; i < arcs.Length; i++)
            {
                if ((arcT[i] -= dt) > 0) continue;
                bool on = Random.value < 0.25f + 0.65f * a;
                arcs[i].enabled = on;
                if (!on) { arcT[i] = Random.Range(0.04f, 0.16f); continue; }
                arcT[i] = Random.Range(0.05f, 0.12f);
                // 아래(코일 쪽) 1개 · 나머지는 위·옆 아무 방향
                float deg = i == 0 ? Random.Range(-115f, -65f) : Random.Range(-30f, 210f);
                var d = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                arcs[i].transform.localPosition = c0 + d * rr * 0.85f;
                arcs[i].transform.localRotation = Quaternion.Euler(0, 0, deg);
                arcs[i].flipY = Random.value < 0.5f;
                arcs[i].sprite = Art.Get("Turret/spark_arc_" + Random.Range(0, 3), Art.PPU, new Vector2(0.02f, 0.5f));
                arcs[i].transform.localScale = new Vector3(Random.Range(0.55f, 1.05f) * (0.8f + 0.4f * a), Random.Range(0.8f, 1.1f), 1);
                var ac = Color.Lerp(col, Color.white, 0.3f); ac.a = 0.45f + 0.55f * a;
                arcs[i].color = ac;
            }
            if (a > 0.6f && Random.value < dt * 4f * a)
                Fx.Sparks((Vector2)transform.TransformPoint(c0), Color.Lerp(col, Color.white, 0.4f), 2, 2.2f, 0.6f);
        }
    }
}
