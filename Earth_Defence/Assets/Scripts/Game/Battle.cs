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
    public class Battle : MonoBehaviour
    {
        public static Battle I;
        public const float StepSec = 1f / 60f;

        public enum St { Play, Pick, Pause, End }
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
        public float speed = 1f;
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

        float acc, spawnAcc, nextWave;
        bool eliteSpawned, bossSpawned;
        public Enemy boss;
        System.Random rnd;

        public static Battle Begin(int stageNo, int seed = 0, bool elite = false)
        {
            var go = new GameObject("Battle");
            var b = go.AddComponent<Battle>();
            b.Setup(stageNo, seed == 0 ? Environment.TickCount : seed, elite);
            return b;
        }

        void Setup(int no, int seed, bool elite)
        {
            I = this;
            stageNo = no; stage = Tables.Stage(no); eliteMode = elite;
            rnd = new System.Random(seed);
            Combat.Seed(seed + 1);
            lo = Loadout.Build(SaveData.Cur);
            run = new Run(seed + 2, lo);
            atk = SaveData.Cur.Atk + lo.AtkTotal;
            baseMax = baseHp = SaveData.Cur.BaseHp + lo.hp;
            shield = shieldMax = lo.B("shield");
            immune = (int)Math.Round(lo.B("immune"));
            need = Need(1);
            nextWave = Tables.Cf("Wave_Len", 30);
            AddUnit(run.turrets[0]);
            World.I.ShowSlots(0);
            Time.timeScale = 1;
        }

        public static double Need(int lv) => Math.Ceiling(Tables.C("Exp_Base", 8) * Math.Pow(Tables.C("Exp_Growth", 1.17), lv - 1));
        public float RunTime => Tables.Cf("Run_Time", 240);
        public float TimeLeft => Mathf.Max(0, RunTime - time);
        public int LevelMax => Tables.Ci("Level_Max", 20);

        TurretUnit AddUnit(TurretInst t)
        {
            var go = new GameObject("Turret_" + t.row.id);
            go.transform.SetParent(transform, false);
            var u = go.AddComponent<TurretUnit>();
            Vector2 pos = t.slot == 0 ? new Vector2(0, World.GroundY - 0.04f) : new Vector2(World.SlotX[t.slot - 1], World.GroundY - 0.06f);
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
            Spawning(dt);
            foreach (var u in units) u.Step(dt);
            foreach (var a in attacks) if (!a.dead) a.Step(dt);
            foreach (var e in enemies) if (!e.dead) e.Step(dt);
            Flush();
            if (st == St.Play && baseHp <= 0) { End(false); return; }
            if (st == St.Play && (pendingLevel > 0 || pendingBoss > 0)) OpenPick();
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
                Spawn(id, new Vector2(RandX(), World.SpawnY - 0.8f * (float)rnd.NextDouble()));   // 11-16: 나오는 줄 아래쪽으로 (UI 에 안 가리게)
            }
            // 웨이브마다 몰려오기 (한 종류가 한 곳에 무리로)
            if (time >= nextWave && time < Tables.Cf("Boss_Time", 210))
            {
                nextWave += Tables.Cf("Wave_Len", 30);
                float cx = RandX() * 0.7f;
                int n = Tables.Ci("Swarm_Size", 8);
                for (int i = 0; i < n; i++) Spawn(stage.swarm, new Vector2(cx + (float)(rnd.NextDouble() - 0.5) * 3f, World.SpawnY + i * 0.35f));
            }
            if (!eliteSpawned && time >= Tables.Cf("Mid_Boss_Time", 120))
            {
                eliteSpawned = true;
                Spawn(stage.elite, new Vector2(0, World.SpawnY + 0.5f));
                Banner?.Invoke(L.T("elite_warn"));
                Audio.Play(Audio.Sfx.EliteWarn);
            }
            if (!bossSpawned && time >= Tables.Cf("Boss_Time", 210))
            {
                bossSpawned = true;
                boss = Spawn(stage.boss, new Vector2(0, World.SpawnY + 1f));
                Banner?.Invoke(L.T("boss_warn"));
                Audio.Play(Audio.Sfx.BossWarn);
                Audio.Music("boss");
            }
        }

        float RandX() => (float)(rnd.NextDouble() * 2 - 1) * (World.HalfW - 0.6f);

        /// <summary>적 하나 만들기 (체력·공격력 = 스테이지 기본 x 적 배율 x 한 판 안에서 조금씩 오름)</summary>
        public Enemy Spawn(string id, Vector2 pos, bool minion = false)
        {
            if (!Tables.Enemies.TryGetValue(id, out var r)) { Debug.LogError("적 없음: " + id); return null; }
            double hp = Tables.C("Mon_HP_Base") * Math.Pow(Tables.C("Mon_HP_Growth"), stageNo - 1) * r.hpMul * stage.hpMul;
            double atk = Tables.C("Mon_Atk_Base") * Math.Pow(Tables.C("Mon_Atk_Growth"), stageNo - 1) * r.atkMul;
            hp *= 1 + Tables.C("Run_HP_Ramp", 1.5) * Mathf.Clamp01(time / RunTime);
            if (r.IsElite) { hp *= Tables.C("Elite_HP_Mul", 30); atk *= Tables.C("Elite_Atk_Mul", 3); }
            if (r.IsBoss) { hp *= Tables.C("Boss_HP_Mul", 140); atk *= Tables.C("Boss_Atk_Mul", 6); }
            if (eliteMode) { hp *= Tables.C("Elite_Mode_HP", 2.6); atk *= Tables.C("Elite_Mode_Atk", 1.6); }
            var go = new GameObject("Enemy_" + id);
            go.transform.SetParent(transform, false);
            var e = go.AddComponent<Enemy>();
            e.Init(r, pos, hp, atk);
            addE.Add(e);
            return e;
        }

        // ---------- 처치 · 기지 피해 ----------
        public void OnKill(Enemy e, TurretInst src)
        {
            kills++;
            Audio.Play(e.row.IsBoss || e.row.IsElite ? Audio.Sfx.BigBoom : Audio.Sfx.Kill);
            goldGot += Tables.C("Kill_Gold", 1) * (1 + 0.1 * (stageNo - 1)) * (e.row.IsBoss ? 30 : e.row.IsElite ? 10 : 1);
            Fx.Hit(e.Pos, e.row.size * 1.3f);
            if (!string.IsNullOrEmpty(e.row.split))
                for (int i = 0; i < e.row.splitN; i++) Spawn(e.row.split, e.Pos + new Vector2((i - (e.row.splitN - 1) / 2f) * 0.6f, 0.1f), true);
            GainExp(e.row.exp);
            if ((e.row.IsBoss || e.row.IsElite) && lo.B("heal_elite") > 0) Heal(lo.B("heal_elite") / 100.0);
            if (e.row.IsBoss) { if (e == boss || boss == null || boss.dead) End(true); return; }
            if (e.row.IsElite && Tables.Ci("Boss_Extra_Card", 1) == 1) pendingBoss++;
        }

        public void GainExp(double v)
        {
            if (level >= LevelMax) return;
            exp += v;
            while (exp >= need && level < LevelMax)
            {
                exp -= need; level++; pendingLevel++;
                need = Need(level);
            }
            if (level >= LevelMax) exp = 0;
        }

        public void HurtBase(double d, Vector2 at)
        {
            if (st == St.End) return;
            // 2차 칩: 처음 N번 막음 → 받는 피해 -N(최소 1) → 보호막이 먼저 받음
            if (immune > 0) { immune--; Fx.Ring(at + Vector2.down * 0.2f, 0.8f, Art.Sky); return; }
            d = Math.Max(1, d - lo.reduce);
            if (shield > 0) { double s = Math.Min(shield, d); shield -= s; d -= s; if (d <= 0) { Fx.Ring(at + Vector2.down * 0.2f, 0.6f, Art.Sky); return; } }
            baseHp = Math.Max(0, baseHp - d);
            Fx.Hit(at + Vector2.down * 0.3f, 1.2f);
            Audio.Play(Audio.Sfx.BaseHit);
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
                if (e.dead || (skip != null && skip.Contains(e)) || e.Pos.y > World.SpawnY - 0.3f) continue;
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
                if (!e.dead && e != not && e.Pos.y < World.SpawnY - 0.3f && Vector2.Distance(e.Pos, at) <= r && rnd.Next(++n) == 0) pick = e;
            return pick;
        }

        public Enemy RandomEnemy()
        {
            int n = 0; Enemy pick = null;
            foreach (var e in enemies) if (!e.dead && e.Pos.y < World.SpawnY - 0.3f && rnd.Next(++n) == 0) pick = e;
            return pick;
        }

        /// <summary>적이 가장 많이 모인 높이 (장판 놓을 곳) — 1칸 높이 칸마다 세어 가장 많은 곳</summary>
        public float CrowdY()
        {
            var cnt = new int[20];
            foreach (var e in enemies)
            {
                if (e.dead) continue;
                int i = Mathf.FloorToInt(e.Pos.y - World.ShieldY);
                if (i >= 0 && i < cnt.Length) cnt[i] += e.row.IsBoss || e.row.IsElite ? 3 : 1;
            }
            int best = 1; for (int i = 0; i < cnt.Length; i++) if (cnt[i] > cnt[best]) best = i;
            return World.ShieldY + best + 0.5f;
        }

        // ---------- 카드 ----------
        void OpenPick()
        {
            offerIsBoss = pendingLevel == 0 && pendingBoss > 0;
            offer = run.Roll(Choices, offerIsBoss);
            if (offer.Count == 0) { pendingLevel = 0; pendingBoss = 0; return; }   // 고를 카드가 없으면 넘어감
            st = St.Pick;
            Audio.Play(Audio.Sfx.LevelUp);
            Changed?.Invoke();
        }

        /// <summary>카드 장수 (칩 "첫 카드 N장 더" = 첫 레벨업에만)</summary>
        int Choices => Tables.Ci("Card_Choices", 3) + (picks == 0 ? run.firstExtra : 0);

        public void Choose(int i)
        {
            if (st != St.Pick || i < 0 || i >= offer.Count) return;
            var c = offer[i];
            picks++;
            Audio.Play(c.kind == "break" ? Audio.Sfx.Break : c.kind == "new" ? Audio.Sfx.Place : Audio.Sfx.CardPick);
            if (c.kind == "break" && lo.B("heal_break") > 0) Heal(lo.B("heal_break") / 100.0);
            int before = run.turrets.Count;
            var t = run.Pick(c);
            if (run.turrets.Count > before) { AddUnit(t); World.I.ShowSlots(run.turrets.Count - 1); }
            else if (t?.unit != null && c.kind == "break") t.unit.RefreshTier();
            if (offerIsBoss) pendingBoss--; else pendingLevel--;
            st = St.Play;
            Changed?.Invoke();
        }

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
            Audio.Duck(0.35f);
            Audio.Play(win ? Audio.Sfx.Victory : Audio.Sfx.Defeat);
            Time.timeScale = 1;
            var s = SaveData.Cur;
            FirstClear = win && stageNo > s.best;
            if (eliteMode) FirstClear = win && stageNo > s.eliteBest;
            double mul = win ? (FirstClear ? Tables.C("First_Clear_Mul", 2) : 1) : Tables.C("Lose_Mul", 0.35) * Mathf.Clamp01(time / RunTime);
            if (eliteMode) mul *= Tables.C("Elite_Mode_Reward", 2);
            RewardGold = Math.Floor((Tables.C("Win_Gold_Base") + Tables.C("Win_Gold_Per_Stage") * (stageNo - 1) + goldGot) * mul);
            RewardShards = Math.Floor((Tables.C("Win_Shards") + Tables.C("Win_Shards_Per_Stage") * (stageNo - 1)) * mul);
            s.gold += RewardGold;
            GiveShards((int)RewardShards);
            if (win && eliteMode) { s.wins++; if (stageNo > s.eliteBest) s.eliteBest = stageNo; }
            else if (win) { s.wins++; if (stageNo > s.best) s.best = stageNo; s.stage = Mathf.Min(Tables.Stages.Count, Mathf.Max(s.stage, stageNo + 1)); }
            s.runs++; s.kills += kills;
            // 2차: 장비 · 칩 떨어짐 (이겼을 때)
            if (win)
            {
                int n = Tables.Ci("Drop_Win_N", 2) + (eliteMode ? Tables.Ci("Drop_Elite_N", 1) : 0) + (FirstClear ? Tables.Ci("Drop_First_N", 1) : 0);
                for (int i = 0; i < n; i++)
                {
                    int g = Gear.DropGrade(stageNo, eliteMode, rnd);
                    if (rnd.NextDouble() < Tables.C("Drop_Gear_Chance", 0.4)) { var it = Gear.NewGear(g, null, rnd); drops.Add($"{it.Row.name} ({it.G.name})"); dropGear++; }
                    else { var c = Gear.NewChip(g, null, rnd); drops.Add($"{c.Row.name} ({Tables.Grade(c.q).name})"); dropChip++; }
                }
            }
            Meta.MissionAdd("play");
            if (win) Meta.MissionAdd("win");
            if (eliteMode) Meta.MissionAdd("elite");
            Meta.MissionAdd("kill", (int)Math.Min(int.MaxValue, kills));
            s.Save();
            Changed?.Invoke();
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
            if (st == St.End || st == St.Pick) return;
            st = on ? St.Pause : St.Play;
            Changed?.Invoke();
        }

        public void Close()
        {
            Time.timeScale = 1;
            World.I.ShowSlots(0, false);
            if (I == this) I = null;
            Destroy(gameObject);
        }
    }
}
