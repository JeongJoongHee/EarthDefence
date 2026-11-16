using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace EarthDef
{
    // ---------- 표 한 줄 (칸 이름은 xlsx 그대로) ----------
    public class TurretRow
    {
        public string id, name, elem, art, kind, desc;
        public float rate, rate2, tick, cd, range, dur, radius, speed;
        public int count, hits, pierce;
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
    public class StageRow { public int stage; public string name, swarm, elite, boss; public string[] enemies; public float hpMul; }

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

        public static void Load(bool force = false)
        {
            if (loaded && !force) return;
            loaded = true;
            config.Clear(); Turrets.Clear(); TurretById.Clear(); TurretLv.Clear(); Cards.Clear(); CardById.Clear();
            Enemies.Clear(); Stages.Clear(); Strings.Clear();

            foreach (var r in Read("Config")) config[r.S("Key")] = r.D("Value");
            foreach (var r in Read("Turret"))
            {
                var t = new TurretRow
                {
                    id = r.S("Id"), name = r.S("Name"), elem = r.S("Elem"), art = r.S("Art"), kind = r.S("Kind"), desc = r.S("Desc"),
                    rate = r.F("Rate"), rate2 = r.F("Rate2"), tick = r.F("Tick"), cd = r.F("Cooldown"), range = r.F("Range"),
                    count = r.I("Count"), hits = r.I("Hits"), dur = r.F("Duration"), radius = r.F("Radius"), pierce = r.I("Pierce"), speed = r.F("Speed"),
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
                    stage = r.I("Stage"), name = r.S("Name"), swarm = r.S("Swarm"), elite = r.S("Elite"), boss = r.S("Boss"),
                    hpMul = r.D("Hp_Mul") > 0 ? r.F("Hp_Mul") : 1f,
                    enemies = r.S("Enemies").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries),
                });
            foreach (var r in Read("String")) Strings[r.S("Key")] = (r.S("KO"), r.S("EN"));
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
        public static void SetC(string key, double v) => config[key] = v;
        public static int Ci(string key, int def = 0) => (int)Math.Round(C(key, def));

        public static StageRow Stage(int n) => Stages[Mathf.Clamp(n - 1, 0, Stages.Count - 1)];
        public static TurretLvRow Lv(int lv) => TurretLv[Mathf.Clamp(lv - 1, 0, TurretLv.Count - 1)];
        public static int MaxTurretLv => TurretLv.Count;

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
        public static Color Color(string e) => e switch
        {
            "phys" => Art.Hex("#d9c19a"), "light" => Art.Hex("#8fd6ff"), "grav" => Art.Hex("#b9a6e0"),
            "fire" => Art.Hex("#f09a7e"), "bolt" => Art.Hex("#ffd966"), _ => Art.Hex("#c9c1b3"),
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
