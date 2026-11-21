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
        Text stageT, timeT, lvT, hpT, speedT, banner, shieldT;
        Image expFill, chargeFill, hpFill, shieldFill;
        RectTransform routeShip, routeDone; float routeX0, routeW;
        RectTransform hpBox, chargeBox, bannerBox;
        // 11-21 특수 능력 단추 (왼쪽 아래 = 대장 카피 버닝 · 오른쪽 아래 = 돔 얼음 파동)
        RectTransform burnBox, domeBox; Image burnCdImg, domeCdImg, burnFace, domeFace; Text burnCdT, domeCdT; int lastBurnSec = -9, lastDomeSec = -9;
        CanvasGroup bannerGroup;
        float bannerT;
        public Popup pick;
        RectTransform root;

        public void Build(Battle battle)
        {
            b = battle; root = (RectTransform)transform;
            // 끌기 영역 (맨 뒤, 투명)
            var drag = gameObject.AddComponent<Image>(); drag.color = new Color(0, 0, 0, 0); drag.raycastTarget = true;

            // 11-18 전투 배치 2판: 위 줄 = ② 카피 방주의 항로 막대 (사용자 "방주 UI 줄 사용") — 일시정지 · 항로(웨이브 점 → 지금 위치 → 보스 행성) · 시계 · 카드 / 레벨 배지 · 경험치 · 배속
            var pause = UIKit.Tile(root, "btn_cream", Color.white, Art.Icon("pause"), 0.5f, "Pause"); pause.rectTransform.TL(28, 34, 104, 104);
            UIKit.Clickable(pause, ShowPause);
            var cards = UIKit.Tile(root, "btn_cream", Color.white, Art.Icon("cards"), 0.6f, "Cards"); cards.rectTransform.TL(948, 34, 104, 104);
            UIKit.Clickable(cards, ShowCards);
            var mid = UIKit.Panel(root, "pill_dark", "Stage"); mid.rectTransform.TL(150, 30, 780, 160); mid.color = new Color(1, 1, 1, 0.94f);
            string stTitle = b.mode == "stage" ? L.F("stage_n", b.stageNo) : L.T("mode_" + b.mode);
            stageT = UIKit.Label(mid.transform, stTitle, 42, b.eliteMode || b.mode != "stage" ? Art.Hex("#d9c6ff") : Color.white, TextAnchor.MiddleLeft); stageT.rectTransform.TL(54, 14, 480, 66);
            if (b.eliteMode) { var cr = UIKit.Img(mid.transform, Art.Icon("crown"), "Elite"); cr.raycastTarget = false; cr.rectTransform.TL(-6, -14, 62, 62); }
            // 8차 수정안 "전투 화면에서도 적의 약점을 표현 (스테이지 1 옆에 텍스트로)": "스테이지 10  약점 불" — 약점 이름은 그 속성 색
            var stRow = b.mode == "stage" ? Tables.Stage(b.stageNo) : null;
            if (stRow != null && !string.IsNullOrEmpty(stRow.weak))
            {
                float x = 54 + stageT.preferredWidth + 18;
                var wl = UIKit.Label(mid.transform, L.T("battle_weak"), 28, Art.Cream, TextAnchor.MiddleLeft, false); wl.name = "WeakLabel";
                wl.rectTransform.TL(x, 22, 80, 54);
                var wv = UIKit.Label(mid.transform, Elem.Name(stRow.weak), 34, Color.Lerp(Elem.Color(stRow.weak), Color.white, 0.35f), TextAnchor.MiddleLeft); wv.name = "Weak";
                wv.rectTransform.TL(x + wl.preferredWidth + 8, 16, 170, 62);
            }
            timeT = UIKit.Label(mid.transform, "", 34, Art.Gold, TextAnchor.MiddleRight, false); timeT.rectTransform.TL(480, 18, 246, 56); timeT.name = "Clock";
            Route(mid.rectTransform);
            expFill = UIKit.Bar(root, Art.Sky, "Exp"); expFill.transform.parent.GetComponent<RectTransform>().TL(150, 212, 780, 44);
            var lvB = UIKit.Panel(root, "pill", "LvBadge"); lvB.color = Art.Purple; lvB.rectTransform.TL(34, 180, 108, 108);
            lvT = UIKit.Label(lvB.transform, "", 34); lvT.rectTransform.Fill(0, 6, 0, 0);
            var sp = UIKit.Btn(root, "btn_cream", null, 34, ToggleSpeed, "Speed"); ((RectTransform)sp.transform).TL(948, 192, 104, 84);
            speedT = UIKit.Label(sp.transform, SpeedText(b.speed), 30, Art.Ink, TextAnchor.MiddleCenter, false); speedT.rectTransform.Fill(0, 8, 0, 0);
            if (Battle.SkillOn && b.lab == null)
            {
                burnBox = SkillButton("SkillBurn", "skill_burn", L.T("skill_burn"), () => { if (b.UseBurn()) Toast.Show(L.T("skill_burn_on"), 1.2f); }, out burnFace, out burnCdImg, out burnCdT);
                domeBox = SkillButton("SkillDome", "skill_frost", L.T("skill_dome"), () => { if (b.UseDome()) Toast.Show(L.T("skill_dome_on"), 1.2f); }, out domeFace, out domeCdImg, out domeCdT);
            }

            // 기지 체력: 사령탑 앞 달 바닥의 이름판 (② 방주처럼 기지에 붙은 판 — 11-18)
            hpBox = UIKit.Rect(root, "BaseHp"); hpBox.sizeDelta = new Vector2(290, 92); hpBox.anchorMin = hpBox.anchorMax = new Vector2(0.5f, 0.5f);
            var hpBg = UIKit.Panel(hpBox, "card_white", "Plate"); hpBg.rectTransform.Fill();
            var heart = UIKit.Img(hpBox, Art.Icon("heart"), "Heart"); heart.raycastTarget = false; heart.rectTransform.TL(14, 12, 64, 64);
            hpT = UIKit.Label(hpBox, "", 34, Art.Ink, TextAnchor.MiddleCenter, false); hpT.rectTransform.TL(82, 4, 196, 42);
            hpFill = UIKit.Bar(hpBox, Art.Green, "HpBar"); hpFill.transform.parent.GetComponent<RectTransform>().TL(82, 48, 192, 30);
            shieldFill = UIKit.Bar(hpFill.transform.parent, Art.Sky, "Shield"); var shTrack = shieldFill.transform.parent.GetComponent<Image>(); if (shTrack) shTrack.color = new Color(1, 1, 1, 0);
            shieldFill.transform.parent.GetComponent<RectTransform>().Fill();
            shieldT = UIKit.Label(hpBox, "", 26, Art.Sky, TextAnchor.MiddleLeft); shieldT.rectTransform.TL(296, 46, 160, 36);
            // 충전 게이지 (가디언 아래)
            chargeFill = UIKit.Bar(root, Art.Gold, "Charge");
            chargeBox = chargeFill.transform.parent.GetComponent<RectTransform>();
            chargeBox.anchorMin = chargeBox.anchorMax = new Vector2(0.5f, 0.5f); chargeBox.sizeDelta = new Vector2(150, 26);
            // 가운데 알림
            // 2차 수정안: 가운데 네모 판 대신 위에서 내려오는 토스트 (경고 아이콘 + 글자, 2초 뒤 사라짐)
            var bp = UIKit.Panel(root, "pill_dark", "Banner"); bannerBox = bp.rectTransform; bp.color = new Color(0.55f, 0.24f, 0.2f, 0.92f);
            bannerBox.Place(0.5f, 1, new Vector2(0, -250), new Vector2(560, 92), new Vector2(0.5f, 0.5f));
            var wi = UIKit.Img(bannerBox, Art.Icon("skull"), "Warn"); wi.raycastTarget = false; wi.rectTransform.Place(0, 0.5f, new Vector2(24, 0), new Vector2(64, 64), new Vector2(0, 0.5f));
            banner = UIKit.Label(bannerBox, "", 40, Art.Gold); banner.rectTransform.Fill(80, 6, 20, 0);
            bannerGroup = bannerBox.gameObject.AddComponent<CanvasGroup>();
            bannerBox.gameObject.SetActive(false);

            b.Changed += OnChanged;
            b.Banner += s => { banner.text = s; bannerBox.gameObject.SetActive(true); bannerT = 2.2f; };
            Refresh();
            // 2차 도움말: 처음 전투 = 끌어서 이동 (손가락이 좌우로) — 11-18 대장 카피 고정이면 안 띄움
            if (Tables.Ci("Guardian_Fixed", 1) != 1) Tutorial.Show("t_move", L.T("tut_move"), null, 5f, true, 0.42f);
        }

        /// <summary>항로 막대 (② 방주): 지난 길 주황 · 남은 길 남색 · 웨이브마다 점 · 중간 보스 왕관 · 끝 = 보스 행성(해골)</summary>
        void Route(RectTransform mid)
        {
            routeX0 = 56; routeW = 600; float y = 112;
            var back = UIKit.Plain(mid, Art.Hex("#56609a"), "RouteBack"); back.rectTransform.TL(routeX0, y - 4, routeW, 8);
            var done = UIKit.Plain(mid, Art.Orange, "RouteDone"); routeDone = done.rectTransform; routeDone.TL(routeX0, y - 4, 0, 8);
            float end = RouteEnd;
            if (b.mode == "stage")
            {
                for (float w = Tables.Cf("Wave_Len", 30); w < end - 1; w += Tables.Cf("Wave_Len", 30))
                {
                    var dot = UIKit.Img(mid, Art.Fx("dot"), "Wave"); dot.raycastTarget = false; dot.color = Art.Cream;
                    dot.rectTransform.Place(0, 1, new Vector2(routeX0 + routeW * w / end, -y), new Vector2(22, 22), new Vector2(0.5f, 0.5f));
                }
                var cr = UIKit.Img(mid, Art.Icon("crown"), "EliteMark"); cr.raycastTarget = false;
                cr.rectTransform.Place(0, 1, new Vector2(routeX0 + routeW * Tables.Cf("Mid_Boss_Time", 120) / end, -y), new Vector2(40, 40), new Vector2(0.5f, 0.5f));
            }
            var pl = UIKit.Img(mid, Art.Fx("dot"), "BossPlanet"); pl.raycastTarget = false; pl.color = Art.Hex("#f6a2b6");
            pl.rectTransform.Place(0, 1, new Vector2(routeX0 + routeW + 34, -y), new Vector2(54, 54), new Vector2(0.5f, 0.5f));
            var sk = UIKit.Img(mid, Art.Icon("skull"), "BossSkull"); sk.raycastTarget = false;
            sk.rectTransform.Place(0, 1, new Vector2(routeX0 + routeW + 34, -y), new Vector2(40, 40), new Vector2(0.5f, 0.5f));
            var ship = UIKit.Img(mid, Art.Icon("play"), "Ship"); ship.raycastTarget = false; ship.color = Art.Cream;
            routeShip = ship.rectTransform; routeShip.Place(0, 1, new Vector2(routeX0, -y), new Vector2(46, 46), new Vector2(0.5f, 0.5f));
        }
        /// <summary>항로 끝 시각: 스테이지 = 보스 등장 · 엘리트 군단 = 버티는 시간 · 무한 = 다음 보스</summary>
        float RouteEnd => b.mode == "stage" ? Tables.Cf("Boss_Time", 210) : b.mode == "survival" ? b.RunTime : Tables.Cf("Endless_Boss_Gap", 150);
        float RouteK => b.mode == "endless" ? Mathf.Repeat(b.time, RouteEnd) / RouteEnd : Mathf.Clamp01(b.time / RouteEnd);

        void OnDestroy() { if (b) b.Changed -= OnChanged; }

        // ---------- 입력: 끌어서 가디언 이동 ----------
        public void OnPointerDown(PointerEventData e) => MoveTo(e.position);
        public void OnDrag(PointerEventData e) => MoveTo(e.position);
        void MoveTo(Vector2 screen)
        {
            if (b == null || b.st != Battle.St.Play || b.Guardian == null || Tables.Ci("Guardian_Fixed", 1) == 1) return;   // 11-18 대장 카피 고정
            if (Tutorial.Cur && Tutorial.Cur.name == "Tutorial_t_move") Tutorial.CloseCur();
            b.Guardian.targetX = World.I.ScreenToWorld(screen).x;
        }

        // 11-21 요청 "속도 0.5배 / 2배까지 추가": 누를 때마다 x1 → x1.5 → x2 → x0.5 → x1 (고른 속도는 앱을 끌 때까지 다음 전투에도)
        static readonly float[] Speeds = { 0.5f, 1f, 1.5f, 2f };
        static string SpeedText(float v) => "x" + v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        public void ToggleSpeed()
        {
            int i = System.Array.FindIndex(Speeds, v => Mathf.Abs(v - b.speed) < 0.01f);
            b.speed = Speeds[(i + 1) % Speeds.Length];
            Battle.LastSpeed = b.speed;
            speedT.text = SpeedText(b.speed);
        }

        /// <summary>특수 능력 단추: 둥근 판 + 아이콘 + 이름 + 쿨타임(위에서 시계 방향으로 줄어드는 어두운 원 + 남은 초)</summary>
        RectTransform SkillButton(string name, string icon, string label, System.Action use, out Image face, out Image cdImg, out Text cdT)
        {
            var box = UIKit.Rect(root, name); box.sizeDelta = new Vector2(150, 186); box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            var ring = UIKit.Img(box, Art.Fx("dot"), "Ring"); ring.color = Art.Ink; ring.rectTransform.TL(3, 3, 144, 144);
            face = UIKit.Img(box, Art.Fx("dot"), "Face"); face.color = Art.Cream; face.rectTransform.TL(9, 9, 132, 132);
            var ic = UIKit.Img(box, Art.Icon(icon), "Icon"); ic.raycastTarget = false; ic.rectTransform.TL(17, 15, 116, 116);
            cdImg = UIKit.Img(box, Art.Fx("dot"), "Cd"); cdImg.raycastTarget = false; cdImg.color = new Color(0.1f, 0.08f, 0.15f, 0.62f);
            cdImg.type = Image.Type.Filled; cdImg.fillMethod = Image.FillMethod.Radial360; cdImg.fillOrigin = (int)Image.Origin360.Top; cdImg.fillClockwise = false;
            cdImg.rectTransform.TL(9, 9, 132, 132);
            cdT = UIKit.Label(box, "", 44, Color.white); cdT.rectTransform.TL(0, 40, 150, 70); cdT.raycastTarget = false;
            var nm = UIKit.Label(box, label, 26, Color.white); nm.rectTransform.TL(-20, 146, 190, 40); nm.raycastTarget = false;
            UIKit.Clickable(face, use);
            return box;
        }

        void SkillRefresh()
        {
            if (burnBox == null) return;
            float x = Tables.Cf("Skill_Btn_X", 4.4f), y = World.GroundY + Tables.Cf("Skill_Btn_Y", 2.7f);
            burnBox.anchoredPosition = UIRoot.I.WorldToUI(new Vector2(-x, y));
            domeBox.anchoredPosition = UIRoot.I.WorldToUI(new Vector2(x, y));
            OneSkill(burnBox, burnFace, burnCdImg, burnCdT, b.skillCdG, b.BurnCdMax, b.CanBurn, b.Burning, ref lastBurnSec);
            OneSkill(domeBox, domeFace, domeCdImg, domeCdT, b.skillCdD, b.DomeCdMax, b.CanDome, false, ref lastDomeSec);
        }

        void OneSkill(RectTransform box, Image face, Image cdImg, Text cdT, float left, float max, bool ready, bool active, ref int lastSec)
        {
            cdImg.fillAmount = left > 0 ? Mathf.Clamp01(left / Mathf.Max(1f, max)) : 0f;
            int sec = left > 0 ? Mathf.CeilToInt(left) : 0;
            if (sec != lastSec) { lastSec = sec; cdT.text = sec > 0 ? sec.ToString() : ""; }
            // 쓸 수 있으면 금빛으로 숨쉬듯 · 쓰는 중(버닝)이면 주황
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            face.color = active ? Color.Lerp(Art.Hex("#ffb347"), Art.Hex("#ffe08a"), k) : ready ? Color.Lerp(Art.Cream, Art.Gold, 0.35f * k) : Art.Cream;
            box.localScale = Vector3.one * (ready ? 1f + 0.04f * k : 1f);
        }

        void Update()
        {
            if (b == null) return;
            Refresh();
            if (bannerT > 0)
            {
                bannerT -= Time.unscaledDeltaTime;
                float age = 2.2f - bannerT;
                float k = Mathf.Clamp01(age / 0.25f); k = 1 - (1 - k) * (1 - k);
                bannerBox.anchoredPosition = new Vector2(0, Mathf.Lerp(-150, -250, k));
                bannerGroup.alpha = Mathf.Clamp01(age / 0.15f) * Mathf.Clamp01(bannerT / 0.4f);
                if (bannerT <= 0) bannerBox.gameObject.SetActive(false);
            }
        }

        // 11-17 최적화: 매 프레임 시계 · 레벨 · 체력 글자를 새로 만들던 것 → 값이 바뀐 때만 (글자 만들기 = 메모리 할당)
        int lastSec = -1, lastLv = -1, lastCharge = -1; double lastHp = -1, lastShield = -1;
        void Refresh()
        {
            int sec = Mathf.Max(0, Mathf.CeilToInt(b.ClockSec));
            if (sec != lastSec)
            {
                lastSec = sec;
                timeT.text = b.mode == "endless" ? L.F("endless_time", Num.Clock(b.ClockSec)) : b.mode == "survival" ? L.F("survive_left", Num.Clock(b.ClockSec)) : Num.Clock(b.TimeLeft);
            }
            if (b.level != lastLv) { lastLv = b.level; lvT.text = b.level >= b.LevelMax ? "MAX" : "Lv\n" + b.level; lvT.lineSpacing = 0.8f; }
            UIKit.SetBar(expFill, b.level >= b.LevelMax ? 1 : (float)(b.exp / b.need));
            float rk = RouteK;
            routeShip.anchoredPosition = new Vector2(routeX0 + routeW * rk, routeShip.anchoredPosition.y);
            routeDone.sizeDelta = new Vector2(routeW * rk, routeDone.sizeDelta.y);
            double hp = System.Math.Ceiling(b.baseHp), sh = System.Math.Ceiling(b.shield);
            if (hp != lastHp) { lastHp = hp; hpT.text = Num.Comma(b.baseHp); UIKit.SetBar(hpFill, (float)(b.baseHp / System.Math.Max(1, b.baseMax))); }
            if (sh != lastShield) { lastShield = sh; shieldT.text = b.shield > 0 ? "+" + Num.Comma(b.shield) : ""; UIKit.SetBar(shieldFill, b.shieldMax > 0 ? (float)(b.shield / b.shieldMax) * 0.35f : 0f); }
            hpBox.anchoredPosition = UIRoot.I.WorldToUI(World.V ? new Vector2(0f, World.GroundY - 1.25f) : new Vector2(Tables.Cf("Hp_Plate_X", -3.6f), World.GroundY - 1.0f));
            SkillRefresh();
            var g = b.Guardian;
            bool showCharge = g != null && g.t.Has("charge");
            if ((showCharge ? 1 : 0) != lastCharge) { lastCharge = showCharge ? 1 : 0; chargeBox.gameObject.SetActive(showCharge); }
            if (showCharge)
            {
                chargeBox.anchoredPosition = UIRoot.I.WorldToUI(new Vector2(g.transform.position.x, g.transform.position.y + 1.75f));   // 11-18: 사령실 유리창 띠와 헷갈려 머리 위로
                chargeFill.color = g.Charging ? Art.Sky : Art.Gold;
                UIKit.SetBar(chargeFill, g.ChargeK);
            }
        }

        void OnChanged()
        {
            if (b.st == Battle.St.Play)
                foreach (var t in b.run.turrets)
                    if (t.stars >= Tables.Ci("Stars_Per_Tier", 4) && t.tier < Tables.Ci("Tier_Max", 3)) { Tutorial.Show("t_star", L.T("tut_star"), null, 4f, false, 0.62f); break; }
            if (b.st == Battle.St.Pick) ShowPick();
            else if (pick != null) { pick.Close(false); pick = null; }
            if (b.st == Battle.St.Slot) ShowSlotPick();
            else if (slotUI) { Destroy(slotUI.gameObject); slotUI = null; }
            if (b.st == Battle.St.End) ShowResult();
        }

        // ---------- 11-18 자리 고르기 (사용자 "터렛을 선택할 때 자리를 선택하는 흐름이 나오며 그 자리에 배치") ----------
        // 카드 창이 닫히고 사령탑의 빈 칸이 반짝 + 칸마다 고른 터렛 그림(흐리게) 단추 + 안내
        // 7차 수정안: 안내는 화면 가운데 크게 ("최대한 잘 보이게", 칸 줄 오른쪽에 — 칸을 가리지 않게) · [카드 다시 고르기] 없음 ("이전에 새로 고침이 있음")
        RectTransform slotUI;
        public void ShowSlotPick()
        {
            if (slotUI) Destroy(slotUI.gameObject);
            Tutorial.CloseCur();      // 카드 도움말이 칸 단추를 가리지 않게
            slotUI = UIKit.Rect(UIRoot.I.popups, "SlotPick").Fill();
            var dim = UIKit.Plain(slotUI, new Color(0.1f, 0.12f, 0.25f, 0.45f), "Dim"); dim.rectTransform.Fill(); dim.raycastTarget = true;
            var turret = Tables.TurretById[b.slotCard.turret];
            var tip = UIKit.Panel(slotUI, "pill_dark", "Tip"); tip.rectTransform.Place(0.5f, 0.5f, new Vector2(Tables.Cf("Slot_Tip_X", 110f), 0), new Vector2(760, 250), new Vector2(0.5f, 0.5f));
            var glow = UIKit.Img(tip.transform, Art.UI("glow_frame"), "Glow"); glow.color = Art.Gold; glow.raycastTarget = false; glow.rectTransform.Fill(-22, -22, -22, -22);
            glow.gameObject.AddComponent<GlowPulse>();
            var ti = UIKit.Img(tip.transform, Art.TurretIcon(turret), "Icon"); ti.raycastTarget = false; ti.preserveAspect = true; ti.rectTransform.TL(26, 30, 190, 190);
            var nm = UIKit.Label(tip.transform, turret.name, 54, Art.Gold, TextAnchor.MiddleLeft); nm.rectTransform.TL(234, 34, 500, 80);
            var ask = UIKit.Label(tip.transform, L.T("slot_pick2"), 44, Color.white, TextAnchor.MiddleLeft); ask.rectTransform.TL(234, 126, 510, 80); ask.name = "Ask";
            // 칸 쪽(왼쪽)을 가리키는 화살표
            var arrow = UIKit.Img(slotUI, Art.Icon("play"), "Arrow"); arrow.raycastTarget = false; arrow.color = Art.Gold;
            arrow.rectTransform.Place(0.5f, 0.5f, new Vector2(Tables.Cf("Slot_Tip_X", 110f) - 380 - 44, 0), new Vector2(70, 70), new Vector2(0.5f, 0.5f));
            arrow.rectTransform.localScale = new Vector3(-1, 1, 1);
            tip.gameObject.AddComponent<SlotTipBob>().arrow = arrow.rectTransform;
            foreach (int s in b.run.FreeSlotList)
            {
                int slot = s;
                var at = UIRoot.I.WorldToUI(World.SlotPos(slot) + new Vector2(0, 0.85f));
                var bt = UIKit.Tile(slotUI, "slot", Color.white, Art.TurretIcon(turret), 0.78f, "Slot" + slot);
                bt.rectTransform.anchorMin = bt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); bt.rectTransform.sizeDelta = new Vector2(170, 170);
                bt.rectTransform.anchoredPosition = at;
                var ic = bt.transform.Find("Icon")?.GetComponent<Image>(); if (ic) ic.color = new Color(1, 1, 1, 0.6f);
                var gl = UIKit.Img(bt.transform, Art.UI("glow_frame"), "Glow"); gl.color = Art.Gold; gl.raycastTarget = false; gl.rectTransform.Fill(-24, -24, -24, -24);
                gl.gameObject.AddComponent<GlowPulse>();
                var plus = UIKit.Img(bt.transform, Art.Icon("plus"), "Plus"); plus.raycastTarget = false; plus.rectTransform.Place(1, 1, new Vector2(10, 10), new Vector2(54, 54), new Vector2(1, 1));
                UIKit.Clickable(bt, () => b.PlaceAt(slot));
            }
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
                var tile = UIKit.Tile(w, "slot", Color.white, Art.TurretIcon(t.row), 0.86f, "Slot" + i); tile.rectTransform.TL(x, 214, 150, 150);
                Stars(w, x + (150 - StarsW(17)) / 2, 380, t.tier, t.stars, 17);
            }
            // 카드 3장
            // 칩 "첫 카드 N장 더" — 4장이면 조금 좁게
            for (int i = 0; i < b.offer.Count; i++) Card(w, i, b.offer[i], b.offer.Count);
            if (Tutorial.Want("t_card")) Tutorial.Show("t_card", L.T("tut_card"), null, 5f, false, 0.86f);
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

        void Card(RectTransform w, int i, CardRow c, int count = 3)
        {
            float cw = count > 3 ? 226 : 290, gap = count > 3 ? 14 : 30;
            float x = (1000 - (count * cw + (count - 1) * gap)) / 2 + i * (cw + gap), y = 476, ch = 660;
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
            var im = UIKit.Img(pic.transform, Art.TurretIcon(turret), "Pic"); im.raycastTarget = false; im.rectTransform.Fill(24, 20, 24, 14);
            var sub = UIKit.Label(card.transform, turret.name + ", " + Elem.Name(turret.elem), 26, Art.Soft, TextAnchor.MiddleCenter, false); sub.rectTransform.TL(0, 362, cw, 44);
            var desc = UIKit.Label(card.transform, c.desc, 30, Art.Ink, TextAnchor.UpperCenter, false);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap; desc.rectTransform.TL(16, 420, cw - 32, 140);
            // 고르면 어떻게 되나: 별 · 단계
            var t = b.run.Get(c.turret);
            if (c.kind == "break" && t != null)
            {   // 요청 사항_05 "돌파 가능 시 테두리 붉은색 효과": 붉은 빛 테두리가 숨쉬듯
                var gl = UIKit.Img(card.transform, Art.UI("glow_frame"), "BreakGlow"); gl.color = Art.Red; gl.raycastTarget = false; gl.rectTransform.Fill(-26, -26, -26, -26);
                gl.gameObject.AddComponent<GlowPulse>();
                Badge(card.transform, cw / 2 - 66, 600, t.tier); Badge(card.transform, cw / 2 + 66, 600, t.tier + 1, true);
                var arrow = UIKit.Label(card.transform, ">", 40, Art.Red, TextAnchor.MiddleCenter, false); arrow.rectTransform.TL(cw / 2 - 22, 576, 44, 50);
            }
            else if (c.kind == "combo")
            {
                var pt = UIKit.Label(card.transform, "+ " + Tables.TurretById[c.partner].name, 26, Art.Red, TextAnchor.MiddleCenter, false); pt.rectTransform.TL(0, 580, cw, 44);
            }
            else
            {
                int tier = t?.tier ?? 1, st = t?.stars ?? 0, after = Mathf.Min(Tables.Ci("Stars_Per_Tier", 4), st + 1);
                int ss = cw < 250 ? 20 : 26;   // 카드 4장(첫 카드 칩)이면 별을 작게 — 카드 밖으로 안 나가게
                Stars(card.transform, (cw - StarsW(ss)) / 2, 600, tier, after, ss, st < after ? after - 1 : -1);
            }
        }

        static void Badge(Transform p, float cx, float y, int n, bool next = false)
        {
            var c = UIKit.Panel(p, "badge", "Tier"); c.color = n >= 2 ? Art.Hex("#f08a7c") : Art.Hex("#c9b4f0");   // 요청 사항_05: 직사각형 · 돌파 단계는 붉은색
            c.rectTransform.TL(cx - 44, y - 24, 88, 48);
            UIKit.Label(c.transform, "Lv." + n, 28).rectTransform.Fill(0, 2, 0, 0);
        }

        /// <summary>단계 + 별 칸 너비</summary>
        public static float StarsW(int size) => size * 2.6f + 6 + Tables.Ci("Stars_Per_Tier", 4) * size * 1.5f;

        /// <summary>단계 "Lv.N" + 보석 4칸 (hl = 새로 켜질 보석) — 11-16 밤 "등급과 다이아 모양이 아쉬움 · 더 크게 · Lv.1 ◆◇◇◇ 강조"</summary>
        public static void Stars(Transform p, float x, float y, int tier, int stars, int size, int hl = -1)
        {
            // 요청 사항_05: Lv 표시는 직사각형 (글자가 가운데에) · 돌파한 단계(Lv 2~)는 붉은색 · 마지막 보석 칸(다 차면 돌파)은 붉은 보석
            var c = UIKit.Panel(p, "badge", "Tier"); c.color = tier >= 2 ? Art.Hex("#f08a7c") : Art.Hex("#c9b4f0"); c.rectTransform.TL(x, y - size * 0.8f, size * 2.6f, size * 1.6f);
            c.pixelsPerUnitMultiplier = Mathf.Max(1f, 44f / (size * 1.6f));
            UIKit.Label(c.transform, "Lv." + tier, Mathf.RoundToInt(size * 1.0f)).rectTransform.Fill(0, 2, 0, 0);
            int n = Tables.Ci("Stars_Per_Tier", 4);
            for (int i = 0; i < n; i++)
            {
                float sx = x + size * 2.6f + 6 + i * (size * 1.5f);
                bool last = i == n - 1;
                var s = UIKit.Img(p, Art.Icon(i < stars ? (last ? "gem_red_on" : "gem_on") : (last ? "gem_red_off" : "gem_off")), "Star" + i); s.raycastTarget = false;
                s.rectTransform.TL(sx, y - size * 0.85f, size * 1.6f, size * 1.7f);
                if (i == hl)
                {
                    var ring = UIKit.Img(p, Art.Fx("ring"), "New"); ring.color = Art.Green2; ring.raycastTarget = false;
                    ring.rectTransform.TL(sx - size * 0.35f, y - size * 1.2f, size * 2.3f, size * 2.4f);
                }
            }
        }

        // ---------- 일시정지 · 고른 카드 ----------
        public void ShowPause()
        {
            if (b.st != Battle.St.Play) return;
            b.Pause(true);
            var p = Popup.Make(L.T("pause"), 760, 520, () => b.Pause(false));
            p.Btn("btn_green", L.T("resume"), 0, 250, 460, () => p.Close());
            p.Btn("btn_red", L.T("give_up"), 0, 100, 460, () => { p.Close(false); b.End(false); });
        }

        public void ShowCards()
        {
            if (b.st != Battle.St.Play) return;
            b.Pause(true);
            var p = Popup.Make(L.T("cards_title"), 960, 1300, () => b.Pause(false));
            // 11-16 밤 "고른 카드: 단계 · 보석을 크게 · 터렛마다 구분선"
            float y = 128;
            foreach (var t in b.run.turrets)
            {
                var tile = UIKit.Tile(p.win, "slot", Color.white, Art.TurretIcon(t.row), 0.86f, "T"); tile.rectTransform.TL(40, y, 124, 124);
                var tt = t; UIKit.Clickable(tile, () => CardList(tt));
                var nm = UIKit.Label(p.win, t.row.name, 38, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(184, y, 380, 56);
                Stars(p.win, 920 - StarsW(32), y + 34, t.tier, t.stars, 32);
                var dv = UIKit.Plain(p.win, new Color(0.23f, 0.16f, 0.11f, 0.2f), "Divider"); dv.rectTransform.TL(40, y + 138, 880, 3);
                var names = new List<string>();
                foreach (var kv in t.picks) if (Tables.CardById.TryGetValue(kv.Key, out var c)) names.Add(kv.Value > 1 ? $"{c.name} x{kv.Value}" : c.name);
                var d = UIKit.Label(p.win, names.Count > 0 ? string.Join(", ", names) : "-", 24, Art.Soft, TextAnchor.UpperLeft, false);
                d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(184, y + 62, 736, 70);
                y += 148;
            }
        }

        /// <summary>고른 카드 목록 (2차 수정안 "고른 카드에서 이미지를 누르면 먹은 카드를 리스트로")</summary>
        public void CardList(TurretInst t)
        {
            var list = new List<(CardRow c, int n)>();
            foreach (var kv in t.picks) if (Tables.CardById.TryGetValue(kv.Key, out var c)) list.Add((c, kv.Value));
            if (t.slot > 0 && Tables.CardById.TryGetValue("new_" + t.row.id, out var nc)) list.Insert(0, (nc, 1));
            var p = Popup.Make(t.row.name, 940, Mathf.Clamp(300 + list.Count * 120, 520, 1500));
            p.Text(L.F("picked_cards", list.Count), 116, 30, Art.Soft);
            var area = UIKit.Rect(p.win, "Area"); area.Fill(24, 130, 24, 170);
            var c0 = UIKit.Scroll(area, "Scroll"); c0.parent.GetComponent<RectTransform>().Fill();
            float y = 4;
            foreach (var (c, n) in list)
            {
                var tag = KindTag[c.kind];
                var r = UIKit.Panel(c0, "card_white", "C_" + c.id); r.rectTransform.TL(0, y, 892, 108);
                var tg = UIKit.Panel(r.transform, "pill", "Kind"); tg.color = tag.col; tg.rectTransform.TL(16, 30, 118, 48);
                UIKit.Label(tg.transform, L.T(tag.label), 24).rectTransform.Fill(0, 3, 0, 0);
                UIKit.Label(r.transform, c.name + (n > 1 ? "  " + L.F("kind_times", n) : ""), 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(150, 8, 720, 48);
                var d = UIKit.Label(r.transform, c.desc, 24, Art.Soft, TextAnchor.MiddleLeft, false); d.rectTransform.TL(150, 54, 720, 44);
                y += 116;
            }
            UIKit.SetScrollH(c0, y + 8);
            p.Btn("btn_green", L.T("ok"), 0, 40, 300, p.Close, 100);
        }

        // ---------- 결과 ----------
        void ShowResult()
        {
            Popup.CloseAll();
            var s = SaveData.Cur;
            string title = b.mode == "endless" ? L.T("mode_endless") : b.mode == "survival" && b.won ? L.T("survival_win") : b.won ? L.T("win") : L.T("lose");
            var p = Popup.Make(title, 900, b.drops.Count > 0 ? 1300 : 1100, null, false);
            var ic = UIKit.Img(p.win, Art.Icon(b.won ? "trophy" : "skull"), "Icon"); ic.rectTransform.Place(0.5f, 1, new Vector2(0, -130), new Vector2(150, 150), new Vector2(0.5f, 1));
            p.Text((b.mode == "endless" ? L.F("endless_time", Num.Clock(b.time)) : b.mode == "survival" ? L.T("mode_survival") : L.F("stage_n", b.stageNo)) + "   " + L.F("kills", b.kills) + "   " + L.F("lv", b.level), 300, 34, Art.Brown);
            if (b.FirstClear) p.Text(L.F("first_clear", Tables.Ci("First_Clear_Mul", 2)), 350, 30, Art.Green2);
            // 8차 수정안 "종료 및 성공 팝업창에서 재화 획득 경계선 필요": 보상 줄 둘레에 테두리 판 (다이아가 있으면 한 줄 더 길게)
            var box = UIKit.Panel(p.win, "card_white", "RewardBox"); box.raycastTarget = false;
            box.rectTransform.Place(0.5f, 1, new Vector2(0, -400), new Vector2(620, b.rewardDia > 0 ? 344 : 244), new Vector2(0.5f, 1));
            var coin = UIKit.Img(p.win, Art.Icon("coin"), "Coin"); coin.rectTransform.Place(0.5f, 1, new Vector2(-170, -420), new Vector2(100, 100), new Vector2(0.5f, 1));
            var g = UIKit.Label(p.win, "+" + Num.F(b.RewardGold), 60, Art.Gold); g.rectTransform.Place(0.5f, 1, new Vector2(50, -420), new Vector2(360, 100), new Vector2(0.5f, 1));
            var shard = UIKit.Img(p.win, Art.Icon("shard"), "Shard"); shard.rectTransform.Place(0.5f, 1, new Vector2(-170, -540), new Vector2(90, 90), new Vector2(0.5f, 1));
            var sh = UIKit.Label(p.win, "+" + b.RewardShards, 52, Art.Sky); sh.rectTransform.Place(0.5f, 1, new Vector2(50, -540), new Vector2(360, 90), new Vector2(0.5f, 1));
            if (b.drops.Count > 0)
            {
                float dy = b.rewardDia > 0 ? 764 : 664;      // 8차: 보상 판 아래 끝(644 · 744)보다 아래
                p.Text(L.T("drops"), dy, 32, Art.Brown);
                var dl = p.Text(string.Join(", ", b.drops), dy + 46, 28, Art.Ink, 150);
                dl.horizontalOverflow = HorizontalWrapMode.Wrap; dl.verticalOverflow = VerticalWrapMode.Overflow;
            }
            if (b.eliteMode) { var cr = UIKit.Img(p.win, Art.Icon("crown"), "Elite"); cr.rectTransform.Place(0.5f, 1, new Vector2(110, -120), new Vector2(80, 80), new Vector2(0.5f, 1)); }
            if (b.rewardDia > 0)
            {
                var dIc = UIKit.Img(p.win, Art.Icon("diamond"), "DiaIcon"); dIc.rectTransform.Place(0.5f, 1, new Vector2(-170, -646), new Vector2(84, 84), new Vector2(0.5f, 1));
                var dT = UIKit.Label(p.win, "+" + Num.F(b.rewardDia) + " " + L.T("dia"), 48, Art.Sky); dT.rectTransform.Place(0.5f, 1, new Vector2(50, -646), new Vector2(360, 90), new Vector2(0.5f, 1));
            }
            // 3차 [초안] 광고 보고 보상 2배 (한 판 1번)
            var ab = p.Btn(b.adDoubled ? "btn_gray" : "btn_purple", null, 0, 200, 600, () =>
            {
                if (b.adDoubled) return;
                Ads.Show(ok => { if (ok && b.AdDouble()) { Toast.Show(L.T("got_double")); ShowResult(); } });
            }, 100);
            var adI = UIKit.Img(ab.transform, Art.Icon("ad"), "Ad"); adI.rectTransform.Place(0, 0.5f, new Vector2(26, 6), new Vector2(66, 66), new Vector2(0, 0.5f));
            UIKit.Label(ab.transform, b.adDoubled ? L.T("got_double") : L.F("ad_double", Tables.Ci("Ad_Reward_Mul", 2)) + (Ads.Draft ? " [초안]" : ""), 32).rectTransform.Fill(100, 14, 10, 4);
            p.Btn("btn_orange", L.T("to_lobby"), -200, 70, 340, Game.ToLobby);
            int stage = b.stageNo; bool elite = b.eliteMode;
            if (b.mode != "stage")
            {   // 도전 모드: 다시 도전 (입장 남았을 때)
                string md = b.mode;
                p.Btn(Store.Tickets(md) > 0 ? "btn_green" : "btn_gray", L.T("ch_start") + $" {Store.Tickets(md)}/{Store.TicketMax(md)}", 200, 70, 340, () => Game.StartMode(md));
                return;
            }
            bool next = b.won && stage < Tables.Stages.Count && (!elite || Meta.EliteOpen(stage + 1));
            var nb = p.Btn("btn_green", next ? L.T("next") : L.T("retry"), 200, 70, 340, () => Game.TryStart(next ? stage + 1 : stage, elite));
            var ne = UIKit.Img(nb.transform, Art.Icon("energy"), "Energy"); ne.raycastTarget = false; ne.rectTransform.Place(1, 1, new Vector2(10, 22), new Vector2(56, 56), new Vector2(1, 1));
            var nc = UIKit.Label(ne.transform, Meta.StaminaCost(elite).ToString(), 26); nc.rectTransform.Place(0.5f, 0, new Vector2(0, -16), new Vector2(60, 34), new Vector2(0.5f, 0.5f));
        }
    }

    /// <summary>7차: 자리 고르기 안내가 살짝 숨쉬고 화살표가 칸 쪽으로 톡톡</summary>
    public class SlotTipBob : MonoBehaviour
    {
        public RectTransform arrow; float t; Vector2 a0;
        void Start() { if (arrow) a0 = arrow.anchoredPosition; }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(t * 4f));
            if (arrow) arrow.anchoredPosition = a0 + new Vector2(-Mathf.Abs(Mathf.Sin(t * 4f)) * 18f, 0);
        }
    }
}
