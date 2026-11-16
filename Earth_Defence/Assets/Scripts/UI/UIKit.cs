using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>UI 를 코드로 만드는 도우미 (낙서풍 판·버튼 = Art/UI 9분할 그림). 글자는 흰색 + 진갈색 테두리·그림자</summary>
    public static class UIKit
    {
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>앵커 한 점(ax,ay: 0~1) 기준 위치·크기</summary>
        public static RectTransform Place(this RectTransform rt, float ax, float ay, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = pivot ?? new Vector2(ax, ay);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        /// <summary>왼쪽 위 기준 (x, y = 아래로 +) — 디자인 시안 좌표 그대로 쓰기</summary>
        public static RectTransform TL(this RectTransform rt, float x, float y, float w, float h) => rt.Place(0, 1, new Vector2(x, -y), new Vector2(w, h), new Vector2(0, 1));

        public static RectTransform Fill(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Img(Transform parent, Sprite s, string name = "Img")
        {
            var rt = Rect(parent, name);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s;
            if (s != null && s.border != Vector4.zero) im.type = Image.Type.Sliced;
            im.preserveAspect = s != null && s.border == Vector4.zero;
            return im;
        }

        /// <summary>9분할 판 (card · btn_green · pill …)</summary>
        public static Image Panel(Transform parent, string kind, string name = "Panel") { var im = Img(parent, Art.UI(kind), name); im.raycastTarget = false; return im; }

        public static Image Plain(Transform parent, Color c, string name = "Plain")
        {
            var im = Rect(parent, name).gameObject.AddComponent<Image>();
            im.color = c; im.raycastTarget = false;
            return im;
        }

        public static Text Label(Transform parent, string s, int size, Color? c = null, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = Rect(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Font; t.text = s; t.fontSize = Art.FontSize(size);
            t.color = c ?? Color.white; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                float o = Mathf.Max(2f, size * 0.07f);
                var ol = rt.gameObject.AddComponent<Outline>(); ol.effectColor = Art.Ink; ol.effectDistance = new Vector2(o, -o);
                var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = Art.Ink; sh.effectDistance = new Vector2(0, -o - 1.5f);
            }
            return t;
        }

        /// <summary>판 그림 버튼 + 글자 (누르면 살짝 작아짐)</summary>
        public static Button Btn(Transform parent, string kind, string label, int size, Action onClick, string name = "Btn")
        {
            var im = Img(parent, Art.UI(kind), name);
            var b = im.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            im.gameObject.AddComponent<PressFx>();
            if (onClick != null) b.onClick.AddListener(() => { Audio.Play(Audio.Sfx.UiClick); onClick(); });
            if (label != null)
            {
                var t = Label(im.transform, label, size);
                t.rectTransform.Fill(8, 14, 8, 4);   // 아래 턱만큼 위로
                t.name = "Label";
            }
            return b;
        }

        public static Text LabelOf(Component b) => b.transform.Find("Label")?.GetComponent<Text>();
        public static void SetKind(Component b, string kind) { var im = b.GetComponent<Image>(); if (im) im.sprite = Art.UI(kind); }

        /// <summary>둥근 칸 + 아이콘 (메뉴 · 강화 아이콘 · 스킬 칸)</summary>
        public static Image Tile(Transform parent, string kind, Color tint, Sprite icon, float iconK = 0.62f, string name = "Tile")
        {
            var bg = Img(parent, Art.UI(kind), name); bg.color = tint;
            var ic = Img(bg.transform, icon, "Icon");
            ic.rectTransform.anchorMin = new Vector2((1 - iconK) / 2, (1 - iconK) / 2 + 0.04f);
            ic.rectTransform.anchorMax = new Vector2(1 - (1 - iconK) / 2, 1 - (1 - iconK) / 2 + 0.04f);
            ic.rectTransform.offsetMin = ic.rectTransform.offsetMax = Vector2.zero;
            ic.raycastTarget = false;
            return bg;
        }

        public static Button Clickable(Graphic g, Action onClick)
        {
            g.raycastTarget = true;
            var b = g.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            g.gameObject.AddComponent<PressFx>();
            b.onClick.AddListener(() => { Audio.Play(Audio.Sfx.UiClick); onClick(); });
            return b;
        }

        /// <summary>게이지 (바탕 + 채움). 채움 비율은 SetBar 로</summary>
        public static Image Bar(Transform parent, Color fill, string name = "Bar")
        {
            var bg = Panel(parent, "track", name);
            var f = Img(bg.transform, Art.UI("fill"), "Fill");
            f.color = fill; f.raycastTarget = false;
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(1, 1);
            f.rectTransform.offsetMin = new Vector2(4, 4); f.rectTransform.offsetMax = new Vector2(-4, -4);
            return f;
        }
        /// <summary>세로 스크롤 (2차): 돌려준 content 에 TL 좌표로 넣고, 끝에 SetScrollH 로 높이를 맞춤</summary>
        public static RectTransform Scroll(Transform parent, string name = "Scroll")
        {
            var view = Rect(parent, name);
            var vi = view.gameObject.AddComponent<Image>(); vi.color = new Color(1, 1, 1, 0.001f);
            view.gameObject.AddComponent<RectMask2D>();
            var content = Rect(view, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0, 100);
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.content = content; sr.viewport = view; sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic; sr.scrollSensitivity = 40; sr.inertia = true;
            return content;
        }
        public static void SetScrollH(RectTransform content, float h) => content.sizeDelta = new Vector2(0, h);

        /// <summary>속성 태그 (11-16 저녁 "타원이 아닌 네모 — 태그를 붙이는 느낌"): 왼쪽 뾰족 + 구멍 · 속성 색 · 이름</summary>
        public static Image ElemTag(Transform parent, string elem, float x, float y, float w, float h, int size)
        {
            var t = Img(parent, Art.UI("tag"), "Elem_" + elem); t.raycastTarget = false; t.color = Elem.Color(elem);
            t.rectTransform.TL(x, y, w, h);
            Label(t.transform, Elem.Name(elem), size, Art.Ink, TextAnchor.MiddleCenter, false).rectTransform.Fill(h * 0.42f, 2, 4, 0);
            return t;
        }

        /// <summary>둥근 빨간 점 (받을 것이 있음)</summary>
        public static Image Dot(Transform parent, float x, float y, float size = 34)
        {
            var d = Img(parent, Art.UI("btn_red"), "Dot"); d.raycastTarget = false; d.type = Image.Type.Simple;
            d.rectTransform.Place(1, 1, new Vector2(x, y), new Vector2(size, size), new Vector2(0.5f, 0.5f));
            return d;
        }

        public static void SetBar(Image fill, float k)
        {
            k = Mathf.Clamp01(k);
            fill.rectTransform.anchorMax = new Vector2(k, 1);
            fill.enabled = k > 0.01f;
        }
    }

    /// <summary>누르면 작아졌다 돌아오는 효과</summary>
    public class PressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 baseScale = Vector3.one; bool down;
        public void OnPointerDown(PointerEventData e) { if (!down) baseScale = transform.localScale; down = true; transform.localScale = baseScale * 0.93f; }
        public void OnPointerUp(PointerEventData e) { if (down) transform.localScale = baseScale; down = false; }
        public void OnPointerExit(PointerEventData e) { if (down) transform.localScale = baseScale; down = false; }
    }
}
