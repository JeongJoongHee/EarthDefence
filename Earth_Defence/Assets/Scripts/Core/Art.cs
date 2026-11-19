using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// Resources/Art 그림 → 스프라이트 (한 번 만든 것은 보관). 그림은 tools/earth_export.py 가 만든다.
    /// 크기 규칙: 게임 그림은 PPU 200 (화면 1080 기준 실제 크기의 2배로 그림) · 배경은 PPU 100 · UI 는 100.
    /// </summary>
    public static class Art
    {
        public const float PPU = 200f;
        // 가디언 (11-16 오후: 카치건 형태 몸 + 따로 도는 소총, tools/earth_export.py export_char 가 값을 찍음)
        // 11-16 저녁 "너무 큼, 0.7~0.8" → 그림 배율 0.75 (몸 324x316 · 총 259x108)
        // 뒤 손(총 회전 중심) = 발에서 (0.414, 0.5085)칸 · 총 피벗 = 뒤 손 · 총구 = 피벗에서 (0.9, 0.09)칸
        public static readonly Vector2 GuardianFoot = new(0.4778f, 0.0426f);
        public static readonly Vector2 GuardianHand = new(0.414f, 0.5085f);
        public static readonly Vector2 GunPivot = new(0.2361f, 0.3333f);
        public static readonly Vector2 GunMuzzle = new(0.9f, 0.09f);
        // 네온 탄 그림은 꼬리가 들어 있어 머리 = 피벗 (tools/earth_export.py SHOT_HEAD 와 같은 값)
        public static Vector2 ShotPivot(string name) => name switch
        {
            "bullet" => new(92f / 120f, 0.5f),
            "bullet_charge" => new(120f / 160f, 0.5f),
            "shell" => new(176f / 232f, 0.5f),
            "missile" => new(138f / 184f, 0.5f),
            "mini_missile" => new(84f / 112f, 0.5f),
            _ => new(0.5f, 0.5f),
        };
        static Material addMat;
        /// <summary>빛 더하기 재질 (네온 번짐 · 불꽃 · 레이저 · 낙뢰). 셰이더가 없으면 null → 보통 그리기</summary>
        public static Material AddMat
        {
            get
            {
                if (addMat) return addMat;
                var sh = Shader.Find("EarthDef/SpriteAdd");
                if (sh) addMat = new Material(sh) { name = "SpriteAdd" };
                return addMat;
            }
        }
        public static void Additive(SpriteRenderer sr) { var m = AddMat; if (m) sr.sharedMaterial = m; }
        static Material hueMat;
        /// <summary>3차 스킨: 색 돌리기 재질 (값은 MaterialPropertyBlock _Hue · _Sat)</summary>
        public static Material HueMat
        {
            get
            {
                if (hueMat) return hueMat;
                var sh = Shader.Find("EarthDef/SpriteHue");
                if (sh) hueMat = new Material(sh) { name = "SpriteHue" };
                return hueMat;
            }
        }
        /// <summary>색을 deg 도 돌림 (빛 더하기 조각 · 효과 색)</summary>
        public static Color HueShift(Color c, float deg, float sat = 1f)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            var r = Color.HSVToRGB(Mathf.Repeat(h + deg / 360f, 1f), Mathf.Clamp01(s * sat), v); r.a = c.a; return r;
        }
        /// <summary>스킨 그림 (UI · 카드 — tools/skin_preview.py 가 같은 식으로 미리 돌려 둠)</summary>
        public static Sprite TurretSkinUI(string art) => Get("Turret/" + art + "_skin", 100f);
        /// <summary>화면 아이콘: 외형(스킨)을 착용했으면 외형 그림 (3차 수정안 "스킨 착용 시 아이콘도 스킨 모양으로")</summary>
        public static Sprite TurretIcon(TurretRow r) => r == null ? null : SaveData.Cur != null && SaveData.Cur.SkinOn(r.id) && Tables.SkinOf(r.id) != null ? TurretSkinUI(r.art) : TurretUI(r.art);
        // 터렛 312x346: 바닥 = 아래에서 12.5px · 쏘는 곳 = 바닥 위 1.36칸
        public static readonly Vector2 TurretFoot = new(0.5f, 12.5f / 346f);
        public const float TurretHead = 1.36f;

        static readonly Dictionary<(string, float, Vector2, Vector4), Sprite> cache = new();
        static Font font;

        public static Font Font => font ? font : (font = Resources.Load<Font>("Font/Jua-Regular"));
        /// <summary>주아는 같은 크기에서 한글이 조금 작아 보여 1.07배</summary>
        public static int FontSize(int size) => Mathf.Max(1, Mathf.RoundToInt(size * 1.07f));

        public static Texture2D Tex(string path)
        {
            var t = Resources.Load<Texture2D>("Art/" + path);
            if (t == null) Debug.LogError("그림 없음: Art/" + path);
            return t;
        }

        public static Sprite Get(string path, float ppu = PPU, Vector2? pivot = null, Vector4 border = default)
        {
            var key = (path, ppu, pivot ?? new Vector2(0.5f, 0.5f), border);
            if (cache.TryGetValue(key, out var s) && s) return s;
            var t = Tex(path);
            if (!t) return null;
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot ?? new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
            s.name = path;
            cache[key] = s;
            return s;
        }

        /// <summary>UI 판 (9분할): 120x120 판 · 모서리 36 · 아래 턱 포함 40</summary>
        public static Sprite UI(string name)
        {
            Vector4 b = name switch
            {
                "pill" or "pill_dark" => new Vector4(40, 39, 40, 39),
                "tag" => new Vector4(38, 20, 20, 20),     // 11-16 속성 태그 (왼쪽 뾰족 + 구멍)
                "glow_frame" => new Vector4(50, 50, 50, 50),
                "minislot" => new Vector4(16, 16, 16, 16),
                "badge" => new Vector4(14, 14, 14, 14),
                "track" or "fill" => new Vector4(19, 19, 19, 19),
                "white" => new Vector4(26, 26, 26, 26),
                "window" => new Vector4(40, 40, 40, 40),
                "band" => new Vector4(34, 4, 34, 34),
                "slot_dash" => new Vector4(30, 30, 30, 30),
                _ => new Vector4(38, 42, 38, 38),          // 120x120 판·버튼 (아래 턱 12px)
            };
            return Get("UI/" + name, 100f, null, b);
        }

        public static Sprite Icon(string name) => Get("Icon/" + name, 100f);
        /// <summary>6차 수정안 "기지 외형 이미지 수정": 외형마다 다른 돔 그림 (Icon/frame_&lt;id&gt; = 목록용 · Bg/dome_&lt;id&gt; = 전장). 없으면 예전 그림</summary>
        public static Sprite FrameIcon(FrameRow f) => (f != null ? (World.V ? Icon("framed_" + f.id) : null) ?? Icon("frame_" + f.id) : null) ?? Icon("base");   // 9차 세로 전장 = 돔 아이콘
        public static Sprite Dome(string id) => (string.IsNullOrEmpty(id) ? null : Get("Bg/dome_" + id, 100f)) ?? Get("Bg/dome", 100f);
        static readonly Dictionary<string, Sprite> fxCache = new();
        /// <summary>효과 그림 — 테두리 없는 것은 이름만으로 바로 찾음 (예전엔 부를 때마다 "Fx/" + 이름 글자를 새로 만듦)</summary>
        public static Sprite Fx(string name, Vector4 border = default)
        {
            if (border != default) return Get("Fx/" + name, PPU, null, border);
            if (fxCache.TryGetValue(name, out var s) && s) return s;
            return fxCache[name] = Get("Fx/" + name, PPU);
        }
        public static Sprite Shot(string name) => Get("Fx/" + name, PPU, ShotPivot(name));
        public static Sprite Turret(string art) => Get("Turret/" + art, PPU, TurretFoot);
        /// <summary>터렛 그림을 UI 에 쓸 때 (가운데 피벗)</summary>
        public static Sprite TurretUI(string art) => art == "guardian" ? Get("Char/guardian_ui", 100f) : Get("Turret/" + art, 100f);
        public static Sprite Guardian => Get("Char/guardian", PPU, GuardianFoot);
        public static Sprite GuardianGun => Get("Char/guardian_gun", PPU, GunPivot);
        public static Sprite Enemy(EnemyRow e, bool white = false) => Get("Enemy/" + e.art + (white ? "_w" : ""), PPU, new Vector2(0.5f, e.pivotY));

        // 11-17 최적화: 색 글자("#ffd45e")를 매번 엔진에서 풀던 것 → 한 번 푼 색은 보관 (탄 · 불꽃 · 낙뢰가 맞을 때마다 불림)
        static readonly Dictionary<string, Color> hexCache = new();
        public static Color Hex(string h)
        {
            if (hexCache.TryGetValue(h, out var c)) return c;
            ColorUtility.TryParseHtmlString(h, out c);
            return hexCache[h] = c;
        }
        public static readonly Color Ink = Hex("#3b281c");
        public static readonly Color Cream = Hex("#fff4dc");
        public static readonly Color Cream2 = Hex("#f3e3c3");
        public static readonly Color Brown = Hex("#8a5e3c");
        public static readonly Color Soft = Hex("#5e4f42");   // 4차 수정안 "팝업 글자가 잘 안 보임" → 7a6a5c 보다 진하게
        public static readonly Color Orange = Hex("#f2ad55");
        public static readonly Color Green = Hex("#8ccb6e");
        public static readonly Color Green2 = Hex("#6fae55");
        public static readonly Color Red = Hex("#e8735f");
        public static readonly Color Gold = Hex("#ffd34d");
        public static readonly Color Sky = Hex("#8fd6ff");
        public static readonly Color Purple = Hex("#a68bd6");
        public static readonly Color Track = Hex("#5b4433");
        public static readonly Color Night = Hex("#2e3866");
        public static readonly Color Gray = Hex("#a8a092");
    }
}
