using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 7차 수정안 "타이틀 + 게임 시작 입장 버튼 · 입장 버튼 이후 계정 연동과 게스트 입장 2가지 선택지
    /// (계정 연동은 구글 · 게스트 입장은 데이터 저장이 없다는 메시지)".
    /// 배경 = 로비와 같은 심플한 하늘 + 큰 지구 + 별똥별 + 지구 위를 걷는 대장 카피 (World.SetLobby).
    /// [게임 시작] → 이미 연동돼 있으면 바로 로비 · 아니면 [구글 계정으로 시작] / [게스트로 시작] 창
    /// → 게스트는 "데이터가 저장되지 않아요" 확인 창을 한 번 더.
    /// 구글 연동은 아직 [초안](Store.Link — 실제 구글 로그인은 출시 단계). 게스트도 이 폰 안에는 저장됨(앱을 지우면 사라짐).
    /// Config Title_On 0 이면 타이틀 없이 바로 로비.
    /// </summary>
    public class Title : MonoBehaviour
    {
        public static Title I;
        RectTransform root, logo;
        float t;

        public static void Show()
        {
            if (I) return;
            I = UIKit.Rect(UIRoot.I.pages, "Title").Fill().gameObject.AddComponent<Title>();
            I.Build();
        }

        void Build()
        {
            root = (RectTransform)transform;
            // 제목 (두 줄): 작은 "카피바라" + 큰 "지구를 지켜라!"
            logo = UIKit.Rect(root, "Logo"); logo.Place(0.5f, 1, new Vector2(0, -330), new Vector2(1000, 400), new Vector2(0.5f, 0.5f));
            var glow = UIKit.Img(logo, Art.Fx("glow"), "Glow"); glow.raycastTarget = false; glow.color = new Color(0.62f, 0.85f, 1f, 0.28f);
            glow.rectTransform.Place(0.5f, 0.5f, Vector2.zero, new Vector2(1100, 520), new Vector2(0.5f, 0.5f));
            var a = UIKit.Label(logo, L.T("title_line1"), 84, Art.Cream); a.rectTransform.Place(0.5f, 0.5f, new Vector2(0, 92), new Vector2(1000, 110), new Vector2(0.5f, 0.5f)); a.name = "Line1";
            var b = UIKit.Label(logo, L.T("title_line2"), 128, Art.Gold); b.rectTransform.Place(0.5f, 0.5f, new Vector2(0, -50), new Vector2(1000, 170), new Vector2(0.5f, 0.5f)); b.name = "Line2";
            var sub = UIKit.Label(logo, L.T("title_sub"), 34, Art.Sky); sub.rectTransform.Place(0.5f, 0.5f, new Vector2(0, -160), new Vector2(1000, 50), new Vector2(0.5f, 0.5f));

            // 8차 수정안 "게임 시작 버튼 하단에 배치 · 크기 0.7배" (Config Title_Btn_Scale · Title_Btn_Bottom)
            float k = Tables.Cf("Title_Btn_Scale", 0.7f);
            var start = UIKit.Btn(root, "btn_green", L.T("title_start"), Mathf.RoundToInt(64 * k), OnStart, "Start");
            ((RectTransform)start.transform).Place(0.5f, 0, new Vector2(0, Tables.Cf("Title_Btn_Bottom", 230f)), new Vector2(640 * k, 170 * k), new Vector2(0.5f, 0));
            startRT = (RectTransform)start.transform;
            var ver = UIKit.Label(root, "v" + Application.version, 26, new Color(1, 1, 1, 0.55f), TextAnchor.LowerRight, false);
            ver.rectTransform.Place(1, 0, new Vector2(-24, 18), new Vector2(300, 40), new Vector2(1, 0));
            PlaceStart();
        }

        RectTransform startRT;
        /// <summary>[게임 시작] = 7차: 지구 위 대장 카피 머리 위 (Dome_Top_Y) → 8차: 화면 아래 (Title_Btn_Bottom, Config Lobby_Btn_Bottom 0 이면 7차 자리)</summary>
        void PlaceStart()
        {
            if (!startRT || World.I == null) return;
            if (Tables.Ci("Lobby_Btn_Bottom", 1) == 1) return;      // 8차: 아래 고정 (Build 에서 놓은 자리)
            var sp = World.I.cam.WorldToScreenPoint(new Vector3(0, Tables.Cf("Dome_Top_Y", -2f), 0));
            var ucam = UIRoot.I.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UIRoot.I.canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, ucam, out var lp)) return;
            startRT.anchoredPosition = new Vector2(0, Mathf.Max(40, lp.y - root.rect.yMin + 40));
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            // 제목이 둥실 · [게임 시작]이 숨쉬듯
            if (logo) logo.anchoredPosition = new Vector2(0, -330 + Mathf.Sin(t * 1.4f) * 10f);
            if (startRT) startRT.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(t * 3f));
            PlaceStart();
        }

        public void OnStart()
        {
            if (Popup.Open != null) return;
            if (Store.Linked) { Enter(); return; }
            Choose();
        }

        /// <summary>입장 방법 고르기: 구글 계정 연동 / 게스트</summary>
        public void Choose()
        {
            var p = Popup.Make(L.T("title_enter"), 860, 700);
            p.Text(L.T("title_enter_desc"), 130, 32, Art.Soft, 100);
            var g = p.Btn("btn_green", L.T("title_google"), 0, 290, 640, () => { p.Close(false); Google(); }, 130);
            g.name = "Google";
            var gu = p.Btn("btn_cream", L.T("title_guest"), 0, 130, 640, () => { p.Close(false); GuestAsk(); }, 130);
            gu.name = "Guest";
            p.Text(L.T("title_draft"), 610, 24, Art.Soft, 40);
        }

        /// <summary>[초안] 구글 계정 연동 — 실제 구글 로그인(Play 게임즈)은 출시 단계에서 붙임</summary>
        public void Google()
        {
            Store.Link();
            Enter();
            Toast.Show(L.T("title_google_ok"));
        }

        /// <summary>게스트: 데이터가 저장되지 않는다는 확인 창</summary>
        public void GuestAsk()
        {
            var p = Popup.Make(L.T("title_guest"), 860, 640);
            var warn = UIKit.Img(p.win, Art.Icon("info"), "Warn"); warn.raycastTarget = false; warn.rectTransform.Place(0.5f, 1, new Vector2(0, -150), new Vector2(90, 90), new Vector2(0.5f, 0.5f));
            p.Text(L.T("title_guest_warn"), 210, 34, Art.Red, 60);
            p.Text(L.T("title_guest_desc"), 280, 28, Art.Ink, 140);
            p.Btn("btn_cream", L.T("cancel"), -170, 50, 300, () => { p.Close(false); Choose(); }, 110);
            p.Btn("btn_orange", L.T("title_guest_go"), 170, 50, 300, () => { p.Close(false); Guest = true; Enter(); }, 110).name = "GuestGo";
        }

        /// <summary>이번에 게스트로 들어왔는지 (시험 · 설정 안내용)</summary>
        public static bool Guest;

        public void Enter()
        {
            Popup.CloseAll();
            I = null;
            Destroy(gameObject);
            // 9차 수정안 "게임 최초 시작 시, 왜 대장 카피가 지구를 지켜야 하는지 설명하는 스토리" → 끝나면 로비
            if (StoryView.Want("prologue")) StoryView.Play("prologue", 0, Game.ToLobby);
            else Game.ToLobby();
        }

        void OnDestroy() { if (I == this) I = null; }
    }
}
