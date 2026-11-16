using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 로비 도전 탭 (3차): 무한 모드 · 엘리트 군단 — 하루 입장 횟수 · 최고 기록 · 스태미나를 쓰지 않음 · 난이도 = 깬 최고 스테이지.
    /// </summary>
    public class ChallengeTab : MonoBehaviour
    {
        static SaveData S => SaveData.Cur;
        Lobby lobby;
        readonly List<(string mode, Text tickets, Text best, Button go)> cards = new();

        public void Build(Lobby l)
        {
            lobby = l;
            var root = (RectTransform)transform;
            Card(root, "endless", 14, "Bg/sky");
            Card(root, "survival", 470, null);
        }

        void Card(RectTransform root, string mode, float y, string _)
        {
            var c = UIKit.Panel(root, "card", "Mode_" + mode); c.rectTransform.TL(30, y, 1020, 440);
            var ic = UIKit.Tile(c.transform, mode == "endless" ? "btn_sky" : "btn_purple", Color.white, Art.Icon(mode == "endless" ? "trophy" : "crown"), 0.62f, "Icon");
            ic.rectTransform.TL(30, 30, 170, 170);
            UIKit.Label(c.transform, L.T("ch_" + mode), 52, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(230, 30, 760, 70);
            var d = UIKit.Label(c.transform, L.T("ch_" + mode + "_desc"), 28, Art.Soft, TextAnchor.UpperLeft, false);
            d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(230, 106, 760, 96);
            var best = UIKit.Label(c.transform, "", 32, Art.Brown, TextAnchor.MiddleLeft, false); best.rectTransform.TL(40, 226, 600, 50);
            var tk = UIKit.Label(c.transform, "", 30, Art.Ink, TextAnchor.MiddleLeft, false); tk.rectTransform.TL(40, 286, 600, 50);
            var go = UIKit.Btn(c.transform, "btn_green", L.T("ch_start"), 50, () => Game.StartMode(mode), "Go"); ((RectTransform)go.transform).TL(620, 250, 360, 150);
            cards.Add((mode, tk, best, go));
            Refresh();
        }

        public void Refresh()
        {
            foreach (var (mode, tk, best, go) in cards)
            {
                int left = Store.Tickets(mode);
                tk.text = L.F("ch_tickets", left, Store.TicketMax(mode)) + "   " + L.F("stage_n", Mathf.Max(1, S.best));
                best.text = mode == "endless" ? L.F("ch_best", Num.Clock(S.endlessBest)) : L.F("ch_best", S.survWins + "번 막아냄");
                UIKit.SetKind(go, left > 0 ? "btn_green" : "btn_gray");
            }
        }
    }

    /// <summary>
    /// 로비 상점 탭 (3차): 다이아 [결제 초안] · 상자(확률 보기) · 무료 · 광고 상자 [초안] · 스태미나 · 골드 · 터렛 스킨 · 기지 외형.
    /// </summary>
    public class ShopTab : MonoBehaviour
    {
        static SaveData S => SaveData.Cur;
        Lobby lobby;
        RectTransform list;
        public static float scrollY;

        public void Build(Lobby l)
        {
            lobby = l;
            var root = (RectTransform)transform;
            var panel = UIKit.Panel(root, "card_white", "Shop"); panel.rectTransform.Fill(20, 6, 20, 6);
            list = UIKit.Scroll(panel.transform, "Scroll"); list.parent.GetComponent<RectTransform>().Fill(8, 12, 8, 8);
            Refresh();
        }

        Text Head(ref float y, string s)
        {
            var t = UIKit.Label(list, s, 40, Art.Ink, TextAnchor.MiddleLeft, false); t.rectTransform.TL(24, y, 900, 60);
            var ln = UIKit.Plain(list, new Color(0.23f, 0.16f, 0.11f, 0.2f), "Line"); ln.rectTransform.TL(20, y + 64, 980, 3);
            y += 80; return t;
        }

        public void Refresh()
        {
            if (list == null) return;
            float keep = list.anchoredPosition.y;
            for (int c = list.childCount - 1; c >= 0; c--) Destroy(list.GetChild(c).gameObject);
            float y = 14;
            // ---- 다이아 (결제 [초안])
            Head(ref y, L.T("shop_dia"));
            var note = UIKit.Label(list, Store.IapReady ? L.T("iap_draft") : L.T("iap_off"), 22, Art.Soft, TextAnchor.MiddleRight, false); note.rectTransform.TL(380, y - 72, 620, 40);
            for (int i = 0; i < Tables.Products.Count; i++)
            {
                var p = Tables.Products[i];
                float x = 20 + i % 3 * 330, yy = y + i / 3 * 300;
                var card = UIKit.Panel(list, "card", "P_" + p.id); card.rectTransform.TL(x, yy, 316, 286);
                var ic = UIKit.Img(card.transform, Art.Icon("diamond"), "Icon"); ic.raycastTarget = false; ic.rectTransform.TL(108, 18, 100, 100);
                UIKit.Label(card.transform, Num.Comma(p.dia), 44, Art.Sky).rectTransform.TL(0, 116, 316, 56);
                UIKit.Label(card.transform, p.name, 24, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 166, 316, 32);
                var pp = p;
                var b = UIKit.Btn(card.transform, Store.IapReady ? "btn_green" : "btn_gray", Num.Comma(p.krw) + "원", 32, () =>
                {
                    if (!Store.IapReady) { Toast.Show(L.T("iap_off")); return; }
                    int n = Store.Purchase(pp); if (n > 0) Toast.Show($"{L.T("dia")} +{Num.Comma(n)}");
                    lobby.Refresh();
                }, "Buy");
                ((RectTransform)b.transform).TL(28, 200, 260, 74);
                if (Store.FirstDouble(p))
                {
                    var tag = UIKit.Panel(card.transform, "pill", "First"); tag.color = Art.Red; tag.rectTransform.TL(-8, -10, 170, 52);
                    UIKit.Label(tag.transform, L.T("first_double"), 24).rectTransform.Fill(0, 4, 0, 0);
                }
            }
            y += Mathf.CeilToInt(Tables.Products.Count / 3f) * 300 + 10;
            // ---- 상자 · 스태미나 · 골드
            var rt = Head(ref y, L.T("shop_box"));
            var rb = UIKit.Btn(list, "btn_orange", L.T("rates"), 28, RatePopup, "Rates"); ((RectTransform)rb.transform).TL(840, y - 78, 160, 64);
            foreach (var it in Tables.ShopItems) y = ItemRow(it, y);
            // ---- 터렛 스킨
            Head(ref y, L.T("skins"));
            foreach (var k in Tables.Skins) y = SkinRow(list, k, y, lobby);
            // ---- 기지 외형
            Head(ref y, L.T("frame"));
            foreach (var f in Tables.Frames) y = FrameRow(list, f, y, lobby);
            UIKit.SetScrollH(list, y + 20);
            list.anchoredPosition = new Vector2(0, keep);
        }

        float ItemRow(ShopItemRow it, float y)
        {
            var r = UIKit.Panel(list, "card", "I_" + it.id); r.rectTransform.TL(20, y, 980, 140);
            string icon = it.kind switch { "stamina" => "energy", "gold" => "coin", "ad_box" => "ad", "chip_box" => "chip", "gear_box" => "g_suit", _ => "chest" };
            var ic = UIKit.Img(r.transform, Art.Icon(icon), "Icon"); ic.raycastTarget = false; ic.rectTransform.TL(24, 20, 100, 100);
            UIKit.Label(r.transform, it.name, 34, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(144, 16, 500, 54);
            string sub = it.desc + (it.perDay > 0 ? "   " + L.F("per_day", it.perDay - Store.Left(it), it.perDay) : "");
            UIKit.Label(r.transform, sub, 24, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(144, 72, 560, 48);
            bool can = Store.Left(it) > 0 && S.dia >= it.costDia;
            var b = UIKit.Btn(r.transform, can ? (it.kind == "ad_box" ? "btn_purple" : "btn_green") : "btn_gray", null, 30, () =>
            {
                if (Store.Left(it) <= 0) return;
                if (S.dia < it.costDia) { Toast.Show(L.T("no_dia")); return; }
                void Give() { var got = Store.Buy(it); if (got != null) GotPopup(got); lobby.Refresh(); }
                if (it.kind == "ad_box") Ads.Show(ok => { if (ok) Give(); }); else Give();
            }, "Buy");
            ((RectTransform)b.transform).TL(720, 24, 240, 92);
            if (it.costDia > 0)
            {
                var di = UIKit.Img(b.transform, Art.Icon("diamond"), "Dia"); di.raycastTarget = false; di.rectTransform.Place(0, 0.5f, new Vector2(18, 6), new Vector2(52, 52), new Vector2(0, 0.5f));
                UIKit.Label(b.transform, Num.Comma(it.costDia), 32).rectTransform.Fill(70, 14, 8, 4);
            }
            else UIKit.Label(b.transform, it.kind == "ad_box" ? L.T("refresh_draft").Replace("[초안] ", "") : L.T("claim"), it.kind == "ad_box" ? 22 : 32).rectTransform.Fill(8, 14, 8, 4);
            return y + 152;
        }

        /// <summary>받은 것을 아이콘으로 하나씩 (2차 수정안 "아이템 구매 시 아이콘 형태로 하나하나") — 상자 · 일괄 합성 같이 씀</summary>
        public static void GotPopup(List<GotItem> got, string title = null)
        {
            int cols = got.Count <= 3 ? got.Count : 4, rows = Mathf.CeilToInt(got.Count / (float)cols);
            float cell = 210, h = Mathf.Min(1700, 330 + rows * (cell + 46));
            var p = Popup.Make(title ?? L.T("reward_get"), Mathf.Max(640, cols * cell + 80), h);
            var area = UIKit.Rect(p.win, "Area"); area.Fill(20, 160, 20, 116);
            var c = UIKit.Scroll(area, "Scroll"); c.parent.GetComponent<RectTransform>().Fill();
            float x0 = (p.win.sizeDelta.x - 40 - cols * cell) / 2;
            for (int i = 0; i < got.Count; i++)
            {
                var g = got[i];
                float x = x0 + i % cols * cell, y = 8 + i / cols * (cell + 46);
                var t = UIKit.Tile(c, "slot", Color.Lerp(Color.white, g.bg, 0.8f), Art.Icon(g.icon), 0.6f, "Got" + i); t.rectTransform.TL(x + 10, y, cell - 20, cell - 20);
                var pi = t.gameObject.AddComponent<PopIn>(); pi.delay = 0.12f * i; pi.sound = true;
                string gname = g.grade > 0 ? $"\n<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(Tables.Grade(g.grade).color, Art.Ink, 0.35f))}>{Tables.Grade(g.grade).name}</color>" : "";
                var nm = UIKit.Label(c, g.name + gname, 22, Art.Ink, TextAnchor.UpperCenter, false); nm.rectTransform.TL(x, y + cell - 16, cell, 50);
                nm.horizontalOverflow = HorizontalWrapMode.Wrap;
                nm.gameObject.AddComponent<PopIn>().delay = 0.12f * i;
            }
            UIKit.SetScrollH(c, rows * (cell + 46) + 20);
            p.Btn("btn_green", L.T("ok"), 0, 30, 320, p.Close, 96);
        }

        /// <summary>상자 등급 확률 (확률형 아이템 표시 — Shop.xlsx Box_Rate 그대로)</summary>
        static void RatePopup()
        {
            var p = Popup.Make(L.T("rate_title"), 760, 300 + Tables.BoxRates.Count * 70);
            float y = 130;
            foreach (var (g, r) in Tables.BoxRates)
            {
                var gr = Tables.Grade(g);
                var tag = UIKit.Panel(p.win, "pill", "G"); tag.color = gr.color; tag.rectTransform.TL(140, y, 220, 56);
                UIKit.Label(tag.transform, gr.name, 30, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
                UIKit.Label(p.win, r.ToString("0.##") + "%", 34, Art.Ink, TextAnchor.MiddleRight, false).rectTransform.TL(380, y, 220, 56);
                y += 70;
            }
            p.Btn("btn_green", L.T("ok"), 0, 40, 300, p.Close, 100);
        }

        // ---------- 스킨 · 외형 (터렛 탭 · 상점 같이 씀) ----------
        public static float SkinRow(Transform list, SkinRow k, float y, Lobby lobby)
        {
            var t = Tables.TurretById[k.turret];
            var r = UIKit.Panel(list, "card", "S_" + k.id); r.rectTransform.TL(20, y, 980, 190);
            var a = UIKit.Tile(r.transform, "slot", Color.white, Art.TurretUI(t.art), 0.86f, "Before"); a.rectTransform.TL(20, 20, 140, 140);
            var b2 = UIKit.Tile(r.transform, "slot", Color.white, Art.TurretSkinUI(t.art), 0.86f, "After"); b2.rectTransform.TL(170, 20, 140, 140);
            UIKit.Label(r.transform, k.name, 34, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(330, 14, 400, 50);
            UIKit.Label(r.transform, $"{L.T("ult")}: {k.ultName} - {k.ultDesc}", 22, Art.Purple, TextAnchor.UpperLeft, false).rectTransform.TL(330, 66, 400, 60);
            UIKit.Label(r.transform, L.F("skin_bonus", t.name, k.dmg), 20, Art.Soft, TextAnchor.UpperLeft, false).rectTransform.TL(330, 126, 400, 50);
            foreach (var x in r.GetComponentsInChildren<Text>()) x.horizontalOverflow = HorizontalWrapMode.Wrap;
            bool has = S.HasSkin(k.turret), on = S.SkinOn(k.turret);
            Button btn;
            if (!has)
            {
                btn = UIKit.Btn(r.transform, S.dia >= k.price ? "btn_green" : "btn_gray", null, 30, () =>
                {
                    if (!Store.BuySkin(k)) { Toast.Show(L.T("no_dia")); return; }
                    lobby?.Refresh(); if (lobby != null && lobby.tab == 1) { Popup.CloseAll(); lobby.SetTab(1); }
                }, "Buy");
                var di = UIKit.Img(btn.transform, Art.Icon("diamond"), "Dia"); di.raycastTarget = false; di.rectTransform.Place(0, 0.5f, new Vector2(18, 6), new Vector2(52, 52), new Vector2(0, 0.5f));
                UIKit.Label(btn.transform, Num.Comma(k.price), 32).rectTransform.Fill(70, 14, 8, 4);
            }
            else btn = UIKit.Btn(r.transform, on ? "btn_purple" : "btn_cream", on ? L.T("equipped") : L.T("equip"), 32, () => { Store.ToggleSkin(k.turret); lobby?.Refresh(); if (lobby != null && lobby.tab == 1) { Popup.CloseAll(); lobby.SetTab(1); } }, "Toggle");
            ((RectTransform)btn.transform).TL(750, 50, 210, 92);
            return y + 202;
        }

        public static float FrameRow(Transform list, FrameRow f, float y, Lobby lobby)
        {
            int star = S.FrameStar(f.id); bool on = S.frame == f.id;
            var r = UIKit.Panel(list, "card", "F_" + f.id); r.rectTransform.TL(20, y, 980, 170);
            var ic = UIKit.Tile(r.transform, "slot", f.tint, Art.Icon("base"), 0.74f, "Icon"); ic.rectTransform.TL(20, 20, 130, 130);
            UIKit.Label(r.transform, f.name + (star > 0 && f.id != "basic" ? "  " + L.F("frame_star", star) : ""), 34, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(170, 14, 520, 50);
            var d = UIKit.Label(r.transform, f.Desc(Mathf.Max(1, star)), 24, Art.Soft, TextAnchor.UpperLeft, false);
            d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(170, 68, 540, 90);
            void Re() { lobby?.Refresh(); Popup.CloseAll(); if (lobby != null && lobby.tab == 1) FramePopup(lobby); }
            if (star <= 0)
            {
                var b = UIKit.Btn(r.transform, S.dia >= f.price ? "btn_green" : "btn_gray", null, 30, () => { if (!Store.BuyFrame(f)) { Toast.Show(L.T("no_dia")); return; } Re(); }, "Buy");
                ((RectTransform)b.transform).TL(740, 40, 220, 92);
                var di = UIKit.Img(b.transform, Art.Icon("diamond"), "Dia"); di.raycastTarget = false; di.rectTransform.Place(0, 0.5f, new Vector2(18, 6), new Vector2(52, 52), new Vector2(0, 0.5f));
                UIKit.Label(b.transform, Num.Comma(f.price), 32).rectTransform.Fill(70, 14, 8, 4);
            }
            else
            {
                var eb = UIKit.Btn(r.transform, on ? "btn_purple" : "btn_cream", on ? L.T("frame_using") : L.T("frame_change"), 30, () => { Store.EquipFrame(f.id); Re(); }, "Equip");
                ((RectTransform)eb.transform).TL(740, 14, 220, 72);
                if (f.id != "basic" && star < 5)
                {
                    var ub = UIKit.Btn(r.transform, Store.CanUpFrame(f.id) ? "btn_green" : "btn_gray", null, 24, () => { if (Store.UpFrame(f.id)) Re(); }, "Up");
                    ((RectTransform)ub.transform).TL(740, 90, 220, 66);
                    var co = UIKit.Img(ub.transform, Art.Icon("coin"), "Coin"); co.raycastTarget = false; co.rectTransform.Place(0, 0.5f, new Vector2(14, 5), new Vector2(42, 42), new Vector2(0, 0.5f));
                    UIKit.Label(ub.transform, Num.F(Store.FrameUpGold(f.id)), 26).rectTransform.Fill(58, 12, 6, 2);
                }
            }
            return y + 182;
        }

        /// <summary>터렛 탭 기지 줄 [외형] — 외형 고르기 창</summary>
        public static void FramePopup(Lobby lobby)
        {
            var p = Popup.Make(L.T("frame"), 1040, 1500);
            var area = UIKit.Rect(p.win, "Area"); area.Fill(10, 20, 10, 110);
            var c = UIKit.Scroll(area, "Scroll"); c.parent.GetComponent<RectTransform>().Fill();
            float y = 6; foreach (var f in Tables.Frames) y = FrameRow(c, f, y, lobby);
            UIKit.SetScrollH(c, y + 10);
        }

        /// <summary>터렛 탭 [스킨] — 그 터렛 스킨 창</summary>
        public static void SkinPopup(SkinRow k, Lobby lobby)
        {
            var p = Popup.Make(L.T("skin"), 1040, 420);
            var area = UIKit.Rect(p.win, "Area"); area.Fill(10, 10, 10, 110);
            SkinRow(area, k, 0, lobby);   // 사거나 끼우면 창을 닫고 터렛 탭을 다시 그림
        }
    }
}
