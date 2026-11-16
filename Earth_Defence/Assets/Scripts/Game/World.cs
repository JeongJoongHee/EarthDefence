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
            groundY = Tables.Cf("Ground_Y", -7.52f); shieldY = Tables.Cf("Shield_Y", -4.6f);
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
        /// <summary>터렛 칸 x (안쪽부터 채움: 왼쪽 안 · 오른쪽 안 · 왼쪽 밖 · 오른쪽 밖)</summary>
        public static readonly float[] SlotX = { -2.72f, 2.72f, -4.3f, 4.3f };
        public float Top => cam.transform.position.y + cam.orthographicSize;
        readonly SpriteRenderer[] slotMarks = new SpriteRenderer[4];
        SpriteRenderer dome, domeGlow, rabbit;
        float rabbitT;
        public bool RabbitDown { get; private set; }
        float glowT; Color glowCol;

        /// <summary>기지 외형 (3차): 돔 색 + 둘레 은은한 빛 (보통 그리기 — 빛 더하기는 밝은 땅에서 하얗게 탐)</summary>
        public void SetFrame(FrameRow f)
        {
            if (!dome) return;
            // 6차 수정안: 외형마다 다른 돔 그림 (그림에 색이 들어 있음) — 그림이 없으면 예전처럼 기본 돔에 색만
            var sp = f != null ? Art.Get("Bg/dome_" + f.id, 100f) : null;
            dome.sprite = sp ? sp : Art.Get("Bg/dome", 100f);
            dome.color = sp ? Color.white : f != null ? f.tint : Color.white;
            glowCol = f != null && f.id != "basic" ? f.glow : new Color(1, 1, 1, 0);
        }

        public void Build(Camera c)
        {
            I = this; cam = c;
            Fit();
            Spr("Sky", Art.Get("Bg/sky", 100f), new Vector2(0, Bottom + 12f), -100);
            // 2차 수정안: 달토끼 — 하늘 그림의 달(915, 780px) 위에서 방아를 찧음 (공이 위 · 아래 두 장을 번갈아)
            rabbit = Spr("MoonRabbit", Art.Get("Bg/moon_rabbit_0", 100f), new Vector2((915 - 540) / 100f, Bottom + 24f - 7.8f), -99);
            Spr("Ground", Art.Get("Bg/ground", 100f), new Vector2(0, Bottom + 2.8f), -50);
            domeGlow = Spr("DomeGlow", Art.Fx("glow"), new Vector2(0, Bottom + 3.1f), -41);
            domeGlow.transform.localScale = new Vector3(15f, 7.5f, 1); domeGlow.color = new Color(1, 1, 1, 0);
            dome = Spr("Dome", Art.Get("Bg/dome", 100f), new Vector2(0, Bottom + 2.8f), -40);
            if (SaveData.Cur != null) SetFrame(Tables.Frame(SaveData.Cur.frame));
            for (int i = 0; i < 4; i++)
            {
                slotMarks[i] = Spr("Slot" + i, Art.Get("Bg/slot", 100f), new Vector2(SlotX[i], GroundY - 0.02f), -30);
            }
            ShowSlots(0);
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
            if (domeGlow) { glowT += Time.deltaTime; var c = glowCol; c.a = glowCol.a > 0 ? 0.22f + 0.08f * Mathf.Sin(glowT * 1.6f) : 0; domeGlow.color = c; }
            if (cam && Mathf.Abs(cam.aspect - lastAspect) > 0.001f) { lastAspect = cam.aspect; Fit(); }
        }

        /// <summary>빈 칸 표시 (터렛이 놓인 칸 수 = used)</summary>
        public void ShowSlots(int used, bool show = true)
        {
            for (int i = 0; i < 4; i++) slotMarks[i].enabled = show && i >= used;
        }

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
