using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-22 「타워_전체_정리.html」 반영 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR12 --play
    /// ① 표(해금 · 새 타워 6종 카드 · 스킨 · 회로 · 지운 카드) ② 의견 6개 ③ 새 타워 6종이 실제로 쏨 · 돌파 · 필살기
    /// ④ 개편(한 발 더 · 감속선 · 집중 빔 · 코일) — 화면 = Logs/PlayTest/r12_*.png
    /// </summary>
    public partial class PlayTestRunner
    {
        static readonly string[] New6 = { "boomerang", "frost_cannon", "flamethrower", "plasma_saw", "meteor_mortar", "prism_beacon" };

        /// <summary>기지 체력을 채우며 빨리 감기 (시험 세이브 전력으론 스테이지 12 · 20 을 오래 못 버팀) · 레벨업 카드는 넘김</summary>
        static void FFK(float sec)
        {
            for (float s = 0; s < sec; s += 1f)
            {
                if (B == null || B.st == Battle.St.End) return;
                B.baseHp = B.baseMax; FF(1f, 2);
            }
            if (B != null && B.st != Battle.St.End) B.baseHp = B.baseMax;
        }

        IEnumerator R12Steps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            var S = SaveData.Cur;
            var font = Object.FindAnyObjectByType<Text>()?.font;
            Lobby.NoIntro = true; Popup.CloseAll();
            S.best = Mathf.Max(S.best, 30); S.stamina = 999;
            Battle.LastSpeed = 1f;

            // ---------- ① 표 ----------
            {
                var want = new Dictionary<string, int> { ["rail"] = 0, ["cannon"] = 5, ["focus"] = 5, ["vortex"] = 5, ["wheel"] = 10, ["tesla"] = 10, ["boomerang"] = 10,
                    ["frost_cannon"] = 15, ["flamethrower"] = 15, ["plasma_saw"] = 15, ["meteor_mortar"] = 20, ["prism_beacon"] = 20 };
                var bad = new List<string>();
                foreach (var kv in want) if (!Tables.TurretById.TryGetValue(kv.Key, out var r) || r.unlock != kv.Value) bad.Add(kv.Key);
                Check(bad.Count == 0 && Tables.Turrets.Count == 18 && Tables.TurretSoon.Count == 0, $"해금 다시 (5: 대포 · 빔 · 소용돌이 / 10: 불바퀴 · 코일 · 부메랑 / 15 / 20) · 터렛 {Tables.Turrets.Count} {string.Join(",", bad)}");
                var miss = new List<string>();
                foreach (var id in New6)
                {
                    int nw = 0, nm = 0, br = 0, ch = 0, cb = 0;
                    foreach (var c in Tables.Cards) if (c.turret == id) { if (c.kind == "new") nw++; else if (c.kind == "normal") nm++; else if (c.kind == "break") br++; else if (c.kind == "chain") ch++; else if (c.kind == "combo") cb++; }
                    int chips = Tables.Chips.FindAll(c => c.turret == id).Count;
                    var sk = Tables.SkinOf(id);
                    if (nw != 1 || nm != 6 || br != 2 || ch != 4 || cb != 2 || chips != 3 || sk == null || !Art.TurretSkinUI(id) || !Art.TurretUI(id)) miss.Add($"{id}({nw}/{nm}/{br}/{ch}/{cb} 회로 {chips})");
                }
                Check(miss.Count == 0, $"새 타워 6종: 카드 새 1 · 일반 6 · 돌파 2 · 연계 4 · 콤보 2 · 스킨 · 회로 3 · 그림 {string.Join(" ", miss)}");
                bool fx = Art.Fx("boomerang") && Art.Fx("frost_ball") && Art.Fx("saw") && Art.Fx("meteor");
                Check(fx, "효과 그림 4장 (부메랑 · 서리탄 · 톱날 · 유성)");
                Check(!Tables.CardById.ContainsKey("d_tick") && !Tables.CardById.ContainsKey("t_shot"), "엑셀 \"제거\": 빠른 진동 · 전기 체인+ 카드 없음");
                var badCh = new HashSet<char>();
                foreach (var c in Tables.Cards) if (c.turret != null && System.Array.IndexOf(New6, c.turret) >= 0 || c.id.StartsWith("g_") || c.id.EndsWith("_shot") || c.id == "d_line" || c.id == "w_fast")
                        foreach (char ch in c.name + c.desc) if (ch > ' ' && font != null && !font.HasCharacter(ch)) badCh.Add(ch);
                foreach (var id in New6) { var r = Tables.TurretById[id]; var sk = Tables.SkinOf(id); foreach (char ch in r.name + r.info + r.desc + sk.name + sk.ultName + sk.ultDesc) if (ch > ' ' && font != null && !font.HasCharacter(ch)) badCh.Add(ch); }
                foreach (var c in Tables.Chips) if (System.Array.IndexOf(New6, c.turret) >= 0) foreach (char ch in c.name + c.desc) if (ch > ' ' && font != null && !font.HasCharacter(ch)) badCh.Add(ch);
                Check(badCh.Count == 0, $"새 글자가 모두 글꼴에 있음 {string.Join("", badCh)}");
                // 멀티샷 개편: 남은 발당 감소 카드 없음
                var pen = Tables.Cards.FindAll(c => c.stats.Exists(s => s.key == "shots" || s.key == "count"));
                Check(pen.Count == 0 && Tables.CardById["r_shot"].stats.Exists(s => s.key == "followup") && Tables.CardById["h_count"].stats.Exists(s => s.key == "sub_bee"),
                      $"멀티샷 → 한 발 더 (남은 발당 감소 카드 {pen.Count})");
            }

            // ---------- ② 의견 6개 ----------
            {
                var g = Tables.TurretById["guardian"];
                Check(System.Math.Abs(Tables.C("Resist_Mul") - 0.5) < 1e-9 && System.Math.Abs(Tables.C("Multi_Penalty") - 0.3) < 1e-9, "저항 0.5배 · 멀티샷 발당 70%");
                var gi = new TurretInst { row = g, metaLv = 1 }; gi.Recalc(); float c1 = gi.Cooldown;
                gi.metaLv = 45; float c45 = gi.Cooldown;
                Check(Mathf.Abs(g.rate - 10f) < 1e-4f && Mathf.Abs(c1 - 0.3f) < 1e-3f && Mathf.Abs(c45 - 0.15f) < 0.006f && Mathf.Abs(g.speed - 20f) < 1e-4f,
                      $"대장 카피 피해 {g.rate}% · 쿨다운 {c1:0.000} → Lv45 {c45:0.000}초 · 탄속 {g.speed}");
                var gs = Tables.CardById["g_shot"]; var ga = Tables.CardById["g_bA"];
                Check(gs.stats[0].key == "crit_dmg" && gs.max == 3 && System.Math.Abs(ga.stats[0].val - 0.05) < 1e-9, $"일제 사격 → {gs.name}(치명 피해, 최대 {gs.max}) · 폭발탄 {ga.stats[0].val * 100}%");
                var ri = new TurretInst { row = Tables.TurretById["rail"], metaLv = 1 }; ri.chip = new Dictionary<string, double> { ["cd_sec"] = Tables.ChipById["tc_rail_star"].Val(7) }; ri.Recalc();
                Check(Tables.TurretById["rail"].pierce == 3 && Tables.ChipById["tc_rail_star"].stat == "cd_sec" && Mathf.Abs(ri.Cooldown - (2.6f - 0.5f)) < 1e-3f,
                      $"관통포 기본 관통 3 · 관통포 별 회로 → 쿨다운 -{Tables.ChipById["tc_rail_star"].Val(7)}초 (궁극 {ri.Cooldown:0.00}초)");
                Check(Tables.Ci("Dome_Skill_Uses") == 1 && Tables.Ci("Dome_Freeze_Boss") == 1 && Mathf.Abs(Tables.Cf("Dome_Freeze_Time") - 2f) < 1e-4f, "돔 빙결: 2초 · 보스도 · 받는 피해 증가 · 한 판 1번");
            }

            // 새 타워 스킨 (필살기 시험)
            foreach (var id in New6) { if (!S.skins.Contains(id)) S.skins.Add(id); if (!S.skinOn.Contains(id)) S.skinOn.Add(id); }

            // ---------- ③ 새 타워 4종 (부메랑 · 서리 · 화염 · 톱날) ----------
            Boomerang.LastReturns = 0; Boomerang.LastApexTicks = 0; Frost.LastHits = 0; SawBlade.LastTicks = 0; SawBlade.LastMoved = 0;
            Game.StartStage(12, 1212); yield return null; Tutorial.CloseCur();
            foreach (var id in new[] { "new_boomerang", "new_frost_cannon", "new_flamethrower", "new_plasma_saw" }) Force(id);
            Check(B.units.Count == 5 && B.run.Get("plasma_saw") != null, $"새 터렛 4종을 칸에 놓음 ({B.units.Count - 1})");
            for (int i = 0; i < 14; i++) { var bt = B.Spawn("beetle", new Vector2(-4f + (i % 7) * 1.3f, 1.0f + (i / 7) * 1.4f)); bt.hp = bt.maxHp = bt.maxHp * 400; }
            FFK(12f);
            yield return Wait(0.5f);
            Capture("r12_1_new4");
            FFK(10f);
            foreach (var u in B.units) Check(u.fired > 0, $"{u.t.row.name} 쏨 ({u.fired}번)");
            Check(Boomerang.LastReturns > 0, $"별빛 부메랑이 되돌아옴 ({Boomerang.LastReturns}번)");
            Check(Frost.LastHits > 0 && B.enemies.Exists(e => !e.dead && e.slow > 0.25f), $"서리 폭발 (한 번에 {Frost.LastHits}마리) · 느려짐");
            Check(SawBlade.LastTicks > 3, $"자기장 톱날이 놓여 돎 (틱 {SawBlade.LastTicks})");
            {
                var st = B.run.Get("plasma_saw"); var e = B.Spawn("beetle", new Vector2(0, 3)); e.Aim(new Vector2(0, -5));
                double p0 = 0, p1 = 0; var rail = new TurretInst { row = Tables.TurretById["rail"] }; rail.Recalc();
                for (int i = 0; i < 40; i++) { p0 = System.Math.Max(p0, Combat.Calc(rail, e, 100, out bool c0) / (c0 ? 2 : 1)); p1 = System.Math.Max(p1, Combat.Calc(st, e, 100, out bool c2) / (c2 ? 2 : 1)); }
                e.dead = true;
                Check(e.row.armor <= 0 || p1 > p0, $"톱날 장갑 무시 {Tables.C("Saw_Armor_Pen")}% (장갑 {e.row.armor:0.##})");
            }
            // 돌파
            foreach (var id in new[] { "bm_dmg", "bm_dmg", "bm_cd", "bm_bA", "sw_dmg", "sw_dmg", "sw_cd", "sw_bA", "fr_dmg", "fr_dmg", "fr_cd", "fr_bB", "fl_dmg", "fl_dmg", "fl_cd", "fl_bA" }) Force(id);
            Check(B.run.Get("boomerang").tier == 2 && B.run.Get("plasma_saw").Has("saw_track") && B.run.Get("frost_cannon").Has("frost_zone") && B.run.Get("flamethrower").Has("lava_pool"), "새 터렛 4종 돌파");
            FFK(14f);
            Check(Boomerang.LastApexTicks > 0, $"정점 회전 ({Boomerang.LastApexTicks}틱)");
            Check(SawBlade.LastMoved > 0.1f, $"자석 추적 톱날이 움직임 ({SawBlade.LastMoved:0.0}칸)");
            yield return Wait(0.5f);
            Capture("r12_2_new4_break");
            foreach (var id in new[] { "boomerang", "frost_cannon", "flamethrower", "plasma_saw" }) { var u = B.run.Get(id).unit; Check(u.ults > 0, $"{u.t.row.name} 필살기 {u.t.skin?.ultName} ({u.ults}번)"); }

            // ---------- ③ 유성 · 증폭탑 + ④ 개편 (드론 · 관통포 한 발 더) ----------
            Popup.CloseAll(); Game.ToLobby(); yield return null; Popup.CloseAll();
            MeteorFall.LastImpacts = 0; TurretUnit.Followups = 0; TurretUnit.ExtraLines = 0;
            Game.StartStage(20, 2020); yield return null; Tutorial.CloseCur();
            foreach (var id in new[] { "new_meteor_mortar", "new_prism_beacon", "new_drone", "new_rail", "r_shot", "d_dmg", "d_dmg", "d_line" }) Force(id);
            for (int i = 0; i < 14; i++) { var bt = B.Spawn("beetle", new Vector2(-4f + (i % 7) * 1.3f, 1.0f + (i / 7) * 1.4f)); bt.hp = bt.maxHp = bt.maxHp * 400; }
            FFK(10f);
            bool sawBuff = B.Buffing;
            yield return Wait(0.5f);
            Capture("r12_3_meteor_prism");
            FFK(30f);
            Check(MeteorFall.LastImpacts >= 3, $"유성 박격포 연발 (떨어진 유성 {MeteorFall.LastImpacts})");
            Check(B.buffPulses > 0, $"별빛 증폭탑 버프 ({B.buffPulses}번)");
            {
                var rail = B.run.Get("rail"); var e = B.Spawn("moth", new Vector2(0, 3)); e.Aim(new Vector2(0, -5));
                float keep = B.buffT; double d0 = double.MaxValue, d1 = double.MaxValue;
                B.buffT = 0; for (int i = 0; i < 40; i++) { double d = Combat.Calc(rail, e, 100, out bool c); if (!c) d0 = System.Math.Min(d0, d); }
                B.buffT = 5; B.buffDmg = 0.25; for (int i = 0; i < 40; i++) { double d = Combat.Calc(rail, e, 100, out bool c); if (!c) d1 = System.Math.Min(d1, d); }
                B.buffT = keep; e.dead = true;
                Check(d1 > d0 * 1.05, $"버프 중 아군 피해 x{d1 / d0:0.00}");
            }
            Check(TurretUnit.Followups > 0, $"한 발 더 (관통포 포탄 일제 사격 {TurretUnit.Followups}번 · 배율 {B.run.Get("rail").FollowMul})");
            Check(TurretUnit.ExtraLines > 0, $"장판 증폭: 감속선 1개 더 ({TurretUnit.ExtraLines}번)");
            {   // 감속선: 처음 닿으면 35% 한 번 (틱마다 쌓이지 않음)
                var e = B.Spawn("moth", new Vector2(0, 8.6f)); e.hold = true; e.hp = e.maxHp = 1e12; B.Tick(Battle.StepSec); e.Teleport(new Vector2(0, 8.6f)); e.slow = 0;   // 부른 적은 다음 틱에 목록에 들어감
                var f = new Field(B.run.Get("drone"), 8.6f, 0.9f, 3f, 0.5f, 1, false, true);
                for (int i = 0; i < 60; i++) f.Step(Battle.StepSec);
                Check(Mathf.Abs(e.slow - Tables.Cf("Drone_Line_Slow", 0.35f)) < 0.02f, $"감속선 둔화 {e.slow * 100:0}% (틱마다 안 쌓임)");
                f.Kill(); e.dead = true;
            }
            foreach (var id in new[] { "meteor_mortar", "prism_beacon" }) { var u = B.run.Get(id).unit; Check(u.ults > 0, $"{u.t.row.name} 필살기 {u.t.skin?.ultName} ({u.ults}번)"); }

            // ---------- ④ 집중 빔 · 코일 개편 ----------
            {
                Popup.CloseAll(); Game.ToLobby(); yield return null; Popup.CloseAll();
                Game.StartStage(12, 77); yield return null; Tutorial.CloseCur();
                Force("new_focus");
                var tg = B.Spawn("beetle", new Vector2(0, 3)); tg.hp = tg.maxHp = 1e15; tg.hold = true;
                FocusBeam.LastBaseRamp = 0;
                var fb = new FocusBeam(B.run.Get("focus"), tg, B.run.Get("focus").Duration, 0.22f);
                for (int i = 0; i < 300; i++) fb.Step(Battle.StepSec);
                Check(!fb.dead && FocusBeam.LastBaseRamp > 0.5f, $"집중 빔: 4초가 지나도 계속 · 피해 점점 +{FocusBeam.LastBaseRamp * 100:0}%");
                fb.Kill(); tg.dead = true;
                Check(Tables.TurretById["tesla"].hits == 1 && !Tables.CardById["t_bounce"].stats.Exists(s => s.key == "shots"), "찌릿 코일 기본 1번 튕김 · 전기 체인 튕긴 번개 70%");
            }

            // ---------- 화면: 실제로 쏘는 모습 (실시간 — 빨리 감기 아님) ----------
            {
                double expKeep = Tables.C("Exp_Base", 8); Tables.SetC("Exp_Base", 1e9);   // 레벨업 카드 창이 화면을 가리지 않게
                Popup.CloseAll(); Game.ToLobby(); yield return null; Popup.CloseAll();
                Game.StartStage(12, 3131); yield return null; Tutorial.CloseCur();
                foreach (var id in new[] { "new_boomerang", "new_frost_cannon", "new_flamethrower", "new_plasma_saw", "fr_dmg", "fr_dmg", "fr_cd", "fr_bB", "bm_dmg", "bm_dmg", "bm_cd", "bm_bB" }) Force(id);
                for (int i = 0; i < 12; i++) { var bt = B.Spawn("beetle", new Vector2(-3.6f + (i % 6) * 1.45f, 0.2f + (i / 6) * 1.6f)); bt.hp = bt.maxHp = bt.maxHp * 400; }
                string[] shots = { "r12_6a_live", "r12_6b_live", "r12_6c_live", "r12_6d_live" };
                foreach (var n in shots) { B.baseHp = B.baseMax; yield return Wait(1.7f); Capture(n); }
                Popup.CloseAll(); Game.ToLobby(); yield return null; Popup.CloseAll();
                Game.StartStage(20, 3232); yield return null; Tutorial.CloseCur();
                foreach (var id in new[] { "new_meteor_mortar", "new_prism_beacon", "new_drone", "new_hive", "h_count", "d_line" }) Force(id);
                for (int i = 0; i < 12; i++) { var bt = B.Spawn("beetle", new Vector2(-3.6f + (i % 6) * 1.45f, 0.6f + (i / 6) * 1.6f)); bt.hp = bt.maxHp = bt.maxHp * 400; }
                string[] shots2 = { "r12_7a_live", "r12_7b_live", "r12_7c_live" };
                foreach (var n in shots2) { B.baseHp = B.baseMax; yield return Wait(1.9f); Capture(n); }
                Tables.SetC("Exp_Base", expKeep);
            }

            // ---------- 화면: 터렛 탭 ----------
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.3f); Popup.CloseAll();
            Game.Lobby.SetTab(1); yield return Wait(0.4f);
            var sr = Object.FindAnyObjectByType<ScrollRect>();
            if (sr) { Canvas.ForceUpdateCanvases(); sr.verticalNormalizedPosition = 0f; }
            yield return Wait(0.3f);
            Capture("r12_4_turret_tab");
            Battle.LastSpeed = 1f;
            running = false;
        }
    }
}
