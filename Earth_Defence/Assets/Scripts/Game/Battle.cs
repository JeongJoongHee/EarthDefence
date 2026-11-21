using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthDef
{
    /// <summary>
    /// 한 판 (기획서 2장): 가디언만으로 시작 → 적을 잡아 경험치 → 레벨업마다 카드 3장 중 1장 → 2분 중간 보스(잡으면 카드 1장 더)
    /// → 3분 30초 마지막 보스 → 보스를 잡으면 승리 · 기지 체력 0 이면 패배.
    /// 전투는 고정 간격(1/60초)으로 돌림 — 화면 주사율과 상관없이 같은 결과 (기획서 19-2).
    /// </summary>
    public partial class Battle : MonoBehaviour
    {
        public static Battle I;
        public const float StepSec = 1f / 60f;

        public enum St { Play, Pick, Pause, End, Slot }
        public St st = St.Play;
        public StageRow stage;
        public int stageNo;
        public Run run;
        public float time;
        public int level = 1;
        public double exp, need;
        public double baseHp, baseMax;
        public long kills;
        public double goldGot;
        public bool won;
        /// <summary>11-21 고른 배속 (x0.5 · 1 · 1.5 · 2) — 앱을 끌 때까지 다음 전투에도</summary>
        public static float LastSpeed = 1f;
        public float speed = LastSpeed;
        // 2차: 엘리트 난이도 · 장비/칩 · 보호막 · 첫 피격 면역
        public bool eliteMode;
        public Loadout lo;
        public double atk, shield, shieldMax;
        public int immune, picks;
        public readonly List<string> drops = new();     // 얻은 장비 · 칩 (결과 창 글자)
        public int dropGear, dropChip;

        public readonly List<Enemy> enemies = new();
        readonly List<Enemy> addE = new();
        readonly List<Attack> attacks = new();
        readonly List<Attack> addA = new();
        public readonly List<TurretUnit> units = new();
        public TurretUnit Guardian => units.Count > 0 ? units[0] : null;

        // 카드 고르기
        public List<CardRow> offer = new();
        public int pendingLevel, pendingBoss;
        public bool offerIsBoss;
        public event Action Changed;
        public event Action<string> Banner;     // 화면 가운데 알림 (보스 등장 등)
        public void Announce(string s) => Banner?.Invoke(s);
        public int bossRages;   // 시험용: 보스가 화난 횟수

        float acc, spawnAcc, nextWave;
        bool eliteSpawned, bossSpawned;
        public Enemy boss;
        System.Random rnd;

        /// <summary>mode = stage(스테이지) · endless(무한 모드) · survival(엘리트 군단) — 3차</summary>
        public static Battle Begin(int stageNo, int seed = 0, bool elite = false, string mode = "stage")
        {
            var go = new GameObject("Battle");
            var b = go.AddComponent<Battle>();
            b.mode = mode;
            b.Setup(stageNo, seed == 0 ? Environment.TickCount : seed, elite);
            return b;
        }

        // ---------- 3차: 도전 모드 · 기지 외형 · 다이아 ----------
        public string mode = "stage";
        public bool IsStage => mode == "stage";
        public FrameRow frame; public double frameVal; float frameT, invulnT; bool revived;
        public double rewardDia; public bool adDoubled;
        float nextElite, nextBoss;
        public int frameFired;   // 시험용: 외형 효과가 난 횟수

        void Setup(int no, int seed, bool elite)
        {
            I = this;
            stageNo = no; stage = Tables.Stage(no); eliteMode = elite && IsStage;
            frame = Store.Frame; frameVal = frame != null ? frame.Val(Store.FrameStar) : 0; frameT = frame != null ? (float)frame.cd : 0;
            SkillStart();      // 11-21 특수 능력 (BattleSkills.cs)
            nextBoss = Tables.Cf("Endless_Boss_Gap", 150); nextElite = mode == "survival" ? 8f : nextBoss * 0.5f;
            rnd = new System.Random(seed);
            Combat.Seed(seed + 1);
            lo = Loadout.Build(SaveData.Cur);
            run = new Run(seed + 2, lo);
            atk = lo.AtkOf(SaveData.Cur);
            baseMax = baseHp = lo.HpOf(SaveData.Cur);
            shield = shieldMax = lo.ShieldOf(SaveData.Cur);
            immune = (int)Math.Round(lo.B("immune"));
            need = NeedOf(1); nextPickAt = 0;
            nextWave = Tables.Cf("Wave_Len", 30);
            AddUnit(run.turrets[0]);
            World.I.ShowSlots(true);
            Time.timeScale = 1;
        }

        /// <summary>6차 수정안 "무한 모드에서 레벨업 창이 너무 빨리 뜸": 무한 모드는 필요 경험치 x Endless_Exp_Mul</summary>
        //   처음 몇 레벨(Endless_Exp_From 아래)은 그대로 — 첫 터렛이 늦으면 적을 못 잡고 막힘 (시험에서 확인)
        double NeedOf(int lv) => Need(lv) * (mode == "endless" && lv >= Tables.Ci("Endless_Exp_From", 5) ? Math.Max(0.1, Tables.C("Endless_Exp_Mul", 1)) : 1);
        /// <summary>무한 모드: 다음 레벨업 창을 열 수 있는 시각 (창 사이 최소 Endless_Pick_Gap 초)</summary>
        public float nextPickAt;
        public int picksOpened;   // 시험용: 연 레벨업 · 보스 카드 창 수
        public static double Need(int lv) => Math.Ceiling(Tables.C("Exp_Base", 8) * Math.Pow(Tables.C("Exp_Growth", 1.17), lv - 1));
        public float RunTime => mode == "survival" ? Tables.Cf("Survival_Time", 180) : mode == "endless" ? 1e6f : Tables.Cf("Run_Time", 240);
        public float TimeLeft => Mathf.Max(0, RunTime - time);
        /// <summary>위 시계: 무한 모드는 버틴 시간, 나머지는 남은 시간</summary>
        public float ClockSec => mode == "endless" ? time : TimeLeft;
        public int LevelMax => mode == "endless" ? Tables.Ci("Endless_Level_Max", 30) : Tables.Ci("Level_Max", 20);

        /// <summary>터렛 시험장 (11-16 밤, Game/Lab.cs) — null 이 아니면 적 나오기 · 경험치 · 기지 피해를 시험장 규칙으로</summary>
        public Lab lab;

        /// <summary>시험장: slot(1~4)에 터렛을 놓음 (있던 것은 뺌) · preset = base · A · B · all</summary>
        public TurretInst LabPlace(int slot, string id, string preset)
        {
            LabRemove(slot);
            var t = run.AddAt(Tables.TurretById[id], slot);
            Lab.ApplyPreset(t, preset);
            AddUnit(t);
            World.I.ShowSlots(false);
            return t;
        }
        public void LabRemove(int slot)
        {
            var t = run.turrets.Find(x => x.slot == slot);
            if (t == null) return;
            if (t.unit) { units.Remove(t.unit); Destroy(t.unit.gameObject); }
            run.turrets.Remove(t);
        }

        TurretUnit AddUnit(TurretInst t)
        {
            var go = new GameObject("Turret_" + t.row.id);
            go.transform.SetParent(transform, false);
            var u = go.AddComponent<TurretUnit>();
            // 11-18 사령탑: 대장 카피 = 가운데 사령실 · 터렛 = 고른 칸의 발판 (World.SlotPos)
            Vector2 pos = t.slot == 0 ? World.GuardianPos : World.SlotPos(t.slot);
            u.Init(t, pos);
            units.Add(u);
            return u;
        }

        // ---------- 고정 간격 진행 ----------
        void Update()
        {
            Time.timeScale = st == St.Play ? speed : 0;
            if (st != St.Play) return;
            acc += Mathf.Min(Time.deltaTime, 0.1f);
            int n = 0;
            while (acc >= StepSec && n < 8 && st == St.Play) { Tick(StepSec); acc -= StepSec; n++; }
        }

        /// <summary>한 칸 진행 (시험에서 직접 부르기도 함)</summary>
        public void Tick(float dt)
        {
            time += dt;
            if (lab != null) lab.Spawn(this, dt); else if (mode == "stage") Spawning(dt); else ModeSpawning(dt);
            FrameStep(dt);
            SkillStep(dt);      // 11-21 버닝 · 쿨타임
            foreach (var u in units) u.Step(dt);
            foreach (var a in attacks) if (!a.dead) a.Step(dt);
            foreach (var e in enemies) if (!e.dead) e.Step(dt);
            Flush();
            if (st == St.Play && baseHp <= 0) { End(false); return; }
            if (st == St.Play && mode == "survival" && time >= RunTime) { End(true); return; }
            if (st == St.Play && (pendingLevel > 0 || pendingBoss > 0) && (mode != "endless" || time >= nextPickAt)) OpenPick();
        }

        void Flush()
        {
            attacks.RemoveAll(a => a.dead);
            if (addA.Count > 0) { attacks.AddRange(addA); addA.Clear(); }
            for (int i = enemies.Count - 1; i >= 0; i--) if (enemies[i].dead) { Destroy(enemies[i].gameObject); enemies.RemoveAt(i); }
            if (addE.Count > 0) { enemies.AddRange(addE); addE.Clear(); }
        }

        public void Add(Attack a) => addA.Add(a);

        // ---------- 적 나오기 ----------
        void Spawning(float dt)
        {
            float k = Mathf.Clamp01(time / RunTime);
            float rate = Mathf.Lerp(Tables.Cf("Spawn_Rate_Start", 0.9f), Tables.Cf("Spawn_Rate_End", 2.6f), k) + Tables.Cf("Spawn_Rate_Per_Stage", 0.03f) * (stageNo - 1);
            if (bossSpawned) rate *= 0.5f;
            if (eliteMode) rate *= Tables.Cf("Elite_Mode_Spawn", 1.25f);
            spawnAcc += rate * dt;
            int alive = enemies.Count + addE.Count, max = Tables.Ci("Max_Alive", 90);
            while (spawnAcc >= 1)
            {
                spawnAcc -= 1;
                if (alive++ >= max) continue;
                string id = stage.enemies[rnd.Next(stage.enemies.Length)];
                Spawn(id, SpawnPoint());
            }
            // 웨이브마다 몰려오기 (한 종류가 한 곳에 무리로)
            if (time >= nextWave && time < Tables.Cf("Boss_Time", 210))
            {
                nextWave += Tables.Cf("Wave_Len", 30);
                Swarm();
            }
            if (!eliteSpawned && time >= Tables.Cf("Mid_Boss_Time", 120))
            {
                eliteSpawned = true;
                Spawn(stage.elite, BigSpawn(0.5f));
                Banner?.Invoke(L.T("elite_warn"));
                Audio.Play(Audio.Sfx.EliteWarn);
            }
            if (!bossSpawned && time >= Tables.Cf("Boss_Time", 210))
            {
                bossSpawned = true;
                boss = Spawn(stage.boss, BigSpawn(1f));
                Banner?.Invoke(L.T("boss_warn"));
                Audio.Play(Audio.Sfx.BossWarn);
                Audio.Music("boss");
            }
        }

        // ---------- 11-18 사령탑: 적이 나오는 곳 · 겨누는 곳 ----------
        /// <summary>나오는 곳: 위 가장자리 오른쪽(Spawn_Top_Share) 또는 오른쪽 가장자리 위쪽 (사용자 "운석은 대각선 오른쪽에서 왼쪽으로 떨어지듯")</summary>
        public Vector2 SpawnPoint()
        {
            // 9차 세로 전장: 예전처럼 위 가장자리 아무 x 에서 (나오는 줄 아래쪽으로 — UI 에 안 가리게)
            if (World.V) return new Vector2(RandX(), World.SpawnY - 0.8f * (float)rnd.NextDouble());
            if (rnd.NextDouble() < Tables.C("Spawn_Top_Share", 0.65))
                return new Vector2(Mathf.Lerp(Tables.Cf("Spawn_Top_X0", -0.5f), World.HalfW - 0.5f, (float)rnd.NextDouble()), World.SpawnY - 0.8f * (float)rnd.NextDouble());
            return new Vector2(World.HalfW + 0.4f, Mathf.Lerp(Tables.Cf("Spawn_Right_Y0", 0f), World.SpawnY - 0.3f, (float)rnd.NextDouble()));
        }
        /// <summary>중간 보스 · 보스: 오른쪽 위 모서리 쪽 (몸이 커서 반쯤 화면 밖)</summary>
        Vector2 BigSpawn(float up) => World.V ? new(RandX() * 0.3f, World.SpawnY + up) : new(Mathf.Min(World.HalfW - 1.6f, 2.6f + (float)rnd.NextDouble()), World.SpawnY + up);
        void Swarm()
        {
            var c = SpawnPoint(); c.x = World.V ? RandX() * 0.7f : Mathf.Min(c.x, World.HalfW - 1f);
            var tgt = AimTarget(c);
            int n = Tables.Ci("Swarm_Size", 8);
            for (int i = 0; i < n; i++)
            {
                var p = c + new Vector2((float)(rnd.NextDouble() - 0.5) * (World.V ? 3f : 2.4f), i * 0.35f);
                var e = Spawn(stage.swarm, p);
                if (!World.V) e?.Aim(tgt + new Vector2(0, (float)(rnd.NextDouble() - 0.5) * 1.2f));   // 무리는 같은 곳을 노림
            }
        }
        float RandX() => (float)(rnd.NextDouble() * 2 - 1) * (World.HalfW - 0.6f);
        /// <summary>겨누는 곳: 방어선 위 사령탑 높이 안에서, 내려오는 각도가 Approach_Min ~ Approach_Max 도인 곳 (무작위)</summary>
        public Vector2 AimTarget(Vector2 from)
        {
            if (World.V) return new Vector2(from.x, World.ShieldY - 1f);      // 9차 세로: 곧게 아래로
            float sx = World.ShieldX, lo = World.GroundY + 0.6f, hi = World.TowerTop - 0.4f;
            float dx = Mathf.Max(0.5f, from.x - sx);
            float a = from.y - dx * Mathf.Tan(Tables.Cf("Approach_Max", 70f) * Mathf.Deg2Rad);
            float b = from.y - dx * Mathf.Tan(Tables.Cf("Approach_Min", 25f) * Mathf.Deg2Rad);
            float l = Mathf.Max(lo, a), h = Mathf.Min(hi, b);
            float ty = l <= h ? Mathf.Lerp(l, h, (float)rnd.NextDouble()) : b < lo ? lo : hi;
            return new Vector2(sx, ty);
        }

        /// <summary>무한 모드 · 엘리트 군단: 시간이 갈수록 많이 · 중간 보스 · 보스(무한만)</summary>
        void ModeSpawning(float dt)
        {
            float min = time / 60f;
            float rate = mode == "endless"
                ? Tables.Cf("Spawn_Rate_End", 3.2f) * 0.6f + Tables.Cf("Endless_Spawn_Growth", 0.35f) * min
                : Mathf.Lerp(Tables.Cf("Spawn_Rate_Start", 1.2f), Tables.Cf("Spawn_Rate_End", 3.2f), Mathf.Clamp01(time / RunTime)) * 1.2f;
            spawnAcc += rate * dt;
            int alive = enemies.Count + addE.Count, max = Tables.Ci("Max_Alive", 90);
            while (spawnAcc >= 1)
            {
                spawnAcc -= 1;
                if (alive++ >= max) continue;
                Spawn(stage.enemies[rnd.Next(stage.enemies.Length)], SpawnPoint());
            }
            if (time >= nextWave)
            {
                nextWave += Tables.Cf("Wave_Len", 30);
                Swarm();
            }
            if (time >= nextElite)
            {
                nextElite += mode == "survival" ? Tables.Cf("Survival_Elite_Gap", 12) : Tables.Cf("Endless_Boss_Gap", 150);
                var els = SurvivalElites;
                Spawn(mode == "survival" ? els[rnd.Next(els.Length)] : stage.elite, BigSpawn(0.5f));
                if (!eliteSpawned) { eliteSpawned = true; Banner?.Invoke(L.T("elite_warn")); Audio.Play(Audio.Sfx.EliteWarn); }
            }
            if (mode == "endless" && time >= nextBoss)
            {
                nextBoss += Tables.Cf("Endless_Boss_Gap", 150);
                boss = Spawn(rnd.Next(2) == 0 ? "planet" : "queen", BigSpawn(1f));
                Banner?.Invoke(L.T("boss_warn")); Audio.Play(Audio.Sfx.BossWarn); Audio.Music("boss");
            }
        }

        /// <summary>기지 외형 효과 (3차): 별똥별 비 · 얼림 · 회복 은 쿨다운마다, 부활 · 반격 · 골드는 다른 곳에서</summary>
        void FrameStep(float dt)
        {
            if (invulnT > 0) invulnT -= dt;
            if (frame == null || frame.cd <= 0 || lab != null) return;
            if ((frameT -= dt) > 0) return;
            frameT = (float)frame.cd;
            switch (frame.effect)
            {
                case "starfall":
                {
                    frameFired++;
                    var g = run.turrets[0];
                    for (int i = enemies.Count - 1; i >= 0; i--) if (i < enemies.Count && !enemies[i].dead) Combat.Hit(g, enemies[i], frameVal, false, true);
                    for (int i = 0; i < 14; i++)
                    {
                        var p = new Vector2(Mathf.Lerp(World.FieldX0 + 0.5f, World.HalfW - 0.4f, (float)rnd.NextDouble()), Mathf.Lerp(World.FieldLo + 1f, World.SpawnY - 1f, (float)rnd.NextDouble()));
                        Fx.Glow(p, 0.5f, new Color(0.75f, 0.82f, 1f, 0.9f), 0.35f);
                        Fx.Sparks(p, new Color(0.85f, 0.9f, 1f), 3, 5f, 1f, 30f, -110f);
                    }
                    Audio.Play(Audio.Sfx.Break, 0.8f);
                    break;
                }
                case "freeze":
                {
                    frameFired++;
                    foreach (var e in enemies)
                        if (!e.dead && e.VY < World.ShieldY + 3.5f) { e.Paralyze((float)frameVal); Fx.Ring(e.Pos, 0.5f, Art.Hex("#9fe8ff")); }   // 방어선까지 3.5칸 안
                    Fx.Ring(World.BaseCenter, 4f, Art.Hex("#9fe8ff"));
                    Audio.Play(Audio.Sfx.Zap);
                    break;
                }
                case "regen":
                    if (baseHp < baseMax) { frameFired++; Heal(frameVal / 100.0); Fx.Glow(World.BaseCenter, 1.2f, new Color(1f, 0.7f, 0.8f, 0.5f), 0.4f); }
                    break;
            }
        }


        /// <summary>스테이지 보정: Early_Stage_N 아래는 Early_HP_Mul → 1 로 곱으로 서서히 · 10의 배수는 Ten_Stage_HP_Mul (무한 · 엘리트 군단은 그대로)</summary>
        /// <summary>적 수치를 읽는 스테이지 번호 (엘리트 난이도는 Elite_Mode_Offset 뒤)</summary>
        public int StatStage => stageNo + (eliteMode ? Tables.Ci("Elite_Mode_Offset", 0) : 0);
        /// <summary>Stage 표 Hp_Mul — 표 끝을 넘으면(엘리트 91~100) 마지막 값에서 10 스테이지마다 Chapter_Over_HP 배</summary>
        public static double StageHpMul(int n)
        {
            int cnt = Tables.Stages.Count;
            if (n <= cnt) return Tables.Stage(n).hpMul;
            return Tables.Stages[cnt - 1].hpMul * Math.Pow(Tables.C("Chapter_Over_HP", 1.3), Math.Ceiling((n - cnt) / 10.0));
        }
        public (double hp, double atk) StageMul(int n)
        {
            int en = Tables.Ci("Early_Stage_N", 0); double k = 1;
            if (en > 1 && n < en) k = Math.Pow(Math.Max(0.001, Tables.C("Early_HP_Mul", 1)), 1 - (n - 1) / (en - 1.0));   // 곱으로 서서히 (3 ≈ 0.07 · 5 ≈ 0.14 · 8 ≈ 0.46 배)
            double ten = n % 10 == 0 ? Tables.C("Ten_Stage_HP_Mul", 1) : 1;
            // 밸런스 2차 "10 스테이지마다 툭툭 · 20 부터": Chapter_Step_From(20) 부터 10 스테이지마다 한 계단 (그 안에선 Mon_HP_Growth 로 완만하게)
            int from = Tables.Ci("Chapter_Step_From", 0); int steps = from > 0 && n >= from ? (n - from) / 10 + 1 : 0;
            double sh = Math.Pow(Tables.C("Chapter_Step_HP", 1), steps), sa = Math.Pow(Tables.C("Chapter_Step_Atk", 1), steps);
            return (k * ten * sh, Math.Sqrt(k) * sa);
        }

        /// <summary>적 하나 만들기 (체력·공격력 = 스테이지 기본 x 적 배율 x 한 판 안에서 조금씩 오름)</summary>
        public Enemy Spawn(string id, Vector2 pos, bool minion = false)
        {
            if (!Tables.Enemies.TryGetValue(id, out var r)) { Debug.LogError("적 없음: " + id); return null; }
            // 밸런스 3차: 엘리트 난이도 = Elite_Mode_Offset 스테이지 뒤의 적 (예전엔 체력 x2.6 고정 — 계단 표가 생기면서 후반 엘리트가 너무 셌음)
            int sn = StatStage;
            double hp = Tables.C("Mon_HP_Base") * Math.Pow(Tables.C("Mon_HP_Growth"), sn - 1) * r.hpMul * StageHpMul(sn);
            double atk = Tables.C("Mon_Atk_Base") * Math.Pow(Tables.C("Mon_Atk_Growth"), sn - 1) * r.atkMul;
            // 밸런스 1차 (11-17): 처음 몇 스테이지는 약하게 (튜토리얼) · 10의 배수 스테이지는 조금 세게 (챕터 벽)
            var sm = StageMul(sn); hp *= sm.hp; atk *= sm.atk;
            // 밸런스 3차: 무한 모드는 깬 최고 스테이지 적의 Endless_Start_Mul 배에서 시작해 1분마다 체력 · 공격력이 커짐
            if (mode == "endless") { hp *= Tables.C("Endless_Start_Mul", 1) * Math.Pow(Tables.C("Endless_Growth", 1.55), time / 60.0); atk *= Math.Pow(Tables.C("Endless_Atk_Growth", 1), time / 60.0); }
            else hp *= 1 + Tables.C("Run_HP_Ramp", 1.5) * Mathf.Clamp01(time / RunTime);
            if (mode == "survival") hp *= Tables.C("Survival_HP", 1.4);
            if (r.IsElite) { hp *= Tables.C("Elite_HP_Mul", 30); atk *= Tables.C("Elite_Atk_Mul", 3); }
            if (r.IsBoss) { hp *= Tables.C("Boss_HP_Mul", 140); atk *= Tables.C("Boss_Atk_Mul", 6); }
            if (eliteMode) { hp *= Tables.C("Elite_Mode_HP", 2.6); atk *= Tables.C("Elite_Mode_Atk", 1.6); }
            var go = new GameObject("Enemy_" + id);
            go.transform.SetParent(transform, false);
            var e = go.AddComponent<Enemy>();
            e.Init(r, pos, hp, atk);
            e.Aim(AimTarget(pos));          // 11-18: 사령탑 쪽으로 비스듬히
            addE.Add(e);
            return e;
        }

        // ---------- 처치 · 기지 피해 ----------
        public void OnKill(Enemy e, TurretInst src)
        {
            kills++;
            Audio.Play(e.row.IsBoss || e.row.IsElite ? Audio.Sfx.BigBoom : Audio.Sfx.Kill);
            goldGot += Tables.C("Kill_Gold", 1) * (1 + 0.1 * (stageNo - 1)) * (e.row.IsBoss ? 30 : e.row.IsElite ? 10 : 1);
            Fx.Hit(e.Pos, e.size * 1.3f);
            if (!string.IsNullOrEmpty(e.row.split))
                for (int i = 0; i < e.row.splitN; i++) Spawn(e.row.split, e.Pos + new Vector2((i - (e.row.splitN - 1) / 2f) * 0.6f, 0.1f), true);
            GainExp(e.row.exp);
            if ((e.row.IsBoss || e.row.IsElite) && lo.B("heal_elite") > 0) Heal(lo.B("heal_elite") / 100.0);
            if (e.row.IsBoss && lab == null && IsStage) { if (e == boss || boss == null || boss.dead) End(true); return; }
            if (e.row.IsElite && lab == null && Tables.Ci("Boss_Extra_Card", 1) == 1) pendingBoss++;
        }

        public void GainExp(double v)
        {
            if (lab != null) return;
            if (level >= LevelMax) return;
            exp += v;
            while (exp >= need && level < LevelMax)
            {
                exp -= need; level++; pendingLevel++;
                need = NeedOf(level);
            }
            if (level >= LevelMax) exp = 0;
        }

        public void HurtBase(double d, Vector2 at, Enemy src = null)
        {
            if (st == St.End) return;
            if (lab != null) { Fx.Hit(at + World.BaseDir * 0.3f, 0.8f); return; }   // 시험장: 기지는 안 닳음 (11-18: 기지 = 왼쪽)
            if (invulnT > 0) { Fx.Ring(at + World.BaseDir * 0.2f, 0.6f, Art.Hex("#f3dcff")); return; }
            // 가시 돔: 때린 적에게 반격
            if (src != null && !src.dead && frame != null && frame.effect == "thorn") { frameFired++; Combat.Hit(run.turrets[0], src, frameVal, false, true); Fx.Sparks(src.Pos, Art.Hex("#b8e08f"), 4, 4f, 0.9f); }
            // 2차 칩: 처음 N번 막음 → 받는 피해 -N(최소 1) → 보호막이 먼저 받음
            if (immune > 0) { immune--; Fx.Ring(at + World.BaseDir * 0.2f, 0.8f, Art.Sky); return; }
            d = Tables.C("Reduce_K", 0) > 0 ? Math.Max(1, d * lo.ReduceMul) : Math.Max(1, d - lo.reduce);   // 밸런스 3차: 방어력 = 비율 감소
            if (shield > 0) { double s = Math.Min(shield, d); shield -= s; d -= s; if (d <= 0) { Fx.Ring(at + World.BaseDir * 0.2f, 0.6f, Art.Sky); return; } }
            baseHp = Math.Max(0, baseHp - d);
            Fx.Hit(at + World.BaseDir * 0.3f, 1.2f);
            Audio.Play(Audio.Sfx.BaseHit);
            // 무지개 돔: 처음 무너질 때 한 번 버팀
            if (baseHp <= 0 && frame != null && frame.effect == "revive" && !revived)
            {
                revived = true; frameFired++;
                baseHp = baseMax * frameVal / 100.0; invulnT = 3f;
                Banner?.Invoke(L.T("frame_eff_revive"));
                Fx.Ring(World.BaseCenter, 5f, Art.Hex("#f3dcff"));
                Audio.Play(Audio.Sfx.Break);
            }
        }

        public void Heal(double frac) { if (st != St.End) baseHp = Math.Min(baseMax, baseHp + baseMax * frac); }

        /// <summary>조건 피해 칩용: 지금 중간 보스 · 보스가 있나</summary>
        public bool BossOnField { get { foreach (var e in enemies) if (!e.dead && (e.row.IsBoss || e.row.IsElite)) return true; return false; } }

        /// <summary>목표 찾기: from 에서 range 안의 가장 가까운 적 (방어막 위쪽, 화면에 보이는 적)</summary>
        public Enemy Nearest(Vector2 from, float range, HashSet<Enemy> skip = null)
        {
            Enemy best = null; float bd = range * range;
            foreach (var e in enemies)
            {
                if (e.dead || (skip != null && skip.Contains(e)) || !World.OnField(e.Pos)) continue;
                float d = (e.Pos - from).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        /// <summary>찌릿 코일: at 둘레 r 안의 무작위 적 (not 빼고, 화면에 보이는 적)</summary>
        public Enemy RandomEnemyNear(Vector2 at, float r, Enemy not)
        {
            int n = 0; Enemy pick = null;
            foreach (var e in enemies)
                if (!e.dead && e != not && World.OnField(e.Pos) && Vector2.Distance(e.Pos, at) <= r && rnd.Next(++n) == 0) pick = e;
            return pick;
        }

        public Enemy RandomEnemy()
        {
            int n = 0; Enemy pick = null;
            foreach (var e in enemies) if (!e.dead && World.OnField(e.Pos) && rnd.Next(++n) == 0) pick = e;
            return pick;
        }

        static readonly int[] crowdCnt = new int[24];
        static readonly string[] SurvivalElites = { "big", "moth_king", "beetle_steel", "mother" };
        /// <summary>적이 가장 많이 모인 높이 (장판 놓을 곳) — 달 바닥부터 1칸 높이 칸마다 세어 가장 많은 곳 (11-18: 예전 = 방어막부터)</summary>
        public float CrowdY()
        {
            var cnt = crowdCnt; Array.Clear(cnt, 0, cnt.Length);
            foreach (var e in enemies)
            {
                if (e.dead) continue;
                int i = Mathf.FloorToInt(e.Pos.y - World.FieldLo);
                if (i >= 0 && i < cnt.Length) cnt[i] += e.row.IsBoss || e.row.IsElite ? 3 : 1;
            }
            int best = World.V ? 1 : 4; for (int i = 0; i < cnt.Length; i++) if (cnt[i] > cnt[best]) best = i;
            return World.FieldLo + best + 0.5f;
        }

        static readonly int[] crowdCntX = new int[12];
        /// <summary>8차 "끈끈이 장판을 가로가 아닌 세로로": 적이 가장 많은 세로줄(1칸 폭)의 가운데 x — 방어선 오른쪽 하늘에서</summary>
        public float CrowdX()
        {
            var cnt = crowdCntX; Array.Clear(cnt, 0, cnt.Length);
            float x0 = World.FieldX0;
            foreach (var e in enemies)
            {
                if (e.dead || !World.OnField(e.Pos)) continue;
                int i = Mathf.FloorToInt(e.Pos.x - x0);
                if (i >= 0 && i < cnt.Length) cnt[i] += e.row.IsBoss || e.row.IsElite ? 3 : 1;
            }
            int n = Mathf.Clamp(Mathf.FloorToInt(World.HalfW - x0), 1, cnt.Length);
            int best = n / 2; for (int i = 0; i < n; i++) if (cnt[i] > cnt[best]) best = i;
            return x0 + best + 0.5f;
        }

        // ---------- 카드 ----------
        void OpenPick()
        {
            offerIsBoss = pendingLevel == 0 && pendingBoss > 0;
            offer = run.Roll(Choices, offerIsBoss);
            if (offer.Count == 0) { pendingLevel = 0; pendingBoss = 0; return; }   // 고를 카드가 없으면 넘어감
            st = St.Pick; picksOpened++;
            Audio.Play(Audio.Sfx.LevelUp);
            Changed?.Invoke();
        }

        /// <summary>카드 장수 (칩 "첫 카드 N장 더" = 첫 레벨업에만)</summary>
        int Choices => Tables.Ci("Card_Choices", 3) + (picks == 0 ? run.firstExtra : 0);

        public void Choose(int i, int slot = 0)
        {
            if (st != St.Pick || i < 0 || i >= offer.Count) return;
            var c = offer[i];
            picks++;
            Audio.Play(c.kind == "break" ? Audio.Sfx.Break : c.kind == "new" ? Audio.Sfx.Place : Audio.Sfx.CardPick);
            if (c.kind == "break" && lo.B("heal_break") > 0) Heal(lo.B("heal_break") / 100.0);
            // 11-18 사용자 "터렛을 고를 때 자리를 고르는 흐름이 나오며 그 자리에 배치": 새 터렛 + 빈 칸이 2개 이상이면 자리 고르기
            if (c.kind == "new" && slot <= 0 && AskSlot && run.FreeSlotList.Count >= 2)
            {
                slotCard = c; st = St.Slot; slotAsks++;
                World.I.ShowSlots(true, true);
                Changed?.Invoke();
                return;
            }
            Finish(c, slot);
        }

        // 7차 수정안: 자리 고르기에서 [카드 다시 고르기]는 뺌 (카드 창에 이미 새로고침이 있음) — 고른 카드는 꼭 놓음

        /// <summary>자리 고르기에서 칸(1~4)을 누름</summary>
        public void PlaceAt(int slot)
        {
            if (st != St.Slot || slotCard == null || !run.FreeSlotList.Contains(slot)) return;
            var c = slotCard; slotCard = null;
            Finish(c, slot);
        }

        void Finish(CardRow c, int slot)
        {
            int before = run.turrets.Count;
            var t = run.Pick(c, slot);
            if (run.turrets.Count > before) { AddUnit(t); World.I.ShowSlots(true); }
            else if (t?.unit != null && c.kind == "break") t.unit.RefreshTier();
            if (offerIsBoss) pendingBoss--; else pendingLevel--;
            st = St.Play;
            if (mode == "endless") nextPickAt = time + Tables.Cf("Endless_Pick_Gap", 0);
            Changed?.Invoke();
        }

        /// <summary>자리 고르기를 할지 (Config Slot_Pick 1) — 밸런스 측정 · 자동 시험은 끔(정해진 순서로 자동)</summary>
        public static bool NoSlotPick;
        bool AskSlot => !NoSlotPick && Tables.Ci("Slot_Pick", 1) == 1 && lab == null;
        public CardRow slotCard;
        public int slotAsks;      // 시험용

        /// <summary>새로고침: 광고(판마다 Refresh_Ad 번) 또는 무료</summary>
        public bool CanRefresh => run.refreshFree > 0 || run.refreshAd > 0;
        public void Refresh()
        {
            if (st != St.Pick || !CanRefresh) return;
            if (run.refreshFree > 0) run.refreshFree--; else run.refreshAd--;
            offer = run.Roll(Choices, offerIsBoss);
            Changed?.Invoke();
        }

        // ---------- 끝 ----------
        public double RewardGold, RewardShards;
        public bool FirstClear;
        public void End(bool win)
        {
            if (st == St.End) return;
            st = St.End; won = win;
            SaveData.BeginBatch();      // 보상 · 떨어진 장비(가방 넘침 분해) · 미션을 한 번에 저장
            try { EndRewards(win); } finally { SaveData.EndBatch(); }
            Changed?.Invoke();
        }

        void EndRewards(bool win)
        {
            Audio.Duck(0.35f);
            Audio.Play(win ? Audio.Sfx.Victory : Audio.Sfx.Defeat);
            Time.timeScale = 1;
            var s = SaveData.Cur;
            FirstClear = IsStage && win && stageNo > s.best;
            if (eliteMode) FirstClear = win && stageNo > s.eliteBest;
            double mul = win ? (FirstClear ? Tables.C("First_Clear_Mul", 2) : 1) : Tables.C("Lose_Mul", 0.35) * Mathf.Clamp01(time / RunTime);
            if (eliteMode) mul *= Tables.C("Elite_Mode_Reward", 2);
            if (frame != null && frame.effect == "gold") goldGot *= 1 + frameVal / 100.0;
            double min = time / 60.0, stMul = 1 + 0.1 * (stageNo - 1);
            if (mode == "endless")
            {
                // 밸런스 3차: 스테이지 승리 골드와 같은 식 — 1분 버티면 승리 골드의 Endless_Gold_Rate 배 (예전 식은 곱 성장이 빠져 후반에 아주 작았음)
                RewardGold = Math.Floor(((Tables.C("Win_Gold_Base") + Tables.C("Win_Gold_Per_Stage") * (stageNo - 1)) * min * Tables.C("Endless_Gold_Rate", 0.25) + goldGot) * Math.Pow(Tables.C("Win_Gold_Growth", 1), stageNo - 1));
                RewardShards = Math.Floor(min * 4);
                rewardDia = Math.Floor(Tables.C("Endless_Dia_Min", 4) * min);
                if (time > s.endlessBest) s.endlessBest = time;
            }
            else if (mode == "survival")
            {
                double k = win ? 1 : Tables.C("Lose_Mul", 0.35) * Mathf.Clamp01(time / RunTime);
                RewardGold = Math.Floor((Tables.C("Win_Gold_Base") + Tables.C("Win_Gold_Per_Stage") * (stageNo - 1) + goldGot) * Math.Pow(Tables.C("Win_Gold_Growth", 1), stageNo - 1) * 1.5 * k);
                RewardShards = Math.Floor((Tables.C("Win_Shards") + Tables.C("Win_Shards_Per_Stage") * (stageNo - 1)) * k);
                if (win) { rewardDia = Tables.C("Survival_Dia", 30); s.survWins++; }
            }
            else
            {
                // 밸런스 1차: 스테이지마다 곱으로도 커지게 (Win_Gold_Growth, 1 = 예전)
                RewardGold = Math.Floor((Tables.C("Win_Gold_Base") + Tables.C("Win_Gold_Per_Stage") * (stageNo - 1) + goldGot) * Math.Pow(Tables.C("Win_Gold_Growth", 1), stageNo - 1) * mul);
                RewardShards = Math.Floor((Tables.C("Win_Shards") + Tables.C("Win_Shards_Per_Stage") * (stageNo - 1)) * mul);
                if (FirstClear) rewardDia = eliteMode ? Tables.C("Dia_Elite_First", 10) : Tables.C("Dia_First_Clear", 5) + (stageNo % 10 == 0 ? Tables.C("Dia_First_Clear_10", 50) : 0);
            }
            if (Store.GoldBoostOn) RewardGold = Math.Floor(RewardGold * Tables.C("Gold_Boost_Mul", 2));   // 밸런스 3차: 골드 부스터 (30일 · 판 골드 x2)
            s.dia += rewardDia;
            s.gold += RewardGold;
            GiveShards((int)RewardShards);
            if (win && eliteMode) { s.wins++; if (stageNo > s.eliteBest) s.eliteBest = stageNo; }
            else if (win && IsStage) { s.wins++; if (stageNo > s.best) s.best = stageNo; s.stage = Mathf.Min(Tables.Stages.Count, Mathf.Max(s.stage, stageNo + 1)); }
            s.runs++; s.kills += kills;
            // 2차: 장비 · 칩 떨어짐 (이겼을 때)
            if (win)
            {
                int n = mode == "survival" ? Tables.Ci("Survival_Drops", 3) : Tables.Ci("Drop_Win_N", 2) + (eliteMode ? Tables.Ci("Drop_Elite_N", 1) : 0) + (FirstClear ? Tables.Ci("Drop_First_N", 1) : 0);
                for (int i = 0; i < n; i++)
                {
                    int g = Gear.DropGrade(stageNo, eliteMode, rnd);
                    if (rnd.NextDouble() < Tables.C("Drop_Gear_Chance", 0.4)) { var it = Gear.NewGear(g, null, rnd); drops.Add(it.Name); dropGear++; }
                    else { var c = Gear.NewChip(g, null, rnd); drops.Add(c.Name); dropChip++; }
                }
            }
            Meta.MissionAdd("play");
            if (win) Meta.MissionAdd("win");
            if (eliteMode) Meta.MissionAdd("elite");
            Meta.MissionAdd("kill", (int)Math.Min(int.MaxValue, kills));
            s.Save();
        }

        /// <summary>결과 창 "광고 보고 보상 2배" [초안] — 한 판에 1번 · 골드 · 조각 · 다이아를 한 번 더</summary>
        public bool AdDouble()
        {
            if (st != St.End || adDoubled) return false;
            adDoubled = true;
            var s = SaveData.Cur; double k = Tables.C("Ad_Reward_Mul", 2) - 1;
            s.gold += RewardGold * k; s.dia += rewardDia * k; GiveShards((int)(RewardShards * k));
            RewardGold *= 1 + k; RewardShards = Math.Floor(RewardShards * (1 + k)); rewardDia *= 1 + k;
            s.Save();
            return true;
        }

        /// <summary>조각을 이번 판에 쓴 터렛 위주로 나눠 줌 (없으면 무작위)</summary>
        public readonly Dictionary<string, int> shardsGot = new();
        void GiveShards(int n)
        {
            var s = SaveData.Cur;
            var used = new List<string>(); foreach (var t in run.turrets) used.Add(t.row.id);
            var open = Tables.Turrets.FindAll(t => s.Unlocked(t));
            for (int i = 0; i < n; i++)
            {
                string id = rnd.NextDouble() < 0.7 ? used[rnd.Next(used.Count)] : open[rnd.Next(open.Count)].id;
                s.shards[s.TIdx(id)]++;
                shardsGot[id] = (shardsGot.TryGetValue(id, out var v) ? v : 0) + 1;
            }
        }

        public void Pause(bool on)
        {
            if (st == St.End || st == St.Pick || st == St.Slot) return;
            st = on ? St.Pause : St.Play;
            Changed?.Invoke();
        }

        public void Close()
        {
            Time.timeScale = 1;
            if (I == this) I = null;
            World.I.ShowSlots(false);
            Destroy(gameObject);
        }
    }
}
