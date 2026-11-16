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
        public static float GroundY => Tables.Cf("Ground_Y", -7.52f);
        public static float ShieldY => Tables.Cf("Shield_Y", -4.6f);
        /// <summary>적이 나오는 높이 = Spawn_Y 와 "위쪽 UI 바로 아래" 중 낮은 쪽 (11-16: 화면이 짧으면 UI 밑에 숨어 내려오던 것).
        /// 19.5:9 이상 폰은 Spawn_Y(10.6) 그대로, 그보다 짧은 화면만 낮아짐</summary>
        public static float SpawnY => Mathf.Min(Tables.Cf("Spawn_Y", 10.6f), I ? I.PlayTop - Tables.Cf("Spawn_Below_Hud", 0.6f) : 99f);
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

        public void Build(Camera c)
        {
            I = this; cam = c;
            Fit();
            Spr("Sky", Art.Get("Bg/sky", 100f), new Vector2(0, Bottom + 12f), -100);
            Spr("Ground", Art.Get("Bg/ground", 100f), new Vector2(0, Bottom + 2.8f), -50);
            Spr("Dome", Art.Get("Bg/dome", 100f), new Vector2(0, Bottom + 2.8f), -40);
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
        }

        float lastAspect;
        void Update()
        {
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
