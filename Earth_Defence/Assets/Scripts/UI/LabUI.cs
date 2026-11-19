using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 터렛 시험장 화면: 위 = 속도 · 적 · 카드 상태 · 대장 카피 쏘기 · 모두 빼기 / 그 아래 터렛 11종 (끌어서 아래 칸에 놓기 · 누르면 빈 칸에)
    /// / 놓인 터렛 위 = 이름 · 카드 상태 · 초당 피해(최근 5초) — 누르면 빼기.
    /// </summary>
    public class LabUI : MonoBehaviour
    {
        public Battle b;
        public string preset = "base";
        public static readonly float[] Speeds = { 0.25f, 0.5f, 1f, 2f, 4f };
        readonly List<(Image bg, float v)> speedBtns = new();
        readonly List<(Image bg, string m)> modeBtns = new();
        readonly List<(Image bg, string p)> presetBtns = new();
        Button gunB;
        readonly Dictionary<int, (RectTransform box, Text name, Text dps)> labels = new();
        readonly Dictionary<TurretInst, Queue<(float t, double d)>> hist = new();
        public static readonly string[] Modes = { "swarm", "dummy", "boss", "none" };

        public void Build(Battle battle)
        {
            b = battle;
            var root = (RectTransform)transform;
            var top = UIKit.Panel(root, "card", "Top"); top.rectTransform.TL(14, 10, 1052, 460);
            UIKit.Label(top.transform, L.T("lab_title"), 40, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(30, 14, 400, 60);
            Row(top.transform, 80, L.T("lab_speed"), 5, i => $"x{Speeds[i]}", i => SetSpeed(Speeds[i]), speedBtns, i => Speeds[i]);
            Row(top.transform, 172, L.T("lab_enemy"), 4, i => L.T("lab_m_" + Modes[i]), i => SetMode(Modes[i]), modeBtns, i => Modes[i]);
            Row(top.transform, 264, L.T("lab_card"), 4, i => L.T("lab_p_" + Lab.Presets[i]), i => SetPreset(Lab.Presets[i]), presetBtns, i => Lab.Presets[i]);
            gunB = UIKit.Btn(top.transform, "btn_cream", "", 28, () => { b.lab.guardianFire = !b.lab.guardianFire; Refresh(); }, "Gun");
            ((RectTransform)gunB.transform).TL(30, 360, 480, 84);
            UIKit.Btn(top.transform, "btn_red", L.T("lab_clear"), 30, () => { for (int s = 1; s <= 4; s++) b.LabRemove(s); }, "Clear").GetComponent<RectTransform>().TL(530, 360, 240, 84);
            UIKit.Btn(top.transform, "btn_orange", L.T("lab_reset_dmg"), 28, () => hist.Clear(), "ResetDps").GetComponent<RectTransform>().TL(784, 360, 240, 84);

            var pal = UIKit.Panel(root, "card_white", "Palette"); pal.rectTransform.TL(14, 478, 1052, 340);
            int n = 0;
            foreach (var t in Tables.Turrets)
            {
                if (t.kind == "gun") continue;
                float x = 22 + n % 6 * 170, y = 14 + n / 6 * 164;
                var tile = UIKit.Tile(pal.transform, "slot", Color.white, Art.TurretUI(t.art), 0.86f, "T_" + t.id); tile.rectTransform.TL(x, y, 120, 120);
                tile.raycastTarget = true;
                tile.gameObject.AddComponent<LabDrag>().Setup(this, t.id);
                UIKit.Label(pal.transform, t.name, 22, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.TL(x - 20, y + 122, 160, 32);
                n++;
            }
            var hint = UIKit.Label(pal.transform, L.T("lab_hint"), 20, Art.Soft, TextAnchor.MiddleLeft, false);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap; hint.rectTransform.TL(866, 180, 172, 150);   // 둘째 줄 빈 칸 (터렛 11개 = 6 + 5)
            for (int s = 0; s <= 4; s++) Label(s);
            SetMode("dummy"); SetSpeed(1f);
        }

        void Row<T>(Transform p, float y, string title, int count, Func<int, string> label, Action<int> act, List<(Image, T)> list, Func<int, T> key)
        {
            UIKit.Label(p, title, 30, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(30, y, 140, 84);
            float w = (1052 - 180 - 20 - (count - 1) * 10) / count;
            for (int i = 0; i < count; i++)
            {
                int k = i;
                var bt = UIKit.Btn(p, "btn_cream", label(i), 30, () => act(k), "B" + i);
                ((RectTransform)bt.transform).TL(180 + i * (w + 10), y, w, 84);
                list.Add((bt.GetComponent<Image>(), key(i)));
            }
        }

        void Label(int slot)
        {
            var box = UIKit.Panel(transform, "pill", "Slot" + slot); box.rectTransform.sizeDelta = new Vector2(230, 96);
            box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            var nm = UIKit.Label(box.transform, "", 24, Art.Ink, TextAnchor.UpperCenter, false); nm.rectTransform.Fill(6, 40, 6, 10);
            var dps = UIKit.Label(box.transform, "", 24, Art.Brown, TextAnchor.LowerCenter, false); dps.rectTransform.Fill(6, 14, 6, 44);
            if (slot > 0) { int s = slot; UIKit.Clickable(box, () => b.LabRemove(s)); }
            labels[slot] = (box.rectTransform, nm, dps);
        }

        public void Place(int slot, string id) => b.LabPlace(slot, id, preset);

        /// <summary>누르기만 하면: 빈 칸 첫 곳 (없으면 칸 1)</summary>
        public void PlaceFree(string id)
        {
            for (int s = 1; s <= 4; s++) if (!b.run.turrets.Exists(t => t.slot == s)) { Place(s, id); return; }
            Place(1, id);
        }

        public void SetSpeed(float v) { b.speed = v; Refresh(); }
        public void SetMode(string m) { b.lab.SetMode(b, m); Refresh(); }
        public void SetPreset(string p)
        {
            preset = p;
            foreach (var t in b.run.turrets) if (t.slot > 0) { Lab.ApplyPreset(t, p); t.unit?.RefreshTier(); }
            hist.Clear();
            Refresh();
        }

        void Refresh()
        {
            foreach (var (bg, v) in speedBtns) bg.sprite = Art.UI(Mathf.Approximately(v, b.speed) ? "btn_orange" : "btn_cream");
            foreach (var (bg, m) in modeBtns) bg.sprite = Art.UI(m == b.lab.mode ? "btn_orange" : "btn_cream");
            foreach (var (bg, p) in presetBtns) bg.sprite = Art.UI(p == preset ? "btn_purple" : "btn_cream");
            UIKit.SetKind(gunB, b.lab.guardianFire ? "btn_green" : "btn_gray");
            UIKit.LabelOf(gunB).text = L.T(b.lab.guardianFire ? "lab_gun_on" : "lab_gun_off");
        }

        /// <summary>최근 5초 초당 피해</summary>
        public double Dps(TurretInst t)
        {
            if (!hist.TryGetValue(t, out var q)) return 0;
            if (q.Count < 2) return 0;
            var a = q.Peek(); float span = b.time - a.t;
            return span > 0.2f ? (t.dmgDone - a.d) / span : 0;
        }

        float sample;
        void Update()
        {
            if (b == null) return;
            if ((sample -= Time.deltaTime) <= 0)
            {
                sample = 0.25f;
                foreach (var t in b.run.turrets)
                {
                    if (!hist.TryGetValue(t, out var q)) hist[t] = q = new Queue<(float, double)>();
                    q.Enqueue((b.time, t.dmgDone));
                    while (q.Count > 1 && b.time - q.Peek().t > 5f) q.Dequeue();
                }
            }
            for (int s = 0; s <= 4; s++)
            {
                var (box, nm, dps) = labels[s];
                var t = b.run.turrets.Find(x => x.slot == s);
                box.gameObject.SetActive(t != null);
                if (t == null) continue;
                // 11-18 사령탑: 칸 오른쪽에 이름 · 초당 피해
                var wp = s == 0 ? (t.unit ? (Vector2)t.unit.transform.position : World.GuardianPos) : World.SlotPos(s);
                box.anchoredPosition = UIRoot.I.WorldToUI(World.V ? new Vector2(Mathf.Clamp(wp.x, -4.25f, 4.25f), World.GroundY + (s == 0 ? 2.2f : 2.5f) + (s >= 3 ? 1.05f : 0)) : new Vector2(wp.x + 2.6f, wp.y + 0.9f));   // 9차 세로 = 칸 위
                nm.text = s == 0 ? t.row.name : $"{t.row.name} {L.T("lab_p_" + PresetOf(t))}";
                dps.text = L.F("lab_dps", Num.F(Dps(t)));
            }
        }

        static string PresetOf(TurretInst t) => t.tier >= 3 ? "all" : t.tier == 2 ? (HasLine(t, "A") ? "A" : "B") : "base";
        static bool HasLine(TurretInst t, string line) { foreach (var id in t.picks.Keys) if (Tables.CardById.TryGetValue(id, out var c) && c.kind == "break" && c.line == line) return true; return false; }
    }

    /// <summary>시험장 터렛 칸: 끌어서 아래 칸에 놓기 · 누르면 빈 칸에</summary>
    public class LabDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        LabUI ui; string id; RectTransform ghost; bool dragging;
        public void Setup(LabUI u, string turret) { ui = u; id = turret; }

        public void OnBeginDrag(PointerEventData e)
        {
            dragging = true;
            var g = UIKit.Img(UIRoot.I.popups, Art.TurretUI(Tables.TurretById[id].art), "Ghost"); g.raycastTarget = false; g.color = new Color(1, 1, 1, 0.8f);
            ghost = g.rectTransform; ghost.sizeDelta = new Vector2(150, 150); ghost.anchorMin = ghost.anchorMax = new Vector2(0.5f, 0.5f);
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (!ghost) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(UIRoot.I.popups, e.position, e.pressEventCamera, out var lp);
            ghost.anchoredPosition = lp;
        }
        public void OnEndDrag(PointerEventData e)
        {
            dragging = false;
            if (ghost) Destroy(ghost.gameObject);
            var w = World.I.ScreenToWorld(e.position);
            if (World.V ? w.y > -1f : w.x > 0f) return;      // 9차 세로 = 위쪽에 놓으면 취소                            // 사령탑에서 먼 곳(오른쪽)에 놓으면 취소 (11-18)
            int best = 1; float bd = 99;
            for (int s = 1; s <= 4; s++) { float d = Vector2.Distance(World.SlotPos(s) + new Vector2(0, 0.7f), w); if (d < bd) { bd = d; best = s; } }
            ui.Place(best, id);
        }
        public void OnPointerClick(PointerEventData e) { if (!dragging && !e.dragging) ui.PlaceFree(id); }
    }
}
