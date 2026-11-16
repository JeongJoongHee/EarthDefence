using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 치트 창 (11-17 사용자 "장비 · 조각 · 강화 조각을 추가할 수 있는 치트"): 설정 창 맨 아래 [치트] 단추.
    /// 장비(등급 · 부위 골라 +1 / +10) · 회로(등급 골라 +1 / +10) · 터렛 조각(모든 열린 터렛 +10 / +100) · 강화 부품(+100 / +1000 / +10000).
    /// 보이는 곳: 에디터 · 개발용 빌드 · Config Cheat_On = 1 일 때만 → 스토어 빌드 전엔 Cheat_On 0 확인.
    /// </summary>
    public static class Cheat
    {
        static SaveData S => SaveData.Cur;
        public static bool Enabled => Application.isEditor || Debug.isDebugBuild || Tables.Ci("Cheat_On", 0) == 1;
        static int grade = 4;
        static string part;   // null = 무작위

        public static void Open()
        {
            if (!Enabled) return;
            var p = Popup.Make(L.T("cheat"), 980, 1240, () => { S.Save(); Game.Lobby?.Refresh(); });
            float y = 116;
            // ---- 등급
            Head(p, ref y, L.T("cheat_grade"));
            for (int g = 1; g <= Tables.GradeMax; g++)
            {
                int gg = g; var gr = Tables.Grade(g);
                var b = UIKit.Btn(p.win, "pill", null, 26, () => { grade = gg; p.Close(false); Open(); }, "G" + g);
                ((RectTransform)b.transform).TL(40 + (g - 1) * 130, y, 122, 72);
                b.GetComponent<Image>().color = g == grade ? gr.color : Color.Lerp(gr.color, Color.white, 0.6f);
                UIKit.Label(b.transform, gr.name, 26, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
                if (g == grade) UIKit.Img(b.transform, Art.UI("glow_frame"), "Sel").rectTransform.Fill(-18, -18, -18, -18);
            }
            y += 96;
            // ---- 장비 부위 (무작위 + 6부위)
            Head(p, ref y, L.T("cheat_part"));
            for (int i = 0; i <= Tables.GearParts.Count; i++)
            {
                string pt = i == 0 ? null : Tables.GearParts[i - 1].part;
                bool on = pt == part;
                var icon = pt == null ? Art.Icon("star") : Art.Icon($"g_{pt}_{grade}");
                var t = UIKit.Tile(p.win, on ? "btn_orange" : "btn_cream", Color.white, icon, 0.72f, "P_" + (pt ?? "random")); t.rectTransform.TL(40 + i * 128, y, 118, 118);
                if (pt == null) UIKit.Label(t.transform, L.T("cheat_random"), 24).rectTransform.Fill(0, 10, 0, 0);
                UIKit.Clickable(t, () => { part = pt; p.Close(false); Open(); });
            }
            y += 140;
            Row(p, ref y, L.T("r_gear"), "g_suit_" + grade, new[] { 1, 10 }, n =>
            {
                for (int i = 0; i < n; i++) Gear.NewGear(grade, part);
                return $"{Tables.Grade(grade).name} {L.T("r_gear")} +{n}";
            });
            Row(p, ref y, L.T("r_chip"), "chip_" + grade, new[] { 1, 10 }, n =>
            {
                for (int i = 0; i < n; i++) Gear.NewChip(grade);
                return $"{Tables.Grade(grade).name} {L.T("r_chip")} +{n}";
            });
            // ---- 조각 · 강화 부품
            Row(p, ref y, L.T("cheat_shards"), "shard", new[] { 10, 100 }, n =>
            {
                foreach (var t in Tables.Turrets) if (S.Unlocked(t)) S.shards[S.TIdx(t.id)] += n;
                return $"{L.T("r_shards")} +{n} ({L.T("cheat_all_turrets")})";
            });
            Row(p, ref y, L.T("r_parts"), "parts", new[] { 100, 1000, 10000 }, n => { S.parts += n; return $"{L.T("r_parts")} +{Num.Comma(n)}"; });
            p.Text(L.T("cheat_note"), y + 10, 24, Art.Soft, 70).horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static void Head(Popup p, ref float y, string s)
        {
            UIKit.Label(p.win, s, 30, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(44, y, 600, 44);
            y += 50;
        }

        /// <summary>한 줄: 아이콘 · 이름 · [+n] 단추들 (누르면 바로 주고 토스트 · 세이브)</summary>
        static void Row(Popup p, ref float y, string name, string icon, int[] amounts, System.Func<int, string> give)
        {
            var r = UIKit.Panel(p.win, "card_white", "Row_" + name); r.rectTransform.TL(30, y, 920, 130);
            UIKit.Img(r.transform, Art.Icon(icon), "Icon").rectTransform.TL(16, 14, 100, 100);
            UIKit.Label(r.transform, name, 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(130, 20, 260, 86);
            for (int i = 0; i < amounts.Length; i++)
            {
                int n = amounts[i];
                var b = UIKit.Btn(r.transform, "btn_green", "+" + Num.Comma(n), 32, () => { var s = give(n); S.Save(); Game.Lobby?.Refresh(); Toast.Show(s); }, "Add" + n);
                ((RectTransform)b.transform).TL(380 + i * 178, 20, 166, 92);
            }
            y += 144;
        }
    }
}
