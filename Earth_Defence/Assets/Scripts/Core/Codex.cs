using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 9차 수정안 "장비와 회로에 대한 도감 정보 추가": 얻어 본 장비(부위 x 등급) · 회로(종류별 최고 등급)를 기억.
    /// 세이브 codex 칸에 "g:부위:등급" · "c:회로 id:등급" — 합성 · 분해로 없어져도 도감엔 남음.
    /// </summary>
    public static class Codex
    {
        public static void Mark(SaveData s, GearItem g) { if (s != null && g != null) Add(s, "g:" + g.part + ":" + g.grade); }
        public static void Mark(SaveData s, ChipItem c) { if (s != null && c != null) Add(s, "c:" + c.id + ":" + c.q); }
        static void Add(SaveData s, string k) { s.codex ??= new List<string>(); if (!s.codex.Contains(k)) s.codex.Add(k); }
        public static bool HasGear(string part, int grade) => SaveData.Cur != null && SaveData.Cur.codex.Contains("g:" + part + ":" + grade);
        /// <summary>그 회로를 얻어 본 가장 높은 등급 (0 = 아직 없음)</summary>
        public static int ChipBest(string id)
        {
            var s = SaveData.Cur; if (s == null) return 0;
            for (int q = Tables.GradeMax; q >= 1; q--) if (s.codex.Contains("c:" + id + ":" + q)) return q;
            return 0;
        }
        public static int GearGot { get { int n = 0; foreach (var p in Tables.GearParts) for (int g = 1; g <= Tables.GearTop; g++) if (HasGear(p.part, g)) n++; return n; } }
        public static int GearAll => Tables.GearParts.Count * Tables.GearTop;     // 11-22 오픈 스펙: 보이는 등급까지
        public static int ChipGot { get { int n = 0; foreach (var c in Tables.Chips) if (Tables.TurretOpen(c.turret) && ChipBest(c.id) > 0) n++; return n; } }
        public static int ChipAll { get { int n = 0; foreach (var c in Tables.Chips) if (Tables.TurretOpen(c.turret)) n++; return n; } }   // 11-23: 숨긴 터렛의 회로는 안 셈
    }

    /// <summary>
    /// 9차 수정안 "장비 탭에서 회로 프리셋 기능 추가 (프리셋 총 3개)": 지금 끼운 회로(어느 부위 몇째 칸에 어느 회로)를 저장 → 불러오면 그대로 다시 끼움.
    /// 회로는 uid 로 기억 — 합성 · 분해로 없어진 회로는 빼고 끼움(몇 개 빠졌는지 알림). 불러올 때 프리셋에 없는 회로는 가방으로.
    /// </summary>
    public static class ChipPreset
    {
        static SaveData S => SaveData.Cur;
        public static int Count => Mathf.Max(1, Tables.Ci("Chip_Presets", 3));
        static void Grow() { S.chipPresets ??= new List<string>(); while (S.chipPresets.Count < Count) S.chipPresets.Add(""); }
        /// <summary>프리셋 i 에 든 회로 수 (0 = 비어 있음)</summary>
        public static int Size(int i) { Grow(); var v = S.chipPresets[i]; return string.IsNullOrEmpty(v) ? 0 : v.Split(',').Length; }
        public static void Save(int i)
        {
            Grow();
            var l = new List<string>();
            foreach (var c in S.chips) if (c.at >= 0) l.Add(c.uid + "@" + c.at);
            S.chipPresets[i] = string.Join(",", l);
            S.Save();
        }
        /// <summary>불러오기 → 빠진(없어진 · 못 끼우는) 회로 수. -1 = 비어 있음</summary>
        public static int Load(int i)
        {
            Grow();
            var v = S.chipPresets[i];
            if (string.IsNullOrEmpty(v)) return -1;
            foreach (var c in S.chips) c.at = -1;
            int miss = 0;
            foreach (var e in v.Split(','))
            {
                var p = e.Split('@');
                if (p.Length != 2 || !int.TryParse(p[0], out int uid) || !int.TryParse(p[1], out int at)) { miss++; continue; }
                var c = S.chips.Find(x => x.uid == uid);
                int part = at / Gear.Slots, slot = at % Gear.Slots;
                if (c == null || part >= Tables.GearParts.Count || !Gear.CanPut(c, part, slot) || Gear.ChipAt(part, slot) != null) { miss++; continue; }
                c.at = at;
            }
            S.Save();
            return miss;
        }
    }
}
