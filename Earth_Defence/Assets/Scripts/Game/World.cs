using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 월드 = 화면 1080 기준 100px 이 1칸. 전장 폭 10.8칸, 화면 아래 끝 = y -9.6 (항상 같음).
    /// 화면이 길면(19.5:9 등) 위쪽 하늘이 더 보일 뿐, 적이 나오는 높이·기지까지 거리는 화면과 상관없이 같음
    /// (기획서 19-2: 창 크기로 이동 거리가 줄어드는 꼼수 방지).
    /// </summary>
    public class World : MonoBehaviour
    {
        public static World I;
        public Camera cam;
        public const float Bottom = -9.6f, HalfW = 5.4f;
        // 11-17 최적화: 이 세 값은 전투 한 칸에 수천 번 읽힘(목표 찾기 · 맞음 판정) — 예전엔 읽을 때마다 표 조회 + 카메라 값(엔진 호출 6번)을 다시 계산.
        //   → 한 번 계산해 두고, 값이 바뀔 수 있는 때(화면 비율이 바뀜 = Fit · 시험에서 Config 를 바꿈 = Tables.SetC)만 다시 계산
        static float groundY, shieldY, spawnY; static bool cached;
        public static void Invalidate() => cached = false;
        static void Recache()
        {
            v = Tables.Ci("Battle_Vertical", 1) == 1;
            groundY = Tables.Cf("Ground_Y", -7.52f); shieldY = Tables.Cf("Shield_Y", -4.6f); shieldX = Tables.Cf("Shield_X", -2.1f);
            spawnY = Mathf.Min(Tables.Cf("Spawn_Y", 10.6f), I ? I.PlayTop - Tables.Cf("Spawn_Below_Hud", 0.6f) : 99f);
            cached = I;      // 월드가 생기기 전(로딩 중)엔 매번 다시 계산
        }
        static bool v;
        /// <summary>9차 수정안 "전투 화면은 하단에서 상단으로 공격하는 방식으로 복구 (이전 버전)" (Config Battle_Vertical 1):
        /// 아래 가운데 돔 + 터렛 4칸 땅 위 한 줄 · 적은 위에서 곧게 내려와 방어막(Shield_Y)에 닿음. 0 = 11-18 우주 사령탑(왼쪽 벽 · 비스듬히)</summary>
        public static bool V { get { if (!cached) Recache(); return v; } }
        /// <summary>적이 다니는 곳의 왼쪽 끝 · 아래 끝 (세로 = 화면 왼쪽 · 방어막 / 사령탑 = 방어선 · 땅)</summary>
        public static float FieldX0 => V ? -HalfW + 0.3f : ShieldX;
        public static float FieldLo => V ? ShieldY : GroundY;
        /// <summary>적이 닿은 곳에서 기지 쪽 (세로 = 아래 · 사령탑 = 왼쪽)</summary>
        public static Vector2 BaseDir => V ? Vector2.down : Vector2.left;
        /// <summary>터렛 머리가 쉴 때 보는 방향 (세로 = 위 90° · 사령탑 = 45°)</summary>
        public static float HeadRest => V ? Tables.Cf("Head_Rest_Deg_V", 90f) : Tables.Cf("Head_Rest_Deg", 45f);
        /// <summary>끈끈이 장판 세로 띠 (8차) — 세로 전장에선 예전처럼 가로 띠</summary>
        public static bool FieldVert => !V && Tables.Ci("Field_Vertical", 1) == 1;
        /// <summary>낙뢰 아래 끝 (세로 = 방어막 · 사령탑 = 지구 윗면)</summary>
        public static float StrikeBottom(float x) => V ? ShieldY - 0.2f : FloorY(x);
        public static float GroundY { get { if (!cached) Recache(); return groundY; } }
        public static float ShieldY { get { if (!cached) Recache(); return shieldY; } }
        /// <summary>적이 나오는 높이 = Spawn_Y 와 "위쪽 UI 바로 아래" 중 낮은 쪽 (11-16: 화면이 짧으면 UI 밑에 숨어 내려오던 것).
        /// 19.5:9 이상 폰은 Spawn_Y(10.6) 그대로, 그보다 짧은 화면만 낮아짐</summary>
        public static float SpawnY { get { if (!cached) Recache(); return spawnY; } }
        /// <summary>위쪽 UI(스테이지 · 경험치 줄) 아래 끝의 월드 높이. UI 는 폰에선 폭 1080 기준(Boot), 넓은 화면은 높이 1920 기준</summary>
        public float PlayTop
        {
            get
            {
                float hud = Tables.Cf("Hud_H", 210f);   // UI 픽셀 (1080 폭 기준)
                float w = cam.orthographicSize * 2 * cam.aspect, h = cam.orthographicSize * 2;
                float world = cam.aspect > 1080f / 1920f ? hud / 1920f * h : hud / 1080f * w;
                return Top - world;
            }
        }
        // ---------- 11-18 전투 배치 2판: 우주 사령탑 (전투배치_시안.html, 그림 tools/tower_art.py) ----------
        // 사령탑은 화면 왼쪽 벽에 붙어 서고, 적은 오른쪽 위에서 비스듬히 날아와 사령탑 앞 방어선(Shield_X)에 닿으면 때림.
        // 칸 좌표는 tools/earth_export.py tower 가 찍는 값과 같게 (땅 Ground_Y 위 높이).
        /// <summary>터렛 칸 1~4 (위 2 · 아래 2 — 1 = 맨 위, 4 = 달 바닥) 의 땅 위 높이 · x</summary>
        public static readonly float[] SlotUp = { 10.9f, 7.725f, 3.285f, 0f };
        public const float SlotXPos = -3.9f;      // 7차 "타워 지지대 컴팩트": 몸통 바로 옆 (예전 -3.1)
        /// <summary>9차 세로 전장의 터렛 칸 x (예전 그대로: 안쪽부터 왼쪽 안 · 오른쪽 안 · 왼쪽 밖 · 오른쪽 밖)</summary>
        public static readonly float[] SlotXV = { -2.72f, 2.72f, -4.3f, 4.3f };
        public static Vector2 SlotPos(int slot) => V ? new(SlotXV[Mathf.Clamp(slot - 1, 0, 3)], FloorY(SlotXV[Mathf.Clamp(slot - 1, 0, 3)]) - 0.06f) : new(SlotXPos, GroundY + SlotUp[Mathf.Clamp(slot - 1, 0, 3)] - (slot == 4 ? 0.06f : 0f));
        /// <summary>대장 카피 자리 = 가운데 사령실 발코니 위 (11-18 "대장 카피는 가운데 고정").
        /// 7차 "중간 카피바라 탄도가 위쪽 터렛과 겹치지 않게": 터렛 줄(오른쪽 끝 -3.35)보다 오른쪽 -2.9 → 위로 쏘는 탄도 터렛을 안 지남</summary>
        public static Vector2 GuardianPos => V ? new(0f, GroundY - 0.04f) : new(-2.9f, GroundY + 5.805f);
        public static float TowerTop => GroundY + 11.85f;
        /// <summary>방어선 x — 적은 이 선(사령탑 앞 · 터렛 오른쪽)에 닿으면 멈춰 때리거나 부딪쳐 터짐</summary>
        public static float ShieldX { get { if (!cached) Recache(); return shieldX; } }
        /// <summary>싸우는 하늘 가운데 (소용돌이 필살기 등)</summary>
        public static Vector2 FieldCenter => V ? new(0f, 1.5f) : new((ShieldX + HalfW) * 0.5f, 1.5f);
        /// <summary>기지 효과(회복 · 얼림 · 부활)가 퍼지는 곳 = 사령실 앞</summary>
        public static Vector2 BaseCenter => V ? new(0f, ShieldY + 0.5f) : new(-3.4f, GroundY + 5.2f);
        /// <summary>8차 전투 바닥 = 큰 지구 윗면: 가운데 x EG_CX · 반지름 EG_R (그림 tools/tower_art.py earth_ground 의 EG_* 와 같게).
        /// 사령탑(왼쪽)은 Ground_Y 그대로, 오른쪽으로 갈수록 둥글게 내려감 → 낙뢰 · 장판 아래 끝을 이 높이에</summary>
        public const float EG_CX = -3.4f, EG_R = 32f;
        /// <summary>9차 세로 전장 바닥 = 가운데(x 0) 지구 윗면 (그림 Bg/ground_earth_v, tools/tower_art.py earth_ground_v 의 EGV_R 과 같게)</summary>
        public const float EGV_R = 40f;
        public static float FloorY(float x)
        {
            float cx = V ? 0f : EG_CX, r = V ? EGV_R : EG_R, dx = x - cx;
            return GroundY - (r - Mathf.Sqrt(Mathf.Max(0, r * r - dx * dx)));
        }
        /// <summary>화면 안 (적을 노릴 수 있음): 위 UI 아래 · 오른쪽 끝 안쪽</summary>
        public static bool OnField(Vector2 p) => p.y < SpawnY - 0.3f && p.x < HalfW - 0.15f;
        /// <summary>7차 "떨어지는 별똥별(적)은 Lv 게이지 밑부터 Fade in": 이 높이(위 UI 아래 끝 = Lv 게이지 아래)부터 나타나기 시작</summary>
        public static float FadeTop => I ? I.PlayTop : SpawnY + 0.6f;
        static float shieldX;

        public float Top => cam.transform.position.y + cam.orthographicSize;
        readonly SpriteRenderer[] slotMarks = new SpriteRenderer[4];
        SpriteRenderer tower, towerGlow, rabbit, moon, sky, ground, earthBig;
        float rabbitT;
        FrameRow frameRow; bool layoutV;
        public bool RabbitDown { get; private set; }
        float glowT; Color glowCol;
        public SpriteRenderer SlotMark(int slot) => slotMarks[Mathf.Clamp(slot - 1, 0, 3)];

        /// <summary>기지 외형 (3차 → 11-18 사령탑 7종): 외형마다 다른 사령탑 그림 + 둘레 은은한 빛</summary>
        public void SetFrame(FrameRow f)
        {
            if (!tower) return;
            frameRow = f;
            if (layoutV)
            {   // 9차 세로 전장: 6차 외형 돔 7종 (Bg/dome_<id>)
                var ds = f != null ? Art.Get("Bg/dome_" + f.id, 100f) : null;
                tower.sprite = ds ? ds : Art.Get("Bg/dome", 100f);
                tower.color = ds ? Color.white : f != null ? f.tint : Color.white;
            }
            else
            {
                var sp = f != null ? TowerSprite("Bg/tower_" + f.id) : null;
                tower.sprite = sp ? sp : TowerSprite("Bg/tower");
                tower.color = Color.white;
            }
            glowCol = f != null && f.id != "basic" ? f.glow : new Color(1, 1, 1, 0);
        }
        /// <summary>사령탑 그림 420x1440 (PPU 100) — 피벗 = 왼쪽 아래에서 땅 높이(아래 40px)</summary>
        static Sprite TowerSprite(string path) => Art.Get(path, 100f, new Vector2(0f, 40f / 1440f));

        public void Build(Camera c)
        {
            I = this; cam = c;
            Fit();
            sky = Spr("Sky", Art.Get("Bg/sky", 100f), new Vector2(0, Bottom + 12f), -100);
            // 2차 수정안: 달토끼 — 달 위에서 방아를 찧음 (공이 위 · 아래 두 장을 번갈아)
            // 7차 "달토끼는 다른 이미지와 겹치지 않게": 하늘 그림에서 달을 빼 따로 — 사령탑 위 왼쪽 위 빈 곳 (적 · 탄은 방어선 오른쪽에만 다님) · PlaceMoon
            moon = Spr("Moon", Art.Get("Bg/moon", 100f), Vector2.zero, -99);
            rabbit = Spr("MoonRabbit", Art.Get("Bg/moon_rabbit_0", 100f), Vector2.zero, -98);
            PlaceMoon();
            // 11-18: 풀밭 + 돔 → 달 바닥(지평선 위 지구) + 왼쪽 벽 사령탑. 7차: 땅 그림 1080x700 = 월드 y -9.6 ~ -2.6 (지구를 크게)
            // 8차 "전투 화면의 바닥은 지구 형태 · 지구로 보이는 그림은 일반 행성으로 (사령탑이 지구에 있고 지구로 떨어지는 운석을 막는 느낌)"
            //   → 바닥 = 큰 지구의 둥근 윗면(Bg/ground_earth, 왼쪽 사령탑 자리는 평평 · 오른쪽으로 갈수록 둥글게 내려감) + 뒤 하늘에 일반 행성
            ground = Spr("Ground", Art.Get("Bg/ground_earth", 100f), new Vector2(0, Bottom + 3.5f), -50);
            towerGlow = Spr("TowerGlow", Art.Fx("glow"), new Vector2(-4.9f, GroundY + 6f), -41);
            towerGlow.transform.localScale = new Vector3(4f, 16f, 1); towerGlow.color = new Color(1, 1, 1, 0);
            tower = Spr("Tower", TowerSprite("Bg/tower"), new Vector2(-HalfW, GroundY), -40);
            // 7차 로비: 사령탑 없이 심플한 하늘 + 아래 큰 지구 (꼭대기 = Lobby_Earth_Top) — 대장 카피가 그 위를 걸음
            earthBig = Spr("EarthBig", Art.Get("Bg/earth_big", 100f, new Vector2(0.5f, 1f)), new Vector2(0, EarthTop + 1f), -50);
            for (int i = 0; i < 4; i++)
                slotMarks[i] = Spr("Slot" + i, Art.Get("Bg/slot", 100f), SlotPos(i + 1) + new Vector2(0, 0.05f), -30);
            ApplyLayout();
            if (SaveData.Cur != null) SetFrame(Tables.Frame(SaveData.Cur.frame));
            ShowSlots(false);
            SetLobby(false);
        }

        /// <summary>9차: 전장 모양(세로 돔 ↔ 사령탑)에 맞춰 바닥 · 기지 그림 · 빛 · 빈 칸 자리를 놓음 (Config Battle_Vertical — 전투에 들어갈 때마다 다시 봄)</summary>
        public void ApplyLayout()
        {
            Invalidate();
            bool nv = V;
            layoutV = nv;
            if (nv)
            {
                ground.sprite = Art.Get("Bg/ground_earth_v", 100f) ?? Art.Get("Bg/ground_earth", 100f);
                tower.sprite = Art.Get("Bg/dome", 100f);
                tower.transform.position = new Vector2(0, Bottom + 2.8f);
                towerGlow.transform.position = new Vector2(0, Bottom + 3.1f); towerGlow.transform.localScale = new Vector3(15f, 7.5f, 1);
            }
            else
            {
                ground.sprite = Art.Get("Bg/ground_earth", 100f);
                tower.sprite = TowerSprite("Bg/tower");
                tower.transform.position = new Vector2(-HalfW, GroundY);
                towerGlow.transform.position = new Vector2(-4.9f, GroundY + 6f); towerGlow.transform.localScale = new Vector3(4f, 16f, 1);
            }
            for (int i = 0; i < 4; i++) slotMarks[i].transform.position = SlotPos(i + 1) + new Vector2(0, 0.05f);
            SetFrame(frameRow ?? (SaveData.Cur != null ? Tables.Frame(SaveData.Cur.frame) : null));
        }

        // ---------- 7차 수정안: 로비(타이틀) 배경 ----------
        public bool Lobby { get; private set; }
        /// <summary>로비 큰 지구 꼭대기 높이 (월드) · 반지름 — 그림 tools/tower_art.py LE_* 와 같게</summary>
        public static float EarthTop => Tables.Cf("Lobby_Earth_Top", -3.4f);
        public const float EarthR = 7.8f;
        /// <summary>로비 지구 표면 높이 (x 에서) · 그 자리의 기울기(도)</summary>
        public static float EarthSurf(float x) => EarthTop - EarthR + Mathf.Sqrt(Mathf.Max(0, EarthR * EarthR - x * x));
        public static float EarthTilt(float x) => -Mathf.Asin(Mathf.Clamp(x / EarthR, -1, 1)) * Mathf.Rad2Deg;
        /// <summary>로비 = 사령탑 · 달 바닥 · 달토끼 숨김, 하늘은 심플판, 큰 지구 + 별똥별 ("기지 외형은 게임 안에서만")</summary>
        public void SetLobby(bool on)
        {
            Lobby = on;
            tower.enabled = ground.enabled = moon.enabled = rabbit.enabled = !on;
            towerGlow.gameObject.SetActive(!on);
            earthBig.enabled = on;
            sky.sprite = Art.Get(on ? "Bg/sky_plain" : "Bg/sky", 100f);
            starT = 0.6f;
            if (layoutV != V) ApplyLayout();      // 9차: Config Battle_Vertical 이 바뀌었으면 (로비 · 전투 모두) 배치를 다시
            if (!on) foreach (var st in stars) st.sr.enabled = false;
            if (on) ShowSlots(false);
        }

        /// <summary>달: 7차 = 사령탑 위 왼쪽 위 → 8차 "달토끼는 우측 상단 (어느 정도 운석을 가려도 괜찮음)" = 오른쪽 위 (Moon_X 4.3) — 위 UI(Lv 게이지) 아래.
        /// 운석 · 탄보다 뒤에 그림(하늘 그림 바로 앞) → 운석이 달 앞을 지나감</summary>
        void PlaceMoon()
        {
            float mx = Tables.Cf("Moon_X", 4.3f);
            float y = PlayTop - Tables.Cf("Moon_Below_Hud", 1.05f);
            if (mx < SlotXPos + 1f) y = Mathf.Max(y, TowerTop + 1.85f);      // 왼쪽(사령탑 위)이면 안테나보다 위
            var p = new Vector2(mx, y);
            moon.transform.position = p;
            rabbit.transform.position = p;      // 달토끼 그림 160x160 의 가운데 = 달 가운데
        }

        // 별똥별 (7차 "배경 화면에 약간의 애니메이션 · 별똥별 떨어지는 느낌", 로비만): 하늘 위쪽에서 왼쪽 아래로 빠르게 긋고 사라짐
        class Star { public SpriteRenderer sr; public Vector2 p, v; public float t, life; }
        readonly System.Collections.Generic.List<Star> stars = new();
        float starT;
        public int StarsShot { get; private set; }     // 시험용
        void StarStep(float dt)
        {
            if (!Lobby) return;
            if ((starT -= dt) <= 0)
            {
                starT = Random.Range(Tables.Cf("Shoot_Star_Gap_Min", 1.2f), Tables.Cf("Shoot_Star_Gap_Max", 3.2f));
                var st = stars.Find(x => !x.sr.enabled);
                if (st == null) { st = new Star { sr = Spr("ShootStar", Art.Get("Bg/shooting_star", 100f, new Vector2(320f / 360f, 0.5f)), Vector2.zero, -90) }; stars.Add(st); }
                // 8차 수정안 "떨어지는 별똥별은 조금 더 천천히" (타이틀 · 로비): 9~13 → Shoot_Star_Speed 4.5~6.5 · 보이는 시간은 그만큼 길게
                float ang = Random.Range(200f, 222f) * Mathf.Deg2Rad, sp = Random.Range(Tables.Cf("Shoot_Star_Speed_Min", 4.5f), Tables.Cf("Shoot_Star_Speed_Max", 6.5f));
                st.p = new Vector2(Random.Range(-3f, HalfW + 1.5f), Random.Range(EarthTop + 4f, Top - 1f));
                st.v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp;
                st.t = 0; st.life = Random.Range(Tables.Cf("Shoot_Star_Life_Min", 1.3f), Tables.Cf("Shoot_Star_Life_Max", 2.0f));
                st.sr.enabled = true;
                st.sr.transform.rotation = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg);
                st.sr.transform.localScale = Vector3.one * Random.Range(0.7f, 1.1f);
                StarsShot++;
            }
            foreach (var st in stars)
            {
                if (!st.sr.enabled) continue;
                st.t += dt; st.p += st.v * dt;
                float k = st.t / st.life;
                if (k >= 1 || st.p.y < EarthSurf(st.p.x) - 0.3f) { st.sr.enabled = false; continue; }     // 지구 뒤로 넘어가면 끝
                st.sr.transform.position = st.p;
                st.sr.color = new Color(1, 1, 1, Mathf.Min(1, k * 6f) * (1 - k * k));
            }
        }

        /// <summary>폭 10.8칸이 꼭 보이게 + 화면 아래 끝을 y -9.6 에 맞춤</summary>
        public void Fit()
        {
            cam.orthographic = true;
            float aspect = cam.aspect > 0 ? cam.aspect : 9f / 16f;
            cam.orthographicSize = Mathf.Max(9.6f, HalfW / aspect);
            cam.transform.position = new Vector3(0, Bottom + cam.orthographicSize, -10);
            cached = false;
        }

        void OnDestroy() { if (I == this) { I = null; cached = false; } }

        float lastAspect;
        void Update()
        {
            if (rabbit)
            {
                rabbitT += Time.deltaTime;
                bool down = Mathf.Repeat(rabbitT, 0.7f) > 0.45f;
                if (down != RabbitDown) { RabbitDown = down; rabbit.sprite = Art.Get("Bg/moon_rabbit_" + (down ? 1 : 0), 100f); }
            }
            if (towerGlow) { glowT += Time.deltaTime; var c = glowCol; c.a = glowCol.a > 0 ? 0.22f + 0.08f * Mathf.Sin(glowT * 1.6f) : 0; towerGlow.color = c; }
            if (cam && Mathf.Abs(cam.aspect - lastAspect) > 0.001f) { lastAspect = cam.aspect; Fit(); if (moon) PlaceMoon(); }
            StarStep(Time.deltaTime);
            // 자리 고르기 중이면 빈 칸이 숨쉬듯 반짝 (11-18 "터렛을 고를 때 자리를 고르는 흐름")
            if (pulse)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                foreach (var m in slotMarks) if (m.enabled) { m.color = Color.Lerp(Color.white, Art.Gold, k); m.transform.localScale = Vector3.one * (1.05f + 0.15f * k); }
            }
        }

        bool pulse;
        /// <summary>빈 칸 표시: 터렛이 없는 칸만 (11-18: 칸을 골라 놓으므로 앞에서부터 채워지지 않음). pick = 자리 고르기 중 반짝</summary>
        public void ShowSlots(bool show, bool pick = false)
        {
            var run = Battle.I != null ? Battle.I.run : null;
            pulse = show && pick;
            for (int i = 0; i < 4; i++)
            {
                bool used = run != null && run.turrets.Exists(t => t.slot == i + 1);
                slotMarks[i].enabled = show && !used;
                if (!pulse) { slotMarks[i].color = Color.white; slotMarks[i].transform.localScale = Vector3.one; }
            }
        }
        /// <summary>예전 호출 모양 (칸 수) — 이제 칸 수는 안 씀</summary>
        public void ShowSlots(int used, bool show = true) => ShowSlots(show);

        public SpriteRenderer Spr(string name, Sprite s, Vector2 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.sortingOrder = order;
            return sr;
        }

        /// <summary>화면 좌표 → 월드</summary>
        public Vector2 ScreenToWorld(Vector2 sp) => cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10));
    }
}
