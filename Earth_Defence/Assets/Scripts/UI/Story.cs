using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 9차 수정안 스토리 대화창 (요청 그림: 왼쪽 동그라미 캐릭터 이미지 + 오른쪽 대화창 + 다음으로 가는 버튼).
    ///  · 처음 시작(Title → 로비 전): "왜 대장 카피가 지구를 지켜야 하는지" = Story 표 prologue
    ///  · [전투 시작] → 스테이지 스토리 s&lt;N&gt; (위에 스테이지 카드: 번호 · 이름 · 약점 · 보스) → 끝나면 전투로
    ///  · 화면 · [다음] 누르면 다음 줄 (글자가 나오는 중이면 한 번에 다 보임) · 오른쪽 위 [건너뛰기] · 설정 "스토리 건너뛰기"면 안 뜸
    /// 대사는 Story.xlsx (tools/fix9_tables.py), 인물: capy 대장 카피 · rabbit 달토끼(방아 찧는 달) · queen 젤리 여왕 · planet 떠돌이 행성 · narr 해설(얼굴 없음).
    /// </summary>
    public class StoryView : MonoBehaviour
    {
        public static StoryView Cur;
        static SaveData S => SaveData.Cur;
        /// <summary>시험에서 끔 (기존 시험 흐름이 대화창에 막히지 않게)</summary>
        public static bool Off;

        /// <summary>이 스토리를 보여 줄지: 켜져 있고 · 설정에서 건너뛰지 않았고 · 대사가 있고 · (처음 시작은 한 번만, 스테이지는 Story_Stage_Repeat 0 이면 처음 한 번만)</summary>
        public static bool Want(string id)
        {
            if (Off || S == null || S.storySkip || Tables.Ci("Story_On", 1) != 1) return false;
            var l = Tables.StoryOf(id);
            if (l == null || l.Count == 0) return false;
            bool once = id == "prologue" || Tables.Ci("Story_Stage_Repeat", 0) == 0;
            return !once || !S.seen.Contains("story:" + id);
        }
        public static string StageId(int stage) => "s" + stage;

        List<StoryRow> lines;
        int idx = -1; float shown, speed; string full = "";
        Action done; int stage;
        Text text, nameT; Image face, ring; RectTransform box, nameTag, faceRT; Button nextB;
        string who; float rabbitT;
        public int Index => idx;
        public int LineCount => lines.Count;
        public string CurText => full;
        public string CurWho => who;
        public bool Typing => shown < full.Length;

        /// <summary>대화 시작 — stage &gt; 0 이면 위에 스테이지 카드. 끝나면(마지막 [다음] · 건너뛰기) done</summary>
        public static StoryView Play(string id, int stage, Action done)
        {
            var l = Tables.StoryOf(id);
            if (l == null || l.Count == 0) { done?.Invoke(); return null; }
            if (S != null && !S.seen.Contains("story:" + id)) { S.seen.Add("story:" + id); S.Save(); }
            if (Cur) Destroy(Cur.gameObject);
            Tutorial.CloseCur();
            var root = UIKit.Rect(UIRoot.I.popups, "Story_" + id).Fill();
            var v = Cur = root.gameObject.AddComponent<StoryView>();
            v.lines = l; v.done = done; v.stage = stage;
            v.speed = Tables.Cf("Story_Type_Speed", 38f);
            v.Build(root);
            v.Next();
            return v;
        }

        void Build(RectTransform root)
        {
            var dim = UIKit.Plain(root, new Color(0.06f, 0.06f, 0.15f, stage > 0 ? 0.62f : 0.62f), "Dim"); dim.rectTransform.Fill(); dim.raycastTarget = true;
            UIKit.Clickable(dim, Next);
            if (stage > 0) StageCard(root);
            // 건너뛰기 (오른쪽 위)
            var sk = UIKit.Btn(root, "btn_cream", L.T("story_skip_btn"), 32, Skip, "Skip");
            ((RectTransform)sk.transform).Place(1, 1, new Vector2(-30, -40), new Vector2(230, 92), new Vector2(1, 1));
            // 대화창 (아래) — 요청 그림: 왼쪽 동그라미 얼굴 · 오른쪽 긴 대화창 · 다음 단추
            box = UIKit.Panel(root, "card_white", "Box").rectTransform;
            box.Place(0.5f, 0, new Vector2(0, 150), new Vector2(1010, 380), new Vector2(0.5f, 0));
            text = UIKit.Label(box, "", 40, Art.Ink, TextAnchor.UpperLeft, false); text.name = "Text";
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.lineSpacing = 1.1f;
            nameTag = UIKit.Panel(root, "pill", "NameTag").rectTransform; nameTag.GetComponent<Image>().color = Art.Orange;
            nameT = UIKit.Label(nameTag, "", 36, Color.white, TextAnchor.MiddleCenter); nameT.rectTransform.Fill(0, 4, 0, 0);
            ring = UIKit.Img(root, Art.UI("portrait"), "Portrait"); ring.raycastTarget = false;
            faceRT = ring.rectTransform; faceRT.Place(0, 0, new Vector2(34, 360), new Vector2(290, 290), new Vector2(0, 0));
            face = UIKit.Img(ring.transform, null, "Face"); face.raycastTarget = false; face.preserveAspect = true;
            face.rectTransform.Fill(28, 28, 28, 28);
            nextB = UIKit.Btn(box, "btn_green", L.T("story_next"), 36, Next, "Next");
            ((RectTransform)nextB.transform).Place(1, 0, new Vector2(-26, 24), new Vector2(240, 96), new Vector2(1, 0));
            var tap = UIKit.Label(box, L.T("story_tap"), 24, Art.Soft, TextAnchor.LowerLeft, false); tap.rectTransform.Fill(40, 30, 300, 0);
        }

        void StageCard(RectTransform root)
        {
            var st = Tables.Stage(stage);
            var card = UIKit.Panel(root, "card", "StageCard"); card.rectTransform.Place(0.5f, 1, new Vector2(0, -230), new Vector2(820, 330), new Vector2(0.5f, 1));
            card.gameObject.AddComponent<PopIn>();
            UIKit.Label(card.transform, L.F("story_stage_head", stage), 64, Art.Orange, TextAnchor.UpperCenter).rectTransform.TL(0, 28, 820, 84);
            UIKit.Label(card.transform, st.name, 44, Art.Ink, TextAnchor.UpperCenter, false).rectTransform.TL(0, 118, 820, 60);
            if (!string.IsNullOrEmpty(st.weak))
            {
                UIKit.Label(card.transform, L.T("battle_weak"), 34, Art.Brown, TextAnchor.MiddleRight, false).rectTransform.TL(140, 206, 220, 70);
                UIKit.ElemTag(card.transform, st.weak, 380, 206, 170, 70, 34);
            }
            if (!string.IsNullOrEmpty(st.boss) && Tables.Enemies.TryGetValue(st.boss, out var b))
            {
                var bi = UIKit.Img(card.transform, Art.Enemy(b), "Boss"); bi.preserveAspect = true; bi.raycastTarget = false;
                bi.rectTransform.TL(600, 176, 150, 130);
                var sk = UIKit.Img(card.transform, Art.Icon("skull"), "BossMark"); sk.raycastTarget = false; sk.rectTransform.TL(690, 166, 54, 54);
            }
        }

        public void Next()
        {
            if (idx >= 0 && Typing) { shown = full.Length; text.text = full; return; }
            idx++;
            if (idx >= lines.Count) { Finish(); return; }
            Audio.Play(Audio.Sfx.UiClick, 0.6f);
            var r = lines[idx];
            string ow = who; who = r.who;
            full = r.text.Replace("\\n", "\n"); shown = speed > 0 ? 0 : full.Length;
            text.text = full.Substring(0, (int)shown);
            bool narr = who == "narr" || string.IsNullOrEmpty(who);
            ring.gameObject.SetActive(!narr);
            nameTag.gameObject.SetActive(!narr);
            // 얼굴이 있으면 글자는 얼굴 오른쪽부터 · 해설은 가운데 넓게
            text.rectTransform.Fill(narr ? 60 : 300, 130, 50, 56);
            text.alignment = narr ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
            if (!narr)
            {
                nameT.text = L.T("who_" + who);
                nameTag.Place(0, 0, new Vector2(310, 492), new Vector2(Mathf.Max(220, nameT.preferredWidth + 70), 82), new Vector2(0, 0));
                face.sprite = Face(who, false);
                face.color = Color.white;
                if (ow != who) { var pi = ring.gameObject.GetComponent<PopIn>(); if (pi) Destroy(pi); ring.gameObject.AddComponent<PopIn>(); }
            }
            UIKit.LabelOf(nextB).text = idx == lines.Count - 1 && stage > 0 ? L.T("story_start") : L.T("story_next");
        }

        static Sprite Face(string who, bool alt)
        {
            switch (who)
            {
                case "capy": return Art.Get("Char/guardian_ui", 100f);
                case "rabbit": return Art.Get("Bg/moon_rabbit_" + (alt ? 1 : 0), 100f);
                default: return Tables.Enemies.TryGetValue(who, out var e) ? Art.Enemy(e) : Art.Get("Char/guardian_ui", 100f);
            }
        }

        public void Skip() { Audio.Play(Audio.Sfx.UiClick, 0.6f); Finish(); }

        bool finished;
        void Finish()
        {
            if (finished) return;
            finished = true;
            if (Cur == this) Cur = null;
            Destroy(gameObject);
            done?.Invoke();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (Typing) { shown = Mathf.Min(full.Length, shown + speed * dt); text.text = full.Substring(0, (int)shown); }
            // 달토끼는 말하는 동안 방아를 찧음 · 다른 인물은 말하는 동안 살짝 들썩
            if (who == "rabbit") { rabbitT += dt; face.sprite = Face("rabbit", Mathf.Repeat(rabbitT, 0.6f) > 0.38f); }
            if (face && faceRT) face.rectTransform.anchoredPosition = new Vector2(0, Typing ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f)) * 6f : 0);   // 동그라미 안에서 (Fill 기준)
        }

        void OnDestroy() { if (Cur == this) Cur = null; }
    }

    /// <summary>
    /// 9차 수정안 "로비 화면에서 대장 카피가 말풍선으로 간단하게 몇 마디씩 함": 몇 초마다(Lobby_Talk_Gap_*) Lobby_Talk 표에서 하나 골라 머리 위 말풍선.
    /// 대장 카피를 누르면 바로 한마디. 창 · 도움말 · 스토리가 떠 있으면 쉼. {stage} = 다음 스테이지.
    /// </summary>
    public class LobbyTalk : MonoBehaviour
    {
        public static LobbyTalk I;
        RectTransform bub; Text txt; float wait, life, t;
        public bool Showing => bub && bub.gameObject.activeSelf;
        public string Said { get; private set; }
        public int Count { get; private set; }

        public static void Attach(RectTransform parent)
        {
            if (Tables.Ci("Lobby_Talk_On", 1) != 1 || Tables.LobbyTalk.Count == 0) return;
            var go = UIKit.Rect(parent, "LobbyTalk"); go.Fill(); go.SetAsFirstSibling();     // 로비 단추 · 판 뒤 (단추를 가리지 않게)
            I = go.gameObject.AddComponent<LobbyTalk>();
            I.Build(go);
        }

        void Build(RectTransform root)
        {
            bub = UIKit.Img(root, Art.Get("UI/bubble", 100f, null, new Vector4(40, 44, 40, 40)), "Bubble").rectTransform;
            var im = bub.GetComponent<Image>(); im.type = Image.Type.Sliced; im.raycastTarget = false;
            bub.anchorMin = bub.anchorMax = new Vector2(0.5f, 0.5f); bub.pivot = new Vector2(0.5f, 0);
            txt = UIKit.Label(bub, "", 34, Art.Ink, TextAnchor.MiddleCenter, false); txt.rectTransform.Fill(26, 46, 26, 14);
            bub.gameObject.SetActive(false);
            wait = 2.5f;
            // 대장 카피 누르기 = 바로 한마디 (몸 둘레 투명 단추)
            var hit = UIKit.Plain(root, new Color(0, 0, 0, 0), "CapyHit"); hit.raycastTarget = true;
            hit.rectTransform.anchorMin = hit.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); hit.rectTransform.sizeDelta = new Vector2(220, 260);
            hitRT = hit.rectTransform;
            UIKit.Clickable(hit, () => Say());
        }
        RectTransform hitRT;

        bool Busy => Popup.Open != null || Tutorial.Cur || StoryView.Cur;

        /// <summary>한마디 (text 없으면 표에서 무작위)</summary>
        public void Say(string text = null)
        {
            if (Tables.LobbyTalk.Count == 0) return;
            if (text == null)
            {
                int sum = 0; foreach (var r in Tables.LobbyTalk) sum += r.weight;
                int k = UnityEngine.Random.Range(0, Mathf.Max(1, sum)); var pick = Tables.LobbyTalk[0];
                foreach (var r in Tables.LobbyTalk) { k -= r.weight; if (k < 0) { pick = r; break; } }
                if (pick.text == Said && Tables.LobbyTalk.Count > 1) { Say(); return; }    // 같은 말 두 번 연속은 피함
                text = pick.text;
            }
            int next = SaveData.Cur != null ? Mathf.Min(Tables.Stages.Count, SaveData.Cur.best + 1) : 1;
            text = text.Replace("{stage}", next.ToString());
            Said = text; Count++;
            txt.text = text;
            float w = Mathf.Clamp(txt.preferredWidth + 70, 260, 760);
            bub.sizeDelta = new Vector2(w, 150);
            bub.gameObject.SetActive(true);
            var pi = bub.GetComponent<PopIn>(); if (pi) Destroy(pi);
            bub.gameObject.AddComponent<PopIn>();
            life = Tables.Cf("Lobby_Talk_Life", 3.4f);
            wait = UnityEngine.Random.Range(Tables.Cf("Lobby_Talk_Gap_Min", 6f), Tables.Cf("Lobby_Talk_Gap_Max", 12f));
        }

        void Update()
        {
            var w = LobbyWalker.I;
            if (!w || !World.I || UIRoot.I == null) { if (bub) bub.gameObject.SetActive(false); return; }
            float dt = Time.unscaledDeltaTime; t += dt;
            // 머리 위 (카피 그림 높이 약 1.5칸) — 화면 밖으로 안 나가게
            var head = ToLocal(new Vector2(w.X, w.transform.position.y + 1.55f));
            var rt = (RectTransform)transform;
            float half = rt.rect.width * 0.5f - bub.sizeDelta.x * 0.5f - 16;
            bub.anchoredPosition = new Vector2(Mathf.Clamp(head.x, -half, half), head.y + 6);
            hitRT.anchoredPosition = ToLocal(new Vector2(w.X, w.transform.position.y + 0.7f));
            if (Showing) { if ((life -= dt) <= 0 || Busy) bub.gameObject.SetActive(false); return; }
            if (Busy) return;
            if ((wait -= dt) <= 0) Say();
        }

        /// <summary>월드 → 이 판 안 좌표 (로비 탭 판은 위 · 아래 줄만큼 화면 판과 가운데가 다름)</summary>
        Vector2 ToLocal(Vector2 w) => transform.InverseTransformPoint(UIRoot.I.pages.TransformPoint(UIRoot.I.WorldToUI(w)));

        void OnDestroy() { if (I == this) I = null; }
    }
}
