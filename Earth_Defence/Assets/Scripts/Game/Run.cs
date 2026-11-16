using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 한 판의 터렛 1개 상태: 단계(1~3) · 별(0~4) · 고른 카드 · 카드로 더해진 수치.
    /// 별 4개가 차면 돌파 카드가 열리고, 돌파를 고르면 다음 단계(별 0부터).
    /// </summary>
    public class TurretInst
    {
        public TurretRow row;
        public int slot;                  // 0 = 가디언, 1~4 = 땅 칸
        public int tier = 1, stars;
        public int metaLv = 1;            // 로비 터렛 레벨
        public readonly Dictionary<string, int> picks = new();
        readonly Dictionary<string, double> stat = new();
        public int shotsCards;            // "탄 +1(-15%)" 카드 수 (발당 감소)
        public TurretUnit unit;           // 화면 물체
        /// <summary>2차: 장비 · 칩이 이 터렛에 더하는 값 (카드와 같은 키 — 판 내내 그대로)</summary>
        public Dictionary<string, double> chip;

        public double S(string k) => stat.TryGetValue(k, out var v) ? v : 0;
        public bool Has(string k) => S(k) > 0;
        public int Picked(string cardId) => picks.TryGetValue(cardId, out var n) ? n : 0;

        public void Recalc()
        {
            stat.Clear(); shotsCards = 0;
            if (chip != null) foreach (var kv in chip) if (kv.Key != "start_stars") stat[kv.Key] = S(kv.Key) + kv.Value;
            foreach (var kv in picks)
            {
                if (!Tables.CardById.TryGetValue(kv.Key, out var c)) continue;
                foreach (var (key, val) in c.stats)
                {
                    stat[key] = S(key) + val * kv.Value;
                    if (key == "shots" || key == "count" || key == "bounce") shotsCards += kv.Value;
                }
            }
        }

        // ---------- 수치 (카드 · 터렛 레벨 반영) ----------
        public TurretLvRow Lv => Tables.Lv(metaLv);
        /// <summary>일반 브래킷: 터렛 레벨 피해 + 카드 피해%</summary>
        public double Ordinary => Lv.dmgBonus + S("dmg") / 100.0;
        /// <summary>탄 +1 카드의 발당 감소 (곱)</summary>
        public double Penalty => Math.Pow(1 - Tables.C("Multi_Penalty"), shotsCards);
        public float Cooldown => (float)(row.cd * Lv.cdMul / (1 + S("cd") / 100.0));
        /// <summary>사거리 (표 0 = 화면 전체 — 장판)</summary>
        public float Range => (row.range > 0 ? row.range * Tables.Cf("Range_Scale", 1.3f) : 30f) * (1 + (float)S("range") / 100f);
        public float Duration => row.dur * (1 + (float)S("dur") / 100f);
        public float Area => 1 + (float)S("area") / 100f;
        public float Tick => row.tick / (1 + (float)S("tick") / 100f);
        public int Shots => row.count + (int)S("shots") + (int)S("shots_free") + (row.kind == "hive" || row.kind == "field" ? (int)S("count") + (int)S("count_free") : 0);
        public int Pierce => row.pierce + (int)S("pierce");
        public double CritRate => Tables.C("Crit_Rate") + S("crit") / 100.0;
    }

    /// <summary>
    /// 한 판의 빌드: 터렛 칸 · 카드 뽑기 · 고르기. 전투가 끝나면 모두 사라짐 (기획서 3장).
    /// 11-15 결정: 카드 규칙 원작대로 — 강화 카드 보장 없음(Card_Upgrade_Guarantee 0), 새로고침은 광고 1번(Refresh_Ad).
    /// </summary>
    public class Run
    {
        public readonly List<TurretInst> turrets = new();
        public readonly List<CardRow> history = new();
        public System.Random rnd;
        public int refreshAd, refreshFree;
        public readonly Loadout lo;
        /// <summary>2차 칩 "첫 카드 N장 더" — 첫 레벨업에만</summary>
        public int firstExtra;

        public Run(int seed, Loadout loadout = null)
        {
            rnd = new System.Random(seed);
            lo = loadout ?? new Loadout();
            refreshAd = Tables.Ci("Refresh_Ad", 1);
            refreshFree = Tables.Ci("Refresh_Free", 0) + (int)Math.Round(lo.B("refresh"));
            firstExtra = (int)Math.Round(lo.B("choices"));
            Add(Tables.TurretById["guardian"]);
        }

        public int MaxSlots => Tables.Ci("Turret_Slots", 4);
        public int FreeSlots => MaxSlots - (turrets.Count - 1);
        public TurretInst Get(string id) => turrets.Find(t => t.row.id == id);
        public bool Owns(string id) => Get(id) != null;

        TurretInst Add(TurretRow r)
        {
            var t = new TurretInst { row = r, slot = turrets.Count, metaLv = SaveData.Cur != null ? SaveData.Cur.TurretLv(r.id) : 1 };
            // 장비 · 칩: 모든 터렛("*") + 이 터렛 전용
            var all = lo.For("*"); var mine = lo.For(r.id);
            if (all != null || mine != null)
            {
                t.chip = new Dictionary<string, double>();
                if (all != null) foreach (var kv in all) t.chip[kv.Key] = kv.Value;
                if (mine != null) foreach (var kv in mine) t.chip[kv.Key] = (t.chip.TryGetValue(kv.Key, out var o) ? o : 0) + kv.Value;
            }
            t.Recalc();
            turrets.Add(t);
            return t;
        }

        int StarsPer => Tables.Ci("Stars_Per_Tier", 4);
        int TierMax => Tables.Ci("Tier_Max", 3);
        bool StarsFull(TurretInst t) => t.stars >= StarsPer;
        /// <summary>별이 다 찼는데 아직 돌파 전이면 (돌파를 골라야 별이 다시 쌓임)</summary>
        bool Blocked(TurretInst t) => StarsFull(t) && t.tier < TierMax;

        public bool Eligible(CardRow c)
        {
            var t = Get(c.turret);
            switch (c.kind)
            {
                case "new": return t == null && FreeSlots > 0 && (SaveData.Cur == null || SaveData.Cur.Unlocked(Tables.TurretById[c.turret]));
                case "normal": return t != null && !Blocked(t) && t.Picked(c.id) < c.max;
                case "break": return t != null && StarsFull(t) && t.tier < TierMax && t.Picked(c.id) == 0;
                case "chain": return t != null && !Blocked(t) && t.Picked(c.id) < c.max && t.Picked(c.req) > 0;
                case "combo": return t != null && Owns(c.partner) && t.Picked(c.id) == 0;
            }
            return false;
        }

        double Weight(CardRow c, bool bossBonus)
        {
            double w = c.weight;
            if (c.kind == "break") w *= Tables.C("Break_Weight_Mul", 4) * (1 + lo.B("break_rate") / 100.0);
            if (bossBonus && (c.kind == "break" || c.kind == "chain" || c.kind == "combo")) w *= 3;
            return w;
        }

        /// <summary>카드 n 장 뽑기 (같은 카드 없이 가중치대로). bossBonus = 보스 처치 카드 (돌파·연계·콤보가 잘 나옴)</summary>
        public List<CardRow> Roll(int n, bool bossBonus = false)
        {
            var pool = new List<CardRow>();
            foreach (var c in Tables.Cards) if (Eligible(c)) pool.Add(c);
            var got = new List<CardRow>();
            while (got.Count < n && pool.Count > 0)
            {
                double sum = 0; foreach (var c in pool) sum += Weight(c, bossBonus);
                double r = rnd.NextDouble() * sum;
                int pick = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++) { r -= Weight(pool[i], bossBonus); if (r <= 0) { pick = i; break; } }
                got.Add(pool[pick]); pool.RemoveAt(pick);
            }
            // 강화 카드 보장 (Config 1 이면) — 3장이 모두 새 터렛이면 1장을 강화로 바꿈
            if (Tables.Ci("Card_Upgrade_Guarantee") == 1 && got.Count > 0 && got.TrueForAll(c => c.kind == "new"))
            {
                var up = pool.Find(c => c.kind != "new");
                if (up != null) got[got.Count - 1] = up;
            }
            return got;
        }

        /// <summary>카드 고르기 → 터렛 추가 · 별 · 단계 · 수치</summary>
        public TurretInst Pick(CardRow c)
        {
            history.Add(c);
            TurretInst t;
            if (c.kind == "new")
            {
                if (FreeSlots <= 0 || Owns(c.turret)) { Debug.LogWarning("빈 칸이 없거나 이미 있는 터렛: " + c.id); return null; }
                t = Add(Tables.TurretById[c.turret]);
                // 칩 "놓으면 별 +N" — 돌파를 기다리는 별 최대에서 멈춤 (기획서 3장)
                t.stars = Mathf.Min(StarsPer, 1 + (int)Math.Round(lo.TS(c.turret, "start_stars")));
                return t;
            }
            t = Get(c.turret);
            if (t == null) { Debug.LogError("카드의 터렛이 없음: " + c.id); return null; }
            t.picks[c.id] = t.Picked(c.id) + 1;
            if (c.kind == "break") { t.tier = Mathf.Min(TierMax, t.tier + 1); t.stars = 0; }
            else if (!StarsFull(t)) t.stars++;
            t.Recalc();
            return t;
        }
    }
}
