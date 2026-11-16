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
        public static readonly string[] Tabs = { "battle", "turret", "gear", "mission" };
        public static int lastTab;
        /// <summary>자동 시험: 로비에 들어올 때 창(출석 · 알림 · 도움말)을 띄우지 않음</summary>
        public static bool NoIntro;
        public int tab;
        RectTransform root, content;
        Text goldT, stamT, stamNext;
        readonly List<(Image bg, Text label, string id)> tabBtns = new();
        Image missionDot, gearDot;
        // 전투 탭
        Text stageT, stageName, bestT, weakT, patrolT, patrolAmt, costT, nextUnlockT;
        Button startB, prevB, nextB, patrolB, normalB, eliteB;
        // 터렛 탭
        Text baseLvT, baseDescT, baseCostT;
        Button baseB;
        readonly List<(string id, Text lv, Text shards, Image bar, Button btn, Text cost)> rows = new();
        public GearTab gearTab;
        public MissionTab missionTab;

        public void Build()
        {
            root = (RectTransform)transform;
            // 위 줄
            var top = UIKit.Rect(root, "TopBar"); top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1); top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 130); top.anchoredPosition = Vector2.zero;
            var gp = UIKit.Panel(top, "pill", "Gold"); gp.rectTransform.TL(26, 26, 300, 84);
            var gi = UIKit.Img(gp.transform, Art.Icon("coin"), "Coin"); gi.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(76, 76), new Vector2(0, 0.5f));
            goldT = UIKit.Label(gp.transform, "", 40, Art.Ink, TextAnchor.MiddleLeft, false); goldT.rectTransform.Fill(92, 6, 16, 0);
            var sp = UIKit.Panel(top, "pill", "Stamina"); sp.rectTransform.TL(346, 26, 330, 84);
            var si = UIKit.Img(sp.transform, Art.Icon("energy"), "Energy"); si.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(76, 76), new Vector2(0, 0.5f));
            stamT = UIKit.Label(sp.transform, "", 38, Art.Ink, TextAnchor.MiddleLeft, false); stamT.rectTransform.Fill(92, 6, 16, 0);
            stamNext = UIKit.Label(top, "", 24, Art.Cream, TextAnchor.MiddleCenter, true); stamNext.rectTransform.TL(346, 106, 330, 30);
            UIKit.Clickable(sp, ShowStamina);
            var set = UIKit.Tile(top, "btn_orange", Color.white, Art.Icon("gear"), 0.6f, "Settings"); set.rectTransform.TL(954, 20, 100, 100);
            UIKit.Clickable(set, Settings.Open);

            // 아래 탭
            var nav = UIKit.Rect(root, "Nav"); nav.anchorMin = new Vector2(0, 0); nav.anchorMax = new Vector2(1, 0); nav.pivot = new Vector2(0.5f, 0);
            nav.sizeDelta = new Vector2(0, 176); nav.anchoredPosition = Vector2.zero;
            var navBg = UIKit.Panel(nav, "card", "NavBg"); navBg.rectTransform.Fill(10, 8, 10, 0);
            string[] icons = { "sword", "tab_turret", "g_helmet", "mission" };
            for (int i = 0; i < Tabs.Length; i++)
            {
                int idx = i;
                var b = UIKit.Img(nav, Art.UI("btn_cream"), "Tab_" + Tabs[i]); b.rectTransform.TL(24 + i * 258, 18, 246, 148);
                UIKit.Clickable(b, () => SetTab(idx));
                var ic = UIKit.Img(b.transform, Art.Icon(icons[i]), "Icon"); ic.raycastTarget = false; ic.rectTransform.Place(0.5f, 1, new Vector2(0, -10), new Vector2(84, 84), new Vector2(0.5f, 1));
                var lb = UIKit.Label(b.transform, L.T("tab_" + Tabs[i]), 34); lb.rectTransform.Place(0.5f, 0, new Vector2(0, 22), new Vector2(240, 44), new Vector2(0.5f, 0));
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
            rows.Clear(); gearTab = null; missionTab = null;
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
            }
            World.I.ShowSlots(0, false);
            Refresh();
        }

        // ---------- 전투 탭 ----------
        void BuildBattle(RectTransform page)
        {
            var title = UIKit.Label(page, L.T("title"), 60); title.rectTransform.TL(0, 4, 1080, 90); title.color = Art.Cream;
            var card = UIKit.Panel(page, "card", "StageCard"); card.rectTransform.TL(90, 110, 900, 420);
            stageT = UIKit.Label(card.transform, "", 64, Art.Ink, TextAnchor.MiddleCenter, false); stageT.rectTransform.TL(0, 24, 900, 80);
            stageName = UIKit.Label(card.transform, "", 38, Art.Brown, TextAnchor.MiddleCenter, false); stageName.rectTransform.TL(0, 106, 900, 54);
            weakT = UIKit.Label(card.transform, "", 30, Art.Soft, TextAnchor.MiddleCenter, false); weakT.rectTransform.TL(0, 166, 900, 46);
            bestT = UIKit.Label(card.transform, "", 28, Art.Soft, TextAnchor.MiddleCenter, false); bestT.rectTransform.TL(0, 214, 900, 44);
            prevB = UIKit.Btn(card.transform, "btn_orange", null, 48, () => Move(-1), "Prev"); ((RectTransform)prevB.transform).TL(30, 90, 110, 110); Arrow(prevB, true);
            nextB = UIKit.Btn(card.transform, "btn_orange", null, 48, () => Move(1), "Next"); ((RectTransform)nextB.transform).TL(760, 90, 110, 110); Arrow(nextB, false);
            // 보통 / 엘리트
            normalB = UIKit.Btn(card.transform, "btn_green", L.T("normal"), 36, () => SetElite(false), "Normal"); ((RectTransform)normalB.transform).TL(150, 290, 290, 96);
            eliteB = UIKit.Btn(card.transform, "btn_purple", L.T("elite_mode"), 36, () => SetElite(true), "Elite"); ((RectTransform)eliteB.transform).TL(460, 290, 290, 96);
            var crown = UIKit.Img(eliteB.transform, Art.Icon("crown"), "Crown"); crown.raycastTarget = false; crown.rectTransform.Place(0, 1, new Vector2(-12, 22), new Vector2(64, 64), new Vector2(0, 1));

            startB = UIKit.Btn(page, "btn_green", null, 60, StartBattle, "Start");
            ((RectTransform)startB.transform).TL(250, 556, 580, 150);
            var sl = UIKit.Label(startB.transform, L.T("start"), 58); sl.rectTransform.Fill(10, 14, 150, 4);
            var ce = UIKit.Img(startB.transform, Art.Icon("energy"), "Energy"); ce.raycastTarget = false; ce.rectTransform.Place(1, 0.5f, new Vector2(-84, 6), new Vector2(64, 64), new Vector2(1, 0.5f));
            costT = UIKit.Label(startB.transform, "", 40); costT.rectTransform.Place(1, 0.5f, new Vector2(-18, 6), new Vector2(70, 60), new Vector2(1, 0.5f));

            // 순찰
            var pc = UIKit.Panel(page, "card", "Patrol"); pc.rectTransform.TL(90, 730, 900, 120);
            var pi = UIKit.Img(pc.transform, Art.Icon("patrol"), "Icon"); pi.rectTransform.TL(20, 14, 84, 84);
            var pt = UIKit.Label(pc.transform, L.T("patrol"), 34, Art.Ink, TextAnchor.MiddleLeft, false); pt.rectTransform.TL(120, 10, 420, 50);
            patrolT = UIKit.Label(pc.transform, "", 24, Art.Soft, TextAnchor.MiddleLeft, false); patrolT.rectTransform.TL(120, 58, 440, 40);
            patrolAmt = UIKit.Label(pc.transform, "", 28, Art.Brown, TextAnchor.MiddleRight, false); patrolAmt.rectTransform.TL(440, 10, 230, 90);
            patrolB = UIKit.Btn(pc.transform, "btn_orange", L.T("claim"), 34, Claim, "Claim"); ((RectTransform)patrolB.transform).TL(690, 14, 190, 86);

            // 다음에 열리는 터렛
            var nu = UIKit.Panel(page, "night", "NextUnlock"); nu.rectTransform.TL(90, 868, 900, 96);
            nextUnlockT = UIKit.Label(nu.transform, "", 30, Art.Cream, TextAnchor.MiddleCenter, true); nextUnlockT.rectTransform.Fill(20, 8, 20, 0);
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
                baseB = UIKit.Btn(r, "btn_green", null, 30, () => { if (Meta.UpgradeBase()) Refresh(); }, "Up"); ((RectTransform)baseB.transform).TL(730, 12, 250, 96);
                baseCostT = CostLabel(baseB.transform);
                y += h + 8;
            }
            foreach (var t in Tables.Turrets)
            {
                var r = UIKit.Rect(c, "Row_" + t.id); r.TL(10, y, 1000, h);
                bool open = S.Unlocked(t);
                var tile = UIKit.Tile(r, "slot", open ? Color.white : new Color(0.75f, 0.72f, 0.68f), Art.TurretUI(t.art), 0.86f, "Icon"); tile.rectTransform.TL(0, 4, 108, 108);
                if (!open) { var lk = UIKit.Img(tile.transform, Art.Icon("lock"), "Lock"); lk.raycastTarget = false; lk.rectTransform.Place(1, 0, new Vector2(4, -4), new Vector2(52, 52), new Vector2(1, 0)); }
                var name = UIKit.Label(r, t.name, 34, open ? Art.Ink : Art.Soft, TextAnchor.MiddleLeft, false); name.rectTransform.TL(126, 4, 300, 50);
                var el = UIKit.Panel(r, "pill", "Elem"); el.color = Elem.Color(t.elem); el.rectTransform.TL(126 + Mathf.Min(300, name.preferredWidth + 14), 12, 90, 38);
                UIKit.Label(el.transform, Elem.Name(t.elem), 22, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 2, 0, 0);
                if (!open)
                {
                    var why = UIKit.Label(r, L.F("unlock_at", t.unlock), 28, Art.Brown, TextAnchor.MiddleLeft, false); why.rectTransform.TL(126, 60, 600, 44);
                    var d = UIKit.Label(r, t.desc.Replace(" · ", ", "), 24, Art.Soft, TextAnchor.MiddleRight, false); d.rectTransform.TL(560, 10, 420, 44);
                    y += h + 8; continue;
                }
                var lv = UIKit.Label(r, "", 30, Art.Brown, TextAnchor.MiddleLeft, false); lv.rectTransform.TL(470, 4, 220, 50);
                var bar = UIKit.Bar(r, Art.Sky, "Shards"); bar.transform.parent.GetComponent<RectTransform>().TL(126, 62, 460, 34);
                var sh = UIKit.Label(bar.transform.parent, "", 22, Color.white); sh.rectTransform.Fill(0, 2, 0, 0);
                var btn = UIKit.Btn(r, "btn_green", null, 30, () => { if (Meta.UpgradeTurret(t.id)) Refresh(); }, "Up"); ((RectTransform)btn.transform).TL(730, 12, 250, 96);
                rows.Add((t.id, lv, sh, bar, btn, CostLabel(btn.transform)));
                y += h + 8;
            }
            UIKit.SetScrollH(c, y + 10);
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
        }

        void RefreshTop()
        {
            goldT.text = Num.F(S.gold);
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
                stageT.text = L.F("stage_n", S.stage);
                stageName.text = st.name;
                var weak = new List<string>();
                foreach (var id in st.enemies) if (Tables.Enemies.TryGetValue(id, out var e) && !weak.Contains(Elem.Name(e.weak))) weak.Add(Elem.Name(e.weak));
                weakT.text = L.F("enemy_weak", string.Join(", ", weak));
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
                TurretRow nxt = null; foreach (var t in Tables.Turrets) if (!S.Unlocked(t) && (nxt == null || t.unlock < nxt.unlock)) nxt = t;
                nextUnlockT.text = nxt != null ? $"{nxt.name}: {L.F("unlock_at", nxt.unlock)}" : L.T("unlocked");
                nextUnlockT.transform.parent.gameObject.SetActive(nxt != null);
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
            var tile = UIKit.Tile(p.win, "slot", Color.white, Art.TurretUI(t.art), 0.86f, "Pic"); tile.rectTransform.Place(0.5f, 1, new Vector2(0, -130), new Vector2(280, 280), new Vector2(0.5f, 1));
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
