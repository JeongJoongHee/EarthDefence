using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>세이브 (PlayerPrefs 에 JSON 한 줄). 판이 끝날 때 · 강화할 때 · 앱을 내리거나 끌 때 저장.</summary>
    [Serializable]
    public class SaveData
    {
        public const string Key = "EarthDef.Save.v1";
        public static SaveData Cur;

        public double gold;
        public int stage = 1;             // 고른 스테이지 (1 부터)
        public int best;                  // 깬 가장 높은 스테이지 (0 = 아직 없음)
        public int baseLv = 1;            // 기지 레벨
        public List<int> tLv = new();     // 터렛 레벨 (Turret 표 순서, 1 부터)
        public List<int> shards = new();  // 터렛 조각 (Turret 표 순서)
        public long patrolFrom;           // 순찰 보상 계산 시작 (유닉스 초)
        public long lastSeen;
        public int runs, wins;
        public long kills;
        public string lang = "ko";
        // ---------- 2차 ----------
        public int stamina = -1;          // -1 = 처음 (최대로 채움)
        public long staminaAt;            // 스태미나를 마지막으로 계산한 시각
        public int staminaAdDay, staminaAdN;
        public int eliteBest;             // 엘리트 난이도로 깬 가장 높은 스테이지
        public bool elite;                // 로비에서 고른 난이도
        public List<GearItem> gear = new();
        public List<ChipItem> chips = new();
        public int nextUid = 1;
        public double parts;              // 부품 (장비 강화 재료 — 분해로 얻음)
        public int attendN, attendLast = -1;   // 받은 출석 일수 · 마지막으로 받은 날
        public int missionDay = -1;
        public List<int> mProg = new(), mGot = new();
        public List<string> tut = new();  // 본 도움말
        public List<string> seen = new(); // 알림을 본 것 (열린 터렛 · 엘리트)
        public float bgm = 0.8f, sfx = 0.8f;
        public bool vib = true, nums = true, fps60 = true;

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        /// <summary>시험에서 시간을 앞당길 때</summary>
        public static long NowOffset;
        public static long Clock => Now + NowOffset;

        public static SaveData Load()
        {
            SaveData s = null;
            var json = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { s = JsonUtility.FromJson<SaveData>(json); } catch (Exception e) { Debug.LogWarning("세이브 읽기 실패: " + e.Message); }
            }
            s ??= New();
            s.Fix();
            Cur = s;
            return s;
        }

        static SaveData New()
        {
            Tables.Load();
            return new SaveData { gold = Tables.C("Start_Gold"), patrolFrom = Clock };
        }

        void Fix()
        {
            Tables.Load();
            while (tLv.Count < Tables.Turrets.Count) tLv.Add(1);
            while (shards.Count < Tables.Turrets.Count) shards.Add(0);
            stage = Mathf.Clamp(stage, 1, Mathf.Min(Tables.Stages.Count, best + 1));
            baseLv = Mathf.Max(1, baseLv);
            if (patrolFrom <= 0) patrolFrom = Clock;
            if (stamina < 0) { stamina = Tables.Ci("Stamina_Max", 60); staminaAt = Clock; }
            gear ??= new(); chips ??= new(); mProg ??= new(); mGot ??= new(); tut ??= new(); seen ??= new();
            foreach (var g in gear) if (g.uid >= nextUid) nextUid = g.uid + 1;
            foreach (var c in chips) if (c.uid >= nextUid) nextUid = c.uid + 1;
        }

        /// <summary>오늘 날짜 번호 (기기 시간대 기준 · 시험에서 NowOffset 으로 넘김)</summary>
        public static int Today => (int)(DateTimeOffset.FromUnixTimeSeconds(Clock).ToLocalTime().Date - new DateTime(2020, 1, 1)).TotalDays;
        public bool Unlocked(TurretRow t) => t.unlock <= 0 || best >= t.unlock;
        public bool Tut(string key) => tut.Contains(key);

        public void Save()
        {
            lastSeen = Clock;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            Cur = null;
            NowOffset = 0;
        }

        // ---------- 편의 ----------
        public int TIdx(string id) => Tables.Turrets.FindIndex(t => t.id == id);
        public int TurretLv(string id) { int i = TIdx(id); return i < 0 ? 1 : tLv[i]; }

        /// <summary>기지 최대 체력 · 공격력</summary>
        public double BaseHp => Tables.C("Base_HP") + Tables.C("Base_HP_Per_Lv") * (baseLv - 1);
        public double Atk => Tables.C("Atk_Base") + Tables.C("Base_Atk_Per_Lv") * (baseLv - 1);
        public double BaseCost => Math.Ceiling(Tables.C("Base_Cost_Base") * Math.Pow(Tables.C("Base_Cost_Growth"), baseLv - 1));
        public bool BaseMax => baseLv >= Tables.Ci("Base_Max_Lv", 300);

        /// <summary>순찰: 시간당 골드 · 조각 (깬 최고 스테이지 기준), 최대 시간까지</summary>
        public double PatrolSec => Math.Min(Clock - patrolFrom, Tables.C("Patrol_Max_Hours") * 3600);
        public double PatrolGold => PatrolSec / 3600.0 * (Tables.C("Patrol_Gold_Hour") + Tables.C("Patrol_Gold_Per_Stage") * Math.Max(0, best - 1));
        public int PatrolShards => (int)Math.Floor(PatrolSec / 3600.0 * Tables.C("Patrol_Shards_Hour"));
    }
}
