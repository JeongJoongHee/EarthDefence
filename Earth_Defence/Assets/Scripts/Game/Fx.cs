using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 잠깐 보이는 효과: 폭발 · 맞음 · 링 · 피해 숫자. 물체를 모아 두고 다시 씀(풀).
    /// 피해 숫자는 월드 캔버스의 글자 (흰 글자 + 진갈색 테두리). 11-16 카치건 방식: 같은 적이 0.3초 안에 또 맞으면 새로 띄우지 않고
    /// 그 숫자에 더함(다시 톡 커짐) · 치명은 크고 붉은 주황 (사용자 "카치건과 동일한 방식").
    /// </summary>
    public class Fx : MonoBehaviour
    {
        public static Fx I;
        class Item { public string kind; public GameObject go; public SpriteRenderer sr; public Text tx; public float t, life, s0, s1, drag; public Vector3 v; public Color c; public bool num, crit; public object key; public double val; }
        readonly Dictionary<object, Item> numByKey = new();
        static readonly Color CritCol = new(1f, 0.45f, 0.35f);
        // 11-16 네온: 빛 더하기로 그리는 효과 (밤하늘에서 빛남)
        static readonly HashSet<string> AddKinds = new() { "glow", "spark", "muzzle", "hit", "ring" };
        const int MaxLive = 450;   // 효과가 너무 많으면 불꽃·빛은 건너뜀 (화면 밀림 방지)
        readonly List<Item> live = new();
        readonly Dictionary<string, Stack<Item>> free = new();
        Canvas numCanvas;
        int numCount;
        const int MaxNums = 40;
        public static int NumCount => I ? I.numCount : 0;

        void Awake()
        {
            I = this;
            var cgo = new GameObject("NumCanvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            numCanvas = cgo.AddComponent<Canvas>();
            numCanvas.renderMode = RenderMode.WorldSpace;
            numCanvas.sortingOrder = 60;
            var rt = (RectTransform)cgo.transform;
            rt.sizeDelta = new Vector2(10, 10); rt.localScale = Vector3.one * 0.01f;
        }

        Item Take(string kind)
        {
            if (free.TryGetValue(kind, out var st) && st.Count > 0) { var it = st.Pop(); it.go.SetActive(true); return it; }
            var n = new Item { go = new GameObject(kind), kind = kind };   // 11-17: 종류를 C# 쪽에 적어 둠 (예전엔 쓸 때마다 물체 이름을 고치고, 돌려줄 때 이름을 읽어 글자를 새로 만듦)
            if (kind == "num")
            {
                n.go.transform.SetParent(numCanvas.transform, false);
                var rt = n.go.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(400, 100);
                n.tx = n.go.AddComponent<Text>();
                n.tx.font = Art.Font; n.tx.alignment = TextAnchor.MiddleCenter; n.tx.raycastTarget = false;
                n.tx.horizontalOverflow = HorizontalWrapMode.Overflow;
                var ol = n.go.AddComponent<SoftOutline>(); ol.effectColor = Art.Ink; ol.effectDistance = new Vector2(3, 3);   // 11-17: 피해 숫자도 8방향 테두리 (4차 수정안 "글자 찢김"이 여기만 남아 있었음)
                var sh = n.go.AddComponent<Shadow>(); sh.effectColor = Art.Ink; sh.effectDistance = new Vector2(0, -4.5f);
                n.num = true;
            }
            else
            {
                n.go.transform.SetParent(transform, false);
                n.sr = n.go.AddComponent<SpriteRenderer>();
                n.sr.sprite = Art.Fx(kind); n.sr.sortingOrder = AddKinds.Contains(kind) ? 52 : 50;
                if (AddKinds.Contains(kind)) Art.Additive(n.sr);
            }
            return n;
        }

        void Play(string kind, Vector2 pos, float s0, float s1, float life, Color c, Vector3 v = default, float rot = 0, float drag = 0)
        {
            var it = Take(kind);
            it.drag = drag;
            it.t = 0; it.life = life; it.s0 = s0; it.s1 = s1; it.c = c; it.v = v;
            it.go.transform.position = pos;
            it.go.transform.rotation = Quaternion.Euler(0, 0, rot);
            live.Add(it);
            Apply(it);
        }

        public static void Burst(Vector2 p, float radius, Color? c = null)
        {
            if (!I) return;
            // burst 그림 192px @PPU200 (별 모양 반지름 80px = 0.4칸) → 반지름 r 이면 배율 r/0.4
            float k = radius / 0.4f;
            I.Play("burst", p, k * 0.4f, k * 0.85f, 0.3f, c ?? Color.white, default, Random.Range(0, 360f));
            var gc = c ?? new Color(1f, 0.72f, 0.4f);
            var ga = gc; ga.a = 0.7f; Glow(p, radius * 1.2f, ga, 0.24f);
            Sparks(p, Color.Lerp(gc, Color.white, 0.4f), 8, 6f * Mathf.Sqrt(Mathf.Max(0.3f, radius)), 1.2f);
        }
        public static void Hit(Vector2 p, float k = 1f, Color? c = null)
        {
            if (!I) return;
            var col = c ?? new Color(1f, 0.93f, 0.7f, 0.75f);
            I.Play("hit", p, 0.45f * k, 0.95f * k, 0.15f, col, default, Random.Range(0, 90f));
            Sparks(p, col, 5, 4.5f * Mathf.Sqrt(k), 0.9f);
        }

        /// <summary>둥근 빛 번짐 (glow 128px = 0.64칸 지름 → 반지름 r 이면 배율 r/0.32)</summary>
        public static void Glow(Vector2 p, float radius, Color c, float life = 0.16f)
        {
            if (!I || I.live.Count > MaxLive) return;
            float k = radius / 0.32f;
            I.Play("glow", p, k * 0.7f, k, life, c);
        }

        /// <summary>불꽃 튐: n 갈래로 짧은 빛 줄이 튀어 나가며 줄어듦</summary>
        public static void Sparks(Vector2 p, Color c, int n, float speed = 4f, float size = 1f, float spreadDeg = 360f, float baseDeg = 0f)
        {
            if (!I) return;
            for (int i = 0; i < n && I.live.Count < MaxLive; i++)
            {
                float a = spreadDeg >= 360f ? Random.Range(0f, 360f) : baseDeg + Random.Range(-spreadDeg / 2, spreadDeg / 2);
                var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                float sp = speed * Random.Range(0.55f, 1.15f), k = size * Random.Range(0.7f, 1.15f);
                I.Play("spark", p + (Vector2)d * 0.08f, k, k * 0.25f, Random.Range(0.16f, 0.28f), c, d * sp, a, 5f);
            }
        }

        /// <summary>총구 섬광 (쏘는 방향으로 네 갈래 빛 + 번짐)</summary>
        public static void Muzzle(Vector2 p, float angleDeg, Color c, float k = 1f)
        {
            if (!I || I.live.Count > MaxLive) return;
            // 11-16 오후: 밝은 기지 건물 위에선 빛 더하기가 하얀 구름처럼 보여 작고 옅게
            var mc = c; mc.a = 0.75f;
            I.Play("muzzle", p, 0.28f * k, 0.48f * k, 0.07f, mc, default, angleDeg + 45f);
            var g = c; g.a = 0.3f; Glow(p, 0.15f * k, g, 0.08f);
        }
        public static void Ring(Vector2 p, float radius, Color c) { if (I) I.Play("ring", p, 0.2f, radius / 0.4f, 0.3f, c); }
        /// <summary>떠오르는 짧은 글자 (필살기 이름 등)</summary>
        public static void Word(Vector2 p, string s, Color c)
        {
            if (!I) return;
            var it = I.Take("num");
            it.crit = false; it.val = 0; it.key = null;
            it.tx.text = s; it.tx.fontSize = Art.FontSize(34);
            it.tx.color = c; it.t = 0; it.life = 1.2f; it.s0 = it.s1 = 1; it.c = c; it.drag = 3f;
            it.v = new Vector3(0, 1.4f);
            // 같은 때 여러 이름이 겹치지 않게: 최근 이름보다 조금 위로
            float now = Time.time; if (now - I.lastWordT < 0.6f) I.wordStack++; else I.wordStack = 0; I.lastWordT = now;
            it.go.transform.position = p + Vector2.up * (0.55f * I.wordStack);
            I.live.Add(it); I.numCount++;
        }
        float lastWordT; int wordStack;

        public static void Puff(Vector2 p, float k = 1f) { if (I) I.Play("puff", p, 0.5f * k, 1.2f * k, 0.35f, new Color(1, 1, 1, 0.8f), new Vector3(0, 0.6f)); }

        public static void Number(Vector2 p, double v, bool crit, object key = null)
        {
            if (!I) return;
            if (key != null && I.numByKey.TryGetValue(key, out var q) && q.go.activeSelf && q.t < 0.3f)
            {
                q.val += v;
                if (crit && !q.crit) { q.crit = true; q.tx.fontSize = Art.FontSize(50); q.c = CritCol; }
                q.tx.text = Num.F(q.val);
                q.t = Mathf.Min(q.t, 0.04f);          // 다시 톡 커짐
                return;
            }
            if (I.numCount >= MaxNums) return;
            var it = I.Take("num");
            it.crit = crit; it.val = v; it.key = key;
            it.tx.text = Num.F(v);
            it.tx.fontSize = Art.FontSize(crit ? 50 : 38);
            it.tx.color = crit ? CritCol : Color.white;
            it.t = 0; it.life = 0.7f; it.s0 = it.s1 = 1; it.c = it.tx.color; it.drag = 5f;
            it.v = new Vector3(Random.Range(-0.3f, 0.3f), 2.6f);
            it.go.transform.position = (Vector3)p + new Vector3(Random.Range(-0.2f, 0.2f), 0.3f, 0);
            I.live.Add(it); I.numCount++;
            if (key != null) I.numByKey[key] = it;
        }

        void Apply(Item it)
        {
            float k = Mathf.Clamp01(it.t / it.life);
            float s = Mathf.Lerp(it.s0, it.s1, 1 - (1 - k) * (1 - k));
            it.go.transform.localScale = it.num ? Vector3.one * (k < 0.12f ? 1.3f - k * 2.5f : 1f) : Vector3.one * s;
            var c = it.c; c.a *= k < 0.6f ? 1 : 1 - (k - 0.6f) / 0.4f;
            if (it.num) it.tx.color = c; else it.sr.color = c;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var it = live[i];
                it.t += dt;
                it.go.transform.position += it.v * dt;
                if (it.drag > 0) it.v *= Mathf.Max(0, 1 - it.drag * dt);
                if (it.t >= it.life)
                {
                    it.go.SetActive(false);
                    string kind = it.kind;
                    if (it.num) { numCount--; if (it.key != null && numByKey.TryGetValue(it.key, out var kq) && kq == it) numByKey.Remove(it.key); it.key = null; }
                    if (!free.TryGetValue(kind, out var st)) free[kind] = st = new Stack<Item>();
                    st.Push(it);
                    live.RemoveAt(i);
                    continue;
                }
                Apply(it);
            }
        }

        public void Clear()
        {
            foreach (var it in live) { it.t = it.life; }
        }
    }
}
