using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 밸런스 3차 (11-17): 터렛 · 카드 세기 측정 — args.txt 에 mode=turret (bash tools/bal_turret.sh &lt;이름&gt;).
    /// 시험장 규칙(기지 안 닳음 · 레벨업 없음)으로 터렛 하나만 놓고 초당 피해(%공격력)를 잼. 약점 · 저항 · 거리 배율은 1 로 두고 잼.
    ///   장면: dummy = 허수아비 15(3줄 x 5, 제자리) · boss = 보스 하나 · swarm = 스테이지 5 적이 계속 내려와 쌓임 (못 죽임)
    ///   구성: base(카드 없음) · A · B(그 줄 돌파 + 연계 + 강화 최대) · all(3단계) · card:&lt;id&gt;(강화 카드 하나만 최대로)
    /// 결과 = Logs/Bal/turret_&lt;label&gt;.json → tools/bal_turret.py
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator TurretMeasure(BalArgs a)
        {
            SaveData.ResetAll(); SaveData.Load(); SaveData.NowOffset = 0;
            foreach (var (k, v) in a.set) Tables.SetC(k, v);
            foreach (var k in new[] { "Weak_Mul", "Resist_Mul", "Pos_Near", "Pos_Far" }) Tables.SetC(k, 1);
            Tables.SetC("Mon_HP_Base", 1e9);      // 무리 장면: 못 죽이게 (공급에 안 막히게)
            var rows = new List<string>();
            string[] scenes = { "dummy", "boss", "swarm" };
            foreach (var tr in Tables.Turrets)
            {
                var cfgs = new List<string> { "base", "A", "B", "all" };
                foreach (var cd in Tables.Cards) if (cd.turret == tr.id && cd.kind == "normal") cfgs.Add("card:" + cd.id);
                foreach (var cfg in cfgs)
                {
                    var res = new double[scenes.Length];
                    for (int si = 0; si < scenes.Length; si++)
                    {
                        Random.InitState(7);
                        Game.StartStage(5, 3); yield return null;
                        Popup.CloseAll(); Tutorial.CloseCur();
                        var b = B; b.lab = new Lab { mode = scenes[si], stage = 5, guardianFire = tr.id == "guardian" };
                        b.stage.weak = "";
                        TurretInst t;
                        string preset = cfg.StartsWith("card:") ? "base" : cfg;
                        if (tr.id == "guardian") { t = b.run.turrets[0]; Lab.ApplyPreset(t, preset); }
                        else t = b.LabPlace(scenes[si] == "boss" ? 2 : 1, tr.id, preset);
                        if (cfg.StartsWith("card:")) { var c = Tables.CardById[cfg.Substring(5)]; t.picks[c.id] = c.max; t.Recalc(); }
                        t.unit?.RefreshTier();
                        float warm = scenes[si] == "swarm" ? 12f : 3f, span = 30f;
                        for (int i = 0; i < (int)(warm * 60); i++) b.Tick(Battle.StepSec);
                        double d0 = t.dmgDone;
                        for (int i = 0; i < (int)(span * 60); i++) b.Tick(Battle.StepSec);
                        res[si] = (t.dmgDone - d0) / b.atk / span * 100.0;
                        Fx.I?.Clear();
                    }
                    rows.Add($"{{\"turret\":\"{tr.id}\",\"cfg\":\"{cfg}\",\"dummy\":{res[0].ToString("0.#", CultureInfo.InvariantCulture)},\"boss\":{res[1].ToString("0.#", CultureInfo.InvariantCulture)},\"swarm\":{res[2].ToString("0.#", CultureInfo.InvariantCulture)}}}");
                    if (!cfg.StartsWith("card:")) Log($"{tr.name} [{cfg}] 허수아비 {res[0]:0} · 보스 {res[1]:0} · 무리 {res[2]:0} (%공격력/초)");
                }
            }
            Directory.CreateDirectory("Logs/Bal");
            File.WriteAllText($"Logs/Bal/turret_{a.label}.json", "[\n" + string.Join(",\n", rows) + "\n]");
            Log($"터렛 측정 끝: {rows.Count}줄 → Logs/Bal/turret_{a.label}.json");
            Tables.Load(true);
        }

        // ------------------------------------------------------------------ 주력 터렛 측정 (mode=feature)
        /// <summary>
        /// 터렛 하나를 주력으로 키운 판이 "얼마나 센 적까지" 깨는지: 같은 세이브(기지 Lv 60 · 터렛 Lv 10 · 장비 없음)로
        /// 판을 시작하자마자 그 터렛을 놓고, 그 터렛 카드(돌파 줄 A 먼저 / B 먼저) → 대장 카피 카드 순으로만 고름 (다른 터렛은 안 놓음).
        /// 적 체력 배율 k 를 이분법으로 바꿔 가며 승률 50% 가 되는 k50 을 찾음 (스테이지 11 · 15 · 18 x 씨앗 2 = 한 번에 6판, 약점 · 저항 배율 1).
        /// 결과 = Logs/Bal/feature_&lt;label&gt;.json → tools/bal_turret.py
        /// </summary>
        static int FeaturePick(Battle b, string feat, string line)
        {
            int bi = 0; double best = double.MinValue;
            for (int i = 0; i < b.offer.Count; i++)
            {
                var c = b.offer[i]; double s;
                bool mine = c.turret == feat, guard = c.turret == "guardian";
                if (c.kind == "new") s = 5;
                else if (mine || (feat == "guardian" && guard))
                    s = c.kind == "break" ? (c.line == line ? 100 : 92) : c.kind == "chain" ? 80 : c.kind == "combo" ? 70 : 60;
                else if (guard) s = c.kind == "break" ? 50 : c.kind == "chain" ? 45 : c.kind == "combo" ? 42 : 40;
                else s = 20;
                foreach (var (k, _) in c.stats) if (k is "dmg" or "shots" or "count" or "cd") s += 2;
                if (s > best) { best = s; bi = i; }
            }
            return bi;
        }

        IEnumerator FeatureFight(int stage, int seed, string feat, string line, FightRec r)
        {
            Game.StartStage(stage, seed); yield return null;
            Popup.CloseAll(); Tutorial.CloseCur();
            var b = B;
            if (feat != "guardian") Force("new_" + feat);
            while (b != null && b.st != Battle.St.End)
            {
                for (int i = 0; i < 900 && b.st != Battle.St.End; i++)
                {
                    if (b.st == Battle.St.Pick) { b.Choose(FeaturePick(b, feat, line)); continue; }
                    if (b.st == Battle.St.Pause) b.Pause(false);
                    b.Tick(Battle.StepSec);
                    if (b.time > 420f && b.st == Battle.St.Play) b.HurtBase(b.baseHp + 1, Vector2.zero);
                }
                yield return null;
            }
            r.win = b.won; r.hp = System.Math.Max(0, b.baseHp); r.hpMax = b.baseMax; r.time = b.time; r.level = b.level;
            r.tierSum = 0; double mine = 0, all = 0;
            foreach (var t in b.run.turrets) { if (t.row.id == feat) { r.tierSum = t.tier; mine = t.dmgDone; } all += t.dmgDone; }
            r.gold = all > 0 ? mine / all : 0;     // (gold 칸을 피해 비중으로 씀)
            Popup.CloseAll();
        }

        IEnumerator FeatureMeasure(BalArgs a)
        {
            SaveData.ResetAll(); SaveData.Load(); SaveData.NowOffset = 0;
            foreach (var (k, v) in a.set) Tables.SetC(k, v);
            Tables.SetC("Weak_Mul", 1); Tables.SetC("Resist_Mul", 1);
            foreach (var st in Tables.Stages) { st.weak = ""; st.hpMul = 1; }
            S.best = 30; S.baseLv = 60; for (int i = 0; i < S.tLv.Count; i++) S.tLv[i] = 10;
            S.Save();
            string snap = JsonUtility.ToJson(S);
            double hp0 = Tables.C("Mon_HP_Base");
            int[] stages = { 11, 15, 18 };
            var rows = new List<string>();
            var only = a.only.Count > 0 ? a.only : null;
            foreach (var tr in Tables.Turrets)
            {
                if (only != null && !only.Contains(tr.id)) continue;
                foreach (var line in new[] { "A" })
                {
                    double logk = 1.5, step = 1.2; int fights = 0; string trace = "";
                    const int Iters = 7;
                    for (int it = 0; it < Iters; it++)
                    {
                        Tables.SetC("Mon_HP_Base", hp0 * System.Math.Pow(2, logk));
                        int wins = 0, n = 0; double tier = 0, share = 0;
                        foreach (int st in stages)
                            for (int sd = 0; sd < 2; sd++)
                            {
                                var r = new FightRec();
                                yield return FeatureFight(st, 9000 + st * 10 + sd, tr.id, line, r);
                                if (r.win) wins++; n++; fights++; tier += r.tierSum; share += r.gold;
                                PlayerPrefs.SetString(SaveData.Key, snap); SaveData.Load();
                            }
                        trace += $"{System.Math.Pow(2, logk):0.00}:{wins}/{n} ";
                        logk += (wins * 2 > n ? 1 : wins * 2 == n ? 0.25 : -1) * step;
                        step *= 0.62;
                        if (it == Iters - 1) rows.Add($"{{\"turret\":\"{tr.id}\",\"line\":\"{line}\",\"k50\":{System.Math.Pow(2, logk).ToString("0.###", CultureInfo.InvariantCulture)},\"tier\":{(tier / n).ToString("0.#", CultureInfo.InvariantCulture)},\"share\":{(share / n).ToString("0.###", CultureInfo.InvariantCulture)},\"trace\":\"{trace.Trim()}\"}}");
                    }
                    Log($"주력 {tr.name} [{line} 먼저] k50 = {System.Math.Pow(2, logk):0.00}  ({trace.Trim()})");
                }
            }
            Tables.SetC("Mon_HP_Base", hp0);
            Directory.CreateDirectory("Logs/Bal");
            File.WriteAllText($"Logs/Bal/feature_{a.label}.json", "[\n" + string.Join(",\n", rows) + "\n]");
            Tables.Load(true);
        }
    }
}
