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
            // 6차 수정안 "패널 추가 — 다이아 · 장비 · 재화 3분류": 위에 패널 단추 3개 (외형 탭과 같은 모양)
            subBtns.Clear();
            for (int i = 0; i < SubKeys.Length; i++)
            {
                int k = i;
                var b = UIKit.Btn(panel.transform, i == sub ? "btn_orange" : "btn_cream", L.T(SubKeys[i]), 34, () => { if (sub == k) return; sub = k; scrollY = 0; SetSub(); }, "ShopSub" + i);
                ((RectTransform)b.transform).TL(22 + i * 336, 20, 320, 96);
                subBtns.Add(b);
            }
            var area = UIKit.Rect(panel.transform, "Area"); area.Fill(8, 12, 8, 128);
            list = UIKit.Scroll(area, "Scroll"); list.parent.GetComponent<RectTransform>().Fill();
            Refresh();
        }

        /// <summary>6차: 0 = 다이아 · 1 = 장비(상자) · 2 = 재화(스태미나 · 골드 · 강화 재료 · 터렛 조각)</summary>
        public static int sub;
        public static readonly string[] SubKeys = { "shop_tab_dia", "shop_tab_gear", "shop_tab_res" };
        readonly List<Button> subBtns = new();
        public static int SubOf(ShopItemRow it) => it.kind is "free_box" or "ad_box" or "gear_box" or "chip_box" ? 1 : 2;
        void SetSub()
        {
            for (int i = 0; i < subBtns.Count; i++) UIKit.SetKind(subBtns[i], i == sub ? "btn_orange" : "btn_cream");
            list.anchoredPosition = Vector2.zero;
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
            if (sub == 0) y = DiaPanel(y);
            else if (sub == 1)
            {   // ---- 상자 (장비 · 회로)
                Head(ref y, L.T("shop_box"));
                var rb = UIKit.Btn(list, "btn_orange", L.T("rates"), 28, RatePopup, "Rates"); ((RectTransform)rb.transform).TL(840, y - 78, 160, 64);
                var bn = UIKit.Label(list, L.T("box_max_note"), 24, Art.Purple, TextAnchor.MiddleLeft, false); bn.rectTransform.TL(28, y - 6, 960, 40);
                y += 40;
                foreach (var it in Tables.ShopItems) if (SubOf(it) == 1) y = ItemRow(it, y);
            }
            else
            {   // ---- 재화
                Head(ref y, L.T("shop_tab_res"));
                foreach (var it in Tables.ShopItems) if (SubOf(it) == 2) y = ItemRow(it, y);
            }
            UIKit.SetScrollH(list, y + 20);
            list.anchoredPosition = new Vector2(0, keep);
        }

        float DiaPanel(float y)
        {
            // ---- 다이아 (결제 [초안])
            Head(ref y, L.T("shop_dia"));
            var note = UIKit.Label(list, Store.IapReady ? L.T("iap_draft") : L.T("iap_off"), 22, Art.Soft, TextAnchor.MiddleRight, false); note.rectTransform.TL(380, y - 72, 620, 40);
            // 월 1회 패키지는 맨 위에 넓은 띠로 (참고영상.txt 메모 "월 1회 구매 9,900원 다이아 폭탄 패키지 10,000")
            foreach (var mp in Tables.Products) if (mp.kind == "monthly") y = MonthlyCard(mp, y);
            int idx = 0;
            for (int i = 0; i < Tables.Products.Count; i++)
            {
                var p = Tables.Products[i];
                if (p.kind == "monthly") continue;
                int slot = idx++;
                float x = 20 + slot % 3 * 330, yy = y + slot / 3 * 300;
                var card = UIKit.Panel(list, "card", "P_" + p.id); card.rectTransform.TL(x, yy, 316, 286);
                var pp = p; bool noads = p.kind == "noads", done = noads && Store.NoAds;
                var ic = UIKit.Img(card.transform, Art.Icon(noads ? "ad" : "diamond"), "Icon"); ic.raycastTarget = false; ic.rectTransform.TL(108, 18, 100, 100);
                if (noads)
                {   // 요청 사항_05 "상품 판매에 광고 제거 추가" [초안]: 광고 그림 위에 빨간 금지 표시
                    var no = UIKit.Img(card.transform, Art.Fx("ring"), "No"); no.color = Art.Red; no.raycastTarget = false; no.rectTransform.Place(0, 1, new Vector2(158, -68), new Vector2(124, 124), new Vector2(0.5f, 0.5f));
                    var sl = UIKit.Plain(card.transform, Art.Red, "Slash"); sl.rectTransform.Place(0, 1, new Vector2(158, -68), new Vector2(104, 10), new Vector2(0.5f, 0.5f)); sl.rectTransform.localEulerAngles = new Vector3(0, 0, -45);
                    UIKit.Label(card.transform, L.T("noads_name"), 38, Art.Red).rectTransform.TL(0, 116, 316, 56);
                    UIKit.Label(card.transform, L.T("noads_desc"), 22, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 166, 316, 32);
                }
                else
                {
                    UIKit.Label(card.transform, Num.Comma(p.dia), 44, Art.Sky).rectTransform.TL(0, 116, 316, 56);
                    UIKit.Label(card.transform, p.name, 24, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 166, 316, 32);
                }
                var b = UIKit.Btn(card.transform, done ? "btn_gray" : Store.IapReady ? "btn_green" : "btn_gray", done ? L.T("noads_done") : Num.Comma(p.krw) + "원", 32, () =>
                {
                    if (done) return;
                    if (!Store.IapReady) { Toast.Show(L.T("iap_off")); return; }
                    int n = Store.Purchase(pp); if (n > 0) Toast.Show(noads ? $"{L.T("noads_name")} {L.T("noads_done")}" : $"{L.T("dia")} +{Num.Comma(n)}");
                    lobby.Refresh();
                }, "Buy");
                ((RectTransform)b.transform).TL(28, 200, 260, 74);
                if (Store.FirstDouble(p))
                {
                    var tag = UIKit.Panel(card.transform, "pill", "First"); tag.color = Art.Red; tag.rectTransform.TL(-8, -10, 170, 52);
                    UIKit.Label(tag.transform, L.T("first_double"), 24).rectTransform.Fill(0, 4, 0, 0);
                }
            }
            return y + Mathf.CeilToInt(idx / 3f) * 300 + 10;
        }

        float MonthlyCard(ProductRow p, float y)
        {
            bool done = Store.MonthDone(p);
            var card = UIKit.Panel(list, "card", "P_" + p.id); card.rectTransform.TL(20, y, 980, 230);
            card.color = Art.Hex("#fff0c4");
            var ic = UIKit.Img(card.transform, Art.Icon("diamond"), "Icon"); ic.raycastTarget = false; ic.rectTransform.TL(40, 40, 150, 150);
            var ic2 = UIKit.Img(card.transform, Art.Icon("diamond"), "Icon2"); ic2.raycastTarget = false; ic2.rectTransform.TL(120, 86, 100, 100);
            UIKit.Label(card.transform, p.name, 40, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(250, 26, 440, 56);
            UIKit.Label(card.transform, Num.Comma(p.dia), 56, Art.Sky, TextAnchor.MiddleLeft).rectTransform.TL(250, 86, 440, 76);
            var tag = UIKit.Panel(card.transform, "pill", "Month"); tag.color = Art.Red; tag.rectTransform.TL(250, 166, 170, 46);
            UIKit.Label(tag.transform, L.T("month_limit"), 26).rectTransform.Fill(0, 4, 0, 0);
            var pp = p;
            var b = UIKit.Btn(card.transform, done ? "btn_gray" : Store.IapReady ? "btn_green" : "btn_gray", done ? L.T("month_done") : Num.Comma(p.krw) + "원", done ? 26 : 38, () =>
            {
                if (Store.MonthDone(pp)) return;
                if (!Store.IapReady) { Toast.Show(L.T("iap_off")); return; }
                int n = Store.Purchase(pp); if (n > 0) Toast.Show($"{L.T("dia")} +{Num.Comma(n)}");
                lobby.Refresh();
            }, "Buy");
            ((RectTransform)b.transform).TL(700, 66, 250, 100);
            return y + 246;
        }

        float ItemRow(ShopItemRow it, float y)
        {
            var r = UIKit.Panel(list, "card", "I_" + it.id); r.rectTransform.TL(20, y, 980, 140);
            string icon = it.kind switch { "stamina" => "energy", "gold" => "coin", "ad_box" => "ad", "chip_box" => "chip_4", "gear_box" => "g_suit_4", "parts_box" => "parts", "shard_box" => "shard", _ => "chest" };
            var ic = UIKit.Img(r.transform, Art.Icon(icon), "Icon"); ic.raycastTarget = false; ic.rectTransform.TL(24, 20, 100, 100);
            UIKit.Label(r.transform, it.name, 34, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(144, 16, 500, 54);
            string desc = it.desc + (it.perDay > 0 ? "   " + L.F("per_day", it.perDay - Store.Left(it), it.perDay) : "");
            UIKit.Label(r.transform, desc, 24, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(144, 72, 560, 48);
            if (Store.PityN > 0 && it.kind is "gear_box" or "chip_box")
            {   // 6차: 천장 — 전설 확정까지 남은 횟수 (장비 · 회로 상자 따로 셈, 10+1 은 같은 셈)
                var pt = UIKit.Label(r.transform, L.F("pity_left", Store.PityLeft(it.kind)), 22, Art.Purple, TextAnchor.MiddleLeft, false);
                pt.rectTransform.TL(390, 16, 320, 50); pt.name = "Pity";
            }
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
            float cell = 210, h = Mathf.Min(1700, 360 + rows * (cell + 46));
            var p = Popup.Make(title ?? L.T("reward_get"), Mathf.Max(640, cols * cell + 80), h);
            var area = UIKit.Rect(p.win, "Area"); area.Fill(20, 160, 20, 116);
            var c = UIKit.Scroll(area, "Scroll"); c.parent.GetComponent<RectTransform>().Fill();
            float x0 = (p.win.sizeDelta.x - 40 - cols * cell) / 2;
            for (int i = 0; i < got.Count; i++)
            {
                var g = got[i];
                float x = x0 + i % cols * cell, y = 30 + i / cols * (cell + 46);
                var t = UIKit.Tile(c, "slot", Color.Lerp(Color.white, g.bg, 0.8f), g.turret != null ? Art.TurretIcon(Tables.TurretById[g.turret]) : Art.Icon(g.icon), g.turret != null ? 0.8f : 0.6f, "Got" + i); t.rectTransform.TL(x + 10, y, cell - 20, cell - 20);
                if (g.turret != null) { var sd = UIKit.Img(t.transform, Art.Icon("shard"), "Shard"); sd.raycastTarget = false; sd.rectTransform.Place(1, 0, new Vector2(6, -6), new Vector2(70, 70), new Vector2(1, 0)); }
                var pi = t.gameObject.AddComponent<PopIn>(); pi.delay = 0.12f * i; pi.sound = true;
                if (g.grade >= 4)
                {   // 3차 수정안 "영웅 등급 이상은 획득창 테두리에 이펙트": 등급 색 빛 테두리가 숨쉬듯 · 전설 이상은 반짝이가 테두리를 돎
                    var gl = UIKit.Img(c, Art.UI("glow_frame"), "Glow"); gl.raycastTarget = false; gl.color = Tables.Grade(g.grade).color;
                    gl.rectTransform.TL(x + 10 - 26, y - 26, cell - 20 + 52, cell - 20 + 52);   // 빛 선이 칸 테두리에 겹치게 (칸 위에 그림)
                    var gp = gl.gameObject.AddComponent<GlowPulse>(); gp.delay = 0.12f * i; gp.sparkle = g.grade >= 5;
                }
                string gname = g.grade > 0 ? $"\n<color=#{ColorUtility.ToHtmlStringRGB(Color.Lerp(Tables.Grade(g.grade).color, Art.Ink, 0.35f))}>{Tables.Grade(g.grade).name}</color>" : "";
                var nm = UIKit.Label(c, g.name + gname, 22, Art.Ink, TextAnchor.UpperCenter, false); nm.rectTransform.TL(x, y + cell - 16, cell, 50);
                nm.horizontalOverflow = HorizontalWrapMode.Wrap;
                nm.gameObject.AddComponent<PopIn>().delay = 0.12f * i;
            }
            UIKit.SetScrollH(c, rows * (cell + 46) + 50);
            p.Btn("btn_green", L.T("ok"), 0, 30, 320, p.Close, 96);
        }

        /// <summary>상자 등급 확률 (확률형 아이템 표시 — Shop.xlsx Box_Rate 그대로)</summary>
        /// <summary>시험용</summary>
        public static void RateOpen() => RatePopup();
        static void RatePopup()
        {
            int shown = 0; foreach (var (g, r) in Tables.BoxRates) if (g <= Store.BoxMax && r > 0) shown++;
            var p = Popup.Make(L.T("rate_title"), 760, 420 + shown * 70);
            float y = 130;
            foreach (var (g, r) in Tables.BoxRates)
            {
                if (g > Store.BoxMax || r <= 0) continue;     // 6차: 신화 · 궁극은 상자에서 안 나옴 (합성으로만)
                var gr = Tables.Grade(g);
                var tag = UIKit.Panel(p.win, "pill", "G"); tag.color = gr.color; tag.rectTransform.TL(140, y, 220, 56);
                UIKit.Label(tag.transform, gr.name, 30, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
                UIKit.Label(p.win, r.ToString("0.##") + "%", 34, Art.Ink, TextAnchor.MiddleRight, false).rectTransform.TL(380, y, 220, 56);
                y += 70;
            }
            p.Text(L.T("box_max_note"), y + 6, 26, Art.Purple, 40);
            if (Store.PityN > 0) p.Text(L.F("pity_note", Store.PityN), y + 50, 26, Art.Purple, 40);
            p.Btn("btn_green", L.T("ok"), 0, 40, 300, p.Close, 100);
        }

        // ---------- 외형 (3차 수정안: 상점 · 터렛 탭에서 빼고 따로 "외형" 메뉴 — 터렛 외형 · 기지 외형) ----------
        public static float SkinRow(Transform list, SkinRow k, float y, System.Action changed)
        {
            var t = Tables.TurretById[k.turret];
            var r = UIKit.Panel(list, "card", "S_" + k.id); r.rectTransform.TL(20, y, 960, 190);
            // 6차 수정안 "터렛 외형은 변경되는 이미지만 사용": 바뀐 그림 한 장만 (예전 = 원래 그림 + 바뀐 그림)
            var b2 = UIKit.Tile(r.transform, "slot", Color.white, Art.TurretSkinUI(t.art), 0.86f, "After"); b2.rectTransform.TL(20, 15, 160, 160);
            UIKit.Label(r.transform, $"{k.name}  ({t.name})", 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(200, 14, 520, 50);
            UIKit.Label(r.transform, $"{L.T("ult")}: {k.ultName} - {k.ultDesc}", 22, Art.Purple, TextAnchor.UpperLeft, false).rectTransform.TL(200, 66, 520, 60);
            UIKit.Label(r.transform, L.F("skin_bonus", t.name, k.dmg), 20, Art.Soft, TextAnchor.UpperLeft, false).rectTransform.TL(200, 126, 520, 50);
            foreach (var x in r.GetComponentsInChildren<Text>()) x.horizontalOverflow = HorizontalWrapMode.Wrap;
            bool has = S.HasSkin(k.turret), on = S.SkinOn(k.turret);
            Button btn;
            if (!has)
            {
                btn = UIKit.Btn(r.transform, S.dia >= k.price ? "btn_green" : "btn_gray", null, 30, () =>
                {
                    if (!Store.BuySkin(k)) { Toast.Show(L.T("no_dia")); return; }
                    changed?.Invoke();
                }, "Buy");
                var di = UIKit.Img(btn.transform, Art.Icon("diamond"), "Dia"); di.raycastTarget = false; di.rectTransform.Place(0, 0.5f, new Vector2(18, 6), new Vector2(52, 52), new Vector2(0, 0.5f));
                UIKit.Label(btn.transform, Num.Comma(k.price), 32).rectTransform.Fill(70, 14, 8, 4);
            }
            else btn = UIKit.Btn(r.transform, on ? "btn_purple" : "btn_cream", on ? L.T("equipped") : L.T("equip"), 32, () => { Store.ToggleSkin(k.turret); changed?.Invoke(); }, "Toggle");
            ((RectTransform)btn.transform).TL(740, 50, 200, 92);
            return y + 202;
        }

        public static float FrameRow(Transform list, FrameRow f, float y, System.Action changed)
        {
            int star = S.FrameStar(f.id); bool on = S.frame == f.id;
            var r = UIKit.Panel(list, "card", "F_" + f.id); r.rectTransform.TL(20, y, 960, 170);
            // 6차 수정안 "기지 외형 이미지 수정": 색만 입힌 같은 집 아이콘 → 외형마다 다른 돔 그림 (tools/earth_export.py frames)
            var ic = UIKit.Tile(r.transform, "slot", Color.white, Art.FrameIcon(f), 0.9f, "Icon"); ic.rectTransform.TL(16, 16, 236, 138);   // 돔 그림은 가로로 길어서 칸도 가로로
            ic.transform.Find("Icon").GetComponent<Image>().preserveAspect = true;
            UIKit.Label(r.transform, f.name + (star > 0 && f.id != "basic" ? "  " + L.F("frame_star", star) : ""), 34, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(270, 14, 440, 50);
            var d = UIKit.Label(r.transform, f.Desc(Mathf.Max(1, star)), 24, Art.Soft, TextAnchor.UpperLeft, false);
            d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(270, 68, 440, 90);
            if (star <= 0)
            {
                var b = UIKit.Btn(r.transform, S.dia >= f.price ? "btn_green" : "btn_gray", null, 30, () => { if (!Store.BuyFrame(f)) { Toast.Show(L.T("no_dia")); return; } changed?.Invoke(); }, "Buy");
                ((RectTransform)b.transform).TL(720, 40, 220, 92);
                var di = UIKit.Img(b.transform, Art.Icon("diamond"), "Dia"); di.raycastTarget = false; di.rectTransform.Place(0, 0.5f, new Vector2(18, 6), new Vector2(52, 52), new Vector2(0, 0.5f));
                UIKit.Label(b.transform, Num.Comma(f.price), 32).rectTransform.Fill(70, 14, 8, 4);
            }
            else
            {
                var eb = UIKit.Btn(r.transform, on ? "btn_purple" : "btn_cream", on ? L.T("frame_using") : L.T("frame_change"), 30, () => { Store.EquipFrame(f.id); changed?.Invoke(); }, "Equip");
                ((RectTransform)eb.transform).TL(720, 14, 220, 72);
                if (f.id != "basic" && star < 5)
                {
                    var ub = UIKit.Btn(r.transform, Store.CanUpFrame(f.id) ? "btn_green" : "btn_gray", null, 24, () => { if (Store.UpFrame(f.id)) changed?.Invoke(); }, "Up");
                    ((RectTransform)ub.transform).TL(720, 90, 220, 66);
                    var co = UIKit.Img(ub.transform, Art.Icon("coin"), "Coin"); co.raycastTarget = false; co.rectTransform.Place(0, 0.5f, new Vector2(14, 5), new Vector2(42, 42), new Vector2(0, 0.5f));
                    UIKit.Label(ub.transform, Num.F(Store.FrameUpGold(f.id)), 26).rectTransform.Fill(58, 12, 6, 2);
                }
            }
            return y + 182;
        }
    }

    /// <summary>획득 테두리 빛: 나타난 뒤 숨쉬듯 밝아졌다 어두워지고 살짝 커졌다 작아짐 · sparkle = 반짝이 2개가 테두리를 돎</summary>
    public class GlowPulse : MonoBehaviour
    {
        public float delay; public bool sparkle;
        float t; Image im; RectTransform[] sp;
        void Start()
        {
            im = GetComponent<Image>();
            if (sparkle)
            {
                sp = new RectTransform[2];
                for (int i = 0; i < 2; i++) { var s = UIKit.Img(transform, Art.Icon("star"), "Sparkle"); s.raycastTarget = false; sp[i] = s.rectTransform; sp[i].sizeDelta = new Vector2(40, 40); sp[i].anchorMin = sp[i].anchorMax = new Vector2(0.5f, 0.5f); }
            }
            Update();
        }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            float a = t < delay ? 0 : Mathf.Clamp01((t - delay) / 0.3f) * (0.8f + 0.2f * Mathf.Sin((t - delay) * 4f));
            var c = im.color; c.a = a; im.color = c;
            transform.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 4f));
            if (sp == null) return;
            var r = ((RectTransform)transform).rect;
            for (int i = 0; i < sp.Length; i++)
            {
                float k = Mathf.Repeat(t * 0.35f + i * 0.5f, 1f) * 4f;   // 네모 둘레를 따라 돎
                float hx = r.width * 0.4f, hy = r.height * 0.4f;
                Vector2 pos = k < 1 ? new Vector2(Mathf.Lerp(-hx, hx, k), hy) : k < 2 ? new Vector2(hx, Mathf.Lerp(hy, -hy, k - 1)) : k < 3 ? new Vector2(Mathf.Lerp(hx, -hx, k - 2), -hy) : new Vector2(-hx, Mathf.Lerp(-hy, hy, k - 3));
                sp[i].anchoredPosition = pos; sp[i].localScale = Vector3.one * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 6f + i))) * (t < delay ? 0 : 1);
                sp[i].localEulerAngles = new Vector3(0, 0, t * 90f);
            }
        }
    }

    /// <summary>외형 (3차 수정안 "스킨 창은 따로 — 기지와 터렛 묶음 · 외형") → 4차 수정안 "외형은 아래 탭(전투 · 터렛 · 장비 …)으로": 탭 화면</summary>
    public static class Looks
    {
        public static int sub;    // 0 = 터렛 외형 · 1 = 기지 외형
        static float scroll = 1f;
        /// <summary>외형 탭 열기 (tab: 0 터렛 · 1 기지 · -1 그대로)</summary>
        public static void Open(int tab = -1)
        {
            if (tab >= 0) { sub = tab; scroll = 1f; }
            Popup.CloseAll();
            Game.Lobby?.SetTab(6);
        }
        public static void Build(RectTransform page)
        {
            var bg = UIKit.Panel(page, "card_white", "Looks"); bg.rectTransform.Fill(20, 6, 20, 6);
            ScrollRect sr = null;
            void Re() { scroll = sr ? sr.verticalNormalizedPosition : 1f; Game.Lobby?.Refresh(); Game.Lobby?.SetTab(6); }
            for (int i = 0; i < 2; i++)
            {
                int k = i;
                var b = UIKit.Btn(bg.transform, i == sub ? "btn_orange" : "btn_cream", L.T(i == 0 ? "looks_turret" : "looks_base"), 34, () => { if (sub != k) { sub = k; scroll = 1f; Game.Lobby?.SetTab(6); } }, "Sub" + i);
                ((RectTransform)b.transform).TL(24 + i * 500, 20, 480, 96);
            }
            var area = UIKit.Rect(bg.transform, "Area"); area.Fill(10, 14, 10, 130);
            var c = UIKit.Scroll(area, "Scroll"); c.parent.GetComponent<RectTransform>().Fill();
            float y = 6;
            if (sub == 0) foreach (var k in Tables.Skins) y = ShopTab.SkinRow(c, k, y, Re);
            else foreach (var f in Tables.Frames) y = ShopTab.FrameRow(c, f, y, Re);
            UIKit.SetScrollH(c, y + 10);
            sr = c.parent.GetComponent<ScrollRect>(); Canvas.ForceUpdateCanvases(); sr.verticalNormalizedPosition = scroll;
        }
    }
}
