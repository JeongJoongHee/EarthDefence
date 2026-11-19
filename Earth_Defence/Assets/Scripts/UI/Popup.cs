using System;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>창: 크림 판 + 주황 제목 띠 (+ 오른쪽 위 X). 전투 결과 · 일시정지 · 고른 카드 목록 · 레벨업.</summary>
    public class Popup : MonoBehaviour
    {
        public RectTransform win;
        // 11-17 결함 고침: 창이 두 겹일 때(회로 착용 안내 위에 확인 창 등) 위 창을 닫으면 Open 이 null 이 되어,
        //   아래 창이 떠 있는데도 "창 없음"으로 보고 토스트 자리 · 로비 알림이 어긋날 수 있었음 → 열린 창 목록의 맨 위를 돌려줌
        static readonly System.Collections.Generic.List<Popup> stack = new();
        public static Popup Open { get { for (int i = stack.Count - 1; i >= 0; i--) { if (stack[i] && !stack[i].closed) return stack[i]; stack.RemoveAt(i); } return null; } }
        Action onClose;
        float t;
        /// <summary>8차: 닫기(X) 단추가 있는 창 = 폰 뒤로 가기로 닫힘</summary>
        public bool CanClose { get; private set; }

        public static Popup Make(string title, float w, float h, Action onClose = null, bool closeBtn = true, float y = 0.53f)
        {
            var root = UIKit.Rect(UIRoot.I.popups, "Popup_" + title).Fill();
            var p = root.gameObject.AddComponent<Popup>();
            p.onClose = onClose; p.CanClose = closeBtn;
            var dim = UIKit.Plain(root, new Color(0.08f, 0.07f, 0.16f, 0.6f), "Dim"); dim.rectTransform.Fill(); dim.raycastTarget = true;
            p.win = UIKit.Panel(root, "window", "Window").rectTransform;
            p.win.Place(0.5f, y, Vector2.zero, new Vector2(w, h), new Vector2(0.5f, 0.5f));
            var band = UIKit.Img(p.win, Art.UI("band"), "Title"); band.color = Art.Orange; band.raycastTarget = false;
            band.rectTransform.anchorMin = new Vector2(0, 1); band.rectTransform.anchorMax = new Vector2(1, 1); band.rectTransform.pivot = new Vector2(0.5f, 1);
            // 4차 수정안 "팝업을 컴팩트하게": 제목 띠 100 → 90
            band.rectTransform.offsetMin = new Vector2(5, -90); band.rectTransform.offsetMax = new Vector2(-5, -5);
            var line = UIKit.Plain(p.win, Art.Ink, "TitleLine"); line.rectTransform.Place(0.5f, 1, new Vector2(0, -90), new Vector2(w - 10, 6), new Vector2(0.5f, 0.5f));
            UIKit.Label(band.transform, title, 46).rectTransform.Fill(0, 4, 0, 0);
            if (closeBtn)
            {
                var x = UIKit.Tile(p.win, "btn_red", Color.white, Art.Icon("x"), 0.56f, "Close"); x.rectTransform.Place(1, 1, new Vector2(16, 16), new Vector2(80, 80), new Vector2(1, 1));
                UIKit.Clickable(x, p.Close);
            }
            stack.Add(p);
            p.win.localScale = Vector3.one * 0.85f;
            return p;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.15f);
            win.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1 - (1 - k) * (1 - k));
        }

        public static void CloseAll()
        {
            if (UIRoot.I == null) return;
            foreach (var p in UIRoot.I.popups.GetComponentsInChildren<Popup>()) p.Close(false);   // 2차: 화면을 바꿀 때 닫기 뒤 할 일(출석 → 다음 알림)이 다시 창을 열지 않게
        }

        bool closed;
        public void Close() => Close(true);
        public void Close(bool callback)
        {
            if (closed) return;
            closed = true;
            stack.Remove(this);
            if (callback) onClose?.Invoke();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public Button Btn(string kind, string label, float x, float y, float w, Action a, float h = 110)
        {
            var b = UIKit.Btn(win, kind, label, 40, a, "Btn_" + label);
            b.GetComponent<RectTransform>().Place(0.5f, 0, new Vector2(x, y), new Vector2(w, h), new Vector2(0.5f, 0));
            return b;
        }

        public Text Text(string s, float y, int size, Color? c = null, float h = 60, bool outline = false)
        {
            var t = UIKit.Label(win, s, size, c ?? Art.Ink, outline: outline);
            t.rectTransform.Place(0.5f, 1, new Vector2(0, -y), new Vector2(win.sizeDelta.x - 60, h), new Vector2(0.5f, 1));
            return t;
        }
    }

    /// <summary>뿅 하고 나타남</summary>
    public class PopIn : MonoBehaviour
    {
        public float delay; float t;
        public bool sound;    // 나타날 때 "뾱" (받은 것 하나씩)
        void Start() { transform.localScale = Vector3.zero; }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (sound && t >= delay) { sound = false; Audio.Play(Audio.Sfx.Coin, 0.7f); }
            float k = Mathf.Clamp01((t - delay) / 0.18f);
            float s = k < 1 ? Mathf.Sin(k * Mathf.PI * 0.75f) / Mathf.Sin(Mathf.PI * 0.75f) : 1;
            transform.localScale = Vector3.one * s;
            if (k >= 1) enabled = false;
        }
    }
}
