using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 밸런스 측정 (11-17 밸런스 1차): BATCH_TIMEOUT=3000 bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunBal --play
    /// 설정 = Earth_Defence/Logs/Bal/args.txt (한 줄에 "이름=값": label · days · battles · patrolH · probe · probeN · maxStage)
    /// 결과 = Earth_Defence/Logs/Bal/bal_&lt;label&gt;.json → tools/balance_report.py 가 보고서 HTML.
    ///
    /// ① 진행 흉내: 새 세이브로 하루 battles 판씩 "다음 스테이지"에 도전 (지면 같은 스테이지 다시). 판 사이엔
    ///    장비 좋은 것 끼우기 · 합성 · 회로 끼우기 → 골드가 가장 싸게 드는 강화부터(기지 · 터렛 · 장비) 돈이 떨어질 때까지.
    ///    하루가 지나면 순찰(patrolH 시간) · 무료 상자 · 출석. 다이아는 쓰지 않음(무과금 · 상자 안 삼).
    ///    판의 카드는 "잘 고르는 사람"(GoodPick)이 고름 — 실제 전투 코드 그대로 (FF 와 같은 1/60초 칸).
    /// ② 공략 차이: probe 스테이지를 처음 도전하는 순간의 세이브로 "잘 고르는 사람" · "아무거나 고르는 사람"을 probeN 번씩
    ///    (씨앗만 다르게) 붙여 이긴 수 · 남은 기지 체력을 비교. 끝나면 세이브를 되돌리고 ①을 이어 감.
    /// </summary>
    public partial class PlayTestRunner
    {
        class BalArgs
        {
            public string label = "now", mode = "";
            public int days = 14, battles = 12, maxStage = 100, probeN = 4;
            public float patrolH = 16;
            public List<int> probe = new() { 3, 5, 10, 15, 20, 30, 40, 50 };
            public readonly List<(string key, double val)> set = new();
            public bool farm = true;
            public int tour = 0, tourN = 8, policy = 3, seed = 1000, maxFarm = 400, retryDaily = 1;
            public bool ads = false, spend = true, stopAtTop = true, modes = true, elite = true;
            public int eliteAhead = 1;
            public double retryGain = 0.1, diaDay = 0;
            public readonly List<(string id, int every)> buy = new();
            public readonly List<float> stageMul = new();
            public readonly List<string> only = new();
            public readonly List<(string id, float mul)> rate = new(), cdMul = new();
        }

        static BalArgs ReadArgs()
        {
            var a = new BalArgs();
            const string p = "Logs/Bal/args.txt";
            if (!File.Exists(p)) return a;
            foreach (var line in File.ReadAllLines(p))
            {
                var kv = line.Split('='); if (kv.Length != 2) continue;
                string k = kv[0].Trim(), v = kv[1].Trim();
                switch (k)
                {
                    case "label": a.label = v; break;
                    case "mode": a.mode = v; break;
                    case "cd": foreach (var x in v.Split(';')) { var q = x.Split(':'); if (q.Length == 2) a.cdMul.Add((q[0].Trim(), float.Parse(q[1], CultureInfo.InvariantCulture))); } break;
                    case "rate": foreach (var x in v.Split(';')) { var q = x.Split(':'); if (q.Length == 2) a.rate.Add((q[0].Trim(), float.Parse(q[1], CultureInfo.InvariantCulture))); } break;
                    case "only": foreach (var x in v.Split(',')) if (x.Trim() != "") a.only.Add(x.Trim()); break;
                    case "days": a.days = int.Parse(v); break;
                    case "battles": a.battles = int.Parse(v); break;
                    case "maxStage": a.maxStage = int.Parse(v); break;
                    case "probeN": a.probeN = int.Parse(v); break;
                    case "patrolH": a.patrolH = float.Parse(v, CultureInfo.InvariantCulture); break;
                    case "farm": a.farm = v == "1"; break;
                    case "tour": a.tour = int.Parse(v); break;
                    case "seed": a.seed = int.Parse(v); break;
                    case "ads": a.ads = v == "1"; break;
                    case "modes": a.modes = v == "1"; break;
                    case "elite": a.elite = v == "1"; break;
                    case "eliteAhead": a.eliteAhead = int.Parse(v); break;
                    case "stopAtTop": a.stopAtTop = v == "1"; break;
                    case "stageMul": foreach (var x in v.Split(',')) if (x.Trim() != "") a.stageMul.Add(float.Parse(x, CultureInfo.InvariantCulture)); break;
                    case "stageMulFile": foreach (var x in File.ReadAllText(v).Split(',')) if (x.Trim() != "") a.stageMul.Add(float.Parse(x, CultureInfo.InvariantCulture)); break;
                    case "spend": a.spend = v == "1"; break;
                    case "retryGain": a.retryGain = double.Parse(v, CultureInfo.InvariantCulture); break;
                    case "maxFarm": a.maxFarm = int.Parse(v); break;
                    case "diaDay": a.diaDay = double.Parse(v, CultureInfo.InvariantCulture); break;
                    case "buy": foreach (var kv3 in v.Split(';')) { var q = kv3.Split(':'); if (q.Length == 2) a.buy.Add((q[0].Trim(), int.Parse(q[1]))); } break;
                    case "tourN": a.tourN = int.Parse(v); break;
                    case "policy": a.policy = int.Parse(v); break;
                    case "set": foreach (var kv2 in v.Split(';')) { var q = kv2.Split(':'); if (q.Length == 2) a.set.Add((q[0].Trim(), double.Parse(q[1], CultureInfo.InvariantCulture))); } break;
                    case "probe": a.probe = new List<int>(); foreach (var s in v.Split(',')) if (s.Trim() != "") a.probe.Add(int.Parse(s)); break;
                }
            }
            return a;
        }

        class FightRec { public bool win; public double hp, hpMax, goldGot, reward; public float time; public int level, turrets, tierSum; public long kills; public double gold; }
        static double diaBought, diaSpent; static int eliteFarms;

        /// <summary>힘(다시 도전할 때를 정하는 대략의 값): (기지 공격력 + 장비) x (1 + 열린 터렛 평균 레벨 피해)</summary>
        static double Power()
        {
            var lo = Loadout.Build(S); double tl = 0; int n = 0;
            foreach (var t in Tables.Turrets) if (S.Unlocked(t)) { tl += Tables.Lv(S.TurretLv(t.id)).dmgBonus; n++; }
            return lo.AtkOf(S) * (1 + (n > 0 ? tl / n : 0));
        }

        /// <summary>다이아 쓰기: 조각 상자(하루 10) → 큰 골드 주머니 → 장비 · 회로 10+1 번갈아 (무과금도 같은 규칙)</summary>
        static bool gearTurn;
        static void Spend(BalArgs a)
        {
            if (!a.spend) return;
            SaveData.BeginBatch();
            try
            {
                for (int guard = 0; guard < 200; guard++)
                {
                    ShopItemRow pick = null;
                    var shard = Tables.ShopItems.Find(i => i.kind == "shard_box");
                    ShopItemRow gold2 = null;
                    foreach (var gi in Tables.ShopItems) if (gi.kind == "gold" && gi.costDia > 0 && Store.CanBuy(gi) && (gold2 == null || gi.value / gi.costDia > gold2.value / gold2.costDia)) gold2 = gi;
                    var box = Tables.ShopItems.Find(i => i.id == (gearTurn ? "i_gear10" : "i_chip10"));
                    bool turretsLeft = Tables.Turrets.Exists(t => S.Unlocked(t) && !Meta.TurretMax(t.id));
                    if (shard != null && turretsLeft && Store.CanBuy(shard)) pick = shard;
                    else if (gold2 != null && Store.CanBuy(gold2) && S.dia >= gold2.costDia + (box != null ? box.costDia * 0.5 : 0)) pick = gold2;
                    else if (box != null && Store.CanBuy(box)) { pick = box; gearTurn = !gearTurn; }
                    else if (gold2 != null && Store.CanBuy(gold2)) pick = gold2;
                    if (pick == null) break;
                    double d0 = S.dia; Store.Buy(pick); diaSpent += d0 - S.dia;
                }
            }
            finally { SaveData.EndBatch(); }
        }

        /// <summary>고르기 규칙 (0 = 아무거나 · 1~5 = 점수 규칙). 겨루기(tour)에서 가장 잘 이긴 규칙을 GoodPolicy 로 씀</summary>
        //  1 = 터렛 2기까지 + 한 터렛 몰아주기 · 2 = 터렛 4칸 먼저 채우기 · 3 = 2 + 약점 속성 크게 · 4 = 돌파 · 콤보 먼저, 터렛은 그다음 · 5 = 터렛 3기 + 약점 · 6 = 5 + 돌파 · 콤보를 새 터렛보다 먼저
        public static int GoodPolicy = 3;
        static readonly (int cap, double newS, double wNew, double wUp, double focus, double brk, double combo, double chain)[] Policies =
        {
            (0, 0, 0, 0, 0, 0, 0, 0),
            (2, 80, 14, 8, 1, 100, 86, 72),
            (4, 90, 14, 8, 1, 100, 86, 72),
            (4, 90, 30, 20, 1, 100, 86, 72),
            (4, 70, 14, 8, 1, 100, 86, 72),
            (3, 90, 30, 20, 1.5, 100, 86, 72),
            (3, 75, 30, 20, 1.5, 100, 86, 72),
        };
        static string StageWeak(Battle b)
        {
            if (!string.IsNullOrEmpty(b.stage.weak)) return b.stage.weak;     // 밸런스 2차: 스테이지 약점
            var weakCnt = new Dictionary<string, int>();
            foreach (var id in b.stage.enemies) if (Tables.Enemies.TryGetValue(id, out var er) && !string.IsNullOrEmpty(er.weak)) weakCnt[er.weak] = (weakCnt.TryGetValue(er.weak, out var n) ? n : 0) + 1;
            if (Tables.Enemies.TryGetValue(b.stage.boss, out var bo) && !string.IsNullOrEmpty(bo.weak)) weakCnt[bo.weak] = (weakCnt.TryGetValue(bo.weak, out var n2) ? n2 : 0) + 2;
            string weak = null; int wb = -1; foreach (var kv in weakCnt) if (kv.Value > wb) { wb = kv.Value; weak = kv.Key; }
            return weak;
        }
        static int PolicyPick(Battle b, int policy, System.Random prnd)
        {
            if (policy <= 0 || policy >= Policies.Length) return prnd.Next(b.offer.Count);
            var P = Policies[policy]; string weak = StageWeak(b);
            int owned = b.run.turrets.Count - 1, bi = 0; double best = double.MinValue;
            for (int i = 0; i < b.offer.Count; i++)
            {
                var c = b.offer[i]; double s;
                switch (c.kind)
                {
                    case "break": s = P.brk; break;
                    case "combo": s = P.combo; break;
                    case "chain": s = P.chain; break;
                    case "new":
                        s = owned < P.cap ? P.newS : 0;
                        if (Tables.TurretById.TryGetValue(c.turret, out var tr) && tr.elem == weak) s += P.wNew;
                        break;
                    default: s = 60; break;
                }
                if (c.kind != "new")
                {
                    var t = b.run.Get(c.turret);
                    if (t != null) { s += (t.tier * 4 + t.stars) * P.focus; if (t.row.elem == weak) s += P.wUp; }
                    foreach (var (k, _) in c.stats) if (k is "dmg" or "shots" or "count" or "cd" or "rate") s += 3;
                }
                s += prnd.NextDouble() * 0.01;     // 같은 점수면 아무거나
                if (s > best) { best = s; bi = i; }
            }
            return bi;
        }

        IEnumerator Fight(int stage, int seed, int policy, FightRec r, bool ad = false, bool elite = false, string mode = "stage")
        {
            Game.StartStage(stage, seed, elite, mode); yield return null;
            Popup.CloseAll(); Tutorial.CloseCur();
            var b = B; var prnd = new System.Random(seed * 7 + 3);
            float cap = mode == "endless" ? 1500f : 480f;
            while (b != null && b.st != Battle.St.End)
            {
                for (int i = 0; i < 600 && b.st != Battle.St.End; i++)
                {
                    if (b.st == Battle.St.Pick) { b.Choose(PolicyPick(b, policy, prnd)); continue; }
                    if (b.st == Battle.St.Pause) b.Pause(false);
                    b.Tick(Battle.StepSec);
                    if (b.time > cap && b.st == Battle.St.Play) { b.baseHp = 0; b.End(false); }   // 보스를 못 잡고 오래 버티면 진 것으로 (무한 모드는 25분까지)
                }
                yield return null;
            }
            r.win = b.won; r.hp = System.Math.Max(0, b.baseHp); r.hpMax = b.baseMax; r.time = b.time; r.level = b.level; r.kills = b.kills; r.gold = b.RewardGold; r.goldGot = b.goldGot;
            r.reward = b.RewardGold / (b.FirstClear ? System.Math.Max(1, Tables.C("First_Clear_Mul", 2)) : 1);
            if (ad) b.AdDouble();
            r.turrets = b.run.turrets.Count - 1; r.tierSum = 0; foreach (var t in b.run.turrets) r.tierSum += t.tier;
            Popup.CloseAll();
        }

        /// <summary>판 사이 정리: 좋은 장비 끼우기 · 합성 · 회로 끼우기 → 가장 싼 강화부터</summary>
        static void Manage()
        {
            SaveData.BeginBatch();
            try
            {
                for (int round = 0; round < 3; round++)
                {
                    foreach (var p in Tables.GearParts)
                    {
                        GearItem cur = Gear.On(p.part), pick = cur;
                        foreach (var g in S.gear) if (!g.on && g.part == p.part && (pick == null || g.grade * 1000 + g.lv > pick.grade * 1000 + pick.lv)) pick = g;
                        if (pick != null && pick != cur) Gear.Equip(pick);
                    }
                    Gear.MergeAllGear(); Gear.MergeAllChipList();
                }
                var chips = S.chips.FindAll(c => c.at < 0 && c.Row != null); chips.Sort((a, b) => b.q.CompareTo(a.q));
                foreach (var c in chips) { var (pp, ss) = Gear.FreeSlotFor(c); if (pp >= 0) Gear.PutChip(c, pp, ss); }
                for (int guard = 0; guard < 3000; guard++)
                {
                    double cheapest = double.MaxValue; System.Action act = null;
                    if (Meta.CanUpgradeBase && S.BaseCost < cheapest) { cheapest = S.BaseCost; act = () => Meta.UpgradeBase(); }
                    foreach (var t in Tables.Turrets)
                        if (Meta.CanUpgradeTurret(t.id) && Meta.Cost(t.id).gold < cheapest) { cheapest = Meta.Cost(t.id).gold; var id = t.id; act = () => Meta.UpgradeTurret(id); }
                    foreach (var g in S.gear)
                        if (g.on && Gear.CanUp(g) && Gear.UpGold(g) < cheapest) { cheapest = Gear.UpGold(g); var gg = g; act = () => Gear.Up(gg); }
                    if (act == null) break;
                    act();
                }
            }
            finally { SaveData.EndBatch(); }
        }

        static double TurretLvAvg() { double s = 0; int n = 0; foreach (var t in Tables.Turrets) if (S.Unlocked(t)) { s += S.TurretLv(t.id); n++; } return n > 0 ? s / n : 0; }
        static string GearDesc() { var sb = new StringBuilder(); foreach (var p in Tables.GearParts) { var g = Gear.On(p.part); sb.Append(g == null ? "-" : $"{g.grade}/{g.lv}").Append(' '); } return sb.ToString().Trim(); }

        IEnumerator BalSteps()
        {
            yield return null;
            Lobby.NoIntro = true; Popup.CloseAll();
            var a = ReadArgs();
            Directory.CreateDirectory("Logs/Bal");
            // 터렛 피해율(Rate · Rate2) 배율 덮어쓰기 — 표를 고치기 전에 재 볼 때
            foreach (var (tid, mul) in a.rate) if (Tables.TurretById.TryGetValue(tid, out var trr)) { trr.rate *= mul; trr.rate2 *= mul; }
            foreach (var (tid, mul) in a.cdMul) if (Tables.TurretById.TryGetValue(tid, out var trc)) trc.cd *= mul;
            if (a.mode == "turret") { yield return TurretMeasure(a); running = false; yield break; }
            if (a.mode == "feature") { yield return FeatureMeasure(a); running = false; yield break; }
            Log($"밸런스 측정 {a.label}: {a.days}일 · 하루 {a.battles}판 · 순찰 {a.patrolH}시간 · 공략 비교 {string.Join(",", a.probe)} x{a.probeN}");
            // 새 세이브
            foreach (var (k, v) in a.set) { Tables.SetC(k, v); Log($"Config {k} = {v}"); }
            for (int i = 0; i < a.stageMul.Count && i < Tables.Stages.Count; i++) Tables.Stages[i].hpMul = a.stageMul[i];   // 스테이지별 적 체력 배율 덮어쓰기 (Stage 표 Hp_Mul)
            if (a.stageMul.Count > 0) Log($"스테이지 배율 {a.stageMul.Count}개 덮어씀 (1: {a.stageMul[0]} · 끝: {a.stageMul[a.stageMul.Count - 1]})");
            SaveData.ResetAll(); SaveData.Load(); SaveData.NowOffset = 0;
            GoodPolicy = a.policy; diaBought = 0; diaSpent = 0; gearTurn = true; eliteFarms = 0;
            var tours = new List<string>();
            var rows = new List<string>(); var probes = new List<string>(); var days = new List<string>();
            var probed = new HashSet<int>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int seed = a.seed, fights = 0; float realStart = Time.realtimeSinceStartup;
            // 빠른 골드 벌기 기록: 열쇠 = 스테이지 (엘리트 난이도는 -스테이지)
            var farmCache = new Dictionary<int, (double gold, long kills, float time, double reward)>();
            var modeCache = new Dictionary<string, (float time, double gold, long kills, bool win, int best, int day)>();
            var modeRows = new List<string>();
            double powerAtLoss = 0, elitePowerAtLoss = 0; int farmsSinceLoss = 0, pushes = 0, quick = 0, elitePushes = 0; bool pushedToday, elitePushedToday;
            var buys = new List<(ProductRow p, int every)>();
            foreach (var (pid, ev) in a.buy) { var pr0 = Tables.Products.Find(x => x.id == pid); if (pr0 != null) buys.Add((pr0, ev)); else Log("상품 없음 " + pid); }
            int off = Tables.Ci("Elite_Mode_Offset", 0);
            for (int day = 1; day <= a.days; day++)
            {
                // 하루 시작: 과금 상품 → 순찰 · 출석 · 무료 상자 (광고를 보는 사람 · 광고 제거를 산 사람은 순찰 2배 · 광고 상자 · 결과 2배)
                if (day > 1) SaveData.NowOffset += 86400;
                foreach (var (pr1, ev) in buys) if ((day - 1) % ev == 0) { int got = Store.Purchase(pr1); if (pr1.kind is "dia" or "monthly") diaBought += got; }
                if (a.diaDay > 0) { S.dia += a.diaDay; diaBought += a.diaDay; }   // 과금 단계를 "하루 다이아 N개"로 잴 때
                bool ads = a.ads || Store.NoAds;
                if (day > 1) { S.patrolFrom = SaveData.Clock - (long)(a.patrolH * 3600); Meta.ClaimPatrol(ads ? Tables.Ci("Patrol_Ad_Mul", 2) : 1); }
                if (Meta.AttendReady) Meta.AttendClaim();
                foreach (var it in Tables.ShopItems)
                    if ((it.kind == "free_box" || (ads && it.kind == "ad_box")) && Store.Left(it) > 0) Store.Buy(it);
                Spend(a); Manage();
                pushedToday = false; elitePushedToday = false;
                int left = a.battles;
                // 도전 모드 (하루 입장 횟수만큼 — 판 수에 넣음): 최고 스테이지가 같으면 지난 기록으로 보상만
                if (a.modes && S.best >= 1)
                    foreach (var mode in new[] { "endless", "survival" })
                        for (int tk = 0; tk < Store.TicketMax(mode) && left > 0; tk++, left--)
                        {
                            if (modeCache.TryGetValue(mode, out var mc) && mc.best == S.best && day - mc.day < 10)
                            {
                                Game.StartStage(S.best, seed++, false, mode); yield return null; Popup.CloseAll();
                                var mb = B; mb.time = mc.time; mb.goldGot = mc.gold; mb.kills = mc.kills;
                                if (mode == "endless") mb.baseHp = 0;
                                mb.End(mc.win); if (ads) mb.AdDouble(); Popup.CloseAll(); quick++;
                            }
                            else
                            {
                                var mr = new FightRec();
                                yield return Fight(S.best, seed++, GoodPolicy, mr, ads, false, mode); fights++;
                                modeCache[mode] = (mr.time, mr.goldGot, mr.kills, mr.win, S.best, day);
                                modeRows.Add($"{{\"day\":{day},\"best\":{S.best},\"mode\":\"{mode}\",\"win\":{(mr.win ? 1 : 0)},\"time\":{mr.time:0},\"hp\":{mr.hp / mr.hpMax:0.##},\"lv\":{mr.level},\"gold\":{mr.gold:0}}}");
                            }
                            Spend(a); Manage();
                        }
                for (int k = 0; k < left; k++)
                {
                    int top = Mathf.Min(Tables.Stages.Count, a.maxStage);
                    int st = Mathf.Min(top, S.best + 1);
                    // 진 뒤에는 힘이 retryGain 만큼 오를 때까지 깬 스테이지에서 골드 벌기 (하루 첫 판 · 너무 오래면 그냥 도전)
                    bool farm = a.farm && powerAtLoss > 0 && S.best >= 1 && Power() < powerAtLoss * (1 + a.retryGain) && (pushedToday || a.retryDaily == 0) && farmsSinceLoss < a.maxFarm;
                    if (S.best >= top) farm = true;
                    double pw = Power();
                    if (farm)
                    {
                        // 엘리트 난이도 도전: 엘리트 n = 보통 n + off 의 적 → 보통을 그만큼 앞서 깼으면 도전 (지면 힘이 오를 때까지 하루 한 번)
                        int en = S.eliteBest + 1;
                        bool eliteOk = a.elite && en <= S.best && en <= top && en + off <= S.best + a.eliteAhead;
                        bool eliteWait = elitePowerAtLoss > 0 && pw < elitePowerAtLoss * (1 + a.retryGain) && elitePushedToday;
                        if (eliteOk && !eliteWait)
                        {
                            var ef = new FightRec();
                            yield return Fight(en, seed++, GoodPolicy, ef, ads, true); fights++; elitePushes++; elitePushedToday = true;
                            if (ef.win) { farmCache[-en] = (ef.goldGot, ef.kills, ef.time, ef.reward); elitePowerAtLoss = 0; } else elitePowerAtLoss = pw;
                            rows.Add($"{{\"day\":{day},\"stage\":{en},\"elite\":1,\"farm\":0,\"win\":{(ef.win ? 1 : 0)},\"hp\":{ef.hp / ef.hpMax:0.###},\"time\":{ef.time:0},\"lv\":{ef.level},\"best\":{S.best},\"pw\":{pw:0}}}");
                            Spend(a); Manage();
                            continue;
                        }
                        // 빠른 골드 벌기: 보통 최고 스테이지 · 엘리트 최고 스테이지 중 보상이 큰 쪽 (지난 승리의 처치 · 시간으로 보상만, 전투 없이)
                        int key = 0; double bestR = -1;
                        if (farmCache.TryGetValue(S.best, out var c1)) { key = S.best; bestR = c1.reward; }
                        if (a.elite && S.eliteBest >= 1 && farmCache.TryGetValue(-S.eliteBest, out var c2) && c2.reward > bestR) { key = -S.eliteBest; bestR = c2.reward; }
                        if (key != 0)
                        {
                            var fc = farmCache[key];
                            Game.StartStage(Mathf.Abs(key), seed++, key < 0); yield return null; Popup.CloseAll();
                            var fb = B; fb.time = fc.time; fb.goldGot = fc.gold; fb.kills = fc.kills; fb.End(true);
                            if (ads) fb.AdDouble();
                            Popup.CloseAll(); quick++; farmsSinceLoss++; fights++;
                            if (key < 0) eliteFarms++;
                            Spend(a); Manage();
                            continue;
                        }
                    }
                    if (!farm && a.tour == st && tours.Count == 0)
                    {   // 고르기 규칙 겨루기: 같은 세이브 · 같은 씨앗으로 규칙마다 tourN 번
                        string snap = JsonUtility.ToJson(S); long off2 = SaveData.NowOffset;
                        for (int pol = 0; pol < Policies.Length; pol++)
                        {
                            int w = 0; double hp = 0;
                            for (int n = 0; n < a.tourN; n++)
                            {
                                var r = new FightRec();
                                yield return Fight(st, 70000 + n, pol, r);
                                if (r.win) { w++; hp += r.hp / r.hpMax; }
                                PlayerPrefs.SetString(SaveData.Key, snap); SaveData.Load(); SaveData.NowOffset = off2;
                            }
                            tours.Add($"{{\"policy\":{pol},\"win\":{w},\"n\":{a.tourN},\"hp\":{(w > 0 ? hp / w : 0):0.###}}}");
                            Log($"규칙 겨루기 스테이지 {st} 규칙 {pol}: {w}/{a.tourN} 이김 · 남은 체력 {(w > 0 ? hp / w : 0):0%}");
                        }
                    }
                    if (!farm && a.probe.Contains(st) && !probed.Contains(st))
                    {   // ② 공략 차이: 지금 세이브로 잘 고르기(약점 노림) · 아무거나 probeN 번씩
                        probed.Add(st);
                        string snap = JsonUtility.ToJson(S); long off2 = SaveData.NowOffset;
                        int gw = 0, rw = 0; double ghp = 0, rhp = 0; var gl = new List<string>(); var rl = new List<string>();
                        for (int pr = 0; pr < a.probeN * 2; pr++)
                        {
                            bool good = pr % 2 == 0; var r = new FightRec();
                            yield return Fight(st, 50000 + st * 100 + pr / 2, good ? GoodPolicy : 0, r);
                            double hpp = r.hp / r.hpMax;
                            if (good) { if (r.win) { gw++; ghp += hpp; } gl.Add($"{(r.win ? 1 : 0)}:{hpp:0.00}:{r.time:0}"); }
                            else { if (r.win) { rw++; rhp += hpp; } rl.Add($"{(r.win ? 1 : 0)}:{hpp:0.00}:{r.time:0}"); }
                            PlayerPrefs.SetString(SaveData.Key, snap); SaveData.Load(); SaveData.NowOffset = off2;
                        }
                        probes.Add($"{{\"stage\":{st},\"day\":{day},\"goodWin\":{gw},\"randWin\":{rw},\"n\":{a.probeN},\"goodHp\":{(gw > 0 ? ghp / gw : 0):0.###},\"randHp\":{(rw > 0 ? rhp / rw : 0):0.###},\"good\":\"{string.Join(" ", gl)}\",\"rand\":\"{string.Join(" ", rl)}\",\"baseLv\":{S.baseLv},\"tLv\":{TurretLvAvg():0.0},\"gear\":\"{GearDesc()}\"}}");
                        Log($"공략 비교 스테이지 {st}: 잘 고름 {gw}/{a.probeN} (남은 체력 {(gw > 0 ? ghp / gw : 0):0%}) · 아무거나 {rw}/{a.probeN} (남은 {(rw > 0 ? rhp / rw : 0):0%})");
                    }
                    var f = new FightRec();
                    double atk = S.Atk; int blv = S.baseLv; double tl = TurretLvAvg(); string gd = GearDesc();
                    yield return Fight(st, seed++, GoodPolicy, f, ads); fights++;
                    if (!farm) { pushes++; pushedToday = true; }
                    if (f.win) { farmCache[st] = (f.goldGot, f.kills, f.time, f.reward); powerAtLoss = 0; farmsSinceLoss = 0; }
                    else if (!farm) { powerAtLoss = pw; farmsSinceLoss = 0; }
                    rows.Add($"{{\"day\":{day},\"stage\":{st},\"farm\":{(farm ? 1 : 0)},\"win\":{(f.win ? 1 : 0)},\"hp\":{f.hp / f.hpMax:0.###},\"time\":{f.time:0},\"lv\":{f.level},\"tur\":{f.turrets},\"tier\":{f.tierSum},\"baseLv\":{blv},\"atk\":{atk:0},\"pw\":{pw:0},\"tLv\":{tl:0.0},\"gear\":\"{gd}\",\"gold\":{S.gold:0},\"dia\":{S.dia:0}}}");
                    Spend(a); Manage();
                    if (S.best >= top && a.stopAtTop) break;
                }
                // 하루 끝: 일일 미션 받기
                for (int mi = 0; mi < Tables.Missions.Count; mi++) if (Meta.MissionReady(mi)) Meta.MissionClaim(mi);
                Spend(a); Manage();
                var lo2 = Loadout.Build(S);
                days.Add($"{{\"day\":{day},\"best\":{S.best},\"eliteBest\":{S.eliteBest},\"baseLv\":{S.baseLv},\"pw\":{Power():0},\"baseAtk\":{S.Atk:0},\"gearAtk\":{lo2.AtkOf(S) - S.Atk:0},\"tLv\":{TurretLvAvg():0.0},\"gear\":\"{GearDesc()}\",\"gold\":{S.gold:0},\"dia\":{S.dia:0},\"diaBought\":{diaBought:0},\"diaSpent\":{diaSpent:0},\"parts\":{S.parts:0},\"pushes\":{pushes},\"elitePushes\":{elitePushes},\"eliteFarms\":{eliteFarms},\"quick\":{quick}}}");
                if (day % 10 == 0 || day <= 3) Log($"{day}일째: 최고 {S.best} (엘리트 {S.eliteBest}) · 기지 Lv {S.baseLv} · 힘 {Power():0} · 터렛 평균 Lv {TurretLvAvg():0.0} · 장비 {GearDesc()} · 도전 {pushes} · 빠른 벌기 {quick} · {Time.realtimeSinceStartup - realStart:0}초");
                if (S.best >= Mathf.Min(Tables.Stages.Count, a.maxStage) && a.stopAtTop) break;
            }
            var setJ = new List<string>(); foreach (var (k, v) in a.set) setJ.Add($"\"{k}\":{v.ToString(CultureInfo.InvariantCulture)}");
            var json = "{\"label\":\"" + a.label + "\",\"set\":{" + string.Join(",", setJ) + "},\"args\":{\"days\":" + a.days + ",\"battles\":" + a.battles + ",\"patrolH\":" + a.patrolH.ToString(CultureInfo.InvariantCulture) + "},\n\"fights\":[\n" + string.Join(",\n", rows) + "],\n\"probes\":[\n" + string.Join(",\n", probes) + "],\n\"tours\":[" + string.Join(",", tours) + "],\n\"modes\":[\n" + string.Join(",\n", modeRows) + "],\n\"days\":[\n" + string.Join(",\n", days) + "]}";
            File.WriteAllText($"Logs/Bal/bal_{a.label}.json", json);
            Log($"밸런스 측정 끝: 판 {fights} · {sw.Elapsed.TotalSeconds:0}초 → Logs/Bal/bal_{a.label}.json");
            SaveData.NowOffset = 0;
            running = false;
        }
    }
}
