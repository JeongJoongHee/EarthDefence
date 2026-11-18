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
            groundY = Tables.Cf("Ground_Y", -7.52f); shieldY = Tables.Cf("Shield_Y", -4.6f); shieldX = Tables.Cf("Shield_X", -2.1f);
            spawnY = Mathf.Min(Tables.Cf("Spawn_Y", 10.6f), I ? I.PlayTop - Tables.Cf("Spawn_Below_Hud", 0.6f) : 99f);
            cached = I;      // 월드가 생기기 전(로딩 중)엔 매번 다시 계산
        }
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
        public const float SlotXPos = -3.1f;
        public static Vector2 SlotPos(int slot) => new(SlotXPos, GroundY + SlotUp[Mathf.Clamp(slot - 1, 0, 3)] - (slot == 4 ? 0.06f : 0f));
        /// <summary>대장 카피 자리 = 가운데 사령실 난간 위 (11-18 "대장 카피는 가운데 고정")</summary>
        public static Vector2 GuardianPos => new(-3.9f, GroundY + 5.805f);
        public static float TowerTop => GroundY + 11.85f;
        /// <summary>방어선 x — 적은 이 선(사령탑 앞 · 터렛 오른쪽)에 닿으면 멈춰 때리거나 부딪쳐 터짐</summary>
        public static float ShieldX { get { if (!cached) Recache(); return shieldX; } }
        /// <summary>싸우는 하늘 가운데 (소용돌이 필살기 등)</summary>
        public static Vector2 FieldCenter => new((ShieldX + HalfW) * 0.5f, 1.5f);
        /// <summary>기지 효과(회복 · 얼림 · 부활)가 퍼지는 곳 = 사령실 앞</summary>
        public static Vector2 BaseCenter => new(-3.6f, GroundY + 5.2f);
        /// <summary>화면 안 (적을 노릴 수 있음): 위 UI 아래 · 오른쪽 끝 안쪽</summary>
        public static bool OnField(Vector2 p) => p.y < SpawnY - 0.3f && p.x < HalfW - 0.15f;
        static float shieldX;

        public float Top => cam.transform.position.y + cam.orthographicSize;
        readonly SpriteRenderer[] slotMarks = new SpriteRenderer[4];
        SpriteRenderer tower, towerGlow, rabbit;
        float rabbitT;
        public bool RabbitDown { get; private set; }
        float glowT; Color glowCol;
        public SpriteRenderer SlotMark(int slot) => slotMarks[Mathf.Clamp(slot - 1, 0, 3)];

        /// <summary>기지 외형 (3차 → 11-18 사령탑 7종): 외형마다 다른 사령탑 그림 + 둘레 은은한 빛</summary>
        public void SetFrame(FrameRow f)
        {
            if (!tower) return;
            var sp = f != null ? TowerSprite("Bg/tower_" + f.id) : null;
            tower.sprite = sp ? sp : TowerSprite("Bg/tower");
            glowCol = f != null && f.id != "basic" ? f.glow : new Color(1, 1, 1, 0);
        }
        /// <summary>사령탑 그림 420x1440 (PPU 100) — 피벗 = 왼쪽 아래에서 땅 높이(아래 40px)</summary>
        static Sprite TowerSprite(string path) => Art.Get(path, 100f, new Vector2(0f, 40f / 1440f));

        public void Build(Camera c)
        {
            I = this; cam = c;
            Fit();
            Spr("Sky", Art.Get("Bg/sky", 100f), new Vector2(0, Bottom + 12f), -100);
            // 2차 수정안: 달토끼 — 하늘 그림의 달(915, 780px) 위에서 방아를 찧음 (공이 위 · 아래 두 장을 번갈아)
            rabbit = Spr("MoonRabbit", Art.Get("Bg/moon_rabbit_0", 100f), new Vector2((915 - 540) / 100f, Bottom + 24f - 7.8f), -99);
            // 11-18: 풀밭 + 돔 → 달 바닥(지평선 위 지구) + 왼쪽 벽 사령탑. 땅 그림 1080x460 = 월드 y -9.6 ~ -5.0
            Spr("Ground", Art.Get("Bg/ground_moon", 100f), new Vector2(0, Bottom + 2.3f), -50);
            towerGlow = Spr("TowerGlow", Art.Fx("glow"), new Vector2(-4.4f, GroundY + 6f), -41);
            towerGlow.transform.localScale = new Vector3(5f, 16f, 1); towerGlow.color = new Color(1, 1, 1, 0);
            tower = Spr("Tower", TowerSprite("Bg/tower"), new Vector2(-HalfW, GroundY), -40);
            if (SaveData.Cur != null) SetFrame(Tables.Frame(SaveData.Cur.frame));
            for (int i = 0; i < 4; i++)
                slotMarks[i] = Spr("Slot" + i, Art.Get("Bg/slot", 100f), SlotPos(i + 1) + new Vector2(0, 0.05f), -30);
            ShowSlots(false);
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
            if (cam && Mathf.Abs(cam.aspect - lastAspect) > 0.001f) { lastAspect = cam.aspect; Fit(); }
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
