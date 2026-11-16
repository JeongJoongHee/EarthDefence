using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 전투 화면 (샘플 2판 배치): 위 HUD(일시정지 · 배속 · 스테이지/남은 시간 · 고른 카드 · 경험치 바) · 돔 위 기지 체력
    /// · 가디언 아래 충전 게이지 · 가운데 알림 · 레벨업 창(내 터렛 5칸 + 카드 3장 + 광고 새로고침) · 결과 창.
    /// 화면을 끌면 가디언이 손가락 x 로 따라감 (안 만지면 제자리 — 11-15 결정).
    /// </summary>
    public class BattleUI : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Battle b;
        Text stageT, timeT, lvT, hpT, speedT, banner;
        Image expFill, chargeFill;
        RectTransform hpBox, chargeBox, bannerBox;
        float bannerT;
        public Popup pick;
        RectTransform root;

        public void Build(Battle battle)
        {
            b = battle; root = (RectTransform)transform;
            // 끌기 영역 (맨 뒤, 투명)
            var drag = gameObject.AddComponent<Image>(); drag.color = new Color(0, 0, 0, 0); drag.raycastTarget = true;

            var pause = UIKit.Tile(root, "btn_orange", Color.white, Art.Icon("pause"), 0.56f, "Pause"); pause.rectTransform.TL(26, 34, 100, 100);
            UIKit.Clickable(pause, ShowPause);
            var sp = UIKit.Btn(root, "pill", null, 38, ToggleSpeed, "Speed"); ((RectTransform)sp.transform).TL(140, 46, 134, 78);
            speedT = UIKit.Label(sp.transform, "x1", 38, Art.Ink, TextAnchor.MiddleCenter, false); speedT.rectTransform.Fill(0, 6, 0, 0);
            var mid = UIKit.Panel(root, "pill", "Stage"); mid.rectTransform.TL(296, 30, 488, 108);
            stageT = UIKit.Label(mid.transform, L.F("stage_n", b.stageNo), 42, Art.Ink, TextAnchor.UpperCenter, false); stageT.rectTransform.Fill(0, 40, 0, 10);
            timeT = UIKit.Label(mid.transform, "", 30, Art.Soft, TextAnchor.LowerCenter, false); timeT.rectTransform.Fill(0, 10, 0, 50);
            var cards = UIKit.Tile(root, "btn_orange", Color.white, Art.Icon("cards"), 0.6f, "Cards"); cards.rectTransform.TL(954, 34, 100, 100);
            UIKit.Clickable(cards, ShowCards);
            expFill = UIKit.Bar(root, Art.Sky, "Exp"); expFill.transform.parent.GetComponent<RectTransform>().TL(30, 160, 1020, 44);
            lvT = UIKit.Label(expFill.transform.parent, "", 32); lvT.rectTransform.Fill(0, 4, 0, 0);

            // 기지 체력 (돔 위)
            hpBox = UIKit.Rect(root, "BaseHp"); hpBox.sizeDelta = new Vector2(400, 70); hpBox.anchorMin = hpBox.anchorMax = new Vector2(0.5f, 0.5f);
            var heart = UIKit.Img(hpBox, Art.Icon("heart"), "Heart"); heart.rectTransform.Place(0.5f, 0.5f, new Vector2(-120, 0), new Vector2(62, 62));
            hpT = UIKit.Label(hpBox, "", 48, Color.white, TextAnchor.MiddleLeft); hpT.rectTransform.Place(0.5f, 0.5f, new Vector2(80, 0), new Vector2(300, 70));
            // 충전 게이지 (가디언 아래)
            chargeFill = UIKit.Bar(root, Art.Gold, "Charge");
            chargeBox = chargeFill.transform.parent.GetComponent<RectTransform>();
            chargeBox.anchorMin = chargeBox.anchorMax = new Vector2(0.5f, 0.5f); chargeBox.sizeDelta = new Vector2(150, 26);
            // 가운데 알림
            bannerBox = UIKit.Panel(root, "night", "Banner").rectTransform; bannerBox.Place(0.5f, 0.62f, Vector2.zero, new Vector2(620, 120));
            banner = UIKit.Label(bannerBox, "", 52, Art.Gold); banner.rectTransform.Fill(0, 10, 0, 0);
            bannerBox.gameObject.SetActive(false);

            b.Changed += OnChanged;
            b.Banner += s => { banner.text = s; bannerBox.gameObject.SetActive(true); bannerT = 2f; };
            Refresh();
        }

        void OnDestroy() { if (b) b.Changed -= OnChanged; }

        // ---------- 입력: 끌어서 가디언 이동 ----------
        public void OnPointerDown(PointerEventData e) => MoveTo(e.position);
        public void OnDrag(PointerEventData e) => MoveTo(e.position);
        void MoveTo(Vector2 screen)
        {
            if (b == null || b.st != Battle.St.Play || b.Guardian == null) return;
            b.Guardian.targetX = World.I.ScreenToWorld(screen).x;
        }

        void ToggleSpeed()
        {
            b.speed = b.speed > 1.01f ? 1f : 1.5f;
            speedT.text = b.speed > 1.01f ? "x1.5" : "x1";
        }

        void Update()
        {
            if (b == null) return;
            Refresh();
            if (bannerT > 0) { bannerT -= Time.unscaledDeltaTime; if (bannerT <= 0) bannerBox.gameObject.SetActive(false); }
        }

        void Refresh()
        {
            timeT.text = Num.Clock(b.TimeLeft);
            lvT.text = b.level >= b.LevelMax ? L.F("lv", b.level) + " MAX" : L.F("lv", b.level);
            UIKit.SetBar(expFill, b.level >= b.LevelMax ? 1 : (float)(b.exp / b.need));
            hpT.text = Num.Comma(b.baseHp);
            hpBox.anchoredPosition = UIRoot.I.WorldToUI(new Vector2(0, World.ShieldY + 0.7f));
            var g = b.Guardian;
            bool showCharge = g != null && g.t.Has("charge");
            chargeBox.gameObject.SetActive(showCharge);
            if (showCharge)
            {
                chargeBox.anchoredPosition = UIRoot.I.WorldToUI(new Vector2(g.transform.position.x, World.GroundY - 0.45f));
                chargeFill.color = g.Charging ? Art.Sky : Art.Gold;
                UIKit.SetBar(chargeFill, g.ChargeK);
            }
        }

        void OnChanged()
        {
            if (b.st == Battle.St.Pick) ShowPick();
            else if (pick != null) { pick.Close(false); pick = null; }
            if (b.st == Battle.St.End) ShowResult();
        }

        // ---------- 레벨업 창 ----------
        public void ShowPick()
        {
            if (pick != null) pick.Close(false);
            var p = pick = Popup.Make(b.offerIsBoss ? L.T("boss_card") : L.T("level_up"), 1000, 1380, null, false, 0.5f);
            var w = p.win;
            p.Text(L.T("pick_card"), 118, 36, Art.Soft);
            // 내 터렛 5칸
            var lab = UIKit.Label(w, L.T("my_turrets"), 30, Art.Ink, TextAnchor.MiddleLeft, false); lab.rectTransform.TL(66, 170, 300, 40);
            for (int i = 0; i <= b.run.MaxSlots; i++)
            {
                float x = 65 + i * 180;
                var t = i < b.run.turrets.Count ? b.run.turrets[i] : null;
                if (t == null)
                {
                    var e = UIKit.Panel(w, "slot_dash", "Empty" + i); e.rectTransform.TL(x, 214, 150, 150);
                    var plus = UIKit.Img(e.transform, Art.Icon("plus"), "Plus"); plus.color = new Color(1, 1, 1, 0.5f); plus.rectTransform.Fill(40, 40, 40, 40);
                    var et = UIKit.Label(w, L.T("empty"), 28, Art.Soft, TextAnchor.MiddleCenter, false); et.rectTransform.TL(x, 372, 150, 40);
                    continue;
                }
                var tile = UIKit.Tile(w, "slot", Color.white, Art.TurretUI(t.row.art), 0.86f, "Slot" + i); tile.rectTransform.TL(x, 214, 150, 150);
                Stars(w, x + 2, 372, t.tier, t.stars, 20);
            }
            // 카드 3장
            for (int i = 0; i < b.offer.Count; i++) Card(w, i, b.offer[i]);
            // 새로고침 (원작대로 광고만 — Refresh_Ad 번)
            var rb = p.Btn(b.CanRefresh ? "btn_orange" : "btn_gray", null, 0, 100, 420, () =>
            {
                if (!b.CanRefresh) return;
                Ads.Show(ok => { if (ok) b.Refresh(); });
            });
            var ad = UIKit.Img(rb.transform, Art.Icon("ad"), "Ad"); ad.rectTransform.Place(0, 0.5f, new Vector2(30, 6), new Vector2(76, 76), new Vector2(0, 0.5f));
            var rl = UIKit.Label(rb.transform, L.T("refresh"), 44); rl.rectTransform.Fill(90, 14, 10, 4);
            int max = Tables.Ci("Refresh_Ad", 1) + Tables.Ci("Refresh_Free", 0);
            var note = UIKit.Label(w, L.F("refresh_ad", b.run.refreshAd + b.run.refreshFree, max) + (Ads.Draft ? "  " + L.T("refresh_draft") : ""), 26, Art.Soft, TextAnchor.MiddleCenter, false);
            note.rectTransform.Place(0.5f, 0, new Vector2(0, 50), new Vector2(940, 40), new Vector2(0.5f, 0.5f));
        }

        static readonly Dictionary<string, (string label, Color col)> KindTag = new()
        {
            ["new"] = ("kind_new", Art.Green), ["normal"] = ("kind_normal", Art.Orange), ["break"] = ("kind_break", Art.Purple),
            ["chain"] = ("kind_chain", Art.Purple), ["combo"] = ("kind_combo", Art.Red),
        };

        void Card(RectTransform w, int i, CardRow c)
        {
            float x = 35 + i * 320, y = 476, cw = 290, ch = 660;
            var turret = Tables.TurretById[c.turret];
            var card = UIKit.Panel(w, "card_white", "Card" + i); card.rectTransform.TL(x, y, cw, ch);
            UIKit.Clickable(card, () => b.Choose(i));
            card.gameObject.AddComponent<PopIn>().delay = i * 0.06f;
            var head = UIKit.Img(card.transform, Art.UI("band"), "Head"); head.color = Elem.Color(turret.elem); head.raycastTarget = false;
            head.rectTransform.anchorMin = new Vector2(0, 1); head.rectTransform.anchorMax = new Vector2(1, 1); head.rectTransform.pivot = new Vector2(0.5f, 1);
            head.rectTransform.offsetMin = new Vector2(6, -96); head.rectTransform.offsetMax = new Vector2(-6, -6);
            var name = UIKit.Label(card.transform, c.name, c.name.Length > 6 ? 34 : 40); name.rectTransform.TL(0, 18, cw, 70);
            var tag = KindTag[c.kind];
            var tg = UIKit.Panel(card.transform, "pill", "Tag"); tg.color = tag.col; tg.rectTransform.Place(0.5f, 1, new Vector2(0, 26), new Vector2(150, 52), new Vector2(0.5f, 0.5f));
            UIKit.Label(tg.transform, L.T(tag.label), 28).rectTransform.Fill(0, 4, 0, 0);
            var pic = UIKit.Panel(card.transform, "slot", "PicBg"); pic.rectTransform.TL(22, 112, cw - 44, 240);
            var im = UIKit.Img(pic.transform, Art.TurretUI(turret.art), "Pic"); im.raycastTarget = false; im.rectTransform.Fill(24, 20, 24, 14);
            var sub = UIKit.Label(card.transform, turret.name + ", " + Elem.Name(turret.elem), 26, Art.Soft, TextAnchor.MiddleCenter, false); sub.rectTransform.TL(0, 362, cw, 44);
            var desc = UIKit.Label(card.transform, c.desc, 30, Art.Ink, TextAnchor.UpperCenter, false);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap; desc.rectTransform.TL(16, 420, cw - 32, 140);
            // 고르면 어떻게 되나: 별 · 단계
            var t = b.run.Get(c.turret);
            if (c.kind == "break" && t != null)
            {
                Badge(card.transform, cw / 2 - 60, 600, t.tier); Badge(card.transform, cw / 2 + 60, 600, t.tier + 1, true);
                var arrow = UIKit.Label(card.transform, ">", 40, Art.Purple, TextAnchor.MiddleCenter, false); arrow.rectTransform.TL(cw / 2 - 30, 576, 60, 50);
            }
            else if (c.kind == "combo")
            {
                var pt = UIKit.Label(card.transform, "+ " + Tables.TurretById[c.partner].name, 26, Art.Red, TextAnchor.MiddleCenter, false); pt.rectTransform.TL(0, 580, cw, 44);
            }
            else
            {
                int tier = t?.tier ?? 1, st = t?.stars ?? 0, after = Mathf.Min(Tables.Ci("Stars_Per_Tier", 4), st + 1);
                Stars(card.transform, 40, 600, tier, after, 26, st < after ? after - 1 : -1);
            }
        }

        static void Badge(Transform p, float cx, float y, int n, bool next = false)
        {
            var c = UIKit.Panel(p, "pill", "Tier"); c.color = next ? Art.Hex("#e4d6f6") : Color.white;
            c.rectTransform.TL(cx - 26, y - 26, 52, 52);
            UIKit.Label(c.transform, n.ToString(), 30, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
        }

        /// <summary>단계 숫자 + 별 4칸 (hl = 새로 켜질 별)</summary>
        static void Stars(Transform p, float x, float y, int tier, int stars, int size, int hl = -1)
        {
            var c = UIKit.Panel(p, "pill", "Tier"); c.rectTransform.TL(x, y - size, size * 2, size * 2);
            UIKit.Label(c.transform, tier.ToString(), Mathf.RoundToInt(size * 1.2f), Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 2, 0, 0);
            int n = Tables.Ci("Stars_Per_Tier", 4);
            for (int i = 0; i < n; i++)
            {
                var s = UIKit.Img(p, Art.Icon(i < stars ? "star_on" : "star_off"), "Star" + i);
                float sx = x + size * 2 + 6 + i * (size * 1.4f);
                s.rectTransform.TL(sx, y - size * 0.75f, size * 1.5f, size * 1.5f);
                if (i == hl)
                {
                    var ring = UIKit.Img(p, Art.Fx("ring"), "New"); ring.color = Art.Green2;
                    ring.rectTransform.TL(sx - size * 0.35f, y - size * 1.1f, size * 2.2f, size * 2.2f);
                }
            }
        }

        // ---------- 일시정지 · 고른 카드 ----------
        void ShowPause()
        {
            if (b.st != Battle.St.Play) return;
            b.Pause(true);
            var p = Popup.Make(L.T("pause"), 760, 520, () => b.Pause(false));
            p.Btn("btn_green", L.T("resume"), 0, 250, 460, () => p.Close());
            p.Btn("btn_red", L.T("give_up"), 0, 100, 460, () => { p.Close(false); b.End(false); });
        }

        void ShowCards()
        {
            if (b.st != Battle.St.Play) return;
            b.Pause(true);
            var p = Popup.Make(L.T("cards_title"), 960, 1300, () => b.Pause(false));
            float y = 130;
            foreach (var t in b.run.turrets)
            {
                var tile = UIKit.Tile(p.win, "slot", Color.white, Art.TurretUI(t.row.art), 0.86f, "T"); tile.rectTransform.TL(40, y, 120, 120);
                var nm = UIKit.Label(p.win, t.row.name, 34, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(180, y, 400, 50);
                Stars(p.win, 520, y + 26, t.tier, t.stars, 20);
                var names = new List<string>();
                foreach (var kv in t.picks) if (Tables.CardById.TryGetValue(kv.Key, out var c)) names.Add(kv.Value > 1 ? $"{c.name} x{kv.Value}" : c.name);
                var d = UIKit.Label(p.win, names.Count > 0 ? string.Join(", ", names) : "-", 24, Art.Soft, TextAnchor.UpperLeft, false);
                d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(180, y + 54, 740, 70);
                y += 140;
            }
        }

        // ---------- 결과 ----------
        void ShowResult()
        {
            Popup.CloseAll();
            var s = SaveData.Cur;
            var p = Popup.Make(b.won ? L.T("win") : L.T("lose"), 900, 980, null, false);
            var ic = UIKit.Img(p.win, Art.Icon(b.won ? "trophy" : "skull"), "Icon"); ic.rectTransform.Place(0.5f, 1, new Vector2(0, -130), new Vector2(150, 150), new Vector2(0.5f, 1));
            p.Text(L.F("stage_n", b.stageNo) + "   " + L.F("kills", b.kills) + "   " + L.F("lv", b.level), 300, 34, Art.Brown);
            if (b.FirstClear) p.Text(L.F("first_clear", Tables.Ci("First_Clear_Mul", 2)), 350, 30, Art.Green2);
            var coin = UIKit.Img(p.win, Art.Icon("coin"), "Coin"); coin.rectTransform.Place(0.5f, 1, new Vector2(-170, -420), new Vector2(100, 100), new Vector2(0.5f, 1));
            var g = UIKit.Label(p.win, "+" + Num.F(b.RewardGold), 60, Art.Gold); g.rectTransform.Place(0.5f, 1, new Vector2(50, -420), new Vector2(360, 100), new Vector2(0.5f, 1));
            var shard = UIKit.Img(p.win, Art.Icon("shard"), "Shard"); shard.rectTransform.Place(0.5f, 1, new Vector2(-170, -540), new Vector2(90, 90), new Vector2(0.5f, 1));
            var sh = UIKit.Label(p.win, "+" + b.RewardShards, 52, Art.Sky); sh.rectTransform.Place(0.5f, 1, new Vector2(50, -540), new Vector2(360, 90), new Vector2(0.5f, 1));
            p.Btn("btn_orange", L.T("to_lobby"), -200, 70, 340, Game.ToLobby);
            int stage = b.stageNo;
            bool next = b.won && stage < Tables.Stages.Count;
            p.Btn("btn_green", next ? L.T("next") : L.T("retry"), 200, 70, 340, () => Game.StartStage(next ? stage + 1 : stage));
        }
    }
}
