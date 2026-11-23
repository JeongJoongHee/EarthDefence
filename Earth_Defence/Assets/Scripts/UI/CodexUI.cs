using System;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 9차 수정안 "장비와 회로에 대한 도감 정보 추가" — 장비 탭 [도감].
    ///  · 장비: 부위 6줄 x 등급 7칸 (얻어 본 것 = 색 · 못 얻은 것 = 흐리게 + 자물쇠) → 누르면 그 장비 정보(능력치 Lv.1 → 최대 Lv)
    ///  · 회로: 종류마다 한 줄 (얻어 본 최고 등급 · 그 등급 효과 · 끼우는 곳) → 누르면 등급별 값
    /// 얻어 본 기록 = Codex (세이브 codex 칸 — 합성 · 분해로 없어져도 남음).
    /// </summary>
    public static class CodexUI
    {
        public static int view;     // 0 = 장비 · 1 = 회로
        public static Popup Cur;

        public static void Open()
        {
            Cur = Popup.Make(L.T("codex"), 1000, 1560);
            var p = Cur;
            var gB = UIKit.Btn(p.win, view == 0 ? "btn_orange" : "btn_cream", L.T("codex_gear"), 36, () => { view = 0; p.Close(false); Open(); }, "TabGear");
            ((RectTransform)gB.transform).TL(40, 112, 300, 92);
            var cB = UIKit.Btn(p.win, view == 1 ? "btn_orange" : "btn_cream", L.T("codex_chip"), 36, () => { view = 1; p.Close(false); Open(); }, "TabChip");
            ((RectTransform)cB.transform).TL(352, 112, 300, 92);
            var got = UIKit.Label(p.win, view == 0 ? L.F("codex_got", Codex.GearGot, Codex.GearAll) : L.F("codex_got", Codex.ChipGot, Codex.ChipAll), 30, Art.Brown, TextAnchor.MiddleRight, false);
            got.name = "Got"; got.rectTransform.TL(660, 112, 300, 92);
            var bag = UIKit.Panel(p.win, "card_white", "List"); bag.rectTransform.TL(30, 220, 940, 1310);
            var list = UIKit.Scroll(bag.transform, "Scroll"); list.parent.GetComponent<RectTransform>().Fill(8, 10, 8, 10);
            if (view == 0) GearList(list); else ChipList(list);
        }

        static void GearList(RectTransform c)
        {
            float y = 12;
            foreach (var part in Tables.GearParts)
            {
                UIKit.Label(c, part.name, 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(18, y, 400, 46);
                y += 50;
                for (int g = 1; g <= Tables.GearTop; g++)     // 11-22 오픈 스펙: 보이는 등급까지
                {
                    int grade = g; var pr = part;
                    bool has = Codex.HasGear(part.part, g);
                    var gr = Tables.Grade(g);
                    var t = UIKit.Tile(c, "slot", has ? Color.Lerp(Color.white, gr.color, 0.75f) : new Color(0.78f, 0.75f, 0.7f), Art.Icon(part.icon + "_" + g), 0.7f, $"G_{part.part}_{g}");
                    t.rectTransform.TL(14 + (g - 1) * 130, y, 122, 122);
                    var ic = t.transform.Find("Icon")?.GetComponent<Image>();
                    if (!has && ic) ic.color = new Color(0.25f, 0.22f, 0.2f, 0.55f);
                    if (!has) { var lk = UIKit.Img(t.transform, Art.Icon("lock"), "Lock"); lk.raycastTarget = false; lk.rectTransform.Place(1, 0, new Vector2(-4, 4), new Vector2(44, 44), new Vector2(1, 0)); }
                    UIKit.Clickable(t, () => GearInfo(pr, grade));
                }
                y += 134;
            }
            UIKit.SetScrollH(c, y + 10);
        }

        static void ChipList(RectTransform c)
        {
            float y = 10;
            // 얻어 본 것 먼저 (등급 높은 순) · 그다음 못 얻은 것 (표 순서)
            var rows = Tables.Chips.FindAll(x => Tables.TurretOpen(x.turret));   // 11-23: 숨긴 터렛의 회로는 뺌
            rows.Sort((a, b) => { int x = Codex.ChipBest(b.id).CompareTo(Codex.ChipBest(a.id)); return x != 0 ? x : Tables.Chips.IndexOf(a).CompareTo(Tables.Chips.IndexOf(b)); });
            foreach (var r in rows)
            {
                var row = r;
                int best = Codex.ChipBest(r.id);
                var bg = UIKit.Panel(c, best > 0 ? "card" : "slot", "C_" + r.id); bg.rectTransform.TL(10, y, 900, 132);
                bg.raycastTarget = true; UIKit.Clickable(bg, () => ChipInfo(row));
                int q = Mathf.Max(1, best);
                var t = UIKit.Tile(bg.transform, "slot", best > 0 ? Color.Lerp(Color.white, Tables.Grade(q).color, 0.75f) : new Color(0.78f, 0.75f, 0.7f), Art.Icon("chip_" + q), 0.62f, "Icon");
                t.raycastTarget = false; t.rectTransform.TL(12, 12, 108, 108);
                if (best <= 0) { var ic = t.transform.Find("Icon")?.GetComponent<Image>(); if (ic) ic.color = new Color(0.25f, 0.22f, 0.2f, 0.55f); }
                UIKit.Label(bg.transform, r.name + (best > 0 ? $"  ({Tables.Grade(best).name})" : ""), 30, best > 0 ? Art.Ink : Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(136, 10, 740, 44);
                var d = UIKit.Label(bg.transform, best > 0 ? r.Desc(best) : L.T("codex_unknown"), 26, best > 0 ? Art.Green2 : Art.Soft, TextAnchor.UpperLeft, false);
                d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(136, 54, 740, 70);
                y += 140;
            }
            UIKit.SetScrollH(c, y + 10);
        }

        static string PartsText(ChipRow r)
        {
            if (Array.IndexOf(r.parts, "all") >= 0) return L.T("codex_all_parts");
            var l = new System.Collections.Generic.List<string>();
            foreach (var p in r.parts) { var gp = Tables.Part(p); if (gp != null) l.Add(gp.name); }
            return string.Join(", ", l);
        }

        /// <summary>장비 하나 정보: 그림 · 이름 · 등급 · 능력치 Lv.1 → 최대 Lv</summary>
        public static void GearInfo(GearPartRow part, int grade)
        {
            var g = new GearItem { part = part.part, grade = grade, lv = 1 };
            var gm = new GearItem { part = part.part, grade = grade, lv = g.LvMax };
            bool has = Codex.HasGear(part.part, grade);
            var p = Popup.Make(L.T("codex"), 880, 620);
            var t = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, g.G.color, 0.75f), Art.Icon(g.Icon), 0.72f, "Pic"); t.rectTransform.TL(40, 120, 170, 170);
            UIKit.Label(p.win, g.Name, 40, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(236, 124, 600, 56);
            UIKit.Label(p.win, $"{g.G.name} {part.name}", 30, g.G.color * 0.75f + new Color(0, 0, 0, 1), TextAnchor.MiddleLeft, false).rectTransform.TL(236, 184, 600, 44);
            UIKit.Label(p.win, has ? "" : L.T("codex_unknown"), 28, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(236, 234, 600, 44);
            float y = 320;
            foreach (var (k, v1, v2) in new[] { (part.stat1, g.V1, gm.V1), (part.stat2, g.V2, gm.V2) })
            {
                if (string.IsNullOrEmpty(k)) continue;
                var bg = UIKit.Panel(p.win, "card_white", "Stat_" + k); bg.rectTransform.TL(40, y, 800, 72);
                UIKit.Label(bg.transform, L.F("codex_stat", Gear.StatName(k), Gear.StatVal(k, v1), g.LvMax, Gear.StatVal(k, v2)), 30, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(10, 4, 10, 0);
                y += 84;
            }
            p.Btn("btn_green", L.T("ok"), 0, 34, 320, p.Close, 100);
        }

        /// <summary>회로 하나 정보: 등급별 값 · 끼우는 곳</summary>
        public static void ChipInfo(ChipRow r)
        {
            int best = Codex.ChipBest(r.id);
            var p = Popup.Make(L.T("codex"), 900, 1260);
            var t = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, Tables.Grade(Mathf.Max(1, best)).color, 0.75f), Art.Icon("chip_" + Mathf.Max(1, best)), 0.7f, "Pic"); t.rectTransform.TL(40, 118, 150, 150);
            UIKit.Label(p.win, r.name, 40, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(214, 120, 640, 56);
            UIKit.Label(p.win, best > 0 ? L.F("codex_best", Tables.Grade(best).name) : L.T("codex_unknown"), 28, best > 0 ? Art.Brown : Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(214, 176, 640, 44);
            var pt = UIKit.Label(p.win, L.F("codex_parts", PartsText(r)) + (string.IsNullOrEmpty(r.turret) || !Tables.TurretById.ContainsKey(r.turret) ? "" : "  /  " + Tables.TurretById[r.turret].name), 26, Art.Soft, TextAnchor.MiddleLeft, false);
            pt.horizontalOverflow = HorizontalWrapMode.Wrap; pt.rectTransform.TL(214, 220, 640, 50);
            UIKit.Label(p.win, L.T("codex_range"), 30, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(44, 288, 400, 48);
            float y = 342;
            for (int q = 1; q <= Tables.ChipTop; q++)
            {
                var gr = Tables.Grade(q);
                var bg = UIKit.Panel(p.win, q <= best ? "card_white" : "slot", "Q" + q); bg.rectTransform.TL(40, y, 820, 92);
                var tag = UIKit.Panel(bg.transform, "badge", "Grade"); tag.color = gr.color; tag.rectTransform.TL(14, 16, 150, 60);
                UIKit.Label(tag.transform, gr.name, 26, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
                var d = UIKit.Label(bg.transform, r.Desc(q), 26, q <= best ? Art.Green2 : Art.Soft, TextAnchor.MiddleLeft, false);
                d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(180, 4, 626, 84);
                y += 100;
            }
            p.Btn("btn_green", L.T("ok"), 0, 34, 320, p.Close, 100);
        }

        // ---------- 회로 프리셋 ----------
        /// <summary>9차 "장비 탭에서 회로 프리셋 기능 추가 (프리셋 총 3개)": 프리셋마다 [저장](지금 끼운 회로) · [불러오기](그대로 다시 끼움)</summary>
        public static Popup Presets(Action changed)
        {
            int n = ChipPreset.Count;
            var p = Popup.Make(L.T("preset_title"), 900, 300 + n * 170);
            var tip = UIKit.Label(p.win, L.T("preset_tip"), 26, Art.Soft, TextAnchor.MiddleCenter, false);
            tip.horizontalOverflow = HorizontalWrapMode.Wrap; tip.rectTransform.TL(40, 108, 820, 70);
            for (int i = 0; i < n; i++)
            {
                int k = i;
                var row = UIKit.Panel(p.win, "card", "Preset" + (i + 1)); row.rectTransform.TL(40, 190 + i * 170, 820, 150);
                UIKit.Label(row.transform, L.F("preset_n", i + 1), 38, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(30, 20, 300, 56);
                int size = ChipPreset.Size(i);
                UIKit.Label(row.transform, size > 0 ? L.F("preset_count", size) : L.T("preset_empty"), 28, size > 0 ? Art.Brown : Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(30, 78, 300, 48);
                var sv = UIKit.Btn(row.transform, "btn_orange", L.T("preset_save"), 34, () =>
                {
                    ChipPreset.Save(k); Toast.Show(L.F("preset_saved", k + 1)); p.Close(false); Presets(changed);
                }, "Save");
                ((RectTransform)sv.transform).TL(330, 26, 220, 100);
                var ld = UIKit.Btn(row.transform, size > 0 ? "btn_green" : "btn_gray", L.T("preset_load"), 34, () =>
                {
                    int miss = ChipPreset.Load(k);
                    if (miss < 0) { Toast.Show(L.T("preset_nothing")); return; }
                    Toast.Show(miss > 0 ? L.F("preset_missing", k + 1, miss) : L.F("preset_loaded", k + 1));
                    Audio.Play(Audio.Sfx.Place);
                    changed?.Invoke();
                    p.Close(false); Presets(changed);
                }, "Load");
                ((RectTransform)ld.transform).TL(566, 26, 230, 100);
            }
            return p;
        }
    }
}
