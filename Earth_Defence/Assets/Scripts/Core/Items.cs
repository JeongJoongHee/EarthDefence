using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>장비 1개 (2차). 부위 6 · 등급 1~7 · 레벨 (등급마다 최대가 다름)</summary>
    [Serializable]
    public class GearItem
    {
        public int uid;
        public string part;
        public int grade = 1, lv = 1;
        public bool on;                   // 끼고 있음
        public GearPartRow Row => Tables.Part(part);
        public GradeRow G => Tables.Grade(grade);
        public int LvMax => G.lvMax;
        public double V1 => (Row.base1 + Row.perLv1 * (lv - 1)) * G.mul;
        public double V2 => (Row.base2 + Row.perLv2 * (lv - 1)) * G.mul;
    }

    /// <summary>받은 것 1개 (상자 · 합성 결과를 아이콘으로 하나씩 보여 줄 때 — 2차 수정안)</summary>
    public class GotItem
    {
        public string name, icon; public Color bg = Color.white; public int grade;
        public static GotItem Of(GearItem g) => new() { name = g.Row.name, icon = g.Row.icon, bg = g.G.color, grade = g.grade };
        public static GotItem Of(ChipItem c) => new() { name = c.Row.name, icon = "chip", bg = Tables.Grade(c.q).color, grade = c.q };
        public static GotItem Res(string icon, string text) => new() { name = text, icon = icon };
    }

    /// <summary>칩 1개 (2차). at = 끼운 곳 (부위 번호 x 6 + 칸, -1 = 가방)</summary>
    [Serializable]
    public class ChipItem
    {
        public int uid;
        public string id;
        public int q = 1, at = -1;
        public ChipRow Row => Tables.ChipById.TryGetValue(id, out var r) ? r : null;
        public double Val => Row?.Val(q) ?? 0;
    }

    /// <summary>
    /// 장비 · 칩 규칙 (기획서 9장을 1인 개발 크기로): 장비 = 부위마다 1개 · 강화(골드 + 부품) · 분해(→ 부품) · 같은 부위·등급 3개 합성 → 한 등급 위.
    /// 칩 = 부위마다 6칸(최고 스테이지로 열림) · 끼울 수 있는 부위가 정해짐 · 같은 칩·등급 3개 합성 → 한 등급 위.
    /// 다이아(유료 재화)는 3차 상점 때 — 그래서 칸 열기 · 합성에 다이아를 쓰지 않음.
    /// </summary>
    public static class Gear
    {
        static SaveData S => SaveData.Cur;
        public const int Slots = 6;
        public static string StatName(string k) => k switch
        {
            "atk" => L.T("st_atk"), "hp" => L.T("st_hp"), "reduce" => L.T("st_reduce"), "crit" => L.T("st_crit"),
            "crit_dmg" => L.T("st_crit_dmg"), "cd" => L.T("st_cd"), _ => k,
        };
        public static string StatVal(string k, double v) => k is "crit" or "crit_dmg" or "cd" ? $"+{v:0.#}%" : k == "reduce" ? Num.F(Math.Round(v, 1)) : "+" + Num.F(v);

        // ---------- 장비 ----------
        public static GearItem On(string part) => S.gear.Find(g => g.on && g.part == part);
        public static void Equip(GearItem g)
        {
            var old = On(g.part); if (old != null) old.on = false;
            g.on = true; S.Save();
        }
        public static void Unequip(GearItem g) { g.on = false; S.Save(); }
        public static double UpGold(GearItem g) => Math.Ceiling(Tables.C("Gear_Up_Gold", 150) * g.G.mul * Math.Pow(Tables.C("Gear_Up_Gold_Growth", 1.1), g.lv - 1));
        public static int UpParts(GearItem g) => Tables.Ci("Gear_Up_Parts", 1) + (g.lv - 1) / 5;
        public static bool CanUp(GearItem g) => g.lv < g.LvMax && S.gold >= UpGold(g) && S.parts >= UpParts(g);
        public static bool Up(GearItem g)
        {
            if (!CanUp(g)) return false;
            S.gold -= UpGold(g); S.parts -= UpParts(g); g.lv++;
            Audio.Play(Audio.Sfx.Upgrade);
            Meta.MissionAdd("up");
            S.Save(); return true;
        }
        public static int ScrapParts(GearItem g) => Tables.Ci("Gear_Scrap_Parts", 2) * g.grade + g.lv / 2;
        public static void Scrap(GearItem g) { S.parts += ScrapParts(g); S.gear.Remove(g); S.Save(); }
        static int MergeN => Mathf.Max(2, Tables.Ci("Merge_N", 3));
        /// <summary>합성할 수 있는 짝 (끼운 것은 빼고) — 같은 부위 · 같은 등급 MergeN 개</summary>
        public static List<GearItem> MergeSet(GearItem g)
        {
            if (g.grade >= Tables.GradeMax) return null;
            var same = S.gear.FindAll(x => !x.on && x.part == g.part && x.grade == g.grade);
            if (!g.on && !same.Contains(g)) same.Add(g);
            if (same.Count < MergeN) return null;
            same.Sort((a, b) => a == g ? -1 : b == g ? 1 : b.lv.CompareTo(a.lv));
            return same.GetRange(0, MergeN);
        }
        public static GearItem Merge(GearItem g)
        {
            var set = MergeSet(g); if (set == null) return null;
            int lv = 1; foreach (var x in set) { lv = Mathf.Max(lv, x.lv); S.gear.Remove(x); }
            var n = NewGear(g.grade + 1, g.part); n.lv = Mathf.Min(lv, n.LvMax);
            Audio.Play(Audio.Sfx.Merge);
            S.Save(); return n;
        }

        // ---------- 칩 ----------
        public static bool SlotOpen(int slot) => S.best >= Tables.Ci("Chip_Slot_" + (slot + 1), 0);
        public static int SlotNeed(int slot) => Tables.Ci("Chip_Slot_" + (slot + 1), 0);
        public static ChipItem ChipAt(int part, int slot) => S.chips.Find(c => c.at == part * Slots + slot);
        public static bool CanPut(ChipItem c, int part, int slot) => c.Row != null && c.Row.Fits(Tables.GearParts[part].part) && SlotOpen(slot);
        public static bool PutChip(ChipItem c, int part, int slot)
        {
            if (!CanPut(c, part, slot)) return false;
            var old = ChipAt(part, slot); if (old != null && old != c) old.at = -1;
            c.at = part * Slots + slot; S.Save(); return true;
        }
        /// <summary>끼울 수 있는 빈 칸 첫 곳 (부위 순서대로) — 없으면 (-1,-1)</summary>
        public static (int part, int slot) FreeSlotFor(ChipItem c)
        {
            for (int p = 0; p < Tables.GearParts.Count; p++)
                for (int s = 0; s < Slots; s++)
                    if (CanPut(c, p, s) && ChipAt(p, s) == null) return (p, s);
            return (-1, -1);
        }
        public static void TakeChip(ChipItem c) { c.at = -1; S.Save(); }
        public static void ScrapChip(ChipItem c) { S.parts += Tables.Ci("Chip_Scrap_Parts", 1) * c.q; S.chips.Remove(c); S.Save(); }
        public static List<ChipItem> ChipMergeSet(ChipItem c)
        {
            if (c.q >= Tables.GradeMax) return null;
            var same = S.chips.FindAll(x => x.at < 0 && x.id == c.id && x.q == c.q);
            if (same.Count < MergeN) return null;
            return same.GetRange(0, MergeN);
        }
        public static ChipItem MergeChip(ChipItem c)
        {
            var set = ChipMergeSet(c); if (set == null) return null;
            foreach (var x in set) S.chips.Remove(x);
            var n = NewChip(c.q + 1, c.id); S.Save(); Audio.Play(Audio.Sfx.Merge); return n;
        }
        /// <summary>일괄 합성 (2차 수정안): 끼우지 않은 장비를 같은 부위 · 등급 3개씩 되는 데까지 → 새로 생긴 장비 (중간 단계는 빼고 마지막 것만)</summary>
        public static List<GearItem> MergeAllGear()
        {
            var made = new List<GearItem>();
            for (int guard = 0; guard < 500; guard++)
            {
                var g = S.gear.Find(x => !x.on && MergeSet(x) != null);
                if (g == null) break;
                var n = Merge(g); if (n == null) break;
                made.Add(n);
            }
            made.RemoveAll(x => !S.gear.Contains(x));      // 다시 합성 재료가 된 것은 빼고
            return made;
        }
        public static int GearMergeCount() { int n = 0; foreach (var grp in GroupCount(true)) n += grp; return n; }
        public static int ChipMergeCount() { int n = 0; foreach (var grp in GroupCount(false)) n += grp; return n; }
        /// <summary>지금 바로 합성할 수 있는 횟수 (한 단계만 셈)</summary>
        static IEnumerable<int> GroupCount(bool gear)
        {
            var d = new Dictionary<string, int>();
            if (gear) foreach (var g in S.gear) { if (g.on || g.grade >= Tables.GradeMax) continue; var k = g.part + g.grade; d[k] = (d.TryGetValue(k, out var v) ? v : 0) + 1; }
            else foreach (var c in S.chips) { if (c.at >= 0 || c.q >= Tables.GradeMax) continue; var k = c.id + c.q; d[k] = (d.TryGetValue(k, out var v) ? v : 0) + 1; }
            foreach (var v in d.Values) yield return v / MergeN;
        }
        /// <summary>칩 일괄 합성 → 새로 생긴 칩 (마지막 것만)</summary>
        public static List<ChipItem> MergeAllChipList()
        {
            var made = new List<ChipItem>();
            for (int guard = 0; guard < 500; guard++)
            {
                var c = S.chips.Find(x => x.at < 0 && ChipMergeSet(x) != null);
                if (c == null) break;
                var n = MergeChip(c); if (n == null) break; made.Add(n);
            }
            made.RemoveAll(x => !S.chips.Contains(x));
            return made;
        }

        /// <summary>가방의 칩을 되는 데까지 모두 합성 → 합성 횟수</summary>
        public static int MergeAllChips()
        {
            int n = 0;
            for (int guard = 0; guard < 500; guard++)
            {
                var c = S.chips.Find(x => x.at < 0 && ChipMergeSet(x) != null);
                if (c == null) break;
                MergeChip(c); n++;
            }
            if (n > 0) S.Save();
            return n;
        }

        // ---------- 새로 얻기 ----------
        public static GearItem NewGear(int grade, string part = null, System.Random rnd = null)
        {
            rnd ??= new System.Random(Environment.TickCount ^ S.nextUid);
            part ??= Tables.GearParts[rnd.Next(Tables.GearParts.Count)].part;
            var g = new GearItem { uid = S.nextUid++, part = part, grade = Mathf.Clamp(grade, 1, Tables.GradeMax), lv = 1 };
            S.gear.Add(g); Trim();
            return g;
        }
        /// <summary>칩: 범용 + 열린 터렛 전용 칩 중에서 가중치대로</summary>
        public static ChipItem NewChip(int q, string id = null, System.Random rnd = null)
        {
            rnd ??= new System.Random(Environment.TickCount ^ (S.nextUid * 31));
            if (id == null)
            {
                var pool = Tables.Chips.FindAll(c => string.IsNullOrEmpty(c.turret) || (Tables.TurretById.TryGetValue(c.turret, out var t) && S.Unlocked(t)));
                int sum = 0; foreach (var c in pool) sum += c.weight;
                int r = rnd.Next(Mathf.Max(1, sum));
                id = pool[pool.Count - 1].id;
                foreach (var c in pool) { r -= c.weight; if (r < 0) { id = c.id; break; } }
            }
            var ch = new ChipItem { uid = S.nextUid++, id = id, q = Mathf.Clamp(q, 1, Tables.GradeMax) };
            S.chips.Add(ch); Trim();
            return ch;
        }
        /// <summary>가방이 넘치면 끼우지 않은 가장 낮은 등급부터 부품으로</summary>
        static void Trim()
        {
            int max = Tables.Ci("Inv_Max", 200);
            while (S.gear.Count > max)
            {
                GearItem low = null; foreach (var g in S.gear) if (!g.on && (low == null || g.grade < low.grade)) low = g;
                if (low == null) break; Scrap(low);
            }
            while (S.chips.Count > max)
            {
                ChipItem low = null; foreach (var c in S.chips) if (c.at < 0 && (low == null || c.q < low.q)) low = c;
                if (low == null) break; ScrapChip(low);
            }
        }

        /// <summary>떨어질 등급: 기본 = 1 + (스테이지-1)/Drop_Grade_Step (+ 엘리트) · 가끔 한 등급 위·아래</summary>
        public static int DropGrade(int stage, bool elite, System.Random rnd)
        {
            int g = 1 + (stage - 1) / Mathf.Max(1, Tables.Ci("Drop_Grade_Step", 12)) + (elite ? Tables.Ci("Elite_Mode_Grade", 1) : 0);
            double r = rnd.NextDouble();
            if (r < Tables.C("Drop_Grade_Up", 0.2)) g++;
            else if (r < Tables.C("Drop_Grade_Up", 0.2) + Tables.C("Drop_Grade_Down", 0.25)) g--;
            return Mathf.Clamp(g, 1, Tables.GradeMax);
        }

        /// <summary>보상 1개 주기 (출석 · 미션 · 상자): gold · shards · stamina · parts · chip(값 = 등급) · gear(값 = 등급). 돌려줌 = 화면 글자</summary>
        public static string Give(string kind, double v, System.Random rnd = null)
        {
            rnd ??= new System.Random(Environment.TickCount);
            switch (kind)
            {
                case "gold": S.gold += v; return $"{L.T("r_gold")} +{Num.F(v)}";
                case "parts": S.parts += v; return $"{L.T("r_parts")} +{Num.F(v)}";
                case "dia": S.dia += v; return $"{L.T("dia")} +{Num.F(v)}";
                case "stamina": Meta.AddStamina((int)v); return $"{L.T("r_stamina")} +{v:0}";
                case "shards":
                {
                    var open = Tables.Turrets.FindAll(t => S.Unlocked(t));
                    for (int i = 0; i < (int)v; i++) S.shards[S.TIdx(open[rnd.Next(open.Count)].id)]++;
                    return $"{L.T("r_shards")} +{v:0}";
                }
                case "chip": { var c = NewChip((int)v, null, rnd); return $"{c.Row.name} ({Tables.Grade(c.q).name})"; }
                case "gear": { var g = NewGear((int)v, null, rnd); return $"{g.Row.name} ({g.G.name})"; }
            }
            return kind;
        }
    }

    /// <summary>
    /// 장비 · 칩을 한 판에 쓸 값으로 모음 (기획서 9-2 칩 적용 경로 scope):
    /// board 보드 전체 스탯 · cond 조건 피해(일반 브래킷) · turret 그 터렛(카드와 같은 키) · all 모든 터렛 · card 카드 지급.
    /// </summary>
    public class Loadout
    {
        public double atk, atkPct, hp, reduce, critDmg;
        public readonly Dictionary<string, double> cond = new(), board = new();
        public readonly Dictionary<string, Dictionary<string, double>> turret = new();   // "*" = 모든 터렛
        public double C(string k) => cond.TryGetValue(k, out var v) ? v : 0;
        public double B(string k) => board.TryGetValue(k, out var v) ? v : 0;
        public double AtkTotal => atk * (1 + atkPct / 100.0);
        public int ChipCount, GearCount;

        void Add(Dictionary<string, double> d, string k, double v) => d[k] = (d.TryGetValue(k, out var o) ? o : 0) + v;
        void AddT(string tid, string k, double v)
        {
            if (!turret.TryGetValue(tid, out var d)) turret[tid] = d = new Dictionary<string, double>();
            Add(d, k, v);
        }
        public Dictionary<string, double> For(string tid) => turret.TryGetValue(tid, out var d) ? d : null;
        public double TS(string tid, string k) { var d = For(tid); return d != null && d.TryGetValue(k, out var v) ? v : 0; }

        void Stat(string k, double v)
        {
            switch (k)
            {
                case "atk": atk += v; break;
                case "atk_pct": atkPct += v; break;
                case "hp": hp += v; break;
                case "reduce": reduce += v; break;
                case "crit_dmg": critDmg += v; break;
                case "crit": AddT("*", "crit", v); break;       // 카드의 crit 키와 같은 %p
                case "cd": AddT("*", "cd", v); break;
                default: Add(board, k, v); break;
            }
        }

        public static Loadout Build(SaveData s)
        {
            var lo = new Loadout();
            if (s == null) return lo;
            foreach (var g in s.gear)
            {
                if (!g.on || g.Row == null) continue;
                lo.GearCount++;
                lo.Stat(g.Row.stat1, g.V1);
                lo.Stat(g.Row.stat2, g.V2);
            }
            foreach (var c in s.chips)
            {
                if (c.at < 0 || c.Row == null) continue;
                int part = c.at / Gear.Slots, slot = c.at % Gear.Slots;
                if (part >= Tables.GearParts.Count || !Gear.SlotOpen(slot)) continue;
                lo.ChipCount++;
                var r = c.Row; double v = c.Val;
                switch (r.scope)
                {
                    case "cond": lo.Add(lo.cond, r.stat, v); break;
                    case "turret": lo.AddT(r.turret, r.stat, v); break;
                    case "all": lo.AddT("*", r.stat, v); break;
                    default: lo.Stat(r.stat, v); break;      // board · card
                }
            }
            return lo;
        }
    }
}
