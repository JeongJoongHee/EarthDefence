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
            public string label = "now";
            public int days = 14, battles = 12, maxStage = 100, probeN = 4;
            public float patrolH = 16;
            public List<int> probe = new() { 3, 5, 10, 15, 20, 30, 40, 50 };
            public readonly List<(string key, double val)> set = new();
            public bool farm = true;
            public int tour = 0, tourN = 8, policy = 3, seed = 1000;
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
                    case "days": a.days = int.Parse(v); break;
                    case "battles": a.battles = int.Parse(v); break;
                    case "maxStage": a.maxStage = int.Parse(v); break;
                    case "probeN": a.probeN = int.Parse(v); break;
                    case "patrolH": a.patrolH = float.Parse(v, CultureInfo.InvariantCulture); break;
                    case "farm": a.farm = v == "1"; break;
                    case "tour": a.tour = int.Parse(v); break;
                    case "seed": a.seed = int.Parse(v); break;
                    case "tourN": a.tourN = int.Parse(v); break;
                    case "policy": a.policy = int.Parse(v); break;
                    case "set": foreach (var kv2 in v.Split(';')) { var q = kv2.Split(':'); if (q.Length == 2) a.set.Add((q[0].Trim(), double.Parse(q[1], CultureInfo.InvariantCulture))); } break;
                    case "probe": a.probe = new List<int>(); foreach (var s in v.Split(',')) if (s.Trim() != "") a.probe.Add(int.Parse(s)); break;
                }
            }
            return a;
        }

        class FightRec { public bool win; public double hp, hpMax; public float time; public int level, turrets, tierSum; public long kills; public double gold; }

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

        IEnumerator Fight(int stage, int seed, int policy, FightRec r)
        {
            Game.StartStage(stage, seed); yield return null;
            Popup.CloseAll(); Tutorial.CloseCur();
            var b = B; var prnd = new System.Random(seed * 7 + 3);
            while (b != null && b.st != Battle.St.End)
            {
                for (int i = 0; i < 600 && b.st != Battle.St.End; i++)
                {
                    if (b.st == Battle.St.Pick) { b.Choose(PolicyPick(b, policy, prnd)); continue; }
                    if (b.st == Battle.St.Pause) b.Pause(false);
                    b.Tick(Battle.StepSec);
                    if (b.time > 480f && b.st == Battle.St.Play) b.HurtBase(b.baseHp + 1, Vector2.zero);   // 보스를 못 잡고 오래 버티면 진 것으로
                }
                yield return null;
            }
            r.win = b.won; r.hp = System.Math.Max(0, b.baseHp); r.hpMax = b.baseMax; r.time = b.time; r.level = b.level; r.kills = b.kills; r.gold = b.RewardGold;
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
            Log($"밸런스 측정 {a.label}: {a.days}일 · 하루 {a.battles}판 · 순찰 {a.patrolH}시간 · 공략 비교 {string.Join(",", a.probe)} x{a.probeN}");
            // 새 세이브
            foreach (var (k, v) in a.set) { Tables.SetC(k, v); Log($"Config {k} = {v}"); }
            SaveData.ResetAll(); SaveData.Load(); SaveData.NowOffset = 0;
            bool lostLast = false; GoodPolicy = a.policy;
            var tours = new List<string>();
            var rows = new List<string>(); var probes = new List<string>(); var days = new List<string>();
            var probed = new HashSet<int>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int seed = a.seed, fights = 0; float realStart = Time.realtimeSinceStartup;
            for (int day = 1; day <= a.days; day++)
            {
                // 하루 시작: 순찰 · 무료 상자 · 출석
                if (day > 1)
                {
                    SaveData.NowOffset += 86400;
                    S.patrolFrom = SaveData.Clock - (long)(a.patrolH * 3600);
                    Meta.ClaimPatrol();
                }
                if (Meta.AttendReady) Meta.AttendClaim();
                var free = Tables.ShopItems.Find(i => i.kind == "free_box"); if (free != null && Store.Left(free) > 0) Store.Buy(free);
                Manage();
                for (int k = 0; k < a.battles; k++)
                {
                    int st = Mathf.Min(Tables.Stages.Count, a.maxStage, S.best + 1);
                    // 진 다음 판은 깬 스테이지에서 골드 벌기 (실제 플레이어처럼) → 그다음 다시 도전
                    bool farm = a.farm && lostLast && S.best >= 1;
                    if (farm) st = S.best;
                    if (!farm && a.tour == st && tours.Count == 0)
                    {   // 고르기 규칙 겨루기: 같은 세이브 · 같은 씨앗으로 규칙마다 tourN 번
                        string snap = JsonUtility.ToJson(S); long off = SaveData.NowOffset;
                        for (int pol = 0; pol < Policies.Length; pol++)
                        {
                            int w = 0; double hp = 0;
                            for (int n = 0; n < a.tourN; n++)
                            {
                                var r = new FightRec();
                                yield return Fight(st, 70000 + n, pol, r);
                                if (r.win) { w++; hp += r.hp / r.hpMax; }
                                PlayerPrefs.SetString(SaveData.Key, snap); SaveData.Load(); SaveData.NowOffset = off;
                            }
                            tours.Add($"{{\"policy\":{pol},\"win\":{w},\"n\":{a.tourN},\"hp\":{(w > 0 ? hp / w : 0):0.###}}}");
                            Log($"규칙 겨루기 스테이지 {st} 규칙 {pol}: {w}/{a.tourN} 이김 · 남은 체력 {(w > 0 ? hp / w : 0):0%}");
                        }
                    }
                    if (!farm && a.probe.Contains(st) && !probed.Contains(st))
                    {   // ② 공략 차이: 지금 세이브로 잘 고르기 · 아무거나 probeN 번씩
                        probed.Add(st);
                        string snap = JsonUtility.ToJson(S); long off = SaveData.NowOffset;
                        int gw = 0, rw = 0; double ghp = 0, rhp = 0; var gl = new List<string>(); var rl = new List<string>();
                        for (int pr = 0; pr < a.probeN * 2; pr++)
                        {
                            bool good = pr % 2 == 0; var r = new FightRec();
                            yield return Fight(st, 50000 + st * 100 + pr / 2, good ? GoodPolicy : 0, r);
                            double hpp = r.hp / r.hpMax;
                            if (good) { if (r.win) { gw++; ghp += hpp; } gl.Add($"{(r.win ? 1 : 0)}:{hpp:0.00}:{r.time:0}"); }
                            else { if (r.win) { rw++; rhp += hpp; } rl.Add($"{(r.win ? 1 : 0)}:{hpp:0.00}:{r.time:0}"); }
                            PlayerPrefs.SetString(SaveData.Key, snap); SaveData.Load(); SaveData.NowOffset = off;
                        }
                        probes.Add($"{{\"stage\":{st},\"day\":{day},\"goodWin\":{gw},\"randWin\":{rw},\"n\":{a.probeN},\"goodHp\":{(gw > 0 ? ghp / gw : 0):0.###},\"randHp\":{(rw > 0 ? rhp / rw : 0):0.###},\"good\":\"{string.Join(" ", gl)}\",\"rand\":\"{string.Join(" ", rl)}\",\"baseLv\":{S.baseLv},\"tLv\":{TurretLvAvg():0.0},\"gear\":\"{GearDesc()}\"}}");
                        Log($"공략 비교 스테이지 {st}: 잘 고름 {gw}/{a.probeN} (남은 체력 {(gw > 0 ? ghp / gw : 0):0%}) · 아무거나 {rw}/{a.probeN} (남은 {(rw > 0 ? rhp / rw : 0):0%})");
                    }
                    var f = new FightRec();
                    double atk = S.Atk; int blv = S.baseLv; double tl = TurretLvAvg(); string gd = GearDesc();
                    yield return Fight(st, seed++, GoodPolicy, f); fights++;
                    lostLast = !farm && !f.win;
                    rows.Add($"{{\"day\":{day},\"stage\":{st},\"farm\":{(farm ? 1 : 0)},\"win\":{(f.win ? 1 : 0)},\"hp\":{f.hp / f.hpMax:0.###},\"time\":{f.time:0},\"lv\":{f.level},\"tur\":{f.turrets},\"tier\":{f.tierSum},\"baseLv\":{blv},\"atk\":{atk:0},\"tLv\":{tl:0.0},\"gear\":\"{gd}\",\"gold\":{S.gold:0},\"dia\":{S.dia:0}}}");
                    Manage();
                    if (S.best >= Mathf.Min(Tables.Stages.Count, a.maxStage)) break;
                }
                days.Add($"{{\"day\":{day},\"best\":{S.best},\"baseLv\":{S.baseLv},\"tLv\":{TurretLvAvg():0.0},\"gear\":\"{GearDesc()}\",\"gold\":{S.gold:0},\"dia\":{S.dia:0},\"parts\":{S.parts:0}}}");
                Log($"{day}일째: 최고 {S.best} · 기지 Lv {S.baseLv} · 터렛 평균 Lv {TurretLvAvg():0.0} · 장비 {GearDesc()} · 판 {fights} · {Time.realtimeSinceStartup - realStart:0}초");
                if (S.best >= Mathf.Min(Tables.Stages.Count, a.maxStage)) break;
            }
            var setJ = new List<string>(); foreach (var (k, v) in a.set) setJ.Add($"\"{k}\":{v.ToString(CultureInfo.InvariantCulture)}");
            var json = "{\"label\":\"" + a.label + "\",\"set\":{" + string.Join(",", setJ) + "},\"args\":{\"days\":" + a.days + ",\"battles\":" + a.battles + ",\"patrolH\":" + a.patrolH.ToString(CultureInfo.InvariantCulture) + "},\n\"fights\":[\n" + string.Join(",\n", rows) + "],\n\"probes\":[\n" + string.Join(",\n", probes) + "],\n\"tours\":[" + string.Join(",", tours) + "],\n\"days\":[\n" + string.Join(",\n", days) + "]}";
            File.WriteAllText($"Logs/Bal/bal_{a.label}.json", json);
            Log($"밸런스 측정 끝: 판 {fights} · {sw.Elapsed.TotalSeconds:0}초 → Logs/Bal/bal_{a.label}.json");
            SaveData.NowOffset = 0;
            running = false;
        }
    }
}
