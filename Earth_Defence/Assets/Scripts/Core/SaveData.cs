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
        // ---------- 3차 ----------
        public double dia = -1;            // 다이아 (-1 = 처음 → Start_Dia)
        public string frame = "basic";     // 끼운 기지 외형
        public List<string> frames = new();     // 가진 외형 "id:별"
        public List<string> skins = new(), skinOn = new();   // 가진 스킨 · 끼운 스킨
        public float endlessBest;          // 무한 모드 최고 버틴 시간(초)
        public int survWins;
        public int dailyDay = -1;
        public List<string> dailyKeys = new(); public List<int> dailyVals = new();   // 하루 횟수 (도전 입장 · 상점)
        public List<string> iapOnce = new();    // 첫 구매 2배 받은 상품
        public int pityGear, chipPity;          // 6차: 장비 · 회로 상자 천장 셈 (전설이 나오면 0) — 새 칸은 옛 세이브에서 0
        public string account = "";        // [초안] 구글 계정
        public bool vib = true, nums = true, fps60 = true;
        public bool storySkip;                  // 9차: 설정 "스토리 건너뛰기"
        public List<string> codex = new();      // 9차 도감: 얻어 본 것 "g:부위:등급" · "c:회로 id:등급" (지금 없어도 남음)
        public List<string> chipPresets = new();   // 9차 회로 프리셋 (칸마다 "uid@자리,uid@자리" · 빈 문자열 = 비어 있음)

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        /// <summary>시험에서 시간을 앞당길 때</summary>
        public static long NowOffset;
        public static long Clock => Now + NowOffset;

        public const string BackupKey = Key + ".bak";
        static SaveData Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<SaveData>(json); } catch (Exception e) { Debug.LogWarning("세이브 읽기 실패: " + e.Message); return null; }
        }

        public static SaveData Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            var s = Parse(json);
            // 11-17 안전장치: 세이브가 깨져 못 읽으면(예전엔 그대로 새 게임이 됨) 지난번에 잘 읽힌 판(백업)에서 되살림.
            //   백업은 켤 때 잘 읽힌 세이브를 한 번 적어 둠 (저장할 때마다 두 번 쓰지 않음)
            if (s == null && !string.IsNullOrEmpty(json))
            {
                s = Parse(PlayerPrefs.GetString(BackupKey, ""));
                if (s != null) Debug.LogWarning("세이브가 깨져 백업에서 되살림");
            }
            else if (s != null) PlayerPrefs.SetString(BackupKey, json);
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
            if (patrolFrom <= 0 || patrolFrom > Clock) patrolFrom = Clock;     // 11-17: 기기 시계를 뒤로 돌린 경우 (순찰 보상이 음수로 보이던 것)
            if (staminaAt > Clock) staminaAt = Clock;
            if (stamina < 0) { stamina = Tables.Ci("Stamina_Base", 30); staminaAt = Clock; }
            gear ??= new(); chips ??= new(); mProg ??= new(); mGot ??= new(); tut ??= new(); seen ??= new();
            frames ??= new(); skins ??= new(); skinOn ??= new(); dailyKeys ??= new(); dailyVals ??= new(); iapOnce ??= new();
            if (dia < 0) dia = Tables.C("Start_Dia", 100);
            codex ??= new(); chipPresets ??= new();
            foreach (var g in gear) Codex.Mark(this, g);      // 9차: 도감이 생기기 전에 얻은 것도 채움
            foreach (var c in chips) Codex.Mark(this, c);
            if (string.IsNullOrEmpty(frame)) frame = "basic";
            foreach (var g in gear) if (g.uid >= nextUid) nextUid = g.uid + 1;
            foreach (var c in chips) if (c.uid >= nextUid) nextUid = c.uid + 1;
        }

        /// <summary>오늘 날짜 번호 (기기 시간대 기준 · 시험에서 NowOffset 으로 넘김)</summary>
        public static int Today => (int)(DateTimeOffset.FromUnixTimeSeconds(Clock).ToLocalTime().Date - new DateTime(2020, 1, 1)).TotalDays;
        public bool Unlocked(TurretRow t) => t.unlock <= 0 || best >= t.unlock;
        public bool Tut(string key) => tut.Contains(key);

        // ---------- 하루 횟수 (기기 시간 밤 12시에 새로) ----------
        void DayCheck() { if (dailyDay != Today) { dailyDay = Today; dailyKeys.Clear(); dailyVals.Clear(); } }
        public int Daily(string key) { DayCheck(); int i = dailyKeys.IndexOf(key); return i < 0 ? 0 : dailyVals[i]; }
        public void DailyAdd(string key, int n = 1) { DayCheck(); int i = dailyKeys.IndexOf(key); if (i < 0) { dailyKeys.Add(key); dailyVals.Add(n); } else dailyVals[i] += n; }

        // ---------- 기지 외형 · 스킨 ----------
        public int FrameStar(string id) { if (id == "basic") return 1; foreach (var f in frames) { var p = f.Split(':'); if (p[0] == id) return p.Length > 1 ? int.Parse(p[1]) : 1; } return 0; }
        public void SetFrameStar(string id, int star) { frames.RemoveAll(f => f.Split(':')[0] == id); frames.Add(id + ":" + star); }
        public bool HasSkin(string turret) => skins.Contains(turret);
        public bool SkinOn(string turret) => skinOn.Contains(turret) && skins.Contains(turret);

        // 11-17 최적화: 일괄 합성 · 상자 10+1 · 전투 끝처럼 한 번에 여러 번 저장하던 곳을 묶어서 마지막에 한 번만 (저장 = 폰 저장소에 쓰기)
        static int batch; static bool batchDirty;
        public static void BeginBatch() => batch++;
        public static void EndBatch()
        {
            if (--batch > 0) return;
            batch = 0;
            if (batchDirty) { batchDirty = false; Cur?.Save(); }
        }

        public void Save()
        {
            if (batch > 0 && this == Cur) { batchDirty = true; return; }
            lastSeen = Clock;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.Save();
            Cur = null; batch = 0; batchDirty = false;
            NowOffset = 0;
        }

        // ---------- 편의 ----------
        public int TIdx(string id) { var ts = Tables.Turrets; for (int i = 0; i < ts.Count; i++) if (ts[i].id == id) return i; return -1; }
        public int TurretLv(string id) { int i = TIdx(id); return i < 0 ? 1 : tLv[i]; }

        /// <summary>기지 최대 체력 · 공격력</summary>
        // 밸런스 1차 (11-17): 레벨마다 곱으로도 커지게 (Base_HP_Growth · Base_Atk_Growth, 1 = 예전처럼 더하기만)
        public double BaseHp => Math.Round(Tables.C("Base_HP") * Math.Pow(Tables.C("Base_HP_Growth", 1), baseLv - 1) + Tables.C("Base_HP_Per_Lv") * (baseLv - 1));
        public double Atk => Math.Round(Tables.C("Atk_Base") * Math.Pow(Tables.C("Base_Atk_Growth", 1), baseLv - 1) + Tables.C("Base_Atk_Per_Lv") * (baseLv - 1));
        public double BaseCost => Math.Ceiling(Tables.C("Base_Cost_Base") * Math.Pow(Tables.C("Base_Cost_Growth"), baseLv - 1));
        public bool BaseMax => baseLv >= Tables.Ci("Base_Max_Lv", 300);

        /// <summary>순찰: 시간당 골드 · 조각 (깬 최고 스테이지 기준), 최대 시간까지</summary>
        public double PatrolSec => Math.Max(0, Math.Min(Clock - patrolFrom, Tables.C("Patrol_Max_Hours") * 3600));
        /// <summary>순찰 시간당 골드 (깬 최고 스테이지 기준) — 순찰 · 상점 골드 주머니가 같이 씀</summary>
        public double PatrolGoldHour => (Tables.C("Patrol_Gold_Hour") + Tables.C("Patrol_Gold_Per_Stage") * Math.Max(0, best - 1)) * Math.Pow(Tables.C("Patrol_Gold_Growth", 1), Math.Max(0, best - 1));
        public double PatrolGold => PatrolSec / 3600.0 * PatrolGoldHour;
        public int PatrolShards => (int)Math.Floor(PatrolSec / 3600.0 * Tables.C("Patrol_Shards_Hour"));
    }
}
