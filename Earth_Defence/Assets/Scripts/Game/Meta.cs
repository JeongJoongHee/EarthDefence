using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 영구 성장 · 로비 규칙: 터렛 레벨(조각 + 골드) · 기지 레벨 · 순찰 보상 (1차)
    /// + 2차: 스태미나 · 일일 미션 · 출석 7일 · 터렛 열림.
    /// </summary>
    public static class Meta
    {
        static SaveData S => SaveData.Cur;

        // ---------- 터렛 · 기지 (1차) ----------
        public static bool TurretMax(string id) => S.TurretLv(id) >= Tables.MaxTurretLv;
        public static TurretLvRow Cost(string id) => Tables.Lv(S.TurretLv(id));
        public static bool CanUpgradeTurret(string id)
        {
            if (TurretMax(id) || !S.Unlocked(Tables.TurretById[id])) return false;
            var c = Cost(id);
            return S.shards[S.TIdx(id)] >= c.shards && S.gold >= c.gold;
        }
        public static bool UpgradeTurret(string id)
        {
            if (!CanUpgradeTurret(id)) return false;
            var c = Cost(id); int i = S.TIdx(id);
            S.shards[i] -= c.shards; S.gold -= c.gold; S.tLv[i]++;
            MissionAdd("up");
            S.Save();
            return true;
        }
        public static bool CanUpgradeBase => !S.BaseMax && S.gold >= S.BaseCost;
        public static bool UpgradeBase()
        {
            if (!CanUpgradeBase) return false;
            S.gold -= S.BaseCost; S.baseLv++;
            MissionAdd("up");
            S.Save();
            return true;
        }
        public static bool CanClaimPatrol => S.PatrolSec >= Tables.C("Patrol_Min_Sec", 60);
        /// <summary>순찰 보상 받기 → (골드, 조각). 조각은 열린 터렛에만</summary>
        public static (double gold, int shards) ClaimPatrol()
        {
            if (!CanClaimPatrol) return (0, 0);
            double g = Math.Floor(S.PatrolGold); int sh = S.PatrolShards;
            S.gold += g;
            var rnd = new System.Random((int)(SaveData.Clock % 100000));
            var open = Tables.Turrets.FindAll(t => S.Unlocked(t));
            for (int i = 0; i < sh; i++) S.shards[S.TIdx(open[rnd.Next(open.Count)].id)]++;
            S.patrolFrom = SaveData.Clock;
            MissionAdd("patrol");
            S.Save();
            return (g, sh);
        }

        // ---------- 스태미나 (기획서 2장: 전투마다 씀 · 시간으로 참) ----------
        public static bool StaminaOn => Tables.Ci("Stamina_On", 1) == 1;
        public static int StaminaMax => Tables.Ci("Stamina_Max", 60);
        static long RegenSec => Math.Max(1, (long)Tables.C("Stamina_Regen_Sec", 360));
        /// <summary>지금 스태미나 (흐른 시간만큼 채워서 돌려줌 · 최대 넘게 받은 것은 그대로 둠)</summary>
        public static int Stamina
        {
            get
            {
                long now = SaveData.Clock;
                if (S.stamina >= StaminaMax) { S.staminaAt = now; return S.stamina; }
                long n = (now - S.staminaAt) / RegenSec;
                if (n > 0)
                {
                    S.stamina = (int)Math.Min(StaminaMax, S.stamina + n);
                    S.staminaAt = S.stamina >= StaminaMax ? now : S.staminaAt + n * RegenSec;
                }
                return S.stamina;
            }
        }
        /// <summary>다음 +1 까지 남은 초 (가득이면 0)</summary>
        public static long StaminaNext => Stamina >= StaminaMax ? 0 : RegenSec - (SaveData.Clock - S.staminaAt);
        public static int StaminaCost(bool elite) => !StaminaOn ? 0 : elite ? Tables.Ci("Stamina_Cost_Elite", 8) : Tables.Ci("Stamina_Cost", 5);
        public static bool CanPay(bool elite) => Stamina >= StaminaCost(elite);
        public static bool Pay(bool elite)
        {
            int c = StaminaCost(elite);
            if (Stamina < c) return false;
            bool wasFull = S.stamina >= StaminaMax;
            S.stamina -= c;
            if (wasFull) S.staminaAt = SaveData.Clock;      // 가득일 때 쓰면 지금부터 다시 참
            S.Save();
            return true;
        }
        public static void AddStamina(int n) { int cur = Stamina; S.stamina = cur + n; S.Save(); }
        public static int StaminaAdLeft
        {
            get { if (S.staminaAdDay != SaveData.Today) { S.staminaAdDay = SaveData.Today; S.staminaAdN = 0; } return Tables.Ci("Stamina_Ad_Per_Day", 3) - S.staminaAdN; }
        }
        public static void StaminaAd()
        {
            if (StaminaAdLeft <= 0) return;
            S.staminaAdN++;
            AddStamina(Tables.Ci("Stamina_Ad", 20));
        }

        // ---------- 엘리트 난이도 (기획서 11-1) ----------
        public static bool EliteOpen(int stage) => S.best >= stage;

        // ---------- 일일 미션 ----------
        static void MissionDayCheck()
        {
            if (S.missionDay == SaveData.Today && S.mProg.Count == Tables.Missions.Count) return;
            S.missionDay = SaveData.Today;
            S.mProg = new List<int>(); S.mGot = new List<int>();
            foreach (var _ in Tables.Missions) { S.mProg.Add(0); S.mGot.Add(0); }
        }
        public static int MissionProg(int i) { MissionDayCheck(); return Tables.Missions[i].kind == "all" ? AllDone() : S.mProg[i]; }
        static int AllDone() { int n = 0; for (int i = 0; i < Tables.Missions.Count; i++) if (Tables.Missions[i].kind != "all" && S.mGot[i] == 1) n++; return n; }
        public static bool MissionGot(int i) { MissionDayCheck(); return S.mGot[i] == 1; }
        public static bool MissionReady(int i) => !MissionGot(i) && MissionProg(i) >= Tables.Missions[i].target;
        public static void MissionAdd(string kind, int n = 1)
        {
            if (S == null || n <= 0) return;
            MissionDayCheck();
            for (int i = 0; i < Tables.Missions.Count; i++)
                if (Tables.Missions[i].kind == kind) S.mProg[i] = Math.Min(Tables.Missions[i].target, S.mProg[i] + n);
        }
        public static string MissionClaim(int i)
        {
            if (!MissionReady(i)) return null;
            S.mGot[i] = 1;
            var m = Tables.Missions[i];
            var s = Gear.Give(m.reward, m.value);
            S.Save();
            return s;
        }
        public static int MissionReadyCount { get { int n = 0; for (int i = 0; i < Tables.Missions.Count; i++) if (MissionReady(i)) n++; return n; } }

        // ---------- 출석 7일 (처음 켠 날부터 — 숨은 조건 없음) ----------
        public static int AttendDays => Mathf.Max(1, Tables.Ci("Attend_Days", 7));
        public static bool AttendReady => S.attendLast != SaveData.Today;
        /// <summary>오늘 받을 칸 (0부터) — 받았으면 오늘 받은 칸</summary>
        public static int AttendIdx => (AttendReady ? S.attendN : S.attendN - 1 + AttendDays) % AttendDays;
        public static string AttendClaim()
        {
            if (!AttendReady) return null;
            var row = Tables.Attend[S.attendN % AttendDays % Tables.Attend.Count];
            S.attendN++; S.attendLast = SaveData.Today;
            var s = Gear.Give(row.reward, row.value);
            S.Save();
            return s;
        }

        // ---------- 터렛 열림 알림 ----------
        /// <summary>새로 열렸는데 아직 알림을 안 본 터렛</summary>
        public static TurretRow NewUnlock()
        {
            foreach (var t in Tables.Turrets)
                if (t.unlock > 0 && S.Unlocked(t) && !S.seen.Contains("t:" + t.id)) return t;
            return null;
        }
        public static void Seen(string key) { if (!S.seen.Contains(key)) { S.seen.Add(key); S.Save(); } }

        /// <summary>보상 글자 (아이콘 이름, 글자)</summary>
        public static string RewardIcon(string kind) => kind switch
        {
            "gold" => "coin", "shards" => "shard", "stamina" => "energy", "parts" => "parts", "chip" => "chip", "gear" => "g_suit", _ => "star",
        };
        public static string RewardText(string kind, double v) => kind switch
        {
            "chip" or "gear" => L.F("grade_n", v) + " " + L.T("r_" + kind),
            _ => Num.F(v),
        };
    }
}
