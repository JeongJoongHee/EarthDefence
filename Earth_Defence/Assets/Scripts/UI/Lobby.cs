using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>영구 성장 (1차): 터렛 레벨(조각 + 골드) · 기지 레벨(골드) · 순찰 보상</summary>
    public static class Meta
    {
        static SaveData S => SaveData.Cur;
        public static bool TurretMax(string id) => S.TurretLv(id) >= Tables.MaxTurretLv;
        public static TurretLvRow Cost(string id) => Tables.Lv(S.TurretLv(id));
        public static bool CanUpgradeTurret(string id)
        {
            if (TurretMax(id)) return false;
            var c = Cost(id);
            return S.shards[S.TIdx(id)] >= c.shards && S.gold >= c.gold;
        }
        public static bool UpgradeTurret(string id)
        {
            if (!CanUpgradeTurret(id)) return false;
            var c = Cost(id); int i = S.TIdx(id);
            S.shards[i] -= c.shards; S.gold -= c.gold; S.tLv[i]++;
            S.Save();
            return true;
        }
        public static bool CanUpgradeBase => !S.BaseMax && S.gold >= S.BaseCost;
        public static bool UpgradeBase()
        {
            if (!CanUpgradeBase) return false;
            S.gold -= S.BaseCost; S.baseLv++;
            S.Save();
            return true;
        }
        public static bool CanClaimPatrol => S.PatrolSec >= Tables.C("Patrol_Min_Sec", 60);
        /// <summary>순찰 보상 받기 → (골드, 조각)</summary>
        public static (double gold, int shards) ClaimPatrol()
        {
            if (!CanClaimPatrol) return (0, 0);
            double g = System.Math.Floor(S.PatrolGold); int sh = S.PatrolShards;
            S.gold += g;
            var rnd = new System.Random((int)(SaveData.Clock % 100000));
            for (int i = 0; i < sh; i++) S.shards[rnd.Next(Tables.Turrets.Count)]++;
            S.patrolFrom = SaveData.Clock;
            S.Save();
            return (g, sh);
        }
    }

    /// <summary>
    /// 로비: 위 = 제목 · 골드 · 스테이지 고르기 · [전투 시작] · 순찰 보상 / 아래 = 기지 + 터렛 7종 강화 목록.
    /// 위쪽은 화면 위에, 목록은 화면 아래에 붙여서 화면이 길어도 짧아도 들어가게.
    /// </summary>
    public class Lobby : MonoBehaviour
    {
        static SaveData S => SaveData.Cur;
        Text goldT, stageT, stageName, bestT, weakT, patrolT, patrolAmt;
        Button startB, prevB, nextB, patrolB, baseB;
        Text baseLvT, baseDescT, baseCostT;
        readonly List<(string id, Text lv, Text shards, Image bar, Button btn, Text cost)> rows = new();
        RectTransform root;

        public void Build()
        {
            root = (RectTransform)transform;
            var top = UIKit.Rect(root, "Top"); top.anchorMin = new Vector2(0, 1); top.anchorMax = new Vector2(1, 1); top.pivot = new Vector2(0.5f, 1); top.sizeDelta = new Vector2(0, 900); top.anchoredPosition = Vector2.zero;

            // 골드
            var gp = UIKit.Panel(top, "pill", "Gold"); gp.rectTransform.TL(30, 30, 330, 84);
            var gi = UIKit.Img(gp.transform, Art.Icon("coin"), "Coin"); gi.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(76, 76), new Vector2(0, 0.5f));
            goldT = UIKit.Label(gp.transform, "", 40, Art.Ink, TextAnchor.MiddleLeft, false); goldT.rectTransform.Fill(92, 6, 16, 0);

            var title = UIKit.Label(top, L.T("title"), 62); title.rectTransform.TL(0, 130, 1080, 100);
            title.color = Art.Cream;

            // 스테이지 카드
            var card = UIKit.Panel(top, "card", "StageCard"); card.rectTransform.TL(90, 250, 900, 330);
            stageT = UIKit.Label(card.transform, "", 64, Art.Ink, TextAnchor.MiddleCenter, false); stageT.rectTransform.TL(0, 26, 900, 80);
            stageName = UIKit.Label(card.transform, "", 38, Art.Brown, TextAnchor.MiddleCenter, false); stageName.rectTransform.TL(0, 110, 900, 54);
            weakT = UIKit.Label(card.transform, "", 30, Art.Soft, TextAnchor.MiddleCenter, false); weakT.rectTransform.TL(0, 172, 900, 46);
            bestT = UIKit.Label(card.transform, "", 28, Art.Soft, TextAnchor.MiddleCenter, false); bestT.rectTransform.TL(0, 228, 900, 44);
            prevB = UIKit.Btn(card.transform, "btn_orange", null, 48, () => Move(-1), "Prev"); ((RectTransform)prevB.transform).TL(30, 100, 110, 110); Arrow(prevB, true);
            nextB = UIKit.Btn(card.transform, "btn_orange", null, 48, () => Move(1), "Next"); ((RectTransform)nextB.transform).TL(760, 100, 110, 110); Arrow(nextB, false);

            startB = UIKit.Btn(top, "btn_green", L.T("start"), 60, () => Game.StartStage(S.stage), "Start");
            ((RectTransform)startB.transform).TL(270, 600, 540, 150);

            // 순찰
            var pc = UIKit.Panel(top, "card", "Patrol"); pc.rectTransform.TL(90, 770, 900, 120);
            var pi = UIKit.Img(pc.transform, Art.Icon("patrol"), "Icon"); pi.rectTransform.TL(20, 14, 84, 84);
            var pt = UIKit.Label(pc.transform, L.T("patrol"), 34, Art.Ink, TextAnchor.MiddleLeft, false); pt.rectTransform.TL(120, 10, 420, 50);
            patrolT = UIKit.Label(pc.transform, "", 24, Art.Soft, TextAnchor.MiddleLeft, false); patrolT.rectTransform.TL(120, 58, 440, 40);
            patrolAmt = UIKit.Label(pc.transform, "", 28, Art.Brown, TextAnchor.MiddleRight, false); patrolAmt.rectTransform.TL(440, 10, 230, 90);
            patrolB = UIKit.Btn(pc.transform, "btn_orange", L.T("claim"), 34, Claim, "Claim"); ((RectTransform)patrolB.transform).TL(690, 14, 190, 86);

            // 아래 목록: 기지 + 터렛
            var list = UIKit.Panel(root, "card_white", "List");
            list.rectTransform.anchorMin = new Vector2(0, 0); list.rectTransform.anchorMax = new Vector2(1, 0); list.rectTransform.pivot = new Vector2(0.5f, 0);
            list.rectTransform.sizeDelta = new Vector2(-40, 990); list.rectTransform.anchoredPosition = new Vector2(0, 14);
            float y = 20, h = 110;
            // 기지
            {
                var r = UIKit.Rect(list.transform, "Base"); r.TL(20, y, 1000, h);
                var tile = UIKit.Tile(r, "slot", Color.white, Art.Icon("base"), 0.8f, "Icon"); tile.rectTransform.TL(0, 4, 104, 104);
                baseLvT = UIKit.Label(r, "", 34, Art.Ink, TextAnchor.MiddleLeft, false); baseLvT.rectTransform.TL(122, 6, 500, 50);
                baseDescT = UIKit.Label(r, "", 26, Art.Soft, TextAnchor.MiddleLeft, false); baseDescT.rectTransform.TL(122, 56, 520, 44);
                baseB = UIKit.Btn(r, "btn_green", null, 30, () => { if (Meta.UpgradeBase()) Refresh(); }, "Up"); ((RectTransform)baseB.transform).TL(740, 10, 250, 96);
                baseCostT = CostLabel(baseB.transform);
                y += h + 8;
            }
            foreach (var t in Tables.Turrets)
            {
                var r = UIKit.Rect(list.transform, "Row_" + t.id); r.TL(20, y, 1000, h);
                var tile = UIKit.Tile(r, "slot", Color.white, Art.TurretUI(t.art), 0.86f, "Icon"); tile.rectTransform.TL(0, 4, 104, 104);
                var name = UIKit.Label(r, t.name, 34, Art.Ink, TextAnchor.MiddleLeft, false); name.rectTransform.TL(122, 4, 300, 50);
                var el = UIKit.Panel(r, "pill", "Elem"); el.color = Elem.Color(t.elem); el.rectTransform.TL(122 + Mathf.Min(300, name.preferredWidth + 14), 12, 90, 38);
                UIKit.Label(el.transform, Elem.Name(t.elem), 22, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 2, 0, 0);
                var lv = UIKit.Label(r, "", 30, Art.Brown, TextAnchor.MiddleLeft, false); lv.rectTransform.TL(470, 4, 220, 50);
                var bar = UIKit.Bar(r, Art.Sky, "Shards"); bar.transform.parent.GetComponent<RectTransform>().TL(122, 62, 460, 34);
                var sh = UIKit.Label(bar.transform.parent, "", 22, Color.white); sh.rectTransform.Fill(0, 2, 0, 0);
                var btn = UIKit.Btn(r, "btn_green", null, 30, () => { if (Meta.UpgradeTurret(t.id)) Refresh(); }, "Up"); ((RectTransform)btn.transform).TL(740, 10, 250, 96);
                rows.Add((t.id, lv, sh, bar, btn, CostLabel(btn.transform)));
                y += h + 8;
            }
            Refresh();
        }

        static void Arrow(Button b, bool left)
        {
            var a = UIKit.Img(b.transform, Art.Icon("play"), "Arrow"); a.raycastTarget = false;
            a.rectTransform.Fill(26, 30, 26, 22); a.rectTransform.localScale = new Vector3(left ? -1 : 1, 1, 1);
        }

        Text CostLabel(Transform b)
        {
            var c = UIKit.Img(b, Art.Icon("coin"), "Coin"); c.rectTransform.Place(0, 0.5f, new Vector2(16, 6), new Vector2(54, 54), new Vector2(0, 0.5f));
            var t = UIKit.Label(b, "", 32); t.rectTransform.Fill(70, 14, 10, 4);
            return t;
        }

        void Move(int d)
        {
            S.stage = Mathf.Clamp(S.stage + d, 1, Mathf.Min(Tables.Stages.Count, S.best + 1));
            Refresh();
        }

        void Claim()
        {
            var (g, sh) = Meta.ClaimPatrol();
            if (g > 0) Fx.Ring(Vector2.zero, 1, Art.Gold);
            Refresh();
        }

        float tick;
        void Update()
        {
            tick -= Time.unscaledDeltaTime;
            if (tick <= 0) { tick = 1f; RefreshPatrol(); }
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
            goldT.text = Num.F(S.gold);
            var st = Tables.Stage(S.stage);
            stageT.text = L.F("stage_n", S.stage);
            stageName.text = st.name;
            var weak = new List<string>();
            foreach (var id in st.enemies) if (Tables.Enemies.TryGetValue(id, out var e) && !weak.Contains(Elem.Name(e.weak))) weak.Add(Elem.Name(e.weak));
            weakT.text = "적 약점: " + string.Join(", ", weak);
            bestT.text = L.F("best", S.best);
            prevB.interactable = S.stage > 1; UIKit.SetKind(prevB, S.stage > 1 ? "btn_orange" : "btn_gray");
            bool canNext = S.stage < Mathf.Min(Tables.Stages.Count, S.best + 1);
            nextB.interactable = canNext; UIKit.SetKind(nextB, canNext ? "btn_orange" : "btn_gray");

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
            RefreshPatrol();
        }
    }
}
