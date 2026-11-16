using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 성능 자동 측정 (11-17 코드 점검): bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunPerf --play
    /// 결과 = Earth_Defence/Logs/Perf/perf_&lt;이름&gt;.json (이름은 Logs/Perf/label.txt — before · after) → tools/perf_report.py 가 전 · 뒤 표 HTML.
    /// 재는 것: ① 전투 한 칸(Tick) 시간 · 메모리 할당 — 적 90마리를 계속 채운 무거운 장면 2가지 (터렛 묶음 A · B)
    ///          ② 실제 프레임(PlayerLoop) 시간 ③ 전투 3판 뒤 남는 물체 · 메모리(누수) ④ 로비 탭 · 창 여는 시간.
    /// 에디터의 프레임 GC 값은 에디터 몫이 섞여서 안 씀 → 게임 코드만 도는 Tick 구간의 스레드 할당량으로 봄.
    /// </summary>
    public partial class PlayTestRunner
    {
        readonly List<string> perf = new();
        void P(string key, double v)
        {
            perf.Add($"\"{key}\": {v.ToString("0.####", CultureInfo.InvariantCulture)}");
            Log($"PERF {key} = {v:0.###}");
        }

        static double Pct(List<double> v, double p) { var s = new List<double>(v); s.Sort(); return s[Mathf.Clamp((int)(s.Count * p), 0, s.Count - 1)]; }
        static double Avg(List<double> v) { double s = 0; foreach (var x in v) s += x; return s / Mathf.Max(1, v.Count); }

        System.Random prnd;
        /// <summary>장면을 무겁게 유지: 적 90마리 채움 · 기지 안 죽음 · 카드 창 안 뜸 · 시간 고정(보스 안 나옴)</summary>
        void PerfKeep()
        {
            B.baseHp = B.baseMax; B.pendingLevel = 0; B.pendingBoss = 0; B.time = 150f;
            if (B.st != Battle.St.Play) B.st = Battle.St.Play;
            int need = 90 - B.enemies.Count;
            for (int i = 0; i < need && i < 6; i++)
            {
                string id = B.stage.enemies[prnd.Next(B.stage.enemies.Length)];
                B.Spawn(id, new Vector2((float)(prnd.NextDouble() * 2 - 1) * 4.6f, Mathf.Lerp(World.ShieldY + 1.5f, World.SpawnY - 0.5f, (float)prnd.NextDouble())));
            }
        }

        IEnumerator PerfBattle(string tag, string[] cards)
        {
            prnd = new System.Random(5);
            Game.StartStage(10, 7); yield return null;
            foreach (var id in cards) Force(id);
            FF(100f, 0);                                   // 레벨업 카드는 첫 장을 고름 (같은 씨앗 → 같은 빌드)
            Log($"PERF {tag} 터렛 {B.run.turrets.Count} · 고른 카드 {B.picks} · 레벨 {B.level}");
            for (int i = 0; i < 120; i++) { PerfKeep(); B.Tick(Battle.StepSec); }
            Fx.I.Clear(); yield return null;

            // ① Tick 만 (게임 규칙 계산): 1800칸 = 게임 시간 30초
            var times = new List<double>(1800);
            var sw = new Stopwatch();
            System.GC.Collect();
            int g0 = System.GC.CollectionCount(0);
            long kills0 = B.kills, alloc = 0, used = Profiler.GetMonoUsedSizeLong(); int spikes = 0;
            for (int i = 0; i < 1800; i++)
            {
                PerfKeep();
                used = Profiler.GetMonoUsedSizeLong();
                sw.Restart(); B.Tick(Battle.StepSec); sw.Stop();
                long u2 = Profiler.GetMonoUsedSizeLong(); if (u2 > used) alloc += u2 - used;      // 쓰는 메모리가 늘어난 만큼 = 그 칸에서 새로 잡은 양 (GC 가 돌면 줄어들어 그 칸은 0)
                times.Add(sw.Elapsed.TotalMilliseconds);
                if (sw.Elapsed.TotalMilliseconds > 1.0) spikes++;
                if (i % 20 == 19) { Fx.I.Clear(); Fx.I.SendMessage("Update"); }     // 효과가 끝없이 쌓이지 않게 (프레임이 안 도는 구간)
            }
            P(tag + "_tick_ms_avg", Avg(times)); P(tag + "_tick_ms_p95", Pct(times, 0.95)); P(tag + "_tick_ms_p99", Pct(times, 0.99)); P(tag + "_tick_ms_max", Pct(times, 1.0));
            P(tag + "_tick_spikes_over_1ms", spikes);
            P(tag + "_tick_alloc_kb", alloc / 1024.0 / 1800.0);
            P(tag + "_gc_count_30s", System.GC.CollectionCount(0) - g0);
            P(tag + "_kills_30s", B.kills - kills0);
            Fx.I.Clear(); yield return null; yield return null;

            // ② 실제 프레임 (PlayerLoop = 게임 한 프레임 전체: 규칙 + 효과 + UI + 그리기 준비)
            var loop = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop");
            var frames = new List<double>(400); var loops = new List<double>(400);
            B.speed = 1; B.st = Battle.St.Play;
            for (int i = 0; i < 400; i++)
            {
                PerfKeep();
                yield return null;
                frames.Add(Time.unscaledDeltaTime * 1000.0);
                if (loop.Valid && loop.LastValue > 0) loops.Add(loop.LastValue / 1e6);
            }
            loop.Dispose();
            P(tag + "_frame_ms_avg", Avg(frames)); P(tag + "_frame_ms_p95", Pct(frames, 0.95)); P(tag + "_frame_ms_max", Pct(frames, 1.0));
            if (loops.Count > 0) { P(tag + "_loop_ms_avg", Avg(loops)); P(tag + "_loop_ms_p95", Pct(loops, 0.95)); }
            P(tag + "_fx_live", Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Length);
        }

        static int CountObjects() => Resources.FindObjectsOfTypeAll<GameObject>().Length;

        double TimeUI(System.Action a, int n = 3)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < n; i++) { a(); Canvas.ForceUpdateCanvases(); }
            return sw.Elapsed.TotalMilliseconds / n;
        }

        IEnumerator PerfSteps()
        {
            Lobby.NoIntro = true;
            yield return null;
            Popup.CloseAll(); Tutorial.CloseCur();
            Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;
            Tables.SetC("Tutorial", 0);
            S.best = 100; S.gold = 1e12; S.dia = 1e6; S.baseLv = 60;
            string label = File.Exists("Logs/Perf/label.txt") ? File.ReadAllText("Logs/Perf/label.txt").Trim() : "run";
            P("boot_ms", Boot.BootMs); P("boot_tables_ms", Boot.BootTablesMs); P("boot_audio_ms", Boot.BootAudioMs);

            // ---- 전투
            yield return PerfBattle("A", new[] { "new_rail", "new_laser", "new_hive", "new_missile" });
            yield return PerfBattle("B", new[] { "new_cannon", "new_vortex", "new_wheel", "new_tesla" });
            yield return PerfBattle("C", new[] { "new_drone", "new_bolt", "new_focus", "new_rail" });

            // ---- 누수: 전투 3판 뒤 물체 수 · 메모리가 처음 로비로 돌아오는가
            Game.ToLobby(); yield return null; yield return null; Popup.CloseAll();
            System.GC.Collect(); yield return Resources.UnloadUnusedAssets(); System.GC.Collect();
            int o0 = CountObjects(); long m0 = Profiler.GetMonoUsedSizeLong();
            for (int k = 0; k < 3; k++)
            {
                Game.StartStage(10, 11 + k); yield return null;
                Force("new_rail"); Force("new_hive");
                FF(60f, 0);
                for (int f = 0; f < 5; f++) yield return null;
                Game.ToLobby(); yield return null; yield return null; Popup.CloseAll();
            }
            for (int f = 0; f < 60; f++) yield return null;           // 효과 풀이 다 돌아오게
            System.GC.Collect(); yield return Resources.UnloadUnusedAssets(); System.GC.Collect();
            P("leak_objects_before", o0); P("leak_objects_after3", CountObjects());
            P("leak_mono_mb_before", m0 / 1048576.0); P("leak_mono_mb_after3", Profiler.GetMonoUsedSizeLong() / 1048576.0);

            // ---- 로비: 탭 바꾸기 · 창 열기 (가방을 꽉 채운 상태)
            for (int i = 0; i < 180; i++) { Gear.NewGear(1 + i % 7); Gear.NewChip(1 + i % 7); }
            foreach (var t in Tables.Turrets) Store.BuySkin(Tables.SkinOf(t.id) ?? Tables.Skins[0]);
            Game.ToLobby(); yield return null; Popup.CloseAll();
            for (int i = 0; i < Lobby.Tabs.Length; i++)
            {
                int idx = i;
                P("ui_tab_" + Lobby.Tabs[i] + "_ms", TimeUI(() => Game.Lobby.SetTab(idx)));
                yield return null;
            }
            Game.Lobby.SetTab(2); yield return null;
            GearTab.showChips = false; P("ui_gear_refresh_ms", TimeUI(() => Game.Lobby.gearTab.Refresh(), 5));
            GearTab.showChips = true; P("ui_chip_refresh_ms", TimeUI(() => Game.Lobby.gearTab.Refresh(), 5));
            P("ui_lobby_refresh_ms", TimeUI(() => Game.Lobby.Refresh(), 5));
            P("ui_popup_part_ms", TimeUI(() => { Popup.CloseAll(); Game.Lobby.gearTab.PartWindow(0); }));
            P("ui_popup_chipplace_ms", TimeUI(() => { Popup.CloseAll(); Game.Lobby.gearTab.ChipPlace(S.chips[0]); }));
            Popup.CloseAll(); Game.Lobby.SetTab(1); yield return null;
            P("ui_popup_up_ms", TimeUI(() => { Popup.CloseAll(); Game.Lobby.UpPopup(Tables.TurretById["rail"]); }));
            P("ui_popup_lvtable_ms", TimeUI(() => { Popup.CloseAll(); Lobby.LvTable(Tables.TurretById["rail"]); }));
            P("ui_popup_lvtable_base_ms", TimeUI(() => { Popup.CloseAll(); Lobby.LvTable(null); }));
            Popup.CloseAll(); yield return null;
            // 열린 회로 칸을 모두 채운 뒤 (후반 상태) — 로비가 1초마다 부르는 빨간 점 검사 · 일괄 합성
            foreach (var c in S.chips.ToArray()) { if (c.at >= 0) continue; var (pp, ss) = Gear.FreeSlotFor(c); if (pp < 0) continue; Gear.PutChip(c, pp, ss); }
            foreach (var gp in Tables.GearParts) { var g = S.gear.Find(x => x.part == gp.part && !x.on); if (g != null) Gear.Equip(g); }
            { var swn = Stopwatch.StartNew(); bool news = false; for (int i = 0; i < 20; i++) news |= GearTab.HasNews; P("ui_hasnews_full_ms", swn.Elapsed.TotalMilliseconds / 20); Log("PERF hasnews=" + news + " chips=" + S.chips.Count + " gear=" + S.gear.Count); }
            { var swm = Stopwatch.StartNew(); int mg = Gear.MergeAllGear().Count, mc = Gear.MergeAllChipList().Count; P("merge_all_ms", swm.Elapsed.TotalMilliseconds); Log($"PERF merged gear {mg} chip {mc}"); }
            var sw = Stopwatch.StartNew();
            Game.StartStage(10, 3); Canvas.ForceUpdateCanvases();
            P("ui_start_battle_ms", sw.Elapsed.TotalMilliseconds);
            yield return null;
            sw.Restart(); Game.ToLobby(); Canvas.ForceUpdateCanvases();
            P("ui_to_lobby_ms", sw.Elapsed.TotalMilliseconds);
            yield return null;
            sw.Restart(); S.Save(); P("save_ms", sw.Elapsed.TotalMilliseconds);

            Directory.CreateDirectory("Logs/Perf");
            File.WriteAllText($"Logs/Perf/perf_{label}.json", "{\n  " + string.Join(",\n  ", perf) + "\n}\n");
            Log("PERF 저장 Logs/Perf/perf_" + label + ".json");
            running = false;
        }
    }
}
