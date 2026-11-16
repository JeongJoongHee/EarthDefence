using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 로비 장비 탭 (2차): 위 = 대장 카피 + 부위 6칸(왼쪽 무기 · 헬멧 · 우주복 / 오른쪽 방패 · 코어 · 부츠) + 능력치 합계
    /// · 가운데 = [장비] [칩] 가방 바꾸기 · 부품 수 · [모두 합성] · 아래 = 가방 (스크롤 격자).
    /// 부위 칸을 누르면 그 부위 창(장비 강화 · 빼기 · 칩 6칸), 가방 칸을 누르면 그 물건 창(끼우기 · 강화 · 합성 · 분해).
    /// </summary>
    public class GearTab : MonoBehaviour
    {
        static SaveData S => SaveData.Cur;
        Lobby lobby;
        RectTransform root, grid;
        Text statT, partsT;
        readonly Text[] statV = new Text[6];
        Button gearB, chipB, mergeB;
        readonly List<(Image tile, Image icon, Text lv, Image[] dots)> slots = new();
        public static bool showChips;

        static readonly string[] Left = { "weapon", "helmet", "suit" }, Right = { "shield", "core", "boots" };

        /// <summary>빨간 점: 빈 부위에 낄 장비가 있거나, 열린 빈 칩 칸에 낄 칩이 있음</summary>
        public static bool HasNews
        {
            get
            {
                var s = SaveData.Cur; if (s == null) return false;
                for (int p = 0; p < Tables.GearParts.Count; p++)
                {
                    string part = Tables.GearParts[p].part;
                    if (Gear.On(part) == null && s.gear.Exists(g => !g.on && g.part == part)) return true;
                }
                foreach (var c in s.chips)
                    if (c.at < 0 && Gear.FreeSlotFor(c).part >= 0) return true;
                return false;
            }
        }

        public void Build(Lobby l)
        {
            lobby = l; root = (RectTransform)transform;
            var top = UIKit.Panel(root, "card", "Top"); top.rectTransform.TL(20, 4, 1040, 610);
            var me = UIKit.Img(top.transform, Art.TurretUI("guardian"), "Guardian"); me.raycastTarget = false;
            me.rectTransform.Place(0.5f, 1, new Vector2(0, -20), new Vector2(330, 330), new Vector2(0.5f, 1));
            for (int i = 0; i < 3; i++) { Slot(top.transform, Left[i], 34, 22 + i * 192); Slot(top.transform, Right[i], 856, 22 + i * 192); }
            // 2차 수정안: 능력치를 네모 칸으로 (3 x 2 + 칩 수)
            string[] keys = { "st_atk", "st_hp", "st_reduce", "st_crit", "st_crit_dmg", "st_cd" };
            for (int i = 0; i < keys.Length; i++)
            {
                var box = UIKit.Panel(top.transform, "card_white", "Stat" + i); box.rectTransform.TL(206 + i % 3 * 210, 352 + i / 3 * 118, 200, 108);
                UIKit.Label(box.transform, L.T(keys[i]).TrimEnd(' ', '-'), 22, Art.Soft, TextAnchor.UpperCenter, false).rectTransform.Fill(4, 50, 4, 12);
                statV[i] = UIKit.Label(box.transform, "", 34, Art.Ink, TextAnchor.LowerCenter, false); statV[i].rectTransform.Fill(4, 18, 4, 40);
            }
            statT = UIKit.Label(top.transform, "", 22, Art.Soft, TextAnchor.MiddleCenter, false); statT.rectTransform.TL(206, 588, 620, 20);

            gearB = UIKit.Btn(root, "btn_orange", "", 34, () => { showChips = false; Refresh(); }, "GearList"); ((RectTransform)gearB.transform).TL(26, 626, 300, 96);
            chipB = UIKit.Btn(root, "btn_cream", "", 34, () => { showChips = true; Refresh(); }, "ChipList"); ((RectTransform)chipB.transform).TL(336, 626, 300, 96);
            var pp = UIKit.Panel(root, "pill", "Parts"); pp.rectTransform.TL(648, 632, 222, 84);
            var pi = UIKit.Img(pp.transform, Art.Icon("parts"), "Icon"); pi.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(70, 70), new Vector2(0, 0.5f));
            partsT = UIKit.Label(pp.transform, "", 34, Art.Ink, TextAnchor.MiddleLeft, false); partsT.rectTransform.Fill(84, 6, 10, 0);
            mergeB = UIKit.Btn(root, "btn_green", L.T("merge_title"), 28, MergeAll, "MergeAll"); ((RectTransform)mergeB.transform).TL(880, 626, 176, 96);

            var inv = UIKit.Panel(root, "card_white", "Bag");
            inv.rectTransform.anchorMin = Vector2.zero; inv.rectTransform.anchorMax = Vector2.one;
            inv.rectTransform.offsetMin = new Vector2(20, 6); inv.rectTransform.offsetMax = new Vector2(-20, -734);
            grid = UIKit.Scroll(inv.transform, "Scroll"); grid.parent.GetComponent<RectTransform>().Fill(8, 14, 8, 8);
        }

        void Slot(Transform p, string part, float x, float y)
        {
            int pi = Tables.PartIdx(part);
            var tile = UIKit.Tile(p, "slot", Color.white, Art.Icon(Tables.Part(part).icon), 0.64f, "Slot_" + part); tile.rectTransform.TL(x, y, 150, 150);
            UIKit.Clickable(tile, () => PartWindow(pi));
            var lv = UIKit.Label(tile.transform, "", 26); lv.rectTransform.Place(0.5f, 0, new Vector2(0, 2), new Vector2(150, 36), new Vector2(0.5f, 0));
            var dots = new Image[Gear.Slots];
            for (int i = 0; i < Gear.Slots; i++)
            {
                dots[i] = UIKit.Img(p, Art.UI("white"), "Dot"); dots[i].raycastTarget = false; dots[i].type = Image.Type.Simple;
                dots[i].rectTransform.TL(x + 4 + i * 24.5f, y + 156, 20, 20);
            }
            slots.Add((tile, tile.transform.Find("Icon").GetComponent<Image>(), lv, dots));
        }

        public void Refresh()
        {
            if (S == null || grid == null) return;
            // 부위 칸
            for (int k = 0; k < slots.Count; k++)
            {
                string part = k % 2 == 0 ? Left[k / 2] : Right[k / 2];
                int pi = Tables.PartIdx(part);
                var g = Gear.On(part);
                var (tile, icon, lv, dots) = slots[k];
                tile.color = g != null ? Color.Lerp(Color.white, g.G.color, 0.75f) : Color.white;
                icon.color = g != null ? Color.white : new Color(1, 1, 1, 0.35f);
                lv.text = g != null ? $"Lv {g.lv}" : "";
                for (int i = 0; i < Gear.Slots; i++)
                {
                    var c = Gear.ChipAt(pi, i);
                    dots[i].color = !Gear.SlotOpen(i) ? new Color(0.35f, 0.3f, 0.28f, 0.5f) : c != null ? Tables.Grade(c.q).color : new Color(1, 1, 1, 0.55f);
                }
            }
            var lo = Loadout.Build(S);
            statV[0].text = "+" + Num.F(lo.AtkTotal); statV[1].text = "+" + Num.F(lo.hp); statV[2].text = "-" + lo.reduce.ToString("0.#");
            statV[3].text = $"+{lo.TS("*", "crit"):0.#}%"; statV[4].text = $"+{lo.critDmg:0.#}%"; statV[5].text = $"+{lo.TS("*", "cd"):0.#}%";
            statT.text = $"{L.T("chip")} {lo.ChipCount}";
            partsT.text = Num.F(S.parts);
            int ng = S.gear.FindAll(x => !x.on).Count, nc = S.chips.FindAll(x => x.at < 0).Count;
            UIKit.LabelOf(gearB).text = L.F("gear_list", ng);
            UIKit.LabelOf(chipB).text = L.F("chip_list", nc);
            UIKit.SetKind(gearB, showChips ? "btn_cream" : "btn_orange");
            UIKit.SetKind(chipB, showChips ? "btn_orange" : "btn_cream");
            UIKit.SetKind(mergeB, Gear.GearMergeCount() + Gear.ChipMergeCount() > 0 ? "btn_green" : "btn_gray");
            // 가방
            for (int c = grid.childCount - 1; c >= 0; c--) Destroy(grid.GetChild(c).gameObject);
            int n = 0; const float cell = 196f;
            if (!showChips)
            {
                var list = S.gear.FindAll(x => !x.on);
                list.Sort((a, b) => b.grade != a.grade ? b.grade.CompareTo(a.grade) : b.lv != a.lv ? b.lv.CompareTo(a.lv) : string.CompareOrdinal(a.part, b.part));
                foreach (var g in list)
                {
                    var t = UIKit.Tile(grid, "slot", Color.Lerp(Color.white, g.G.color, 0.75f), Art.Icon(g.Row.icon), 0.6f, "G" + g.uid);
                    t.rectTransform.TL(14 + n % 5 * cell, 10 + n / 5 * cell, 180, 180);
                    var gg = g; UIKit.Clickable(t, () => GearWindow(gg));
                    UIKit.Label(t.transform, $"Lv {g.lv}", 26).rectTransform.Place(0.5f, 0, new Vector2(0, 4), new Vector2(180, 36), new Vector2(0.5f, 0));
                    if (Gear.MergeSet(g) != null) UIKit.Dot(t.transform, -14, -14, 30);
                    n++;
                }
            }
            else
            {
                var list = S.chips.FindAll(x => x.at < 0 && x.Row != null);
                list.Sort((a, b) => b.q != a.q ? b.q.CompareTo(a.q) : string.CompareOrdinal(a.id, b.id));
                foreach (var c in list)
                {
                    var t = UIKit.Tile(grid, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon("chip"), 0.48f, "C" + c.uid);
                    t.rectTransform.TL(14 + n % 5 * cell, 10 + n / 5 * cell, 180, 180);
                    var cc = c; UIKit.Clickable(t, () => ChipWindow(cc));
                    var nm = UIKit.Label(t.transform, Short(c.Row.name), 22); nm.rectTransform.Place(0.5f, 0, new Vector2(0, 4), new Vector2(176, 34), new Vector2(0.5f, 0));
                    if (Gear.ChipMergeSet(c) != null) UIKit.Dot(t.transform, -14, -14, 30);
                    n++;
                }
            }
            if (n == 0)
            {
                var e = UIKit.Label(grid, "-", 40, Art.Soft, TextAnchor.MiddleCenter, false); e.rectTransform.TL(0, 30, 1000, 60);
            }
            UIKit.SetScrollH(grid, 20 + Mathf.CeilToInt(n / 5f) * cell);
        }

        static string Short(string s) => s.Length > 7 ? s.Substring(0, 7) : s;

        /// <summary>일괄 합성 창 (2차 수정안 "장비창에 일괄합성창 추가"): 장비 · 칩 각각 몇 번 되는지 → [합성] → 새로 생긴 것을 아이콘으로</summary>
        public void MergeAll()
        {
            var p = Popup.Make(L.T("merge_title"), 860, 640, () => { Refresh(); lobby.Refresh(); });
            void Row(float y, string icon, string title, int n, System.Action act)
            {
                var r = UIKit.Panel(p.win, "card", "Row"); r.rectTransform.TL(40, y, 780, 170);
                var ic = UIKit.Img(r.transform, Art.Icon(icon), "Icon"); ic.rectTransform.TL(24, 30, 110, 110);
                UIKit.Label(r.transform, title, 36, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(150, 22, 360, 60);
                UIKit.Label(r.transform, n > 0 ? L.F("merge_can", n) : L.T("merge_none"), 26, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(150, 86, 360, 50);
                var b = UIKit.Btn(r.transform, n > 0 ? "btn_purple" : "btn_gray", L.T("merge"), 36, () => { if (n > 0) act(); }, "Go"); ((RectTransform)b.transform).TL(540, 40, 210, 96);
            }
            Row(130, "g_suit", L.T("merge_gear"), Gear.GearMergeCount(), () =>
            {
                var made = Gear.MergeAllGear(); p.Close(false); Refresh(); lobby.Refresh();
                var got = new List<GotItem>(); foreach (var g in made) got.Add(GotItem.Of(g));
                if (got.Count > 0) ShopTab.GotPopup(got, L.T("merge_title"));
            });
            Row(320, "chip", L.T("merge_chip"), Gear.ChipMergeCount(), () =>
            {
                var made = Gear.MergeAllChipList(); p.Close(false); Refresh(); lobby.Refresh();
                var got = new List<GotItem>(); foreach (var c in made) got.Add(GotItem.Of(c));
                if (got.Count > 0) ShopTab.GotPopup(got, L.T("merge_title"));
            });
        }

        // ---------- 부위 창 ----------
        public void PartWindow(int pi)
        {
            var row = Tables.GearParts[pi];
            var p = Popup.Make(row.name, 980, 1360, () => { Refresh(); lobby.Refresh(); });
            var g = Gear.On(row.part);
            if (g != null)
            {
                GearInfo(p.win, g, 128);
                var up = p.Btn(Gear.CanUp(g) ? "btn_green" : "btn_gray", null, -200, 870, 400, () => { if (Gear.Up(g)) { p.Close(false); PartWindow(pi); } }, 120);
                UpLabel(up, g);
                p.Btn("btn_orange", L.T("unequip"), 230, 870, 300, () => { Gear.Unequip(g); p.Close(false); PartWindow(pi); }, 120);
            }
            else p.Text(L.T("empty_part"), 200, 40, Art.Soft, 60);
            // 칩 칸
            var cl = UIKit.Label(p.win, L.T("chip_slots"), 34, Art.Ink, TextAnchor.MiddleLeft, false); cl.rectTransform.TL(50, 520, 400, 50);
            var desc = new List<string>();
            for (int i = 0; i < Gear.Slots; i++)
            {
                int si = i;
                float x = 46 + i * 150;
                if (!Gear.SlotOpen(i))
                {
                    var lk = UIKit.Tile(p.win, "slot", new Color(0.7f, 0.66f, 0.6f), Art.Icon("lock"), 0.5f, "Lock" + i); lk.rectTransform.TL(x, 576, 136, 136);
                    UIKit.Label(p.win, L.F("chip_locked", Gear.SlotNeed(i)), 22, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(x - 6, 716, 148, 34);
                    continue;
                }
                var c = Gear.ChipAt(pi, i);
                if (c != null && c.Row != null)
                {
                    var t = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon("chip"), 0.5f, "Chip" + i); t.rectTransform.TL(x, 576, 136, 136);
                    UIKit.Clickable(t, () => { p.Close(false); ChipWindow(c, () => PartWindow(pi)); });
                    UIKit.Label(p.win, Short(c.Row.name), 22, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.TL(x - 6, 716, 148, 34);
                    desc.Add($"- {c.Row.name} ({Tables.Grade(c.q).name}): {c.Row.Desc(c.q)}");
                }
                else
                {
                    var e = UIKit.Panel(p.win, "slot_dash", "Empty" + i); e.rectTransform.TL(x, 576, 136, 136);
                    var plus = UIKit.Img(e.transform, Art.Icon("plus"), "Plus"); plus.color = new Color(1, 1, 1, 0.5f); plus.rectTransform.Fill(36, 36, 36, 36);
                    UIKit.Clickable(e, () => { p.Close(false); ChipChooser(pi, si); });
                }
            }
            var dt = UIKit.Label(p.win, desc.Count > 0 ? string.Join("\n", desc) : "", 26, Art.Brown, TextAnchor.UpperLeft, false);
            dt.horizontalOverflow = HorizontalWrapMode.Wrap; dt.rectTransform.TL(50, 770, 880, 200);
        }

        void UpLabel(Button b, GearItem g)
        {
            if (g.lv >= g.LvMax) { UIKit.Label(b.transform, L.T("max"), 40).rectTransform.Fill(8, 14, 8, 4); return; }
            UIKit.Label(b.transform, L.T("enhance"), 36).rectTransform.Fill(10, 52, 10, 4);
            var c = UIKit.Img(b.transform, Art.Icon("coin"), "Coin"); c.rectTransform.Place(0, 0, new Vector2(22, 18), new Vector2(40, 40), new Vector2(0, 0));
            UIKit.Label(b.transform, Num.F(Gear.UpGold(g)), 26, Color.white, TextAnchor.MiddleLeft).rectTransform.Place(0, 0, new Vector2(66, 18), new Vector2(150, 40), new Vector2(0, 0));
            var pr = UIKit.Img(b.transform, Art.Icon("parts"), "Parts"); pr.rectTransform.Place(0, 0, new Vector2(220, 18), new Vector2(40, 40), new Vector2(0, 0));
            UIKit.Label(b.transform, $"{Gear.UpParts(g)}", 26, Color.white, TextAnchor.MiddleLeft).rectTransform.Place(0, 0, new Vector2(264, 18), new Vector2(120, 40), new Vector2(0, 0));
        }

        /// <summary>장비 정보 (그림 · 이름 · 등급 · 레벨 · 능력치 2개)</summary>
        static void GearInfo(RectTransform w, GearItem g, float y, GearItem cmp = null)
        {
            var t = UIKit.Tile(w, "slot", Color.Lerp(Color.white, g.G.color, 0.75f), Art.Icon(g.Row.icon), 0.64f, "Pic"); t.rectTransform.TL(50, y, 220, 220);
            var nm = UIKit.Label(w, g.Row.name, 44, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(300, y + 4, 640, 60);
            var gr = UIKit.Panel(w, "pill", "Grade"); gr.color = g.G.color; gr.rectTransform.TL(300, y + 70, 170, 52);
            UIKit.Label(gr.transform, g.G.name, 28, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
            UIKit.Label(w, L.F("gear_lv", g.lv, g.LvMax), 32, Art.Brown, TextAnchor.MiddleLeft, false).rectTransform.TL(490, y + 70, 300, 52);
            string Line(string k, double v, double? o) => $"{Gear.StatName(k)} {Gear.StatVal(k, v)}" + (o.HasValue ? $"  ({(v >= o.Value ? "+" : "-")}{Gear.StatVal(k, System.Math.Abs(v - o.Value)).TrimStart('+')})" : "");
            var s1 = UIKit.Label(w, Line(g.Row.stat1, g.V1, cmp?.V1), 32, Art.Ink, TextAnchor.MiddleLeft, false); s1.rectTransform.TL(300, y + 132, 640, 44);
            var s2 = UIKit.Label(w, Line(g.Row.stat2, g.V2, cmp?.V2), 32, Art.Ink, TextAnchor.MiddleLeft, false); s2.rectTransform.TL(300, y + 178, 640, 44);
        }

        // ---------- 가방 장비 창 ----------
        void GearWindow(GearItem g)
        {
            var p = Popup.Make(g.Row.name, 980, 1000, () => { Refresh(); lobby.Refresh(); });
            var on = Gear.On(g.part);
            GearInfo(p.win, g, 128, on);
            if (on != null) p.Text($"{L.T("equip")}: {on.Row.name} ({on.G.name}) Lv {on.lv}", 380, 28, Art.Soft);
            p.Btn("btn_green", L.T("equip"), -230, 420, 400, () => { Gear.Equip(g); p.Close(); }, 120);
            var up = p.Btn(Gear.CanUp(g) ? "btn_orange" : "btn_gray", null, 230, 420, 400, () => { if (Gear.Up(g)) { p.Close(false); GearWindow(g); } }, 120);
            UpLabel(up, g);
            var set = Gear.MergeSet(g);
            int same = S.gear.FindAll(x => !x.on && x.part == g.part && x.grade == g.grade).Count;
            p.Btn(set != null ? "btn_purple" : "btn_gray", $"{L.T("merge")} {same}/{Tables.Ci("Merge_N", 3)}", -230, 250, 400, () =>
            {
                var n = Gear.Merge(g); if (n == null) return;
                p.Close(false); Toast.Show($"{n.Row.name} ({n.G.name})"); GearWindow(n);
            }, 120);
            p.Btn("btn_red", $"{L.T("scrap")} +{Gear.ScrapParts(g)}", 230, 250, 400, () => { Gear.Scrap(g); p.Close(); }, 120);
            p.Text(g.grade >= Tables.GradeMax ? "" : $"{L.T("merge")}: {Tables.Grade(g.grade).name} x{Tables.Ci("Merge_N", 3)} > {Tables.Grade(g.grade + 1).name}", 870, 26, Art.Soft);
        }

        // ---------- 칩 창 ----------
        void ChipWindow(ChipItem c, System.Action back = null)
        {
            var r = c.Row;
            var p = Popup.Make(r.name, 940, 980, () => { Refresh(); lobby.Refresh(); back?.Invoke(); });
            var t = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon("chip"), 0.5f, "Pic"); t.rectTransform.TL(50, 128, 200, 200);
            var gr = UIKit.Panel(p.win, "pill", "Grade"); gr.color = Tables.Grade(c.q).color; gr.rectTransform.TL(280, 134, 170, 52);
            UIKit.Label(gr.transform, Tables.Grade(c.q).name, 28, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
            if (!string.IsNullOrEmpty(r.turret) && Tables.TurretById.TryGetValue(r.turret, out var tr))
                UIKit.Label(p.win, tr.name, 28, Art.Brown, TextAnchor.MiddleLeft, false).rectTransform.TL(470, 134, 400, 52);
            var d = UIKit.Label(p.win, r.Desc(c.q), 36, Art.Ink, TextAnchor.UpperLeft, false); d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(280, 200, 620, 130);
            var parts = new List<string>();
            foreach (var gp in Tables.GearParts) if (r.Fits(gp.part)) parts.Add(gp.name);
            p.Text(string.Join(", ", parts), 350, 28, Art.Soft, 80).horizontalOverflow = HorizontalWrapMode.Wrap;
            if (c.at >= 0) p.Btn("btn_orange", L.T("unequip"), -230, 300, 400, () => { Gear.TakeChip(c); p.Close(); }, 120);
            else p.Btn("btn_green", L.T("equip"), -230, 300, 400, () =>
            {
                var (pp, ss) = Gear.FreeSlotFor(c);
                if (pp < 0) { Toast.Show(L.T("no_chip")); return; }
                Gear.PutChip(c, pp, ss); p.Close();
            }, 120);
            int same = S.chips.FindAll(x => x.at < 0 && x.id == c.id && x.q == c.q).Count;
            p.Btn(Gear.ChipMergeSet(c) != null ? "btn_purple" : "btn_gray", $"{L.T("merge")} {same}/{Tables.Ci("Merge_N", 3)}", 230, 300, 400, () =>
            {
                var n = Gear.MergeChip(c); if (n == null) return;
                p.Close(false); Toast.Show($"{n.Row.name} ({Tables.Grade(n.q).name})"); ChipWindow(n, back);
            }, 120);
            p.Btn("btn_red", $"{L.T("scrap")} +{Tables.Ci("Chip_Scrap_Parts", 1) * c.q}", 0, 140, 400, () => { Gear.ScrapChip(c); p.Close(); }, 120);
        }

        /// <summary>빈 칩 칸 → 끼울 수 있는 칩 고르기</summary>
        void ChipChooser(int pi, int slot)
        {
            var part = Tables.GearParts[pi];
            var p = Popup.Make(part.name + " " + L.T("chip"), 980, 1400, () => PartWindow(pi));
            var list = S.chips.FindAll(c => c.at < 0 && c.Row != null && c.Row.Fits(part.part));
            list.Sort((a, b) => b.q != a.q ? b.q.CompareTo(a.q) : string.CompareOrdinal(a.id, b.id));
            if (list.Count == 0) { p.Text(L.T("no_chip"), 300, 36, Art.Soft); return; }
            var area = UIKit.Rect(p.win, "Area"); area.Fill(30, 30, 30, 120);
            var c0 = UIKit.Scroll(area, "Scroll"); c0.parent.GetComponent<RectTransform>().Fill();
            float y = 6;
            foreach (var c in list)
            {
                var row = UIKit.Panel(c0, "card_white", "Row"); row.rectTransform.TL(0, y, 920, 130);
                var t = UIKit.Tile(row.transform, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon("chip"), 0.5f, "Pic"); t.rectTransform.TL(14, 12, 106, 106);
                UIKit.Label(row.transform, $"{c.Row.name} ({Tables.Grade(c.q).name})", 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(140, 14, 760, 48);
                var ds = UIKit.Label(row.transform, c.Row.Desc(c.q), 26, Art.Soft, TextAnchor.MiddleLeft, false); ds.rectTransform.TL(140, 66, 760, 44);
                var cc = c; UIKit.Clickable(row, () => { Gear.PutChip(cc, pi, slot); p.Close(); });
                y += 140;
            }
            UIKit.SetScrollH(c0, y + 10);
        }
    }
}
