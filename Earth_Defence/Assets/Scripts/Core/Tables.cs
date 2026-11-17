using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace EarthDef
{
    // ---------- 표 한 줄 (칸 이름은 xlsx 그대로) ----------
    public class TurretRow
    {
        public string id, name, elem, art, kind, desc, info;
        public float rate, rate2, tick, cd, range, dur, radius, speed, knock;
        public int count, hits, pierce;
        public int unlock;                // 2차: 이 스테이지(보통)를 깨면 열림 (0 = 처음부터)
    }
    public class TurretLvRow { public int lv, shards; public double dmgBonus, cdMul, gold; }
    public class CardRow
    {
        public string id, turret, kind, line, name, desc, partner, req;
        public int max, weight;
        public readonly List<(string key, double val)> stats = new();
        public bool IsTurretCard => kind == "new";
    }
    public class EnemyRow
    {
        public string id, name, rank, family, art, move, weak, resist, split, summon;
        public float speed, hpMul, atkMul, size, pivotY, armor;
        public int exp, splitN;
        public bool crash;
        public bool IsBoss => rank == "boss";
        public bool IsElite => rank == "elite";
    }
    public class StageRow { public int stage; public string name, swarm, elite, boss, weak; public string[] enemies; public float hpMul; }   // weak = 밸런스 2차 스테이지 약점 (이 스테이지 적은 모두 이 속성에 약함)
    // ---------- 2차: 장비 · 칩 · 미션 · 출석 ----------
    public class GearPartRow { public string part, name, icon, stat1, stat2; public double base1, perLv1, base2, perLv2; }
    public class GradeRow { public int grade, lvMax; public string name, chipPrefix; public Color color; public double mul; }
    public class ChipRow
    {
        public string id, name, desc, scope, turret, stat; public double max; public int weight;
        public string[] parts;
        public bool Fits(string part) => Array.IndexOf(parts, "all") >= 0 || Array.IndexOf(parts, part) >= 0;
        /// <summary>등급 q(1~7) 값 = Max x q/7 (작은 값은 소수 둘째 자리까지)</summary>
        public double Val(int q) { double v = max * q / 7.0; return max >= 20 ? Math.Round(v) : Math.Round(v, max < 1 ? 3 : 1); }
        public string Desc(int q)
        {
            double v = Val(q);
            string s = stat == "paralyze" ? Math.Round(v * 100) + "%" : v.ToString(v == Math.Floor(v) ? "0" : "0.#");
            return desc.Replace("{0}", s);
        }
    }
    public class MissionRow { public string id, kind, name, reward; public int target; public double value; }
    public class AttendRow { public int day; public string reward; public double value; }
    // ---------- 3차: 기지 외형 · 스킨 · 상점 ----------
    public class FrameRow
    {
        public string id, name, effect, desc; public double baseV, perStar, cd; public int price; public Color tint, glow;
        public double Val(int star) => baseV + perStar * (Mathf.Max(1, star) - 1);
        public string Desc(int star) { double v = Val(star); return desc.Replace("{0}", v.ToString(v == Math.Floor(v) ? "0" : "0.##")).Replace("{1}", cd.ToString("0")); }
    }
    public class SkinRow { public string id, turret, name, ult, ultName, ultDesc; public float hue, sat; public int ultCharge, price; public double ultRate, dmg; }
    public class ProductRow { public string id, name, productId, kind; public int dia, krw; public bool firstDouble; }
    public class ShopItemRow { public string id, name, kind, desc; public int costDia, perDay; public double value; }

    /// <summary>
    /// 기획 표 (원본 = Earth_Defence/Table/*.xlsx → [지구 방위대 → 표 변환] 또는 재생할 때 자동으로 Resources/Tables/*.csv).
    /// 처음 값은 tools/make_tables.py 가 만듦. 이름(Name)은 표에서 고치면 게임에도 바로 바뀜.
    /// </summary>
    public static class Tables
    {
        static bool loaded;
        static readonly Dictionary<string, double> config = new();
        public static readonly List<TurretRow> Turrets = new();
        public static readonly Dictionary<string, TurretRow> TurretById = new();
        public static readonly List<TurretLvRow> TurretLv = new();
        public static readonly List<CardRow> Cards = new();
        public static readonly Dictionary<string, CardRow> CardById = new();
        public static readonly Dictionary<string, EnemyRow> Enemies = new();
        public static readonly List<StageRow> Stages = new();
        public static readonly Dictionary<string, (string ko, string en)> Strings = new();
        public static readonly List<GearPartRow> GearParts = new();
        public static readonly List<GradeRow> Grades = new();
        public static readonly List<ChipRow> Chips = new();
        public static readonly Dictionary<string, ChipRow> ChipById = new();
        public static readonly List<MissionRow> Missions = new();
        public static readonly List<AttendRow> Attend = new();
        public static readonly List<FrameRow> Frames = new();
        public static readonly Dictionary<string, string> GearNames = new();   // "부위_등급" → 등급별 장비 이름 (3차 수정안)
        public static readonly List<SkinRow> Skins = new();
        public static readonly List<ProductRow> Products = new();
        public static readonly List<ShopItemRow> ShopItems = new();
        public static readonly List<(int grade, double rate)> BoxRates = new();
        public static FrameRow Frame(string id) => Frames.Find(f => f.id == id) ?? (Frames.Count > 0 ? Frames[0] : null);
        public static SkinRow SkinOf(string turret) => Skins.Find(s => s.turret == turret);

        public static void Load(bool force = false)
        {
            if (loaded && !force) return;
            loaded = true;
            config.Clear(); Turrets.Clear(); TurretById.Clear(); TurretLv.Clear(); Cards.Clear(); CardById.Clear();
            Enemies.Clear(); Stages.Clear(); Strings.Clear();
            GearParts.Clear(); Grades.Clear(); Chips.Clear(); ChipById.Clear(); Missions.Clear(); Attend.Clear();
            Frames.Clear(); GearNames.Clear(); Skins.Clear(); Products.Clear(); ShopItems.Clear(); BoxRates.Clear();

            foreach (var r in Read("Config")) config[r.S("Key")] = r.D("Value");
            foreach (var r in Read("Turret"))
            {
                var t = new TurretRow
                {
                    id = r.S("Id"), name = r.S("Name"), elem = r.S("Elem"), art = r.S("Art"), kind = r.S("Kind"), desc = r.S("Desc"),
                    rate = r.F("Rate"), rate2 = r.F("Rate2"), tick = r.F("Tick"), cd = r.F("Cooldown"), range = r.F("Range"),
                    count = r.I("Count"), hits = r.I("Hits"), dur = r.F("Duration"), radius = r.F("Radius"), pierce = r.I("Pierce"), speed = r.F("Speed"),
                    unlock = r.I("Unlock"), knock = r.F("Knock"), info = r.S("Info"),
                };
                Turrets.Add(t); TurretById[t.id] = t;
            }
            foreach (var r in Read("Turret_Level"))
                TurretLv.Add(new TurretLvRow { lv = r.I("Lv"), dmgBonus = r.D("Dmg_Bonus"), cdMul = r.D("Cd_Mul"), shards = r.I("Shards"), gold = r.D("Gold") });
            foreach (var r in Read("Card"))
            {
                var c = new CardRow
                {
                    id = r.S("Id"), turret = r.S("Turret"), kind = r.S("Kind"), line = r.S("Line"), name = r.S("Name"), desc = r.S("Desc"),
                    max = Mathf.Max(1, r.I("Max")), weight = Mathf.Max(1, r.I("Weight")), partner = r.S("Partner"), req = r.S("Requires"),
                };
                for (int i = 1; i <= 3; i++)
                {
                    string k = r.S("Stat" + i);
                    if (!string.IsNullOrEmpty(k)) c.stats.Add((k, r.D("Val" + i)));
                }
                Cards.Add(c); CardById[c.id] = c;
            }
            foreach (var r in Read("Enemy"))
            {
                var e = new EnemyRow
                {
                    id = r.S("Id"), name = r.S("Name"), rank = r.S("Rank"), family = r.S("Family"), art = r.S("Art"), move = r.S("Move"),
                    speed = r.F("Speed"), hpMul = r.F("Hp_Mul"), atkMul = r.F("Atk_Mul"), exp = r.I("Exp"), size = r.F("Size"), pivotY = r.F("Pivot_Y"),
                    weak = r.S("Weak"), resist = r.S("Resist"), armor = r.F("Armor"), crash = r.I("Crash") == 1,
                    split = r.S("Split"), splitN = r.I("Split_N"), summon = r.S("Summon"),
                };
                Enemies[e.id] = e;
            }
            foreach (var r in Read("Stage"))
                Stages.Add(new StageRow
                {
                    stage = r.I("Stage"), name = r.S("Name"), swarm = r.S("Swarm"), elite = r.S("Elite"), boss = r.S("Boss"), weak = r.S("Weak"),
                    hpMul = r.D("Hp_Mul") > 0 ? r.F("Hp_Mul") : 1f,
                    enemies = r.S("Enemies").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries),
                });
            foreach (var r in Read("String")) Strings[r.S("Key")] = (r.S("KO"), r.S("EN"));
            foreach (var r in Read("Gear_Part"))
                GearParts.Add(new GearPartRow
                {
                    part = r.S("Part"), name = r.S("Name"), icon = r.S("Icon"), stat1 = r.S("Stat1"), base1 = r.D("Base1"), perLv1 = r.D("Per_Lv1"),
                    stat2 = r.S("Stat2"), base2 = r.D("Base2"), perLv2 = r.D("Per_Lv2"),
                });
            foreach (var r in Read("Gear_Grade"))
                Grades.Add(new GradeRow { grade = r.I("Grade"), name = r.S("Name"), color = Art.Hex(r.S("Color")), mul = r.D("Mul"), lvMax = r.I("Lv_Max"), chipPrefix = r.S("Chip_Prefix") });
            foreach (var r in Read("Gear_Name")) GearNames[r.S("Part") + "_" + r.I("Grade")] = r.S("Name");
            foreach (var r in Read("Chip"))
            {
                var c = new ChipRow
                {
                    id = r.S("Id"), name = r.S("Name"), desc = r.S("Desc"), scope = r.S("Scope"), turret = r.S("Turret"), stat = r.S("Stat"),
                    max = r.D("Max"), weight = Mathf.Max(1, r.I("Weight")),
                    parts = r.S("Parts").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries),
                };
                Chips.Add(c); ChipById[c.id] = c;
            }
            foreach (var r in Read("Mission"))
                Missions.Add(new MissionRow { id = r.S("Id"), kind = r.S("Kind"), target = Mathf.Max(1, r.I("Target")), name = r.S("Name"), reward = r.S("Reward"), value = r.D("Value") });
            foreach (var r in Read("Attend")) Attend.Add(new AttendRow { day = r.I("Day"), reward = r.S("Reward"), value = r.D("Value") });
            foreach (var r in Read("Frame"))
                Frames.Add(new FrameRow
                {
                    id = r.S("Id"), name = r.S("Name"), effect = r.S("Effect"), desc = r.S("Desc"), baseV = r.D("Base"), perStar = r.D("Per_Star"),
                    cd = r.D("Cooldown"), price = r.I("Price_Dia"), tint = Art.Hex(r.S("Tint")), glow = Art.Hex(r.S("Glow")),
                });
            foreach (var r in Read("Skin"))
                Skins.Add(new SkinRow
                {
                    id = r.S("Id"), turret = r.S("Turret"), name = r.S("Name"), hue = r.F("Hue"), sat = r.D("Sat") > 0 ? r.F("Sat") : 1f, ult = r.S("Ult"),
                    ultName = r.S("Ult_Name"), ultDesc = r.S("Ult_Desc"), ultCharge = Mathf.Max(1, r.I("Ult_Charge")), ultRate = r.D("Ult_Rate"), dmg = r.D("Dmg"), price = r.I("Price_Dia"),
                });
            foreach (var r in Read("Product"))
                Products.Add(new ProductRow { id = r.S("Id"), name = r.S("Name"), productId = r.S("Product_Id"), dia = r.I("Dia"), krw = r.I("Price_KRW"), firstDouble = r.I("First_Double") == 1, kind = string.IsNullOrEmpty(r.S("Kind")) ? "dia" : r.S("Kind") });
            foreach (var r in Read("Item"))
                ShopItems.Add(new ShopItemRow { id = r.S("Id"), name = r.S("Name"), kind = r.S("Kind"), desc = r.S("Desc"), costDia = r.I("Cost_Dia"), perDay = r.I("Per_Day"), value = r.D("Value") });
            foreach (var r in Read("Box_Rate")) BoxRates.Add((r.I("Grade"), r.D("Rate")));
        }

        public static double C(string key, double def = 0)
        {
            Load();
            if (config.TryGetValue(key, out var v)) return v;
            Debug.LogWarning("Config 에 없음: " + key);
            return def;
        }
        public static float Cf(string key, float def = 0) => (float)C(key, def);
        /// <summary>시험·촬영용으로 Config 값을 잠깐 바꿈 (표는 그대로)</summary>
        public static void SetC(string key, double v) { config[key] = v; World.Invalidate(); }
        public static int Ci(string key, int def = 0) => (int)Math.Round(C(key, def));

        public static StageRow Stage(int n) => Stages[Mathf.Clamp(n - 1, 0, Stages.Count - 1)];
        public static TurretLvRow Lv(int lv) => TurretLv[Mathf.Clamp(lv - 1, 0, TurretLv.Count - 1)];
        public static int MaxTurretLv => TurretLv.Count;
        public static GradeRow Grade(int g) => Grades[Mathf.Clamp(g - 1, 0, Grades.Count - 1)];
        public static int GradeMax => Grades.Count;
        // 11-17 최적화: 장비 능력치 · 이름 · 아이콘을 읽을 때마다 불림 — 람다 찾기(부를 때마다 메모리 할당) 대신 반복문 (부위는 6개)
        public static GearPartRow Part(string p) { for (int i = 0; i < GearParts.Count; i++) if (GearParts[i].part == p) return GearParts[i]; return null; }
        public static int PartIdx(string p) { for (int i = 0; i < GearParts.Count; i++) if (GearParts[i].part == p) return i; return -1; }

        // ---------- csv ----------
        public class Row
        {
            public Dictionary<string, string> v;
            public string S(string k) => v.TryGetValue(k, out var s) ? s.Trim() : "";
            public double D(string k) => double.TryParse(S(k), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
            public float F(string k) => (float)D(k);
            public int I(string k) => (int)Math.Round(D(k));
        }

        static List<Row> Read(string name)
        {
            var list = new List<Row>();
            var ta = Resources.Load<TextAsset>("Tables/" + name);
            if (!ta) { Debug.LogError("표 없음: Tables/" + name); return list; }
            var lines = Csv(ta.text.TrimStart('﻿'));
            if (lines.Count == 0) return list;
            var head = lines[0];
            for (int i = 1; i < lines.Count; i++)
            {
                var l = lines[i];
                if (l.Count == 0 || string.IsNullOrWhiteSpace(l[0])) continue;
                var d = new Dictionary<string, string>();
                for (int c = 0; c < head.Count && c < l.Count; c++) if (!string.IsNullOrEmpty(head[c])) d[head[c].Trim()] = l[c];
                list.Add(new Row { v = d });
            }
            return list;
        }

        static List<List<string>> Csv(string text)
        {
            var rows = new List<List<string>>(); var row = new List<string>(); var cell = new System.Text.StringBuilder();
            bool q = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (q)
                {
                    if (ch == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else q = false; }
                    else cell.Append(ch);
                }
                else if (ch == '"') q = true;
                else if (ch == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (ch == '\n' || ch == '\r')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = new List<string>();
                }
                else cell.Append(ch);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }
    }

    /// <summary>화면 글자 (String 표). 한글 먼저, 영어는 맨 마지막에 채움 (11-15 결정)</summary>
    public static class L
    {
        public static string T(string key)
        {
            Tables.Load();
            if (!Tables.Strings.TryGetValue(key, out var s)) return key;
            bool en = SaveData.Cur != null && SaveData.Cur.lang == "en";
            var t = en && !string.IsNullOrEmpty(s.en) ? s.en : s.ko;
            return t.Replace("\\n", "\n");
        }
        public static string F(string key, params object[] a) => string.Format(T(key), a);
    }

    /// <summary>5속성: 물리 · 빛 · 중력 · 불 · 번개 (적마다 약점 · 저항)</summary>
    public static class Elem
    {
        public static string Name(string e) => e switch { "phys" => "물리", "light" => "빛", "grav" => "중력", "fire" => "불", "bolt" => "번개", _ => "" };
        public static readonly string[] All = { "phys", "light", "grav", "fire", "bolt" };
        static readonly Color Phys = Art.Hex("#d9c19a"), Light = Art.Hex("#8fd6ff"), Grav = Art.Hex("#b9a6e0"), Fire = Art.Hex("#f09a7e"), Bolt = Art.Hex("#ffd966"), None = Art.Hex("#c9c1b3");
        public static Color Color(string e) => e switch
        {
            "phys" => Phys, "light" => Light, "grav" => Grav, "fire" => Fire, "bolt" => Bolt, _ => None,
        };
    }

    /// <summary>숫자 표기 (1234 → 1.23K)</summary>
    public static class Num
    {
        static readonly string[] Named = { "", "K", "M", "B", "T" };
        public static string F(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "-";
            if (v < 0) return "-" + F(-v);
            if (v < 1000) return Math.Floor(v).ToString("0");
            int k = (int)Math.Floor(Math.Log10(v) / 3);
            double m = v / Math.Pow(10, k * 3);
            if (m >= 1000) { k++; m /= 1000; }
            string s = m < 10 ? (Math.Floor(m * 100) / 100).ToString("0.00") : m < 100 ? (Math.Floor(m * 10) / 10).ToString("0.0") : Math.Floor(m).ToString("0");
            if (s.Contains(".")) s = s.TrimEnd('0').TrimEnd('.');
            return s + (k < Named.Length ? Named[k] : "e" + k * 3);
        }
        static readonly string[] ResUnit = { "K", "M", "B", "T" };
        /// <summary>6차 수정안 "금화 · 다이아는 100만부터 K": 100만 아래는 쉼표 숫자 그대로(999,999), 100만부터 천 단위 K(1,000K ~ 999,999K) · 10억부터 M … — 길어야 8글자</summary>
        public static string Res(double v)
        {
            if (double.IsNaN(v) || v < 0) v = 0;
            if (double.IsInfinity(v)) return "-";
            v = Math.Floor(v);
            if (v < 1e6) return v.ToString("#,0");
            double d = 1e3; int i = 0;
            while (v / d >= 1e6 && i < ResUnit.Length - 1) { d *= 1e3; i++; }
            return Math.Floor(v / d).ToString("#,0") + ResUnit[i];
        }
        /// <summary>쉼표 (2,840) — 기지 체력처럼 사용자가 정확한 숫자를 보는 곳</summary>
        public static string Comma(double v) => Math.Max(0, Math.Ceiling(v)).ToString("#,0");
        public static string Clock(float sec) { int s = Mathf.Max(0, Mathf.CeilToInt(sec)); return $"{s / 60:00} : {s % 60:00}"; }
        public static string Dur(double sec)
        {
            int s = Mathf.Max(0, (int)sec);
            if (s >= 3600) return $"{s / 3600}시간 {s % 3600 / 60}분";
            if (s >= 60) return $"{s / 60}분";
            return $"{s}초";
        }
    }
}
