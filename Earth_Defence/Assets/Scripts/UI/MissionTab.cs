using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>로비 미션 탭 (2차): 위 = 출석 7일 줄 + [오늘 출석 받기] / 아래 = 일일 미션 (진행 막대 · 보상 · [받기]). 매일 밤 12시(기기 시간)에 새로.</summary>
    public class MissionTab : MonoBehaviour
    {
        static SaveData S => SaveData.Cur;
        Lobby lobby;
        RectTransform root, list;
        readonly List<Image> attendTiles = new();
        Button attendB;

        public void Build(Lobby l)
        {
            lobby = l; root = (RectTransform)transform;
            var at = UIKit.Panel(root, "card", "Attend"); at.rectTransform.TL(20, 4, 1040, 430);
            var ic = UIKit.Img(at.transform, Art.Icon("calendar"), "Icon"); ic.rectTransform.TL(26, 18, 74, 74);
            UIKit.Label(at.transform, L.T("attend_title"), 40, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(112, 22, 600, 66);
            AttendRow(at.transform, 104, attendTiles);
            attendB = UIKit.Btn(at.transform, "btn_green", "", 36, () => { ClaimAttend(); }, "Claim"); ((RectTransform)attendB.transform).TL(300, 318, 440, 98);

            var mp = UIKit.Panel(root, "card_white", "Missions");
            mp.rectTransform.anchorMin = Vector2.zero; mp.rectTransform.anchorMax = Vector2.one;
            mp.rectTransform.offsetMin = new Vector2(20, 6); mp.rectTransform.offsetMax = new Vector2(-20, -448);
            var mi = UIKit.Img(mp.transform, Art.Icon("mission"), "Icon"); mi.rectTransform.TL(26, 18, 70, 70);
            UIKit.Label(mp.transform, L.T("mission_title"), 40, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(108, 20, 500, 66);
            UIKit.Label(mp.transform, L.T("mission_reset"), 24, Art.Soft, TextAnchor.MiddleRight, false).rectTransform.TL(560, 30, 440, 50);
            list = UIKit.Scroll(mp.transform, "Scroll"); list.parent.GetComponent<RectTransform>().Fill(8, 12, 8, 100);
        }

        /// <summary>출석 7칸 (로비 탭 · 들어올 때 창 같이 씀)</summary>
        static void AttendRow(Transform p, float y, List<Image> tiles)
        {
            tiles.Clear();
            int days = Meta.AttendDays, today = Meta.AttendIdx;
            float w = 136, gap = 10, x0 = (1040 - (days * w + (days - 1) * gap)) / 2;
            for (int i = 0; i < days && i < Tables.Attend.Count; i++)
            {
                var r = Tables.Attend[i];
                bool got = i < today || (i == today && !Meta.AttendReady);
                bool now = i == today && Meta.AttendReady;
                var t = UIKit.Panel(p, now ? "btn_orange" : got ? "btn_gray" : "slot", "Day" + i);
                t.rectTransform.TL(x0 + i * (w + gap), y, w, 196);
                UIKit.Label(t.transform, L.F("attend_day", r.day), 26, now ? Color.white : Art.Ink, TextAnchor.MiddleCenter, now).rectTransform.Place(0.5f, 1, new Vector2(0, -8), new Vector2(w, 34), new Vector2(0.5f, 1));
                var ic = UIKit.Img(t.transform, Art.Icon(Meta.RewardIcon(r.reward)), "Icon"); ic.raycastTarget = false;
                ic.rectTransform.Place(0.5f, 1, new Vector2(0, -44), new Vector2(76, 76), new Vector2(0.5f, 1));
                var amt = UIKit.Label(t.transform, Meta.RewardText(r.reward, r.value), r.reward is "chip" or "gear" ? 20 : 26, Color.white);
                amt.rectTransform.Place(0.5f, 0, new Vector2(0, 16), new Vector2(w, 40), new Vector2(0.5f, 0));
                if (got) { var ck = UIKit.Img(t.transform, Art.Icon("check"), "Got"); ck.raycastTarget = false; ck.rectTransform.Place(0.5f, 0.5f, new Vector2(0, 10), new Vector2(80, 80)); }
                tiles.Add(t);
            }
        }

        void ClaimAttend()
        {
            var s = Meta.AttendClaim();
            if (s != null) Toast.Show(L.T("reward_get") + " " + s);
            lobby.Refresh();
            lobby.SetTab(lobby.tab);
        }

        /// <summary>로비에 들어왔는데 오늘 출석을 안 받았으면 (처음 켠 날 포함)</summary>
        public static void ShowAttend(Action next)
        {
            var p = Popup.Make(L.T("attend_title"), 1060, 640, next);
            var row = UIKit.Rect(p.win, "Row"); row.TL(10, 130, 1040, 210);
            AttendRow(row, 0, new List<Image>());
            p.Btn("btn_green", L.T("attend_claim"), 0, 70, 520, () =>
            {
                var s = Meta.AttendClaim();
                p.Close();
                if (s != null) Toast.Show(L.T("reward_get") + " " + s);
                Game.Lobby?.Refresh();
            }, 120);
        }

        public void Refresh()
        {
            if (list == null) return;
            UIKit.SetKind(attendB, Meta.AttendReady ? "btn_green" : "btn_gray");
            UIKit.LabelOf(attendB).text = Meta.AttendReady ? L.T("attend_claim") : L.T("attend_done");
            for (int c = list.childCount - 1; c >= 0; c--) Destroy(list.GetChild(c).gameObject);
            float y = 6;
            for (int i = 0; i < Tables.Missions.Count; i++)
            {
                var m = Tables.Missions[i]; int idx = i;
                int prog = Meta.MissionProg(i); bool got = Meta.MissionGot(i), ready = Meta.MissionReady(i);
                var r = UIKit.Panel(list, got ? "btn_gray" : "card", "M_" + m.id); r.rectTransform.TL(10, y, 980, 150);
                UIKit.Label(r.transform, m.name.Replace("{0}", Num.F(m.target)), 32, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(26, 14, 560, 50);
                var bar = UIKit.Bar(r.transform, Art.Green, "Prog"); bar.transform.parent.GetComponent<RectTransform>().TL(26, 76, 440, 40);
                UIKit.SetBar(bar, (float)prog / m.target);
                UIKit.Label(bar.transform.parent, $"{Num.F(prog)}/{Num.F(m.target)}", 24, Color.white).rectTransform.Fill(0, 2, 0, 0);
                var ic = UIKit.Img(r.transform, Art.Icon(Meta.RewardIcon(m.reward)), "Reward"); ic.rectTransform.TL(500, 28, 84, 84);
                UIKit.Label(r.transform, Meta.RewardText(m.reward, m.value), 26, Art.Brown, TextAnchor.MiddleLeft, false).rectTransform.TL(590, 40, 170, 60);
                var b = UIKit.Btn(r.transform, ready ? "btn_green" : "btn_gray", got ? L.T("claimed") : L.T("claim"), 32, () =>
                {
                    var s = Meta.MissionClaim(idx);
                    if (s != null) Toast.Show(L.T("reward_get") + " " + s);
                    lobby.Refresh();
                }, "Claim");
                ((RectTransform)b.transform).TL(770, 22, 190, 100);
                if (ready) UIKit.Dot(b.transform, -12, -10, 30);
                y += 162;
            }
            UIKit.SetScrollH(list, y + 10);
        }
    }

    /// <summary>
    /// 설정 (2차): 배경음 · 효과음(값만 저장 — 소리는 다음 작업) · 진동 · 피해 숫자 · 프레임 60/30 · 도움말 다시 보기.
    /// </summary>
    public static class Settings
    {
        static SaveData S => SaveData.Cur;
        public static void Open()
        {
            var p = Popup.Make(L.T("settings"), 900, 1180, () => S.Save());
            float y = 140;
            Volume(p, ref y, L.T("set_bgm"), "music", () => S.bgm, v => S.bgm = v);
            Volume(p, ref y, L.T("set_sfx"), "sound", () => S.sfx, v => S.sfx = v);
            Toggle(p, ref y, L.T("set_vib"), () => S.vib, v => S.vib = v);
            Toggle(p, ref y, L.T("set_nums"), () => S.nums, v => S.nums = v);
            Toggle(p, ref y, L.T("set_fps"), () => S.fps60, v => { S.fps60 = v; Apply(); });
            p.Btn("btn_orange", L.T("set_tutorial"), 0, 70, 520, () => { S.tut.Clear(); S.Save(); Toast.Show(L.T("ok")); p.Close(); }, 110);
        }

        public static void Apply() { Application.targetFrameRate = S == null || S.fps60 ? 60 : 30; }

        static void Volume(Popup p, ref float y, string name, string icon, Func<float> get, Action<float> set)
        {
            var ic = UIKit.Img(p.win, Art.Icon(icon), "Icon"); ic.rectTransform.TL(60, y + 6, 76, 76);
            UIKit.Label(p.win, name, 36, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(150, y, 260, 88);
            var val = UIKit.Label(p.win, "", 36, Art.Brown, TextAnchor.MiddleCenter, false); val.rectTransform.TL(560, y, 140, 88);
            void Show() => val.text = Mathf.RoundToInt(get() * 100) + "%";
            var m = UIKit.Btn(p.win, "btn_cream", "-", 48, () => { set(Mathf.Clamp01(Mathf.Round((get() - 0.1f) * 10) / 10)); Show(); }, "Minus");
            ((RectTransform)m.transform).TL(440, y, 110, 88);
            var pl = UIKit.Btn(p.win, "btn_cream", "+", 48, () => { set(Mathf.Clamp01(Mathf.Round((get() + 0.1f) * 10) / 10)); Show(); }, "Plus");
            ((RectTransform)pl.transform).TL(710, y, 110, 88);
            Show();
            y += 110;
        }

        static void Toggle(Popup p, ref float y, string name, Func<bool> get, Action<bool> set)
        {
            UIKit.Label(p.win, name, 36, Art.Ink, TextAnchor.MiddleLeft, false).rectTransform.TL(150, y, 360, 88);
            Button b = null;
            b = UIKit.Btn(p.win, "btn_green", "", 36, () => { set(!get()); Show(); }, "Toggle");
            ((RectTransform)b.transform).TL(560, y, 260, 88);
            void Show() { UIKit.SetKind(b, get() ? "btn_green" : "btn_gray"); UIKit.LabelOf(b).text = get() ? L.T("on") : L.T("off"); }
            Show();
            y += 110;
        }
    }

    /// <summary>화면 가운데 위 잠깐 알림</summary>
    public class Toast : MonoBehaviour
    {
        float t;
        public static void Show(string s)
        {
            if (UIRoot.I == null) return;
            foreach (var old in UIRoot.I.popups.GetComponentsInChildren<Toast>()) Destroy(old.gameObject);
            var box = UIKit.Panel(UIRoot.I.popups, "night", "Toast"); box.rectTransform.Place(0.5f, 0.74f, Vector2.zero, new Vector2(900, 110));
            var tx = UIKit.Label(box.transform, s, 36, Art.Cream); tx.rectTransform.Fill(20, 10, 20, 0);
            tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            box.gameObject.AddComponent<Toast>();
        }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.one * (t < 0.12f ? 0.8f + t / 0.12f * 0.2f : 1f);
            if (t > 1.8f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// 도움말 (튜토리얼, 2차): 처음 한 번씩 말풍선 + 손가락. 눌러서 닫기 · 시간이 지나면 닫힘. Config Tutorial 0 = 끔.
    /// 전투 = 끌어서 이동 · 첫 카드 · 첫 별 4개 / 로비 = 터렛 강화 · 장비 · 엘리트.
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public static Tutorial Cur;
        float t, life;
        RectTransform hand, target;
        bool sway;
        static SaveData S => SaveData.Cur;
        public static bool Want(string key) => Tables.Ci("Tutorial", 1) == 1 && S != null && !S.Tut(key);

        /// <summary>target 위(또는 아래)에 말풍선 · 손가락. target 없으면 화면 가운데. sway = 손가락이 좌우로 (끌기)</summary>
        public static Tutorial Show(string key, string text, RectTransform target = null, float life = 6f, bool sway = false, float y = 0.6f)
        {
            if (!Want(key)) return null;
            S.tut.Add(key); S.Save();
            if (Cur) Destroy(Cur.gameObject);
            var root = UIKit.Rect(UIRoot.I.popups, "Tutorial_" + key).Fill();
            var tu = Cur = root.gameObject.AddComponent<Tutorial>();
            tu.life = life; tu.target = target; tu.sway = sway;
            var hit = UIKit.Plain(root, new Color(0, 0, 0, 0), "Hit"); hit.rectTransform.Fill(); hit.raycastTarget = !sway;
            if (!sway) UIKit.Clickable(hit, tu.Close);
            var box = UIKit.Panel(root, "card", "Bubble");
            Vector2 at = new Vector2(0, (y - 0.5f) * 1920f);
            if (target) at = (Vector2)root.InverseTransformPoint(target.TransformPoint(target.rect.center)) + new Vector2(0, target.rect.height * 0.5f + 210);
            at.x = Mathf.Clamp(at.x, -540 + 440, 540 - 440);
            box.rectTransform.Place(0.5f, 0.5f, at, new Vector2(860, 170));
            var tx = UIKit.Label(box.transform, text, 34, Art.Ink, TextAnchor.MiddleCenter, false); tx.rectTransform.Fill(24, 16, 24, 8);
            tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            var tap = UIKit.Label(box.transform, L.T("tap_close"), 22, Art.Soft, TextAnchor.LowerRight, false); tap.rectTransform.Fill(0, 4, 26, 0);
            tap.gameObject.SetActive(!sway);
            tu.hand = UIKit.Img(root, Art.Icon("hand"), "Hand").rectTransform; tu.hand.GetComponent<Image>().raycastTarget = false;
            tu.hand.sizeDelta = new Vector2(120, 120);
            tu.hand.anchoredPosition = target ? (Vector2)root.InverseTransformPoint(target.TransformPoint(target.rect.center)) + new Vector2(30, -40) : at + new Vector2(0, -190);
            box.gameObject.AddComponent<PopIn>();
            return tu;
        }

        Vector2 handBase;
        void Start() { handBase = hand.anchoredPosition; }
        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (sway) hand.anchoredPosition = handBase + new Vector2(Mathf.Sin(t * 3f) * 200f, 0);
            else hand.anchoredPosition = handBase + new Vector2(0, Mathf.Abs(Mathf.Sin(t * 5f)) * 22f);
            if (t > life) Close();
        }
        public void Close() { if (Cur == this) Cur = null; Destroy(gameObject); }
        public static void CloseCur() { if (Cur) Cur.Close(); }
    }
}
