using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 로비 (2차): 위 줄 = 골드 · 스태미나 · 설정 / 아래 탭 4개 = 전투 · 터렛 · 장비 · 미션.
    /// 전투 탭 = 스테이지 고르기 · 보통/엘리트 · [전투 시작](스태미나) · 순찰 보상 · 다음에 열리는 터렛.
    /// 터렛 탭 = 기지 + 터렛 11종 강화 (스크롤 · 잠긴 터렛은 열리는 스테이지 표시).
    /// 장비 탭 = GearTab · 미션 탭 = MissionTab. 로비에 들어오면 출석 → 새 터렛 알림 → 도움말 순서로 한 번씩.
    /// </summary>
    public class Lobby : MonoBehaviour
    {
        static SaveData S => SaveData.Cur;
        public static readonly string[] Tabs = { "battle", "turret", "gear", "mission", "challenge", "shop" };   // 3차: 도전 · 상점 (뒤에 붙여 탭 번호 유지)
        public static int lastTab;
        /// <summary>자동 시험: 로비에 들어올 때 창(출석 · 알림 · 도움말)을 띄우지 않음</summary>
        public static bool NoIntro;
        public int tab;
        RectTransform root, content;
        Text goldT, stamT, stamNext, diaT;
        readonly List<(Image bg, Text label, string id)> tabBtns = new();
        Image missionDot, gearDot;
        // 전투 탭
        Text stageT, stageName, bestT, weakT, patrolT, patrolAmt, costT;
        Button startB, prevB, nextB, patrolB, normalB, eliteB;
        // 터렛 탭
        Text baseLvT, baseDescT, baseCostT;
        Button baseB;
        readonly List<(string id, Text lv, Text shards, Image bar, Button btn, Text cost)> rows = new();
        public GearTab gearTab;
        public MissionTab missionTab;
        public ChallengeTab challengeTab;
        public ShopTab shopTab;

        public void Build()
        {
            root = (RectTransform)transform;
            // 위 줄
            var top = UIKit.Rect(root, "TopBar"); top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1); top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 130); top.anchoredPosition = Vector2.zero;
            var gp = UIKit.Panel(top, "pill", "Gold"); gp.rectTransform.TL(20, 26, 270, 84);
            var gi = UIKit.Img(gp.transform, Art.Icon("coin"), "Coin"); gi.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(76, 76), new Vector2(0, 0.5f));
            goldT = UIKit.Label(gp.transform, "", 40, Art.Ink, TextAnchor.MiddleLeft, false); goldT.rectTransform.Fill(92, 6, 16, 0);
            var sp = UIKit.Panel(top, "pill", "Stamina"); sp.rectTransform.TL(300, 26, 290, 84);
            var si = UIKit.Img(sp.transform, Art.Icon("energy"), "Energy"); si.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(76, 76), new Vector2(0, 0.5f));
            stamT = UIKit.Label(sp.transform, "", 38, Art.Ink, TextAnchor.MiddleLeft, false); stamT.rectTransform.Fill(92, 6, 16, 0);
            stamNext = UIKit.Label(top, "", 24, Art.Cream, TextAnchor.MiddleCenter, true); stamNext.rectTransform.TL(300, 106, 290, 30);
            UIKit.Clickable(sp, ShowStamina);
            // 3차: 다이아 (누르면 상점)
            var dp = UIKit.Panel(top, "pill", "Dia"); dp.rectTransform.TL(600, 26, 240, 84);
            // 3차 수정안: 외형 메뉴 (터렛 외형 · 기지 외형)
            var lk = UIKit.Tile(top, "btn_purple", Color.white, Art.Icon("brush"), 0.6f, "Looks"); lk.rectTransform.TL(850, 20, 96, 100);
            UIKit.Clickable(lk, () => Looks.Open());
            var di = UIKit.Img(dp.transform, Art.Icon("diamond"), "Dia"); di.rectTransform.Place(0, 0.5f, new Vector2(10, 0), new Vector2(70, 70), new Vector2(0, 0.5f));
            diaT = UIKit.Label(dp.transform, "", 38, Art.Ink, TextAnchor.MiddleLeft, false); diaT.rectTransform.Fill(90, 6, 16, 0);
            UIKit.Clickable(dp, () => SetTab(5));
            var set = UIKit.Tile(top, "btn_orange", Color.white, Art.Icon("gear"), 0.6f, "Settings"); set.rectTransform.TL(954, 20, 100, 100);
            UIKit.Clickable(set, Settings.Open);

            // 아래 탭
            var nav = UIKit.Rect(root, "Nav"); nav.anchorMin = new Vector2(0, 0); nav.anchorMax = new Vector2(1, 0); nav.pivot = new Vector2(0.5f, 0);
            nav.sizeDelta = new Vector2(0, 176); nav.anchoredPosition = Vector2.zero;
            var navBg = UIKit.Panel(nav, "card", "NavBg"); navBg.rectTransform.Fill(10, 8, 10, 0);
            string[] icons = { "sword", "tab_turret", "g_helmet", "mission", "crown", "shop" };
            float tw = (1080 - 36) / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int idx = i;
                var b = UIKit.Img(nav, Art.UI("btn_cream"), "Tab_" + Tabs[i]); b.rectTransform.TL(20 + i * tw, 18, tw - 8, 148);
                UIKit.Clickable(b, () => SetTab(idx));
                var ic = UIKit.Img(b.transform, Art.Icon(icons[i]), "Icon"); ic.raycastTarget = false; ic.rectTransform.Place(0.5f, 1, new Vector2(0, -12), new Vector2(76, 76), new Vector2(0.5f, 1));
                var lb = UIKit.Label(b.transform, L.T("tab_" + Tabs[i]), 30); lb.rectTransform.Place(0.5f, 0, new Vector2(0, 22), new Vector2(tw, 44), new Vector2(0.5f, 0));
                tabBtns.Add((b, lb, Tabs[i]));
                if (Tabs[i] == "mission") missionDot = UIKit.Dot(b.transform, -18, -14);
                if (Tabs[i] == "gear") gearDot = UIKit.Dot(b.transform, -18, -14);
            }
            content = UIKit.Rect(root, "Content");
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one; content.offsetMin = new Vector2(0, 184); content.offsetMax = new Vector2(0, -140);
            SetTab(Mathf.Clamp(lastTab, 0, Tabs.Length - 1));
            Invoke(nameof(Intro), 0.05f);
        }

        // ---------- 탭 ----------
        public void SetTab(int i)
        {
            tab = lastTab = i;
            for (int c = content.childCount - 1; c >= 0; c--) Destroy(content.GetChild(c).gameObject);
            rows.Clear(); gearTab = null; missionTab = null; challengeTab = null; shopTab = null;
            stageT = null; baseLvT = null;
            for (int k = 0; k < tabBtns.Count; k++)
            {
                tabBtns[k].bg.sprite = Art.UI(k == i ? "btn_orange" : "btn_cream");
                tabBtns[k].bg.rectTransform.localScale = Vector3.one * (k == i ? 1.04f : 1f);
            }
            var page = UIKit.Rect(content, "Page_" + Tabs[i]).Fill();
            switch (Tabs[i])
            {
                case "battle": BuildBattle(page); break;
                case "turret": BuildTurret(page); break;
                case "gear": gearTab = page.gameObject.AddComponent<GearTab>(); gearTab.Build(this); break;
                case "mission": missionTab = page.gameObject.AddComponent<MissionTab>(); missionTab.Build(this); break;
                case "challenge": challengeTab = page.gameObject.AddComponent<ChallengeTab>(); challengeTab.Build(this); break;
                case "shop": shopTab = page.gameObject.AddComponent<ShopTab>(); shopTab.Build(this); break;
            }
            World.I.ShowSlots(0, false);
            Refresh();
        }

        // ---------- 전투 탭 ----------
        // 11-16 저녁 사용자 의견: 제목 · "스테이지 N 을 깨면 열려요" 빼기 · 스테이지 정보를 풍부하게(이름 · 약점 강조) ·
        //   [전투 시작]은 아래 · 순찰 칸은 [전투 시작] 정도 폭 · 기지 쪽은 비워 대장 카피가 어슬렁거리게 (LobbyWalker)
        // 11-16 밤 2차 의견: 약점 → 그 밑에 보통 · 엘리트 → 그 밑에 "나오는 적"(접기 · 펼치기) · 순찰 · 전투 시작은 돔 바로 위
        RectTransform enemyRow, weakRow, enemyBox, patrolRT, startRT;
        Image eliteTile, bossTile, ddArrow, stageCard;
        Text eliteName, bossName, ddLabel;
        public static bool enemiesOpen;
        const float CardShut = 416, CardOpen = 716;   // 2차 수정안 "스테이지 정보창 네모 크기를 조금 줄인다" (476 · 784 → 416 · 716)
        void BuildBattle(RectTransform page)
        {
            var card = stageCard = UIKit.Panel(page, "card", "StageCard"); card.rectTransform.TL(60, 12, 960, enemiesOpen ? CardOpen : CardShut);
            stageT = UIKit.Label(card.transform, "", 30, Art.Brown, TextAnchor.MiddleCenter, false); stageT.rectTransform.TL(0, 16, 960, 40);
            stageName = UIKit.Label(card.transform, "", 56); stageName.rectTransform.TL(120, 52, 720, 72);
            prevB = UIKit.Btn(card.transform, "btn_orange", null, 44, () => Move(-1), "Prev"); ((RectTransform)prevB.transform).TL(20, 30, 90, 90); Arrow(prevB, true);
            nextB = UIKit.Btn(card.transform, "btn_orange", null, 44, () => Move(1), "Next"); ((RectTransform)nextB.transform).TL(850, 30, 90, 90); Arrow(nextB, false);
            var ln = UIKit.Plain(card.transform, new Color(0.23f, 0.16f, 0.11f, 0.25f), "Line"); ln.rectTransform.TL(36, 136, 888, 4);
            // 약점 (속성 태그로 크게)
            var wl = UIKit.Label(card.transform, L.T("weak_title"), 32, Art.Ink, TextAnchor.MiddleLeft, false); wl.rectTransform.TL(40, 150, 190, 56);
            weakRow = UIKit.Rect(card.transform, "Weak"); weakRow.TL(210, 150, 730, 56);
            // 보통 / 엘리트 + 기록
            normalB = UIKit.Btn(card.transform, "btn_green", L.T("normal"), 34, () => SetElite(false), "Normal"); ((RectTransform)normalB.transform).TL(200, 222, 270, 80);
            eliteB = UIKit.Btn(card.transform, "btn_purple", L.T("elite_mode"), 34, () => SetElite(true), "Elite"); ((RectTransform)eliteB.transform).TL(490, 222, 270, 80);
            var crown = UIKit.Img(eliteB.transform, Art.Icon("crown"), "Crown"); crown.raycastTarget = false; crown.rectTransform.Place(0, 1, new Vector2(-12, 22), new Vector2(60, 60), new Vector2(0, 1));
            bestT = UIKit.Label(card.transform, "", 24, Art.Soft, TextAnchor.MiddleCenter, false); bestT.rectTransform.TL(0, 304, 960, 30);
            // 나오는 적 (접기 · 펼치기)
            var dd = UIKit.Btn(card.transform, "btn_cream", null, 30, ToggleEnemies, "EnemiesToggle"); ((RectTransform)dd.transform).TL(36, 338, 888, 66);
            ddLabel = UIKit.Label(dd.transform, "", 32, Art.Ink, TextAnchor.MiddleCenter, false); ddLabel.rectTransform.Fill(10, 12, 70, 4);
            ddArrow = UIKit.Img(dd.transform, Art.Icon("play"), "Arrow"); ddArrow.raycastTarget = false;
            ddArrow.rectTransform.Place(1, 0.5f, new Vector2(-28, 6), new Vector2(42, 42), new Vector2(1, 0.5f));
            enemyBox = UIKit.Rect(card.transform, "EnemyBox"); enemyBox.TL(-20, 410, 1000, 300);
            weakT = UIKit.Label(enemyBox, "", 22, Art.Soft, TextAnchor.MiddleRight, false); weakT.rectTransform.TL(500, 0, 476, 34);
            enemyRow = UIKit.Rect(enemyBox, "Enemies"); enemyRow.TL(40, 36, 940, 110);
            (eliteTile, eliteName) = BossTile(enemyBox, 44, L.T("elite_title"));
            (bossTile, bossName) = BossTile(enemyBox, 524, L.T("boss_title"));
            enemyBox.gameObject.SetActive(enemiesOpen);

            // 순찰(위) + [전투 시작](아래) — 돔 바로 위 (PlaceOverDome 이 화면 높이에 맞춰 놓음)
            var pc = UIKit.Panel(page, "card", "Patrol"); patrolRT = pc.rectTransform; patrolRT.Place(0.5f, 0, new Vector2(0, 540), new Vector2(660, 116), new Vector2(0.5f, 0));
            var pi = UIKit.Img(pc.transform, Art.Icon("patrol"), "Icon"); pi.rectTransform.TL(14, 16, 80, 80);
            var pt = UIKit.Label(pc.transform, L.T("patrol"), 30, Art.Ink, TextAnchor.MiddleLeft, false); pt.rectTransform.TL(104, 10, 300, 46);
            patrolT = UIKit.Label(pc.transform, "", 21, Art.Soft, TextAnchor.MiddleLeft, false); patrolT.rectTransform.TL(104, 56, 330, 40);
            patrolAmt = UIKit.Label(pc.transform, "", 24, Art.Brown, TextAnchor.MiddleRight, false); patrolAmt.rectTransform.TL(330, 10, 160, 90);
            patrolB = UIKit.Btn(pc.transform, "btn_orange", L.T("claim"), 32, Claim, "Claim"); ((RectTransform)patrolB.transform).TL(500, 14, 146, 84);

            startB = UIKit.Btn(page, "btn_green", null, 60, StartBattle, "Start");
            startRT = (RectTransform)startB.transform; startRT.Place(0.5f, 0, new Vector2(0, 380), new Vector2(600, 150), new Vector2(0.5f, 0));
            var sl = UIKit.Label(startB.transform, L.T("start"), 58); sl.rectTransform.Fill(10, 14, 150, 4);
            var ce = UIKit.Img(startB.transform, Art.Icon("energy"), "Energy"); ce.raycastTarget = false; ce.rectTransform.Place(1, 0.5f, new Vector2(-84, 6), new Vector2(64, 64), new Vector2(1, 0.5f));
            costT = UIKit.Label(startB.transform, "", 40); costT.rectTransform.Place(1, 0.5f, new Vector2(-18, 6), new Vector2(70, 60), new Vector2(1, 0.5f));
            PlaceOverDome();
        }

        void ToggleEnemies()
        {
            enemiesOpen = !enemiesOpen;
            enemyBox.gameObject.SetActive(enemiesOpen);
            stageCard.rectTransform.sizeDelta = new Vector2(960, enemiesOpen ? CardOpen : CardShut);
            Refresh();
        }

        /// <summary>[전투 시작] 아래 끝 = 돔 그림 꼭대기(월드 Dome_Top_Y) 바로 위 · 순찰은 그 위 — 화면 길이와 상관없이 돔에 붙음</summary>
        void PlaceOverDome()
        {
            if (!startRT || UIRoot.I == null || World.I == null) return;
            var page = (RectTransform)startRT.parent;
            var sp = World.I.cam.WorldToScreenPoint(new Vector3(0, Tables.Cf("Dome_Top_Y", -4.05f), 0));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, sp, UIRoot.I.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UIRoot.I.canvas.worldCamera, out var lp)) return;
            float y = lp.y - page.rect.yMin + 8;               // 페이지 아래 끝에서 잰 높이
            y = Mathf.Max(y, 20);
            startRT.anchoredPosition = new Vector2(0, y);
            patrolRT.anchoredPosition = new Vector2(0, y + 150 + 16);
        }

        (Image, Text) BossTile(Transform p, float x, string title)
        {
            var t = UIKit.Img(p, Art.UI("slot"), "BossTile"); t.rectTransform.TL(x, 160, 120, 120); t.raycastTarget = false;
            var ic = UIKit.Img(t.transform, null, "Icon"); ic.rectTransform.Fill(10, 14, 10, 8); ic.raycastTarget = false; ic.preserveAspect = true;
            var tl = UIKit.Label(p, title, 26, Art.Soft, TextAnchor.MiddleLeft, false); tl.rectTransform.TL(x + 136, 168, 320, 40);
            var nm = UIKit.Label(p, "", 34, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(x + 136, 210, 330, 56);
            return (ic, nm);
        }

        /// <summary>적 그림 칸 + 아래에 약점 색 띠</summary>
        static void EnemyTile(Transform p, EnemyRow e, float x, float size)
        {
            var t = UIKit.Img(p, Art.UI("slot"), "E_" + e.id); t.rectTransform.TL(x, 0, size, size); t.raycastTarget = false;
            var ic = UIKit.Img(t.transform, Art.Enemy(e), "Icon"); ic.preserveAspect = true; ic.raycastTarget = false; ic.rectTransform.Fill(8, 16, 8, 6);
            var w = UIKit.Img(t.transform, Art.UI("white"), "Weak"); w.type = Image.Type.Simple; w.raycastTarget = false; w.color = Elem.Color(e.weak);
            w.rectTransform.Place(0.5f, 0, new Vector2(0, 6), new Vector2(size * 0.55f, 12), new Vector2(0.5f, 0));
        }

        static void Arrow(Button b, bool left)
        {
            var a = UIKit.Img(b.transform, Art.Icon("play"), "Arrow"); a.raycastTarget = false;
            a.rectTransform.Fill(26, 30, 26, 22); a.rectTransform.localScale = new Vector3(left ? -1 : 1, 1, 1);
        }

        int MaxStage => Mathf.Min(Tables.Stages.Count, S.elite ? S.best : S.best + 1);
        void Move(int d)
        {
            S.stage = Mathf.Clamp(S.stage + d, 1, Mathf.Max(1, MaxStage));
            Refresh();
        }

        void SetElite(bool on)
        {
            if (on && !Meta.EliteOpen(1)) { Toast.Show(L.T("elite_locked")); return; }
            S.elite = on;
            if (on) S.stage = Mathf.Clamp(S.stage, 1, Mathf.Max(1, S.best));
            S.Save();
            Refresh();
        }

        public void StartBattle() => Game.TryStart(S.stage, S.elite);

        void Claim()
        {
            var (g, sh) = Meta.ClaimPatrol();
            if (g > 0) { Fx.Ring(Vector2.zero, 1, Art.Gold); Toast.Show($"{L.T("r_gold")} +{Num.F(g)}, {L.T("r_shards")} +{sh}"); }
            Refresh();
        }

        // ---------- 터렛 탭 ----------
        void BuildTurret(RectTransform page)
        {
            var list = UIKit.Panel(page, "card_white", "List"); list.rectTransform.Fill(20, 6, 20, 6);
            var c = UIKit.Scroll(list.transform, "Scroll"); c.parent.GetComponent<RectTransform>().Fill(10, 16, 10, 10);
            float y = 14, h = 118;
            {
                var r = UIKit.Rect(c, "Base"); r.TL(10, y, 1000, h);
                var tile = UIKit.Tile(r, "slot", Color.white, Art.Icon("base"), 0.8f, "Icon"); tile.rectTransform.TL(0, 4, 108, 108);
                baseLvT = UIKit.Label(r, "", 34, Art.Ink, TextAnchor.MiddleLeft, false); baseLvT.rectTransform.TL(126, 8, 500, 50);
                baseDescT = UIKit.Label(r, "", 26, Art.Soft, TextAnchor.MiddleLeft, false); baseDescT.rectTransform.TL(126, 58, 560, 44);
                baseB = UIKit.Btn(r, "btn_green", null, 30, () => UpPopup(null), "Up"); ((RectTransform)baseB.transform).TL(730, 12, 250, 96);
                baseCostT = CostLabel(baseB.transform);
                Divider(c, y + h + 3);
                y += h + 8;
            }
            foreach (var t in Tables.Turrets)
            {
                var r = UIKit.Rect(c, "Row_" + t.id); r.TL(10, y, 1000, h);
                bool open = S.Unlocked(t);
                var tile = UIKit.Tile(r, "slot", open ? Color.white : new Color(0.75f, 0.72f, 0.68f), Art.TurretIcon(t), 0.86f, "Icon"); tile.rectTransform.TL(0, 4, 108, 108);
                var tt = t; UIKit.Clickable(tile, () => TurretInfo(tt));
                Divider(c, y + h + 3);
                if (!open) { var lk = UIKit.Img(tile.transform, Art.Icon("lock"), "Lock"); lk.raycastTarget = false; lk.rectTransform.Place(1, 0, new Vector2(4, -4), new Vector2(52, 52), new Vector2(1, 0)); }
                var name = UIKit.Label(r, t.name, 34, open ? Art.Ink : Art.Soft, TextAnchor.MiddleLeft, false); name.rectTransform.TL(126, 4, 300, 50);
                UIKit.ElemTag(r, t.elem, 126 + Mathf.Min(300, name.preferredWidth + 14), 10, 104, 42, 22);
                if (!open)
                {
                    var why = UIKit.Label(r, L.F("unlock_at", t.unlock), 28, Art.Brown, TextAnchor.MiddleLeft, false); why.rectTransform.TL(126, 60, 600, 44);
                    y += h + 8; continue;
                }
                var lv = UIKit.Label(r, "", 30, Art.Brown, TextAnchor.MiddleLeft, false); lv.rectTransform.TL(470, 4, 220, 50);
                var bar = UIKit.Bar(r, Art.Sky, "Shards"); bar.transform.parent.GetComponent<RectTransform>().TL(126, 62, 460, 34);
                var sh = UIKit.Label(bar.transform.parent, "", 22, Color.white); sh.rectTransform.Fill(0, 2, 0, 0);
                var btn = UIKit.Btn(r, "btn_green", null, 30, () => UpPopup(t), "Up"); ((RectTransform)btn.transform).TL(730, 12, 250, 96);
                rows.Add((t.id, lv, sh, bar, btn, CostLabel(btn.transform)));
                y += h + 8;
            }
            UIKit.SetScrollH(c, y + 10);
        }

        /// <summary>강화 창 (2차 수정안 "터렛 강화할 때 어느 능력치가 올라가는지 알려주는 팝업"): 지금 → 다음 · 비용 · [강화] (창은 그대로 → 여러 번)</summary>
        public void UpPopup(TurretRow t)
        {
            var p = Popup.Make(t != null ? t.name : L.T("base"), 880, 860, Refresh);
            void Build()
            {
                for (int i = p.win.childCount - 1; i >= 0; i--) { var ch = p.win.GetChild(i); if (ch.name.StartsWith("U_")) Destroy(ch.gameObject); }
                var root = UIKit.Rect(p.win, "U_Root"); root.Fill();
                var tile = UIKit.Tile(root, "slot", Color.white, t != null ? Art.TurretIcon(t) : Art.Icon("base"), t != null ? 0.86f : 0.7f, "Pic"); tile.rectTransform.TL(40, 124, 170, 170);
                var rows = new List<(string name, string now, string next)>();
                bool max; string costTxt; bool can;
                if (t != null)
                {
                    int lv = S.TurretLv(t.id); var a = Tables.Lv(lv); var b2 = Tables.Lv(lv + 1);
                    max = Meta.TurretMax(t.id);
                    rows.Add(("Lv", lv.ToString(), max ? "-" : (lv + 1).ToString()));
                    rows.Add((L.T("up_dmg"), $"+{a.dmgBonus * 100:0}%", max ? "-" : $"+{b2.dmgBonus * 100:0}%"));
                    rows.Add((L.T("up_cd"), $"x{1 / a.cdMul:0.00}", max ? "-" : $"x{1 / b2.cdMul:0.00}"));
                    int nx = -1; for (int k = lv + 1; k <= Tables.MaxTurretLv; k++) if (Tables.Lv(k).cdMul < a.cdMul) { nx = k; break; }
                    var c = Meta.Cost(t.id);
                    costTxt = max ? L.T("max") : $"{L.F("shards", S.shards[S.TIdx(t.id)], c.shards)}   {L.T("r_gold")} {Num.F(c.gold)}";
                    can = Meta.CanUpgradeTurret(t.id);
                    if (nx > 0 && !max) UIKit.Label(root, L.F("up_next_cd", nx), 24, Art.Purple, TextAnchor.MiddleLeft, false).rectTransform.TL(40, 540, 800, 40);
                }
                else
                {
                    int lv = S.baseLv; max = S.BaseMax;
                    double hp = S.BaseHp, atk = S.Atk;
                    rows.Add(("Lv", lv.ToString(), max ? "-" : (lv + 1).ToString()));
                    rows.Add((L.T("up_hp"), Num.Comma(hp), max ? "-" : Num.Comma(hp + Tables.C("Base_HP_Per_Lv"))));
                    rows.Add((L.T("up_atk"), Num.F(atk), max ? "-" : Num.F(atk + Tables.C("Base_Atk_Per_Lv"))));
                    costTxt = max ? L.T("max") : $"{L.T("r_gold")} {Num.F(S.BaseCost)}";
                    can = Meta.CanUpgradeBase;
                }
                for (int i = 0; i < rows.Count; i++)
                {
                    float y = 130 + i * 62;
                    UIKit.Label(root, rows[i].name, 30, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(240, y, 200, 56);
                    UIKit.Label(root, rows[i].now, 34, Art.Ink, TextAnchor.MiddleRight, false).rectTransform.TL(430, y, 160, 56);
                    UIKit.Label(root, ">", 34, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(600, y, 50, 56);
                    UIKit.Label(root, rows[i].next, 36, Art.Green2, TextAnchor.MiddleLeft, false).rectTransform.TL(660, y, 200, 56);
                }
                UIKit.Label(root, costTxt, 28, Art.Brown, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 470, 880, 50);
                var ub = UIKit.Btn(root, can ? "btn_green" : "btn_gray", max ? L.T("max") : L.T("enhance"), 44, () =>
                {
                    bool ok = t != null ? Meta.UpgradeTurret(t.id) : Meta.UpgradeBase();
                    if (ok) { Fx.Ring(Vector2.zero, 1, Art.Gold); Build(); Refresh(); }
                }, "U_Up");
                ((RectTransform)ub.transform).Place(0.5f, 0, new Vector2(0, 50), new Vector2(420, 120), new Vector2(0.5f, 0));
                ub.transform.SetParent(root, true);
            }
            Build();
        }

        static void Divider(Transform c, float y)
        {
            var d = UIKit.Plain(c, new Color(0.23f, 0.16f, 0.11f, 0.18f), "Divider"); d.rectTransform.TL(20, y, 980, 3);
        }

        /// <summary>터렛 설명 창 (11-16 저녁 사용자 "터렛 그림을 누르면 어떤 방식으로 공격하는지")</summary>
        public static void TurretInfo(TurretRow t)
        {
            // 11-16 밤 "정보창을 조금 더 컴팩트하게"
            var p = Popup.Make(t.name, 900, 860);
            var tile = UIKit.Tile(p.win, "slot", Color.white, Art.TurretIcon(t), 0.86f, "Pic"); tile.rectTransform.TL(40, 124, 196, 196);
            var nm = UIKit.Label(p.win, t.name, 46, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(260, 128, 600, 60);
            UIKit.ElemTag(p.win, t.elem, 260, 196, 122, 50, 26);
            bool open = SaveData.Cur.Unlocked(t);
            var lv = UIKit.Label(p.win, open ? L.F("lv", SaveData.Cur.TurretLv(t.id)) : L.F("unlock_at", t.unlock), 28, open ? Art.Brown : Art.Red, TextAnchor.MiddleLeft, false);
            lv.rectTransform.TL(398, 196, 460, 50);
            var st = UIKit.Label(p.win, t.kind == "field" ? L.F("tinfo_stat_field", Num.F(t.rate), t.cd) : L.F("tinfo_stat", Num.F(t.rate + t.rate2), t.cd, t.range),
                                 24, Art.Soft, TextAnchor.UpperLeft, false);
            st.horizontalOverflow = HorizontalWrapMode.Wrap; st.rectTransform.TL(260, 256, 600, 70);
            var info = UIKit.Label(p.win, string.IsNullOrEmpty(t.info) ? t.desc : t.info, 30, Art.Ink, TextAnchor.UpperLeft, false);
            info.horizontalOverflow = HorizontalWrapMode.Wrap; info.rectTransform.TL(40, 340, 820, 120);
            var lines = new List<string>();
            foreach (var c in Tables.Cards)
                if (c.turret == t.id && c.kind == "break") lines.Add($"{L.T("kind_break")} {c.line}: {c.name} - {c.desc}");
            var bl = UIKit.Label(p.win, string.Join("\n", lines), 26, Art.Purple, TextAnchor.UpperLeft, false);
            bl.horizontalOverflow = HorizontalWrapMode.Wrap; bl.rectTransform.TL(40, 470, 820, 120);
            p.Btn("btn_green", L.T("ok"), 0, 40, 320, p.Close, 100);
        }

        Text CostLabel(Transform b)
        {
            var c = UIKit.Img(b, Art.Icon("coin"), "Coin"); c.rectTransform.Place(0, 0.5f, new Vector2(16, 6), new Vector2(54, 54), new Vector2(0, 0.5f));
            var t = UIKit.Label(b, "", 32); t.rectTransform.Fill(70, 14, 10, 4);
            return t;
        }

        // ---------- 갱신 ----------
        float tick;
        void Update()
        {
            tick -= Time.unscaledDeltaTime;
            if (tick <= 0) { tick = 1f; RefreshTop(); if (patrolT) RefreshPatrol(); }
            if (startRT) PlaceOverDome();
        }

        void RefreshTop()
        {
            goldT.text = Num.F(S.gold);
            diaT.text = Num.Comma(S.dia);
            int st = Meta.Stamina;
            stamT.text = $"{st}/{Meta.StaminaMax}";
            long nx = Meta.StaminaNext;
            stamNext.text = nx > 0 ? L.F("stamina_next", $"{nx / 60}:{nx % 60:00}") : "";
            missionDot.enabled = Meta.MissionReadyCount > 0 || Meta.AttendReady;
            gearDot.enabled = GearTab.HasNews;
            if (costT) costT.text = Meta.StaminaCost(S.elite).ToString();
        }

        void RefreshPatrol()
        {
            patrolT.text = L.F("patrol_desc", Num.Dur(S.PatrolSec), Tables.Ci("Patrol_Max_Hours", 12));
            patrolAmt.text = $"{Num.F(S.PatrolGold)}골드\n조각 {S.PatrolShards}";
            bool can = Meta.CanClaimPatrol;
            patrolB.interactable = can; UIKit.SetKind(patrolB, can ? "btn_orange" : "btn_gray");
        }

        public void Refresh()
        {
            RefreshTop();
            if (stageT)
            {
                if (S.elite && !Meta.EliteOpen(1)) S.elite = false;
                S.stage = Mathf.Clamp(S.stage, 1, Mathf.Max(1, MaxStage));
                var st = Tables.Stage(S.stage);
                stageT.text = L.F("stage_n", S.stage) + (S.elite ? "  " + L.T("elite_mode") : "");
                stageT.color = S.elite ? Art.Purple : Art.Brown;
                stageName.text = st.name;
                var weak = new List<string>();
                foreach (var id in st.enemies) if (Tables.Enemies.TryGetValue(id, out var e) && !weak.Contains(e.weak)) weak.Add(e.weak);
                for (int c = weakRow.childCount - 1; c >= 0; c--) Destroy(weakRow.GetChild(c).gameObject);
                for (int i = 0; i < weak.Count; i++) UIKit.ElemTag(weakRow, weak[i], i * 150, 2, 138, 56, 30);
                for (int c = enemyRow.childCount - 1; c >= 0; c--) Destroy(enemyRow.GetChild(c).gameObject);
                int ne = Mathf.Min(8, st.enemies.Length);
                float sz = ne > 7 ? 104 : 110;
                for (int i = 0; i < ne; i++) if (Tables.Enemies.TryGetValue(st.enemies[i], out var e2)) EnemyTile(enemyRow, e2, i * (sz + 12), sz);
                weakT.text = L.T("weak_hint");
                ddLabel.text = L.F(enemiesOpen ? "enemies_hide" : "enemies_show", st.enemies.Length);
                ddArrow.rectTransform.localEulerAngles = new Vector3(0, 0, enemiesOpen ? 90 : -90);
                if (Tables.Enemies.TryGetValue(st.elite, out var el)) { eliteTile.sprite = Art.Enemy(el); eliteName.text = el.name; }
                if (Tables.Enemies.TryGetValue(st.boss, out var bo)) { bossTile.sprite = Art.Enemy(bo); bossName.text = bo.name; }
                bestT.text = S.elite ? L.T("elite_desc") + "  " + L.F("best", S.eliteBest) : L.F("best", S.best);
                bestT.color = S.elite ? Art.Purple : Art.Soft;
                bool canPrev = S.stage > 1, canNext = S.stage < MaxStage;
                prevB.interactable = canPrev; UIKit.SetKind(prevB, canPrev ? "btn_orange" : "btn_gray");
                nextB.interactable = canNext; UIKit.SetKind(nextB, canNext ? "btn_orange" : "btn_gray");
                bool eo = Meta.EliteOpen(1);
                UIKit.SetKind(normalB, S.elite ? "btn_cream" : "btn_green");
                UIKit.SetKind(eliteB, !eo ? "btn_gray" : S.elite ? "btn_purple" : "btn_cream");
                UIKit.SetKind(startB, S.elite ? "btn_purple" : "btn_green");
                costT.text = Meta.StaminaCost(S.elite).ToString();
                RefreshPatrol();
            }
            if (baseLvT)
            {
                baseLvT.text = $"{L.T("base")} Lv {S.baseLv}";
                baseDescT.text = L.F("base_hp", Num.Comma(S.BaseHp)) + ", " + L.F("base_atk", Num.F(S.Atk));
                baseCostT.text = S.BaseMax ? L.T("max") : Num.F(S.BaseCost);
                UIKit.SetKind(baseB, Meta.CanUpgradeBase ? "btn_green" : "btn_gray");
                foreach (var (id, lv, sh, bar, btn, cost) in rows)
                {
                    int i = S.TIdx(id);
                    lv.text = L.F("lv", S.tLv[i]);
                    var c = Meta.Cost(id);
                    bool max = Meta.TurretMax(id);
                    sh.text = max ? L.T("max") : L.F("shards", S.shards[i], c.shards);
                    UIKit.SetBar(bar, max ? 1 : c.shards <= 0 ? 1 : (float)S.shards[i] / c.shards);
                    cost.text = max ? L.T("max") : Num.F(c.gold);
                    UIKit.SetKind(btn, Meta.CanUpgradeTurret(id) ? "btn_green" : "btn_gray");
                }
            }
            if (gearTab) gearTab.Refresh();
            if (missionTab) missionTab.Refresh();
            if (challengeTab) challengeTab.Refresh();
            if (shopTab) shopTab.Refresh();
        }

        // ---------- 스태미나 창 ----------
        public static void ShowStamina() => ShowStamina(false);
        public static void ShowStamina(bool lack)
        {
            var p = Popup.Make(L.T("stamina"), 820, 640);
            var ic = UIKit.Img(p.win, Art.Icon("energy"), "Icon"); ic.rectTransform.Place(0.5f, 1, new Vector2(0, -126), new Vector2(130, 130), new Vector2(0.5f, 1));
            p.Text(lack ? L.T("stamina_lack") : $"{Meta.Stamina}/{Meta.StaminaMax}", 270, 44, lack ? Art.Red : Art.Ink, 60);
            long nx = Meta.StaminaNext;
            p.Text(nx > 0 ? L.F("stamina_next", $"{nx / 60}:{nx % 60:00}") : L.T("stamina_full"), 334, 30, Art.Soft);
            int left = Meta.StaminaAdLeft;
            var b = p.Btn(left > 0 ? "btn_orange" : "btn_gray", null, 0, 120, 560, () =>
            {
                if (Meta.StaminaAdLeft <= 0) return;
                Ads.Show(ok => { if (ok) { Meta.StaminaAd(); p.Close(); Toast.Show($"{L.T("r_stamina")} +{Tables.Ci("Stamina_Ad", 20)}"); Game.Lobby?.Refresh(); } });
            }, 120);
            var ad = UIKit.Img(b.transform, Art.Icon("ad"), "Ad"); ad.rectTransform.Place(0, 0.5f, new Vector2(26, 6), new Vector2(76, 76), new Vector2(0, 0.5f));
            UIKit.Label(b.transform, L.F("stamina_ad", Tables.Ci("Stamina_Ad", 20)), 40).rectTransform.Fill(100, 14, 10, 4);
            p.Text(L.F("stamina_ad_left", left, Tables.Ci("Stamina_Ad_Per_Day", 3)) + (Ads.Draft ? "  " + L.T("refresh_draft") : ""), 548, 26, Art.Soft);
        }

        // ---------- 들어올 때 한 번씩: 출석 → 새 터렛 → 도움말 ----------
        void Intro()
        {
            if (NoIntro || Popup.Open != null || Game.Lobby != this || this == null) return;
            if (Meta.AttendReady && Tables.Attend.Count > 0) { MissionTab.ShowAttend(Intro); return; }
            var nt = Meta.NewUnlock();
            if (nt != null) { Meta.Seen("t:" + nt.id); ShowUnlock(nt, Intro); return; }
            Hints();
        }

        static void ShowUnlock(TurretRow t, System.Action next)
        {
            var p = Popup.Make(L.T("unlocked"), 820, 760, next);
            var tile = UIKit.Tile(p.win, "slot", Color.white, Art.TurretIcon(t), 0.86f, "Pic"); tile.rectTransform.Place(0.5f, 1, new Vector2(0, -130), new Vector2(280, 280), new Vector2(0.5f, 1));
            tile.gameObject.AddComponent<PopIn>();
            p.Text(t.name, 430, 52, Art.Ink, 70);
            p.Text($"{Elem.Name(t.elem)}, {t.desc.Replace(" · ", ", ")}", 500, 30, Art.Soft, 50);
            p.Btn("btn_green", L.T("ok"), 0, 50, 360, p.Close);
        }

        /// <summary>로비 도움말 (튜토리얼): 하나만, 처음 한 번씩</summary>
        void Hints()
        {
            if (S.runs >= 1 && Tutorial.Want("t_turret")) { Tutorial.Show("t_turret", L.T("tut_turret"), TabPos(1)); return; }
            if (S.gear.Count > 0 && Tutorial.Want("t_gear")) { Tutorial.Show("t_gear", L.T("tut_gear"), TabPos(2)); return; }
            if (S.best >= 1 && Tutorial.Want("t_elite") && eliteB) { Tutorial.Show("t_elite", L.T("tut_elite"), (RectTransform)eliteB.transform); return; }
        }

        RectTransform TabPos(int i) => tabBtns[i].bg.rectTransform;
    }
}
