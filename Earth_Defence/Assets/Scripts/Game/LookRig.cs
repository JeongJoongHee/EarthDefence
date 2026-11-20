using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 11-20 코스튬 · 탈것: 대장 카피 = 코스튬 몸 그림 + 탈것(뒤 층 · 앞 층)을 겹쳐 그림 → "코스튬 x 탈것" 이 최종 모습.
    /// 전투(TurretUnit) · 로비(LobbyWalker)는 SpriteRenderer 로, 장비 탭 · 외형 탭은 UI Image 로 (LookView).
    /// 그림 = tools/look_art.py (코스튬 피벗 = 발 · 탈것 피벗 = 자리 → 두 피벗을 같은 점에 두면 올라탄 모습)
    /// </summary>
    public class MountRig : MonoBehaviour
    {
        public SpriteRenderer back, front;
        public LookRow row;
        public float lift, bobY;
        float t;
        public bool Bob => row != null && row.bob && Tables.Cf("Mount_Bob", 1f) > 0;

        /// <summary>몸(owner) 아래에 탈것 층을 붙임. 없으면 null · lift = 발을 얼마나 올릴지 (월드 칸)</summary>
        public static MountRig Attach(Transform owner, string mountId, int bodyOrder, float scale = 1f)
        {
            var row = Tables.Mount(mountId);
            var sp = Art.Mount(mountId);
            if (row == null || sp == null) return null;
            var go = new GameObject("Mount");
            go.transform.SetParent(owner, false);
            go.transform.localScale = Vector3.one * scale;
            var m = go.AddComponent<MountRig>();
            m.row = row; m.t = Random.value * 10f;
            m.back = Layer(go.transform, "MountBack", sp, bodyOrder - 1);
            var fs = Art.MountFront(mountId);
            if (fs) m.front = Layer(go.transform, "MountFront", fs, bodyOrder + 1);
            m.lift = row.lift * scale * Tables.Cf("Mount_Lift_Mul", 1f);
            return m;
        }

        static SpriteRenderer Layer(Transform p, string name, Sprite s, int order)
        {
            var g = new GameObject(name); g.transform.SetParent(p, false);
            var r = g.AddComponent<SpriteRenderer>(); r.sprite = s; r.sortingOrder = order;
            return r;
        }

        public void Flip(bool left) { if (back) back.flipX = left; if (front) front.flipX = left; }

        /// <summary>몸이 기울어도 탈것은 수평 (몸만 젖힘) · 떠다니는 탈것은 위아래 둥실 — 바뀐 높이(월드)를 돌려줌</summary>
        public float Step(float dt, bool level = true)
        {
            if (level) transform.rotation = Quaternion.identity;
            if (!Bob) return 0;
            t += dt;
            float y = Mathf.Sin(t * 1.8f) * Tables.Cf("Mount_Bob_Amp", 0.06f);
            float d = y - bobY; bobY = y;
            return d;
        }
    }

    /// <summary>UI 에 코스튬 + 탈것을 겹쳐 그림 (장비 탭 · 외형 탭 미리 보기 · 목록 칸). 상자(box) 안에 맞춤 · maxScale = 그림 1px 당 UI 크기 상한</summary>
    public static class LookView
    {
        public static RectTransform Build(Transform parent, string costume, string mount, Vector2 box, float maxScale = 1f, string name = "Look")
        {
            var root = UIKit.Rect(parent, name);
            root.sizeDelta = box;
            var body = Art.CostumeFloat(costume);
            var mb = Art.Mount(mount); var mf = Art.MountFront(mount);
            float ms = Tables.Mount(mount)?.scale ?? 1f;
            // 발(= 자리)을 원점으로 한 각 층의 사각형(그림 px) → 합친 범위
            // 그림 전체가 아니라 실제로 그려진 범위로 (모자 · 꼬리 자리로 비워 둔 여백 때문에 작게 보이지 않게)
            Rect R(Sprite s, float k) { if (s == null) return Rect.zero; var d = Art.DrawnRect(s); return new Rect((d.x - s.pivot.x) * k, (d.y - s.pivot.y) * k, d.width * k, d.height * k); }
            Rect u = R(body, 1);
            foreach (var s in new[] { mb, mf }) if (s) { var r = R(s, ms); u = Rect.MinMaxRect(Mathf.Min(u.xMin, r.xMin), Mathf.Min(u.yMin, r.yMin), Mathf.Max(u.xMax, r.xMax), Mathf.Max(u.yMax, r.yMax)); }
            if (u.width <= 0) return root;
            float sc = Mathf.Min(maxScale, box.x / u.width, box.y / u.height);
            Vector2 origin = -u.center * sc;
            void Add(Sprite s, float k, string n)
            {
                if (!s) return;
                var im = UIKit.Img(root, s, n); im.raycastTarget = false;
                var rt = im.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
                rt.sizeDelta = s.rect.size * k * sc;
                rt.anchoredPosition = origin;
            }
            Add(mb, ms, "MountBack"); Add(body, 1, "Body"); Add(mf, ms, "MountFront");
            return root;
        }
    }
}
