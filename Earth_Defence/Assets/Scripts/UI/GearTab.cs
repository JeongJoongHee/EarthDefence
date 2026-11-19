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

        static bool[] newsOn = new bool[6], newsBag = new bool[6], newsOcc = new bool[36]; static readonly bool[] newsOpen = new bool[Gear.Slots];
        /// <summary>빨간 점: 빈 부위에 낄 장비가 있거나, 열린 빈 칩 칸에 낄 칩이 있음</summary>
        public static bool HasNews
        {
            get
            {
                var s = SaveData.Cur; if (s == null) return false;
                // 11-17 최적화: 한 번 훑어 "부위마다 낀 것 · 가방에 있는 것" · "찬 회로 칸"을 적어 두고 판단
                int np = Tables.GearParts.Count;
                if (newsOn.Length < np) { newsOn = new bool[np]; newsBag = new bool[np]; }
                if (newsOcc.Length < np * Gear.Slots) newsOcc = new bool[np * Gear.Slots];
                System.Array.Clear(newsOn, 0, newsOn.Length); System.Array.Clear(newsBag, 0, newsBag.Length); System.Array.Clear(newsOcc, 0, newsOcc.Length);
                foreach (var g in s.gear) { int pi = Tables.PartIdx(g.part); if (pi < 0) continue; if (g.on) newsOn[pi] = true; else newsBag[pi] = true; }
                for (int i = 0; i < np; i++) if (!newsOn[i] && newsBag[i]) return true;
                bool anyFree = false;
                foreach (var c in s.chips) if (c.at >= 0 && c.at < newsOcc.Length) newsOcc[c.at] = true;
                for (int sl = 0; sl < Gear.Slots; sl++)
                {
                    newsOpen[sl] = Gear.SlotOpen(sl);
                    if (newsOpen[sl]) for (int pi = 0; pi < np; pi++) if (!newsOcc[pi * Gear.Slots + sl]) anyFree = true;
                }
                if (!anyFree) return false;
                foreach (var c in s.chips)
                {
                    if (c.at >= 0) continue;
                    var row = c.Row; if (row == null) continue;
                    for (int pi = 0; pi < np; pi++)
                    {
                        if (!row.Fits(Tables.GearParts[pi].part)) continue;
                        for (int sl = 0; sl < Gear.Slots; sl++) if (newsOpen[sl] && !newsOcc[pi * Gear.Slots + sl]) return true;
                    }
                }
                return false;
            }
        }

        public void Build(Lobby l)
        {
            lobby = l; root = (RectTransform)transform;
            var top = UIKit.Panel(root, "card", "Top"); top.rectTransform.TL(20, 4, 1040, 610);
            // 7차 수정안 "장비 탭 카피바라는 2D 로 그냥 둥실둥실 다니는 느낌 · 총은 없고 우주복": 총 없는 그림(Char/guardian_float) 이
            //   장비 칸 사이 빈 곳을 천천히 떠다님 (Float2D — 6차의 3D 몸 돌림 · 빛 · 그늘 · 발밑 그림자는 뺌)
            var me = UIKit.Img(top.transform, Art.Get("Char/guardian_float", 100f), "Guardian"); me.raycastTarget = false; me.preserveAspect = true;
            me.rectTransform.Place(0.5f, 1, new Vector2(0, -300), new Vector2(380, 410), new Vector2(0.5f, 0.5f));
            me.gameObject.AddComponent<Float2D>();
            for (int i = 0; i < 3; i++) { Slot(top.transform, Left[i], 34, 22 + i * 192); Slot(top.transform, Right[i], 856, 22 + i * 192); }
            // 9차 수정안: 도감(장비 · 회로) · 회로 프리셋 3개 — 대장 카피 아래 빈 곳
            var cx = UIKit.Btn(top.transform, "btn_sky", L.T("codex"), 30, CodexUI.Open, "Codex"); ((RectTransform)cx.transform).TL(224, 512, 286, 84);
            var ci = UIKit.Img(cx.transform, Art.Icon("book"), "Icon"); ci.raycastTarget = false; ci.rectTransform.Place(0, 0.5f, new Vector2(10, 4), new Vector2(64, 64), new Vector2(0, 0.5f));
            UIKit.LabelOf(cx).rectTransform.offsetMin = new Vector2(60, UIKit.LabelOf(cx).rectTransform.offsetMin.y);
            var pr = UIKit.Btn(top.transform, "btn_purple", L.T("preset_title"), 30, () => CodexUI.Presets(() => { Refresh(); lobby.Refresh(); }), "Preset"); ((RectTransform)pr.transform).TL(530, 512, 286, 84);
            var pi0 = UIKit.Img(pr.transform, Art.Icon("chip"), "Icon"); pi0.raycastTarget = false; pi0.rectTransform.Place(0, 0.5f, new Vector2(10, 4), new Vector2(60, 60), new Vector2(0, 0.5f));
            UIKit.LabelOf(pr).rectTransform.offsetMin = new Vector2(60, UIKit.LabelOf(pr).rectTransform.offsetMin.y);
            // 2차 수정안: 능력치를 네모 칸으로 (3 x 2 + 칩 수)
            string[] keys = { "st_atk", "st_hp", "st_reduce_total", "st_crit", "st_crit_dmg", "st_cd" };
            for (int i = 0; i < keys.Length; i++)
            {
                var box = UIKit.Panel(root, "card_white", "Stat" + i); box.rectTransform.TL(24 + i % 3 * 346, 624 + i / 3 * 70, 336, 62);
                UIKit.Label(box.transform, L.T(keys[i]).TrimEnd(' ', '-'), 26, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.Fill(20, 6, 120, 2);
                statV[i] = UIKit.Label(box.transform, "", 32, Art.Ink, TextAnchor.MiddleRight, false); statV[i].rectTransform.Fill(120, 6, 20, 2);
            }
            statT = UIKit.Label(top.transform, "", 22, Art.Soft, TextAnchor.MiddleCenter, false); statT.rectTransform.TL(206, 588, 620, 20); statT.gameObject.SetActive(false);

            gearB = UIKit.Btn(root, "btn_orange", "", 34, () => { showChips = false; Refresh(); }, "GearList"); ((RectTransform)gearB.transform).TL(26, 768, 300, 96);
            chipB = UIKit.Btn(root, "btn_cream", "", 34, () => { showChips = true; Refresh(); }, "ChipList"); ((RectTransform)chipB.transform).TL(336, 768, 300, 96);
            // 부품 (요청 사항_05 "장비 강화 재료라는 안내 필요"): 위에 "강화 재료" 글자 · 누르면 설명 토스트
            var pp = UIKit.Panel(root, "pill", "Parts"); pp.rectTransform.TL(648, 770, 222, 92);
            var pi = UIKit.Img(pp.transform, Art.Icon("parts"), "Icon"); pi.rectTransform.Place(0, 0.5f, new Vector2(8, 0), new Vector2(70, 70), new Vector2(0, 0.5f));
            UIKit.Label(pp.transform, L.T("parts_label"), 20, Art.Brown, TextAnchor.UpperLeft, false).rectTransform.Fill(84, 46, 6, 8);
            partsT = UIKit.Label(pp.transform, "", 32, Art.Ink, TextAnchor.LowerLeft, false); partsT.rectTransform.Fill(84, 12, 10, 36);
            UIKit.Clickable(pp, () => Toast.Show(L.T("parts_info"), 2.6f));
            mergeB = UIKit.Btn(root, "btn_green", L.T("merge_title"), 28, MergeAll, "MergeAll"); ((RectTransform)mergeB.transform).TL(880, 768, 176, 96);

            var inv = UIKit.Panel(root, "card_white", "Bag");
            inv.rectTransform.anchorMin = Vector2.zero; inv.rectTransform.anchorMax = Vector2.one;
            inv.rectTransform.offsetMin = new Vector2(20, 6); inv.rectTransform.offsetMax = new Vector2(-20, -876);
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
                // 3차 수정안 "회로 칸이 잘 안 보임": 점 → 테두리 있는 작은 칸 (안에 회로 아이콘 · 잠김은 어둡게)
                dots[i] = UIKit.Img(p, Art.UI("minislot"), "MiniSlot"); dots[i].raycastTarget = false;
                dots[i].rectTransform.TL(x + 1 + i * 25, y + 154, 23, 23);   // 4차 수정안 "UI 마감 어긋남": 장비 칸 폭(150) 안에 딱 맞게
                var ci = UIKit.Img(dots[i].transform, null, "Chip"); ci.raycastTarget = false; ci.rectTransform.Fill(2, 2, 2, 2); ci.preserveAspect = true;
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
                icon.sprite = Art.Icon(g != null ? g.Icon : Tables.Part(part).icon);
                icon.color = g != null ? Color.white : new Color(1, 1, 1, 0.35f);
                lv.text = g != null ? $"Lv {g.lv}" : "";
                for (int i = 0; i < Gear.Slots; i++)
                {
                    var c = Gear.ChipAt(pi, i);
                    bool open = Gear.SlotOpen(i);
                    dots[i].color = !open ? new Color(0.45f, 0.4f, 0.36f) : c != null ? Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.6f) : Color.white;
                    var ci = dots[i].transform.Find("Chip").GetComponent<Image>();
                    ci.sprite = !open ? Art.Icon("lock") : c != null ? Art.Icon(c.Icon) : null;
                    ci.enabled = ci.sprite != null; ci.color = open ? Color.white : new Color(1, 1, 1, 0.7f);
                }
            }
            var lo = Loadout.Build(S);
            // 밸런스 3차: 회로의 "기지 공격력 · 체력 +%" 도 더해서 보여 줌 · 받는 피해는 방어력을 비율로 바꾼 값
            statV[0].text = "+" + Num.F(lo.AtkOf(S) - S.Atk); statV[1].text = "+" + Num.F(lo.HpOf(S) - S.BaseHp);
            statV[2].text = Tables.C("Reduce_K", 0) > 0 ? $"-{(1 - lo.ReduceMul) * 100:0.#}%" : "-" + lo.reduce.ToString("0.#");
            statV[3].text = $"+{lo.TS("*", "crit"):0.#}%"; statV[4].text = $"+{lo.critDmg:0.#}%"; statV[5].text = $"+{lo.TS("*", "cd"):0.#}%";
            statT.text = $"{L.T("chip")} {lo.ChipCount}";
            partsT.text = Num.F(S.parts);
            int ng = S.gear.FindAll(x => !x.on).Count, nc = S.chips.FindAll(x => x.at < 0).Count;
            UIKit.LabelOf(gearB).text = L.F("gear_list", ng);
            UIKit.LabelOf(chipB).text = L.F("chip_list", nc);
            UIKit.SetKind(gearB, showChips ? "btn_cream" : "btn_orange");
            UIKit.SetKind(chipB, showChips ? "btn_orange" : "btn_cream");
            UIKit.SetKind(mergeB, Gear.GearMergeCount() + Gear.ChipMergeCount() > 0 ? "btn_green" : "btn_gray");
            // 가방 (11-17 최적화: 칸을 매번 지우고 새로 만들던 것 → 한 번 만든 칸을 다시 씀 · 합성 빨간 점은 같은 것 개수를 한 번 세어 판단)
            int n = 0, need = Gear.MergeNeed;
            if (!showChips)
            {
                var list = S.gear.FindAll(x => !x.on);
                list.Sort((a, b) => b.grade != a.grade ? b.grade.CompareTo(a.grade) : b.lv != a.lv ? b.lv.CompareTo(a.lv) : string.CompareOrdinal(a.part, b.part));
                var cnt = Gear.Counts(true);
                foreach (var g in list)
                {
                    var t = TileAt(n);
                    t.bg.color = Color.Lerp(Color.white, g.G.color, 0.75f);
                    t.icon.sprite = Art.Icon(g.Icon); IconK(t.icon, 0.6f);
                    var gg = g; t.act = () => GearWindow(gg);
                    if (t.label.fontSize != Art.FontSize(26)) t.label.fontSize = Art.FontSize(26);
                    t.label.text = "Lv " + g.lv;
                    t.dot.enabled = g.grade < Tables.GradeMax && cnt.TryGetValue((g.part, g.grade), out var k) && k >= need;
                    n++;
                }
            }
            else
            {
                var list = S.chips.FindAll(x => x.at < 0 && x.Row != null);
                list.Sort((a, b) => b.q != a.q ? b.q.CompareTo(a.q) : string.CompareOrdinal(a.id, b.id));
                var cnt = Gear.Counts(false);
                foreach (var c in list)
                {
                    var t = TileAt(n);
                    t.bg.color = Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f);
                    t.icon.sprite = Art.Icon(c.Icon); IconK(t.icon, 0.48f);
                    var cc = c; t.act = () => ChipWindow(cc);
                    if (t.label.fontSize != Art.FontSize(22)) t.label.fontSize = Art.FontSize(22);
                    t.label.text = Short(c.Name);
                    t.dot.enabled = c.q < Tables.GradeMax && cnt.TryGetValue((c.id, c.q), out var k) && k >= need;
                    n++;
                }
            }
            for (int i = 0; i < bag.Count; i++) { bool on = i < n; if (bag[i].bg.gameObject.activeSelf != on) bag[i].bg.gameObject.SetActive(on); }
            if (!emptyT) { emptyT = UIKit.Label(grid, "-", 40, Art.Soft, TextAnchor.MiddleCenter, false); emptyT.rectTransform.TL(0, 30, 1000, 60); }
            emptyT.enabled = n == 0;
            UIKit.SetScrollH(grid, 20 + Mathf.CeilToInt(n / 5f) * Cell);
        }

        class BagTile { public Image bg, icon, dot; public Text label; public System.Action act; }
        readonly List<BagTile> bag = new();
        Text emptyT;
        const float Cell = 196f;
        BagTile TileAt(int i)
        {
            while (bag.Count <= i)
            {
                int k = bag.Count; var bt = new BagTile();
                bt.bg = UIKit.Tile(grid, "slot", Color.white, null, 0.6f, "Bag" + k);
                bt.bg.rectTransform.TL(14 + k % 5 * Cell, 10 + k / 5 * Cell, 180, 180);
                bt.icon = bt.bg.transform.Find("Icon").GetComponent<Image>(); bt.icon.preserveAspect = true;
                UIKit.Clickable(bt.bg, () => bt.act?.Invoke());
                bt.label = UIKit.Label(bt.bg.transform, "", 26); bt.label.rectTransform.Place(0.5f, 0, new Vector2(0, 4), new Vector2(180, 36), new Vector2(0.5f, 0));
                bt.dot = UIKit.Dot(bt.bg.transform, -14, -14, 30);
                bag.Add(bt);
            }
            return bag[i];
        }
        static void IconK(Image ic, float k)
        {
            var rt = ic.rectTransform; var mn = new Vector2((1 - k) / 2, (1 - k) / 2 + 0.04f);
            if (rt.anchorMin != mn) { rt.anchorMin = mn; rt.anchorMax = new Vector2(1 - (1 - k) / 2, 1 - (1 - k) / 2 + 0.04f); }
        }

        static string Short(string s) => s.Length > 7 ? s.Substring(0, 7) : s;

        /// <summary>일괄 합성 창 (2차 수정안 "장비창에 일괄합성창 추가"): 장비 · 칩 각각 몇 번 되는지 → [합성] → 새로 생긴 것을 아이콘으로</summary>
        public void MergeAll()
        {
            var p = Popup.Make(L.T("merge_title"), 840, 560, () => { Refresh(); lobby.Refresh(); });
            void Row(float y, string icon, string title, int n, System.Action act)
            {
                var r = UIKit.Panel(p.win, "card", "Row"); r.rectTransform.TL(30, y, 780, 160);
                var ic = UIKit.Img(r.transform, Art.Icon(icon), "Icon"); ic.rectTransform.TL(20, 22, 112, 112);
                UIKit.Label(r.transform, title, 36, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(150, 18, 360, 60);
                UIKit.Label(r.transform, n > 0 ? L.F("merge_can", n) : L.T("merge_none"), 28, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(150, 80, 360, 50);
                var b = UIKit.Btn(r.transform, n > 0 ? "btn_purple" : "btn_gray", L.T("merge"), 36, () => { if (n > 0) act(); }, "Go"); ((RectTransform)b.transform).TL(540, 32, 210, 96);
            }
            Row(116, "g_suit_4", L.T("merge_gear"), Gear.GearMergeCount(), () =>
            {
                var made = Gear.MergeAllGear(); p.Close(false); Refresh(); lobby.Refresh();
                var got = new List<GotItem>(); foreach (var g in made) got.Add(GotItem.Of(g));
                if (got.Count > 0) ShopTab.GotPopup(got, L.T("merge_title"));
            });
            Row(292, "chip_4", L.T("merge_chip"), Gear.ChipMergeCount(), () =>
            {
                var made = Gear.MergeAllChipList(); p.Close(false); Refresh(); lobby.Refresh();
                var got = new List<GotItem>(); foreach (var c in made) got.Add(GotItem.Of(c));
                if (got.Count > 0) ShopTab.GotPopup(got, L.T("merge_title"));
            });
        }

        // ---------- 부위 창 (요청 사항_05: 장비 정보 → [강화] [빼기] → 회로 칸 → 회로 설명, 위에서부터 겹치지 않게) ----------
        public void PartWindow(int pi)
        {
            var row = Tables.GearParts[pi];
            var g = Gear.On(row.part);
            var p = Popup.Make(row.name, 960, g != null ? 1010 : 720, () => { Refresh(); lobby.Refresh(); });
            float cy;
            if (g != null)
            {
                GearInfo(p.win, g, 116);
                var up = UIKit.Btn(p.win, g.lv < g.LvMax ? "btn_green" : "btn_gray", L.T("enhance"), 40, () => { p.Close(false); GearUpPopup(g, () => PartWindow(pi)); }, "Up");
                ((RectTransform)up.transform).TL(60, 356, 420, 100);
                var un = UIKit.Btn(p.win, "btn_orange", L.T("unequip"), 40, () => { Gear.Unequip(g); p.Close(false); PartWindow(pi); }, "Unequip");
                ((RectTransform)un.transform).TL(500, 356, 400, 100);
                cy = 484;
            }
            else { p.Text(L.T("empty_part"), 130, 40, Art.Soft, 60); cy = 214; }
            var ln = UIKit.Plain(p.win, new Color(0.23f, 0.16f, 0.11f, 0.2f), "ChipLine"); ln.rectTransform.TL(40, cy - 14, 880, 3);
            var cl = UIKit.Label(p.win, L.T("chip_slots"), 32, Art.Ink, TextAnchor.MiddleLeft, false); cl.rectTransform.TL(50, cy, 400, 48);
            var desc = new List<string>();
            float sy = cy + 56;
            for (int i = 0; i < Gear.Slots; i++)
            {
                int si = i;
                float x = 46 + i * 148;
                if (!Gear.SlotOpen(i))
                {
                    var lk = UIKit.Tile(p.win, "slot", new Color(0.7f, 0.66f, 0.6f), Art.Icon("lock"), 0.5f, "Lock" + i); lk.rectTransform.TL(x, sy, 132, 132);
                    UIKit.Label(p.win, L.F("chip_locked", Gear.SlotNeed(i)), 22, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(x - 8, sy + 136, 148, 34);
                    continue;
                }
                var c = Gear.ChipAt(pi, i);
                if (c != null && c.Row != null)
                {
                    var t = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon(c.Icon), 0.62f, "Chip" + i); t.rectTransform.TL(x, sy, 132, 132);
                    UIKit.Clickable(t, () => { p.Close(false); ChipWindow(c, () => PartWindow(pi)); });
                    UIKit.Label(p.win, Short(c.Name), 22, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.TL(x - 8, sy + 136, 148, 34);
                    desc.Add($"{c.Name} ({Tables.Grade(c.q).name}): {c.Row.Desc(c.q)}");
                }
                else
                {
                    var e = UIKit.Panel(p.win, "slot_dash", "Empty" + i); e.rectTransform.TL(x, sy, 132, 132);
                    var plus = UIKit.Img(e.transform, Art.Icon("plus"), "Plus"); plus.color = new Color(1, 1, 1, 0.5f); plus.rectTransform.Fill(34, 34, 34, 34);
                    UIKit.Clickable(e, () => { p.Close(false); ChipChooser(pi, si); });
                }
            }
            var dt = UIKit.Label(p.win, desc.Count > 0 ? string.Join("\n", desc) : "", 28, Art.Ink, TextAnchor.UpperLeft, false);
            dt.horizontalOverflow = HorizontalWrapMode.Wrap; dt.rectTransform.TL(50, sy + 184, 860, 170);
        }

        /// <summary>착용 완료 창 (요청 사항_05 "장비 착용 시 '착용되었습니다.' + 확인")</summary>
        public static void Equipped(string icon, string name, Color bg, System.Action after = null)
        {
            var p = Popup.Make(L.T("equip"), 700, 560, after);
            var t = UIKit.Tile(p.win, "slot", bg, Art.Icon(icon), 0.7f, "Pic"); t.rectTransform.Place(0.5f, 1, new Vector2(0, -112), new Vector2(150, 150), new Vector2(0.5f, 1));
            t.gameObject.AddComponent<PopIn>();
            p.Text(name, 276, 34, Art.Ink, 50);
            p.Text(L.T("equipped_done"), 330, 38, Art.Green2, 56);
            p.Btn("btn_green", L.T("ok"), 0, 36, 320, p.Close, 100);
        }

        /// <summary>회로 착용 확인 (요청 사항_05 "회로를 + 칸이나 회로가 있는 칸에 넣을 때 옵션 안내 → [확인] 착용되었습니다 · [취소] 이전 단계")</summary>
        static void ChipConfirm(ChipItem c, int part, int slot, System.Action ok)
        {
            var cur = Gear.ChipAt(part, slot);
            var p = Popup.Make(L.T("chip_confirm"), 860, cur != null && cur != c ? 660 : 590);
            var t = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon(c.Icon), 0.7f, "Pic"); t.rectTransform.TL(40, 116, 150, 150);
            UIKit.Label(p.win, $"{c.Name} ({Tables.Grade(c.q).name})", 36, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(214, 122, 620, 54);
            var d = UIKit.Label(p.win, c.Row.Desc(c.q), 32, Art.Green2, TextAnchor.UpperLeft, false); d.horizontalOverflow = HorizontalWrapMode.Wrap; d.rectTransform.TL(214, 182, 610, 90);
            var q = UIKit.Label(p.win, L.F("chip_confirm_q", Tables.GearParts[part].name, slot + 1), 32, Art.Ink, TextAnchor.MiddleCenter, false);
            q.horizontalOverflow = HorizontalWrapMode.Wrap; q.rectTransform.TL(30, 292, 800, 90);
            if (cur != null && cur != c)
            {
                var sw = UIKit.Label(p.win, L.F("chip_swap", $"{cur.Name} ({Tables.Grade(cur.q).name})"), 28, Art.Red, TextAnchor.MiddleCenter, false);
                sw.horizontalOverflow = HorizontalWrapMode.Wrap; sw.rectTransform.TL(30, 384, 800, 70);
            }
            p.Btn("btn_cream", L.T("cancel"), -200, 40, 340, p.Close, 104);
            p.Btn("btn_green", L.T("ok"), 200, 40, 340, () => { p.Close(); ok(); }, 104);
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

        /// <summary>장비 강화 창 (4차 수정안 "장비 강화 시 어느 능력치가 얼마나 올라가는지 알려주는 팝업"):
        /// Lv · 능력치 2개 지금 > 다음 (+늘어나는 값) · 필요한 골드 · 부품 · [강화] (창은 그대로 → 여러 번)</summary>
        public void GearUpPopup(GearItem g, System.Action back = null)
        {
            var p = Popup.Make(L.T("gear_up_title"), 880, 700, () => { Refresh(); lobby.Refresh(); back?.Invoke(); });
            void Build()
            {
                for (int i = p.win.childCount - 1; i >= 0; i--) { var ch = p.win.GetChild(i); if (ch.name.StartsWith("U_")) Destroy(ch.gameObject); }
                var root = UIKit.Rect(p.win, "U_Root"); root.Fill();
                var t = UIKit.Tile(root, "slot", Color.Lerp(Color.white, g.G.color, 0.75f), Art.Icon(g.Icon), 0.72f, "Pic"); t.rectTransform.TL(36, 116, 170, 170);
                UIKit.Label(root, g.Name, 36, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(226, 118, 620, 52);
                bool max = g.lv >= g.LvMax;
                var nx = new GearItem { part = g.part, grade = g.grade, lv = Mathf.Min(g.lv + 1, g.LvMax) };
                var rows = new List<(string, string, string)>
                {
                    ("Lv", g.lv.ToString(), max ? "-" : nx.lv.ToString()),
                    (Gear.StatName(g.Row.stat1), Gear.StatVal(g.Row.stat1, g.V1), max ? "-" : $"{Gear.StatVal(g.Row.stat1, nx.V1)}"),
                    (Gear.StatName(g.Row.stat2), Gear.StatVal(g.Row.stat2, g.V2), max ? "-" : $"{Gear.StatVal(g.Row.stat2, nx.V2)}"),
                };
                for (int i = 0; i < rows.Count; i++)
                {
                    float y = 180 + i * 72;
                    var bg = UIKit.Panel(root, "card_white", "U_Row"); bg.rectTransform.TL(226, y, 620, 64);
                    UIKit.Label(bg.transform, rows[i].Item1, 28, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(16, 4, 200, 56);
                    UIKit.Label(bg.transform, rows[i].Item2, 32, Art.Ink, TextAnchor.MiddleRight, false).rectTransform.TL(200, 4, 170, 56);
                    UIKit.Label(bg.transform, ">", 32, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(372, 4, 44, 56);
                    UIKit.Label(bg.transform, rows[i].Item3, 34, Art.Green2, TextAnchor.MiddleLeft, false).rectTransform.TL(420, 4, 190, 56);
                }
                if (!max)
                {   // 늘어나는 값 한 줄 (예: 공격력 +6 · 기지 체력 +40)
                    string up = $"{Gear.StatName(g.Row.stat1)} {Gear.StatVal(g.Row.stat1, nx.V1 - g.V1)}   {Gear.StatName(g.Row.stat2)} {Gear.StatVal(g.Row.stat2, nx.V2 - g.V2)}";
                    UIKit.Label(root, up, 30, Art.Green2, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 410, 880, 46);
                    var cost = UIKit.Rect(root, "U_Cost"); cost.TL(0, 470, 880, 60);
                    var ci = UIKit.Img(cost, Art.Icon("coin"), "Coin"); ci.rectTransform.TL(230, 4, 52, 52);
                    UIKit.Label(cost, Num.F(Gear.UpGold(g)), 32, S.gold >= Gear.UpGold(g) ? Art.Ink : Art.Red, TextAnchor.MiddleLeft, false).rectTransform.TL(290, 4, 200, 52);
                    var pi = UIKit.Img(cost, Art.Icon("parts"), "Parts"); pi.rectTransform.TL(500, 4, 52, 52);
                    UIKit.Label(cost, $"{Num.F(S.parts)} / {Gear.UpParts(g)}", 32, S.parts >= Gear.UpParts(g) ? Art.Ink : Art.Red, TextAnchor.MiddleLeft, false).rectTransform.TL(560, 4, 260, 52);
                }
                var ub = UIKit.Btn(root, Gear.CanUp(g) ? "btn_green" : "btn_gray", max ? L.T("max") : L.T("enhance"), 44, () =>
                {
                    if (Gear.Up(g)) { Fx.Ring(Vector2.zero, 1, Art.Gold); Build(); Refresh(); lobby.Refresh(); }
                }, "U_Up");
                ((RectTransform)ub.transform).Place(0.5f, 0, new Vector2(0, 40), new Vector2(420, 116), new Vector2(0.5f, 0));
            }
            Build();
        }

        /// <summary>장비 정보 (그림 · 이름 · 등급 · 레벨 · 능력치 2개)</summary>
        static void GearInfo(RectTransform w, GearItem g, float y, GearItem cmp = null)
        {
            var t = UIKit.Tile(w, "slot", Color.Lerp(Color.white, g.G.color, 0.75f), Art.Icon(g.Icon), 0.72f, "Pic"); t.rectTransform.TL(44, y, 220, 220);
            var nm = UIKit.Label(w, g.Name, 40, Art.Ink, TextAnchor.MiddleLeft, false); nm.rectTransform.TL(286, y, 640, 56);
            var gr = UIKit.Panel(w, "pill", "Grade"); gr.color = g.G.color; gr.rectTransform.TL(286, y + 60, 150, 48);
            UIKit.Label(gr.transform, g.G.name, 28, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
            UIKit.Label(w, L.F("gear_lv", g.lv, g.LvMax), 30, Art.Brown, TextAnchor.MiddleLeft, false).rectTransform.TL(452, y + 60, 300, 48);
            // 4차 수정안 "정보창 글자가 잘 안 보임": 능력치 2개를 흰 칸에 크게 (비교는 초록 · 빨강)
            void Stat(int i, string k, double v, double? o)
            {
                var box = UIKit.Panel(w, "card_white", "Stat" + i); box.rectTransform.TL(286 + i * 320, y + 116, 308, 104);
                UIKit.Label(box.transform, Gear.StatName(k), 24, Art.Soft, TextAnchor.UpperCenter, false).rectTransform.Fill(4, 50, 4, 10);
                string d = o.HasValue && System.Math.Abs(v - o.Value) > 1e-6 ? $" <color=#{(v >= o.Value ? "3f8f3a" : "c8503c")}>{(v >= o.Value ? "+" : "-")}{Gear.StatVal(k, System.Math.Abs(v - o.Value)).TrimStart('+', '-')}</color>" : "";
                var vt = UIKit.Label(box.transform, Gear.StatVal(k, v) + d, 34, Art.Ink, TextAnchor.LowerCenter, false); vt.rectTransform.Fill(4, 16, 4, 36);
            }
            Stat(0, g.Row.stat1, g.V1, cmp?.V1); Stat(1, g.Row.stat2, g.V2, cmp?.V2);
        }

        // ---------- 가방 장비 창 ----------
        void GearWindow(GearItem g)
        {
            var p = Popup.Make(g.Name, 960, 900, () => { Refresh(); lobby.Refresh(); });
            var on = Gear.On(g.part);
            GearInfo(p.win, g, 116, on);
            if (on != null) p.Text($"{L.T("equip")}: {on.Name} Lv {on.lv}", 350, 28, Art.Soft);
            p.Btn("btn_green", L.T("equip"), -230, 380, 400, () => { Gear.Equip(g); p.Close(); Equipped(g.Icon, g.Name, Color.Lerp(Color.white, g.G.color, 0.75f)); }, 110);
            p.Btn(g.lv < g.LvMax ? "btn_orange" : "btn_gray", L.T("enhance"), 230, 380, 400, () => { p.Close(false); GearUpPopup(g, () => GearWindow(g)); }, 110);
            var set = Gear.MergeSet(g);
            int same = S.gear.FindAll(x => !x.on && x.part == g.part && x.grade == g.grade).Count;
            p.Btn(set != null ? "btn_purple" : "btn_gray", $"{L.T("merge")} {same}/{Tables.Ci("Merge_N", 3)}", -230, 240, 400, () =>
            {
                var n = Gear.Merge(g); if (n == null) return;
                p.Close(false); Toast.Show(n.Name); GearWindow(n);
            }, 110);
            p.Btn("btn_red", $"{L.T("scrap")} +{Gear.ScrapParts(g)}", 230, 240, 400, () => { Gear.Scrap(g); p.Close(); }, 110);
            p.Text(g.grade >= Tables.GradeMax ? "" : $"{L.T("merge")}: {Tables.Grade(g.grade).name} x{Tables.Ci("Merge_N", 3)} > {Tables.Grade(g.grade + 1).name}", 790, 28, Art.Soft);
        }

        // ---------- 회로 창 (4차 수정안: 능력 글자 크게 · 착용 가능 부위를 아이콘 + 이름으로 · 컴팩트) ----------
        public void ChipWindow(ChipItem c, System.Action back = null)
        {
            var r = c.Row;
            var p = Popup.Make(c.Name, 940, 1000, () => { Refresh(); lobby.Refresh(); back?.Invoke(); });
            var t = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon(c.Icon), 0.7f, "Pic"); t.rectTransform.TL(40, 116, 190, 190);
            var gr = UIKit.Panel(p.win, "pill", "Grade"); gr.color = Tables.Grade(c.q).color; gr.rectTransform.TL(256, 120, 150, 48);
            UIKit.Label(gr.transform, Tables.Grade(c.q).name, 28, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(0, 4, 0, 0);
            if (!string.IsNullOrEmpty(r.turret) && Tables.TurretById.TryGetValue(r.turret, out var tr))
            {
                var ti = UIKit.Img(p.win, Art.TurretIcon(tr), "Turret"); ti.rectTransform.TL(424, 112, 60, 60);
                UIKit.Label(p.win, tr.name, 28, Art.Brown, TextAnchor.MiddleLeft, false).rectTransform.TL(490, 120, 400, 48);
            }
            // 능력 (흰 칸 · 큰 글자)
            var ab = UIKit.Panel(p.win, "card_white", "Ability"); ab.rectTransform.TL(256, 180, 644, 156);
            UIKit.Label(ab.transform, L.T("chip_ability"), 24, Art.Soft, TextAnchor.MiddleLeft, false).rectTransform.TL(22, 8, 300, 34);
            var d = UIKit.Label(ab.transform, r.Desc(c.q), 36, Art.Ink, TextAnchor.MiddleLeft, false); d.horizontalOverflow = HorizontalWrapMode.Wrap; d.verticalOverflow = VerticalWrapMode.Truncate;
            d.rectTransform.TL(22, 44, 604, 98);
            d.resizeTextForBestFit = true; d.resizeTextMinSize = Art.FontSize(24); d.resizeTextMaxSize = Art.FontSize(36);
            // 착용 가능 부위
            UIKit.Label(p.win, L.T("chip_fit"), 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(44, 354, 600, 48);
            int n = 0;
            foreach (var gp in Tables.GearParts)
            {
                if (!r.Fits(gp.part)) continue;
                var pill = UIKit.Panel(p.win, "card_white", "Fit_" + gp.part); pill.rectTransform.TL(40 + n % 3 * 290, 408 + n / 3 * 92, 280, 82);
                var on = Gear.On(gp.part);
                var ic = UIKit.Img(pill.transform, Art.Icon(on != null ? on.Icon : gp.icon), "Icon"); ic.rectTransform.TL(10, 8, 64, 64);
                UIKit.Label(pill.transform, gp.name, 28, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(80, 6, 196, 66);
                n++;
            }
            if (c.at >= 0) p.Btn("btn_orange", L.T("unequip"), -230, 200, 400, () => { Gear.TakeChip(c); p.Close(); }, 110);
            else p.Btn("btn_green", L.T("equip"), -230, 200, 400, () =>
            {
                p.Close(false); ChipPlace(c, back);
            }, 110);
            int same = S.chips.FindAll(x => x.at < 0 && x.id == c.id && x.q == c.q).Count;
            p.Btn(Gear.ChipMergeSet(c) != null ? "btn_purple" : "btn_gray", $"{L.T("merge")} {same}/{Tables.Ci("Merge_N", 3)}", 230, 200, 400, () =>
            {
                var nn = Gear.MergeChip(c); if (nn == null) return;
                p.Close(false); Toast.Show(nn.Name); ChipWindow(nn, back);
            }, 110);
            p.Btn("btn_red", $"{L.T("scrap")} +{Tables.Ci("Chip_Scrap_Parts", 1) * c.q}", 0, 60, 400, () => { Gear.ScrapChip(c); p.Close(); }, 110);
        }

        /// <summary>회로 착용 안내 (3차 수정안 "칩 착용을 눌렀을 때 어디에 착용할지 안내 팝업"):
        /// 6부위 x 6칸 — 착용할 수 있는 부위는 밝게(빈 칸 초록), 못 하는 부위는 어둡게 · 빈 칸을 누르면 착용 · 회로가 있는 칸을 누르면 바꿔 끼움</summary>
        public void ChipPlace(ChipItem c, System.Action back = null)
        {
            var p = Popup.Make(L.T("chip_where"), 1000, 1340, () => { Refresh(); lobby.Refresh(); back?.Invoke(); });
            var head = UIKit.Tile(p.win, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon(c.Icon), 0.7f, "Me"); head.rectTransform.TL(40, 110, 116, 116);
            UIKit.Label(p.win, $"{c.Name} ({Tables.Grade(c.q).name})", 34, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(176, 112, 780, 52);
            var ds = UIKit.Label(p.win, c.Row.Desc(c.q), 30, Art.Green2, TextAnchor.MiddleLeft, false); ds.rectTransform.TL(176, 166, 780, 50);
            UIKit.Label(p.win, L.T("chip_tap_slot"), 26, Art.Hex("#6a4fa8"), TextAnchor.MiddleCenter, false).rectTransform.TL(0, 232, 1000, 40);
            for (int pi = 0; pi < Tables.GearParts.Count; pi++)
            {
                var part = Tables.GearParts[pi]; bool fit = c.Row.Fits(part.part);
                float y = 284 + pi * 154;
                var row = UIKit.Panel(p.win, fit ? "card" : "btn_gray", "P_" + part.part); row.rectTransform.TL(30, y, 940, 144);
                var ic = UIKit.Img(row.transform, Art.Icon(Gear.On(part.part)?.Icon ?? part.icon), "Icon"); ic.rectTransform.TL(26, 8, 84, 84);
                if (!fit) ic.color = new Color(1, 1, 1, 0.45f);
                UIKit.Label(row.transform, part.name, 24, fit ? Art.Ink : Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(0, 92, 136, 34);
                if (!fit) { UIKit.Label(row.transform, L.T("chip_nofit"), 28, Art.Soft, TextAnchor.MiddleCenter, false).rectTransform.TL(260, 36, 660, 60); continue; }
                for (int si = 0; si < Gear.Slots; si++)
                {
                    int ppi = pi, ssi = si; float x = 140 + si * 130;
                    bool open = Gear.SlotOpen(si); var cur = Gear.ChipAt(pi, si);
                    var sl = UIKit.Tile(row.transform, "slot", !open ? new Color(0.6f, 0.56f, 0.5f) : cur != null ? Color.Lerp(Color.white, Tables.Grade(cur.q).color, 0.7f) : Art.Hex("#cdeeb8"),
                                        !open ? Art.Icon("lock") : cur != null ? Art.Icon(cur.Icon) : Art.Icon("plus"), cur != null ? 0.62f : 0.42f, "S" + si);
                    sl.rectTransform.TL(x, 14, 110, 110);
                    if (!open) { UIKit.Label(sl.transform, L.F("chip_locked", Gear.SlotNeed(si)), 18, Art.Ink, TextAnchor.LowerCenter, false).rectTransform.Fill(0, 12, 0, 0); continue; }
                    if (cur == c) { UIKit.Panel(sl.transform, "glow_frame", "Here").rectTransform.Fill(-14, -14, -14, -14); continue; }
                    UIKit.Clickable(sl, () => ChipConfirm(c, ppi, ssi, () =>
                    {
                        Gear.PutChip(c, ppi, ssi); p.Close();
                        Equipped(c.Icon, $"{c.Name} ({Tables.Grade(c.q).name})", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f));
                    }));
                }
            }
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
                var t = UIKit.Tile(row.transform, "slot", Color.Lerp(Color.white, Tables.Grade(c.q).color, 0.75f), Art.Icon(c.Icon), 0.5f, "Pic"); t.rectTransform.TL(14, 12, 106, 106);
                UIKit.Label(row.transform, c.Name, 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(140, 14, 760, 48);
                var ds = UIKit.Label(row.transform, c.Row.Desc(c.q), 26, Art.Soft, TextAnchor.MiddleLeft, false); ds.rectTransform.TL(140, 66, 760, 44);
                var cc = c; UIKit.Clickable(row, () => ChipConfirm(cc, pi, slot, () =>
                {
                    Gear.PutChip(cc, pi, slot); p.Close();
                    Equipped(cc.Icon, $"{cc.Name} ({Tables.Grade(cc.q).name})", Color.Lerp(Color.white, Tables.Grade(cc.q).color, 0.75f));
                }));
                y += 140;
            }
            UIKit.SetScrollH(c0, y + 10);
        }
    }

    /// <summary>7차 수정안: 장비 탭 대장 카피 — 무중력처럼 둥실둥실 떠다님 (2D). 칸 사이 빈 곳(가운데 ±Range) 안에서 다음 자리를 골라 천천히 가고,
    /// 가는 쪽을 보며(좌우 뒤집기) 속도만큼 살짝 기울고 · 위아래로 출렁 · 천천히 빙글</summary>
    public class Float2D : MonoBehaviour
    {
        public static Vector2 Range = new(120f, 90f);
        RectTransform rt; Vector2 p0, pos, vel, target; float t, wait, face = 1;
        public Vector2 Offset => pos;         // 시험용: 처음 자리에서 떨어진 정도
        public float Face => face;
        void Start() { rt = (RectTransform)transform; p0 = rt.anchoredPosition; Next(); }
        void Next() { target = new Vector2(Random.Range(-Range.x, Range.x), Random.Range(-Range.y, Range.y)); wait = Random.Range(2.2f, 3.6f); }
        /// <summary>8차 수정안 "대장 카피가 좌우로 바뀌는 게 아니라 제자리에서 둥실둥실 뜨는 느낌" — 1 = 제자리(위아래로만 둥실 · 몸은 늘 오른쪽) · 0 = 7차처럼 떠다님</summary>
        public static bool InPlace = true;
        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            t += dt;
            if (InPlace)
            {   // 위아래 둥실(느린 큰 물결 + 빠른 작은 물결) + 아주 살짝 기울기 — 뒤집지 않음
                pos = new Vector2(0, Mathf.Sin(t * 1.5f) * 16f + Mathf.Sin(t * 3.1f) * 3f);
                face = 1;
                rt.anchoredPosition = p0 + pos;
                rt.localScale = Vector3.one;
                rt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 0.9f) * 3f);
                return;
            }
            if ((wait -= dt) <= 0) Next();
            pos = Vector2.SmoothDamp(pos, target, ref vel, 1.4f, 90f, dt);
            if (Mathf.Abs(vel.x) > 6f) face = Mathf.MoveTowards(face, vel.x < 0 ? -1 : 1, dt * 3f);
            float bob = Mathf.Sin(t * 1.7f) * 10f;
            rt.anchoredPosition = p0 + pos + new Vector2(0, bob);
            rt.localScale = new Vector3(Mathf.Abs(face) < 0.15f ? 0.15f * Mathf.Sign(face == 0 ? 1 : face) : face, 1, 1);
            rt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 0.8f) * 7f - vel.x * 0.08f * Mathf.Sign(face));
        }
    }
}
