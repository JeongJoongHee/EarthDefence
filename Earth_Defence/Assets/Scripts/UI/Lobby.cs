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
        public static readonly string[] Tabs = { "battle", "turret", "gear", "mission", "challenge", "shop", "looks" };   // 3차: 도전 · 상점 · 4차 수정안: 외형 (뒤에 붙여 탭 번호 유지)
        static readonly int[] TabOrder = { 0, 1, 2, 6, 3, 4, 5 };   // 아래 줄에 보이는 순서 (외형은 장비 다음)
        public static int lastTab;
        /// <summary>자동 시험: 로비에 들어올 때 창(출석 · 알림 · 도움말)을 띄우지 않음</summary>
        public static bool NoIntro;
        public int tab;
        RectTransform root, content;
        Text goldT, stamT, stamNext, diaT;
        (Image bg, Text label, string id)[] tabBtns;
        Image missionDot, gearDot;
        // 전투 탭
        Text stageT, stageName, bestT, weakT, patrolT, costT;
        Image patrolDot;
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
            goldT = UIKit.Label(gp.transform, "", 40, Art.Ink, TextAnchor.MiddleRight, false); goldT.rectTransform.Fill(92, 6, 26, 0);   // 6차 수정안 "재화 숫자는 우측에"
            var sp = UIKit.Panel(top, "pill", "Stamina"); sp.rectTransform.TL(300, 26, 290, 84);
            var si = UIKit.Img(sp.transform, Art.Icon("energy"), "Energy"); si.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(76, 76), new Vector2(0, 0.5f));
            stamT = UIKit.Label(sp.transform, "", 38, Art.Ink, TextAnchor.MiddleRight, false); stamT.rectTransform.Fill(92, 6, 26, 0);
            stamNext = UIKit.Label(top, "", 24, Art.Cream, TextAnchor.MiddleCenter, true); stamNext.rectTransform.TL(300, 106, 290, 30);
            UIKit.Clickable(sp, ShowStamina);
            // 3차: 다이아 (누르면 상점)
            var dp = UIKit.Panel(top, "pill", "Dia"); dp.rectTransform.TL(600, 26, 300, 84);
            var di = UIKit.Img(dp.transform, Art.Icon("diamond"), "Dia"); di.rectTransform.Place(0, 0.5f, new Vector2(10, 0), new Vector2(70, 70), new Vector2(0, 0.5f));
            diaT = UIKit.Label(dp.transform, "", 38, Art.Ink, TextAnchor.MiddleRight, false); diaT.rectTransform.Fill(90, 6, 26, 0);
            UIKit.Clickable(dp, () => { ShopTab.sub = 0; SetTab(5); });   // 6차: 다이아 칸을 누르면 상점의 다이아 패널
            var set = UIKit.Tile(top, "btn_orange", Color.white, Art.Icon("gear"), 0.6f, "Settings"); set.rectTransform.TL(954, 20, 100, 100);
            UIKit.Clickable(set, Settings.Open);

            // 아래 탭
            var nav = UIKit.Rect(root, "Nav"); nav.anchorMin = new Vector2(0, 0); nav.anchorMax = new Vector2(1, 0); nav.pivot = new Vector2(0.5f, 0);
            nav.sizeDelta = new Vector2(0, 176); nav.anchoredPosition = Vector2.zero;
            var navBg = UIKit.Panel(nav, "card", "NavBg"); navBg.rectTransform.Fill(10, 8, 10, 0);
            string[] icons = { "sword", "tab_turret", "g_helmet", "mission", "crown", "shop", "brush" };
            float tw = (1080 - 52) / (float)Tabs.Length;   // 요청 사항_05 "테두리 안으로 정렬": 바탕 판(10~1070) 안쪽 16px
            tabBtns = new (Image, Text, string)[Tabs.Length];
            for (int pos = 0; pos < TabOrder.Length; pos++)
            {
                int idx = TabOrder[pos];
                var b = UIKit.Img(nav, Art.UI("btn_cream"), "Tab_" + Tabs[idx]); b.rectTransform.TL(26 + pos * tw, 20, tw - 8, 136);
                UIKit.Clickable(b, () => SetTab(idx));
                var ic = UIKit.Img(b.transform, Art.Icon(icons[idx]), "Icon"); ic.raycastTarget = false; ic.rectTransform.Place(0.5f, 1, new Vector2(0, -8), new Vector2(70, 70), new Vector2(0.5f, 1));
                var lb = UIKit.Label(b.transform, L.T("tab_" + Tabs[idx]), 28); lb.rectTransform.Place(0.5f, 0, new Vector2(0, 20), new Vector2(tw, 42), new Vector2(0.5f, 0));
                tabBtns[idx] = (b, lb, Tabs[idx]);
                if (Tabs[idx] == "mission") missionDot = UIKit.Dot(b.transform, -18, -14);
                if (Tabs[idx] == "gear") gearDot = UIKit.Dot(b.transform, -18, -14);
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
            for (int k = 0; k < tabBtns.Length; k++)
            {
                tabBtns[k].bg.sprite = Art.UI(k == i ? "btn_orange" : "btn_cream");
                tabBtns[k].bg.rectTransform.localScale = Vector3.one;   // 요청 사항_05: 고른 탭도 판 밖으로 안 나가게 (색으로만 구분)
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
                case "looks": Looks.Build(page); break;
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
        const float CardShut = 300, CardOpen = 720;   // 요청 사항_05: 적 약점 줄을 [적 정보] 안으로 옮겨 352 → 300   // 2차 수정안 476 → 416 · 4차 수정안 "스테이지 칸 조금 더 작게" → 352
        void BuildBattle(RectTransform page)
        {
            LobbyTalk.Attach(page);      // 9차: 대장 카피 말풍선 (판 맨 뒤)
            var card = stageCard = UIKit.Panel(page, "card", "StageCard"); card.rectTransform.TL(60, 12, 960, enemiesOpen ? CardOpen : CardShut);
            stageT = UIKit.Label(card.transform, "", 26, Art.Brown, TextAnchor.MiddleCenter, false); stageT.rectTransform.TL(0, 12, 960, 34);
            stageName = UIKit.Label(card.transform, "", 50); stageName.rectTransform.TL(120, 42, 720, 62);
            prevB = UIKit.Btn(card.transform, "btn_orange", null, 44, () => Move(-1), "Prev"); ((RectTransform)prevB.transform).TL(24, 24, 80, 80); Arrow(prevB, true);
            nextB = UIKit.Btn(card.transform, "btn_orange", null, 44, () => Move(1), "Next"); ((RectTransform)nextB.transform).TL(856, 24, 80, 80); Arrow(nextB, false);
            var ln = UIKit.Plain(card.transform, new Color(0.23f, 0.16f, 0.11f, 0.25f), "Line"); ln.rectTransform.TL(36, 114, 888, 4);
            // 보통 / 엘리트 + 기록
            normalB = UIKit.Btn(card.transform, "btn_green", L.T("normal"), 32, () => SetElite(false), "Normal"); ((RectTransform)normalB.transform).TL(210, 128, 250, 72);
            eliteB = UIKit.Btn(card.transform, "btn_purple", L.T("elite_mode"), 32, () => SetElite(true), "Elite"); ((RectTransform)eliteB.transform).TL(500, 128, 250, 72);
            var crown = UIKit.Img(eliteB.transform, Art.Icon("crown"), "Crown"); crown.raycastTarget = false; crown.rectTransform.Place(0, 1, new Vector2(-12, 20), new Vector2(54, 54), new Vector2(0, 1));
            bestT = UIKit.Label(card.transform, "", 22, Art.Soft, TextAnchor.MiddleCenter, false); bestT.rectTransform.TL(0, 202, 960, 26);
            // [적 정보] (4차 수정안: "나오는 적 2종 + 보스 보기" → "적 정보" · 하늘색 단추로 잘 보이게)
            var dd = UIKit.Btn(card.transform, "btn_sky", null, 30, ToggleEnemies, "EnemiesToggle"); ((RectTransform)dd.transform).TL(250, 232, 460, 58);
            ddLabel = UIKit.Label(dd.transform, "", 30); ddLabel.rectTransform.Fill(10, 10, 50, 2);
            ddArrow = UIKit.Img(dd.transform, Art.Icon("play"), "Arrow"); ddArrow.raycastTarget = false;
            ddArrow.rectTransform.Place(1, 0.5f, new Vector2(-18, 5), new Vector2(36, 36), new Vector2(1, 0.5f));
            enemyBox = UIKit.Rect(card.transform, "EnemyBox"); enemyBox.TL(-20, 300, 1000, 410);
            // 요청 사항_05 "적 약점 정보 제거 → 적 정보 칸으로 이동": 펼치면 맨 위에 약점 태그
            var wl = UIKit.Label(enemyBox, L.T("weak_title"), 30, Art.Ink, TextAnchor.MiddleLeft, false); wl.rectTransform.TL(64, 4, 170, 52);
            weakRow = UIKit.Rect(enemyBox, "Weak"); weakRow.TL(230, 4, 730, 52);
            // 4차 수정안 "칸 아래 색 = 그 적의 약점 — 잘 보이게 강조": 노란 띠 + 진한 글자
            var hint = UIKit.Panel(enemyBox, "pill", "WeakHint"); hint.color = Art.Hex("#ffe08a"); hint.rectTransform.TL(220, 66, 600, 50);
            weakT = UIKit.Label(hint.transform, "", 26, Art.Ink, TextAnchor.MiddleCenter, false); weakT.rectTransform.Fill(10, 4, 10, 0);
            enemyRow = UIKit.Rect(enemyBox, "Enemies"); enemyRow.TL(40, 126, 940, 150);
            (eliteTile, eliteName) = BossTile(enemyBox, 44, L.T("elite_title"));
            (bossTile, bossName) = BossTile(enemyBox, 524, L.T("boss_title"));
            enemyBox.gameObject.SetActive(enemiesOpen);

            // [전투 시작](위) + 순찰(아래, 4차 수정안 "순찰 보상을 전투 시작 버튼 아래로") — 돔 바로 위 (PlaceOverDome)
            // 6차 수정안 "순찰 보상 크기를 전투 시작 크기와 맞추고, 누르면 몇 분 순찰 · 얼마 얻었는지 안내 창 (받기 / 광고 2배)"
            patrolB = UIKit.Btn(page, "btn_orange", null, 40, PatrolPopup, "Patrol"); patrolRT = (RectTransform)patrolB.transform;
            patrolRT.Place(0.5f, 0, new Vector2(0, 380), new Vector2(600, 150), new Vector2(0.5f, 0));
            var pi = UIKit.Img(patrolB.transform, Art.Icon("patrol"), "Icon"); pi.raycastTarget = false; pi.rectTransform.Place(0, 0.5f, new Vector2(26, 6), new Vector2(104, 104), new Vector2(0, 0.5f));
            var pt = UIKit.Label(patrolB.transform, L.T("patrol"), 46); pt.rectTransform.Fill(140, 62, 20, 10);
            patrolT = UIKit.Label(patrolB.transform, "", 24, Art.Cream); patrolT.rectTransform.Fill(140, 16, 20, 80);
            patrolDot = UIKit.Dot(patrolB.transform, -16, -14, 34);

            startB = UIKit.Btn(page, "btn_green", null, 60, StartBattle, "Start");
            startRT = (RectTransform)startB.transform; startRT.Place(0.5f, 0, new Vector2(0, 520), new Vector2(600, 150), new Vector2(0.5f, 0));
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

        /// <summary>토스트 자리 (4차 수정안): 전투 탭이면 스테이지 칸 아래 끝과 [전투 시작] 위 끝 사이 가운데 (월드 좌표)</summary>
        public bool ToastSpot(out Vector3 pos)
        {
            pos = default;
            if (tab != 0 || !stageCard || !startRT) return false;
            var a = new Vector3[4]; var b = new Vector3[4];
            stageCard.rectTransform.GetWorldCorners(a); startRT.GetWorldCorners(b);
            pos = new Vector3((a[0].x + a[3].x) / 2, (a[0].y + b[1].y) / 2, a[0].z);
            return a[0].y > b[1].y;
        }

        /// <summary>순찰 아래 끝 = 돔 그림 꼭대기(월드 Dome_Top_Y) 바로 위 · [전투 시작]은 그 위 — 화면 길이와 상관없이 돔에 붙음</summary>
        void PlaceOverDome()
        {
            if (!startRT || UIRoot.I == null || World.I == null) return;
            var page = (RectTransform)startRT.parent;
            if (Tables.Ci("Lobby_Btn_Bottom", 1) == 1)
            {   // 8차 수정안 "로비에서 전투 시작과 순찰 보상도 하단에 배치": 아래 탭 바로 위 (순찰 아래 · [전투 시작] 그 위)
                float yb = Tables.Cf("Lobby_Btn_Gap", 24f);
                if (Mathf.Abs(patrolRT.anchoredPosition.y - yb) < 0.01f && Mathf.Abs(patrolRT.anchoredPosition.x) < 0.01f) return;
                patrolRT.anchoredPosition = new Vector2(0, yb);
                startRT.anchoredPosition = new Vector2(0, yb + 150 + 18);
                return;
            }
            // 11-18 사령탑: 돔이 없어져 사령탑 오른쪽 하늘 가운데(Lobby_Btn_X)에 — 높이는 예전 돔 꼭대기 그대로
            // 7차: 로비에서 사령탑을 빼고 큰 지구 → 가운데(Lobby_Btn_X 0) · 지구 위 대장 카피 머리 위(Dome_Top_Y)
            var sp = World.I.cam.WorldToScreenPoint(new Vector3(Tables.Cf("Lobby_Btn_X", 0f), Tables.Cf("Dome_Top_Y", -2f), 0));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, sp, UIRoot.I.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UIRoot.I.canvas.worldCamera, out var lp)) return;
            float y = lp.y - page.rect.yMin + 8;               // 페이지 아래 끝에서 잰 높이
            // 7차: 화면이 짧은 폰(16:9)에서 [적 정보]를 펼치면 스테이지 칸과 겹칠 수 있음 → 스테이지 칸 아래 끝까지만 올라감 (그땐 대장 카피 앞으로 내려옴)
            if (stageCard)
            {
                var c = new Vector3[4]; stageCard.rectTransform.GetWorldCorners(c);
                var ucam = UIRoot.I.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : UIRoot.I.canvas.worldCamera;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(page, RectTransformUtility.WorldToScreenPoint(ucam, c[0]), ucam, out var cb))
                    y = Mathf.Min(y, cb.y - page.rect.yMin - 20 - 150 - 18 - 150);
            }
            y = Mathf.Max(y, 20);
            float x = Mathf.Clamp(lp.x - page.rect.center.x, -page.rect.width / 2 + 310, page.rect.width / 2 - 310);
            if (Mathf.Abs(patrolRT.anchoredPosition.y - y) < 0.01f && Mathf.Abs(patrolRT.anchoredPosition.x - x) < 0.01f) return;      // 11-17: 매 프레임 같은 자리를 다시 넣지 않음 (화면이 바뀐 때만)
            patrolRT.anchoredPosition = new Vector2(x, y);
            startRT.anchoredPosition = new Vector2(x, y + 150 + 18);
        }

        (Image, Text) BossTile(Transform p, float x, string title)
        {
            var t = UIKit.Img(p, Art.UI("slot"), "BossTile"); t.rectTransform.TL(x, 282, 116, 116); t.raycastTarget = false;
            var ic = UIKit.Img(t.transform, null, "Icon"); ic.rectTransform.Fill(10, 14, 10, 8); ic.raycastTarget = false; ic.preserveAspect = true;
            var tl = UIKit.Label(p, title, 26, Art.Soft, TextAnchor.MiddleLeft, false); tl.rectTransform.TL(x + 132, 288, 320, 40);
            var nm = UIKit.Label(p, "", 34, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(x + 132, 330, 330, 56);
            return (ic, nm);
        }

        /// <summary>적 그림 칸 + 아래에 약점 색 띠</summary>
        static void EnemyTile(Transform p, EnemyRow e, float x, float size)
        {
            var t = UIKit.Img(p, Art.UI("slot"), "E_" + e.id); t.rectTransform.TL(x, 0, size, size); t.raycastTarget = false;
            var ic = UIKit.Img(t.transform, Art.Enemy(e), "Icon"); ic.preserveAspect = true; ic.raycastTarget = false; ic.rectTransform.Fill(8, 16, 8, 6);
            // 4차 수정안 "약점 색을 잘 보이게": 칸 아래 굵은 색 띠 + 그 밑에 약점 이름
            var w = UIKit.Panel(p, "pill", "Weak"); w.color = Elem.Color(e.weak);
            w.rectTransform.TL(x + size * 0.08f, size - 14, size * 0.84f, 40);
            UIKit.Label(w.transform, Elem.Name(e.weak), 22, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 3, 0, 0);
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

        /// <summary>6차 수정안: 순찰 안내 창 — 몇 분 순찰했고 무엇을 얼마 얻는지 · [받기] = 그대로 · [광고 보고 2배] = Patrol_Ad_Mul 배</summary>
        public void PatrolPopup()
        {
            if (!Meta.CanClaimPatrol)
            {
                Toast.Show(L.F("patrol_wait", Num.Dur(Tables.C("Patrol_Min_Sec", 60) - S.PatrolSec)));
                return;
            }
            double gold = System.Math.Floor(S.PatrolGold); int sh = S.PatrolShards, mul = Tables.Ci("Patrol_Ad_Mul", 2);
            var p = Popup.Make(L.T("patrol"), 860, 800);
            var ic = UIKit.Img(p.win, Art.Icon("patrol"), "PatrolIcon"); ic.raycastTarget = false; ic.rectTransform.Place(0.5f, 1, new Vector2(0, -116), new Vector2(120, 120), new Vector2(0.5f, 1));
            p.Text(L.F("patrol_popup", Num.Dur(S.PatrolSec)), 246, 40, Art.Ink, 60);
            p.Text(L.F("patrol_max", Tables.Ci("Patrol_Max_Hours", 12)), 306, 26, Art.Soft, 40);
            void Got(float x, string icon, string amt, string name)
            {
                var t = UIKit.Tile(p.win, "slot", Color.white, Art.Icon(icon), 0.66f, "Got_" + icon); t.rectTransform.Place(0.5f, 1, new Vector2(x, -362), new Vector2(170, 170), new Vector2(0.5f, 1));
                t.gameObject.AddComponent<PopIn>();
                var a = UIKit.Label(p.win, amt, 40); a.rectTransform.Place(0.5f, 1, new Vector2(x, -540), new Vector2(320, 56), new Vector2(0.5f, 1));
                UIKit.Label(p.win, name, 26, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.Place(0.5f, 1, new Vector2(x, -592), new Vector2(320, 36), new Vector2(0.5f, 1));
            }
            Got(-160, "coin", "+" + Num.Res(gold), L.T("r_gold"));
            Got(160, "shard", "+" + sh, L.T("r_shards"));
            p.Btn("btn_orange", L.T("claim"), -200, 40, 340, () => { p.Close(); Claim(1); }, 120).name = "PatrolClaim";
            var ad = p.Btn("btn_purple", "", 200, 40, 340, () => Ads.Show(ok => { if (!ok) return; p.Close(); Claim(mul); }), 120);
            ad.name = "ClaimAd";
            var adi = UIKit.Img(ad.transform, Art.Icon("ad"), "Ad"); adi.raycastTarget = false; adi.rectTransform.Place(0, 0.5f, new Vector2(18, 6), new Vector2(64, 64), new Vector2(0, 0.5f));
            var al = UIKit.LabelOf(ad); if (al) { al.text = L.T("patrol_ad"); al.fontSize = Art.FontSize(32); al.rectTransform.Fill(84, 14, 14, 4); }
        }

        void Claim(int mul)
        {
            var (g, sh) = Meta.ClaimPatrol(mul);
            if (g > 0) { Fx.Ring(Vector2.zero, 1, Art.Gold); Toast.Show(L.F("patrol_got", $"{L.T("r_gold")} +{Num.F(g)}, {L.T("r_shards")} +{sh}") + (mul > 1 ? $" (x{mul})" : "")); }
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
            // 11-21 콘텐츠 확장 제안서 "타워는 일단 이미지만 추가": 곧 열리는 터렛 (그림 · 해금 예정 스테이지만, 누르면 설명 토스트 · Config Turret_Soon_Show)
            if (Tables.Ci("Turret_Soon_Show", 1) == 1 && Tables.TurretSoon.Count > 0)
            {
                var head = UIKit.Label(c, L.T("turret_soon_head"), 32, Art.Purple, TextAnchor.MiddleLeft, false); head.rectTransform.TL(20, y + 6, 600, 52);
                head.name = "SoonHead";
                y += 64;
                foreach (var t in Tables.TurretSoon)
                {
                    var r = UIKit.Rect(c, "Soon_" + t.id); r.TL(10, y, 1000, h);
                    var tile = UIKit.Tile(r, "slot", new Color(0.75f, 0.72f, 0.68f), Art.TurretUI(t.id), 0.86f, "Icon"); tile.rectTransform.TL(0, 4, 108, 108);
                    var tt = t; UIKit.Clickable(tile, () => Toast.Show(tt.desc, 2.6f));
                    var lk = UIKit.Img(tile.transform, Art.Icon("lock"), "Lock"); lk.raycastTarget = false; lk.rectTransform.Place(1, 0, new Vector2(4, -4), new Vector2(52, 52), new Vector2(1, 0));
                    var name = UIKit.Label(r, t.name, 34, Art.Soft, TextAnchor.MiddleLeft, false); name.rectTransform.TL(126, 4, 330, 50);
                    UIKit.ElemTag(r, t.elem, 126 + Mathf.Min(330, name.preferredWidth + 14), 10, 104, 42, 22);
                    UIKit.Label(r, L.F("turret_soon_at", t.unlock), 28, Art.Brown, TextAnchor.MiddleLeft, false).rectTransform.TL(126, 60, 700, 44);
                    Divider(c, y + h + 3);
                    y += h + 8;
                }
            }
            UIKit.SetScrollH(c, y + 10);
        }

        /// <summary>강화 창 (2차 수정안 "터렛 강화할 때 어느 능력치가 올라가는지 알려주는 팝업"): 지금 → 다음 · 비용 · [강화] (창은 그대로 → 여러 번)</summary>
        public void UpPopup(TurretRow t)
        {
            var p = Popup.Make(t != null ? t.name : L.T("base"), 860, 680, Refresh);
            void Build()
            {
                for (int i = p.win.childCount - 1; i >= 0; i--) { var ch = p.win.GetChild(i); if (ch.name.StartsWith("U_")) Destroy(ch.gameObject); }
                var root = UIKit.Rect(p.win, "U_Root"); root.Fill();
                var tile = UIKit.Tile(root, "slot", Color.white, t != null ? Art.TurretIcon(t) : Art.Icon("base"), t != null ? 0.86f : 0.7f, "Pic"); tile.rectTransform.TL(36, 116, 170, 170);
                // 4차 수정안 "레벨별 능력 정보를 보여주는 아이콘": (i) → 레벨별 표
                var inf = UIKit.Btn(root, "btn_sky", null, 26, () => LvTable(t), "U_Info"); ((RectTransform)inf.transform).TL(30, 294, 182, 76);
                var ii = UIKit.Img(inf.transform, Art.Icon("info"), "Icon"); ii.raycastTarget = false; ii.rectTransform.Place(0, 0.5f, new Vector2(8, 5), new Vector2(50, 50), new Vector2(0, 0.5f));
                UIKit.Label(inf.transform, L.T("lv_table"), 26).rectTransform.Fill(56, 12, 4, 2);
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
                    if (nx > 0 && !max) UIKit.Label(root, L.F("up_next_cd", nx), 26, Art.Purple, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 420, 860, 40);
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
                    float y = 124 + i * 72;
                    var bg = UIKit.Panel(root, "card_white", "U_Row"); bg.rectTransform.TL(226, y, 600, 64);
                    UIKit.Label(bg.transform, rows[i].name, 30, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(18, 4, 200, 56);
                    UIKit.Label(bg.transform, rows[i].now, 34, Art.Ink, TextAnchor.MiddleRight, false).rectTransform.TL(200, 4, 170, 56);
                    UIKit.Label(bg.transform, ">", 34, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(372, 4, 50, 56);
                    UIKit.Label(bg.transform, rows[i].next, 36, Art.Green2, TextAnchor.MiddleLeft, false).rectTransform.TL(424, 4, 170, 56);
                }
                UIKit.Label(root, costTxt, 30, Art.Brown, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 372, 860, 50);
                var ub = UIKit.Btn(root, can ? "btn_green" : "btn_gray", max ? L.T("max") : L.T("enhance"), 44, () =>
                {
                    bool ok = t != null ? Meta.UpgradeTurret(t.id) : Meta.UpgradeBase();
                    if (ok) { Fx.Ring(Vector2.zero, 1, Art.Gold); Build(); Refresh(); }
                }, "U_Up");
                ((RectTransform)ub.transform).Place(0.5f, 0, new Vector2(0, 40), new Vector2(420, 116), new Vector2(0.5f, 0));
                ub.transform.SetParent(root, true);
            }
            Build();
        }

        /// <summary>레벨별 능력 표 (4차 수정안): 터렛 = 레벨 · 피해 · 공격 속도 / 기지 = 레벨 · 체력 · 공격력. 지금 레벨 줄은 주황 · 그 줄로 스크롤</summary>
        public static void LvTable(TurretRow t)
        {
            var p = Popup.Make((t != null ? t.name : L.T("base")) + " " + L.T("lv_table"), 860, 1400);
            string[] head = t != null ? new[] { "Lv", L.T("up_dmg"), L.T("up_cd") } : new[] { "Lv", L.T("up_hp"), L.T("up_atk") };
            var hd = UIKit.Panel(p.win, "btn_cream", "Head"); hd.rectTransform.TL(40, 112, 780, 64);
            for (int k = 0; k < 3; k++) UIKit.Label(hd.transform, head[k], 28, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.TL(k * 260, 2, 260, 54);
            var area = UIKit.Rect(p.win, "Area"); area.Fill(40, 30, 40, 190);
            var c = UIKit.Scroll(area, "Scroll"); c.parent.GetComponent<RectTransform>().Fill();
            int cur = t != null ? SaveData.Cur.TurretLv(t.id) : SaveData.Cur.baseLv;
            int max = t != null ? Tables.MaxTurretLv : Mathf.Min(Tables.Ci("Base_Max_Lv", 300), cur + 40);
            float y = 0, curY = 0;
            for (int lv = 1; lv <= max; lv++)
            {
                string a, b;
                if (t != null) { var r = Tables.Lv(lv); a = $"+{r.dmgBonus * 100:0}%"; b = $"x{1 / r.cdMul:0.00}"; }
                else { a = Num.Comma(Tables.C("Base_HP") + Tables.C("Base_HP_Per_Lv") * (lv - 1)); b = Num.F(Tables.C("Atk_Base") + Tables.C("Base_Atk_Per_Lv") * (lv - 1)); }
                bool now = lv == cur; if (now) curY = y;
                var row = UIKit.Plain(c, now ? Art.Hex("#ffd59a") : lv % 2 == 0 ? new Color(1, 1, 1, 0.55f) : new Color(1, 1, 1, 0), "L" + lv); row.rectTransform.TL(0, y, 780, 52);
                UIKit.Label(row.transform, now ? $"Lv {lv} ({L.T("lv_now")})" : $"Lv {lv}", 26, now ? Art.Ink : Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 0, 260, 52);
                UIKit.Label(row.transform, a, 28, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.TL(260, 0, 260, 52);
                UIKit.Label(row.transform, b, 28, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.TL(520, 0, 260, 52);
                y += 52;
            }
            UIKit.SetScrollH(c, y);
            c.anchoredPosition = new Vector2(0, Mathf.Max(0, curY - 300));
        }

        static void Divider(Transform c, float y)
        {
            var d = UIKit.Plain(c, new Color(0.23f, 0.16f, 0.11f, 0.18f), "Divider"); d.rectTransform.TL(20, y, 980, 3);
        }

        /// <summary>터렛 설명 창 (11-16 저녁 "터렛 그림을 누르면 어떤 방식으로 공격하는지") —
        /// 요청 사항_05 "칸으로 나누기 · 설명을 그림 아래로": 위 = 그림 · 이름 · 속성 · Lv / 아래 = [능력] [설명] [돌파] 칸</summary>
        public static void TurretInfo(TurretRow t)
        {
            var lines = new List<string>();
            foreach (var c in Tables.Cards)
                if (c.turret == t.id && c.kind == "break") lines.Add($"{L.T("kind_break")} {c.line}: {c.name} - {c.desc}");
            var p = Popup.Make(t.name, 880, 300 + 132 + 182 + (lines.Count > 0 ? 172 : 0) + 150);   // 칸 수에 맞춘 높이
            var tile = UIKit.Tile(p.win, "slot", Color.white, Art.TurretIcon(t), 0.86f, "Pic"); tile.rectTransform.TL(40, 112, 170, 170);
            var nm = UIKit.Label(p.win, t.name, 44, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(236, 120, 600, 60);
            UIKit.ElemTag(p.win, t.elem, 236, 192, 122, 50, 26);
            bool open = SaveData.Cur.Unlocked(t);
            var lv = UIKit.Label(p.win, open ? L.F("lv", SaveData.Cur.TurretLv(t.id)) : L.F("unlock_at", t.unlock), 28, open ? Art.Brown : Art.Red, TextAnchor.MiddleLeft, false);
            lv.rectTransform.TL(374, 192, 460, 50);
            float y = 300;
            Section(p.win, ref y, L.T("ti_stat"), t.kind == "field" ? L.F("tinfo_stat_field", Num.F(t.rate), t.cd) : L.F("tinfo_stat", Num.F(t.rate + t.rate2), t.cd, t.range), Art.Ink, 28, 70);
            Section(p.win, ref y, L.T("ti_desc"), string.IsNullOrEmpty(t.info) ? t.desc : t.info, Art.Ink, 28, 120);
            if (lines.Count > 0) Section(p.win, ref y, L.T("ti_break"), string.Join("\n", lines), Art.Hex("#6a4fa8"), 26, 110);
            p.Btn("btn_green", L.T("ok"), 0, 36, 320, p.Close, 96);
        }

        /// <summary>정보 칸: 왼쪽 위 작은 제목 띠 + 흰 칸 안의 글</summary>
        static void Section(RectTransform w, ref float y, string title, string body, Color col, int size, float h)
        {
            var box = UIKit.Panel(w, "card_white", "Sec_" + title); box.rectTransform.TL(36, y + 18, 808, h + 30);
            var tg = UIKit.Panel(w, "pill", "SecTag"); tg.color = Art.Hex("#ffd59a"); tg.rectTransform.TL(56, y, 150, 42);
            UIKit.Label(tg.transform, title, 24, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 3, 0, 0);
            var t = UIKit.Label(box.transform, body, size, col, TextAnchor.MiddleLeft, false); t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.rectTransform.Fill(22, 8, 22, 22);
            y += h + 62;
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
            // 6차 수정안: 금화 · 다이아는 100만부터 K (Num.Res) · 칸보다 길면 글자를 줄여 칸 안에 (UIKit.Fit)
            UIKit.Fit(goldT, Num.Res(S.gold), 40);
            UIKit.Fit(diaT, Num.Res(S.dia), 38);
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
            bool can = Meta.CanClaimPatrol;
            UIKit.SetKind(patrolB, can ? "btn_orange" : "btn_gray");
            patrolDot.enabled = can;
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
                if (!string.IsNullOrEmpty(st.weak)) weak.Add(st.weak);      // 밸런스 2차: 스테이지 약점을 맨 앞에
                foreach (var id in st.enemies) if (Tables.Enemies.TryGetValue(id, out var e) && !weak.Contains(e.weak)) weak.Add(e.weak);
                for (int c = weakRow.childCount - 1; c >= 0; c--) Destroy(weakRow.GetChild(c).gameObject);
                for (int i = 0; i < weak.Count; i++) UIKit.ElemTag(weakRow, weak[i], i * 150, 2, 138, 56, 30);
                for (int c = enemyRow.childCount - 1; c >= 0; c--) Destroy(enemyRow.GetChild(c).gameObject);
                int ne = Mathf.Min(8, st.enemies.Length);
                float sz = ne > 7 ? 102 : 110;
                for (int i = 0; i < ne; i++) if (Tables.Enemies.TryGetValue(st.enemies[i], out var e2)) EnemyTile(enemyRow, e2, i * (sz + 12), sz);
                weakT.text = string.IsNullOrEmpty(st.weak) ? L.T("weak_hint") : L.F("stage_weak_hint", Elem.Name(st.weak), Tables.C("Weak_Mul", 1.5).ToString("0.#"));
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
            if (Meta.AttendReady && Tables.Attend.Count > 0)
            {
                if (Tables.Ci("Attend_Auto", 1) != 1) { MissionTab.ShowAttend(Intro); return; }
                var got = Meta.AttendClaim();   // 4차 수정안 "출석 보상 획득은 토스트 메시지로"
                if (got != null) { Toast.Show(L.F("attend_toast", (S.attendN - 1) % Meta.AttendDays + 1, got), 2.6f); Refresh(); }
            }
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
