using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-21 콘텐츠 확장 (제안서 그림 → 게임): bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunExpand --play
    /// 새 적 16 (그림 · 표 · 스테이지 배치 · 소환) · 챕터 하늘 · 하늘 행성 · 터렛 탭 "곧 열리는 터렛" — 화면 = Logs/PlayTest/ex_*.png
    /// </summary>
    public partial class PlayTestRunner
    {
        static readonly string[] ExNormal = { "shooter_ufo", "shield_bot", "heal_jelly", "frost_mite", "bomb_rock", "mirror_cube", "blink_bug", "lava_slime" };
        static readonly string[] ExElite = { "cryo_golem", "carrier_pod", "prism_core", "magma_titan" };
        static readonly string[] ExBoss = { "frost_emperor", "mecha_crab", "ring_wyrm", "jelly_king" };

        IEnumerator ExpandSteps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            var S = SaveData.Cur;
            var font = Object.FindAnyObjectByType<Text>()?.font;

            // ---------- 1) 표 · 그림 ----------
            var all = new List<string>(); all.AddRange(ExNormal); all.AddRange(ExElite); all.AddRange(ExBoss);
            {
                var miss = new List<string>(); var bad = new HashSet<char>();
                foreach (var id in all)
                {
                    if (!Tables.Enemies.TryGetValue(id, out var r)) { miss.Add(id + "(표)"); continue; }
                    if (!Art.Enemy(r) || !Art.Enemy(r, true)) miss.Add(id + "(그림)");
                    foreach (char ch in r.name) if (ch > ' ' && font != null && !font.HasCharacter(ch)) bad.Add(ch);
                }
                Check(miss.Count == 0, $"새 적 {all.Count}종 표 · 그림 · 맞을 때 흰 그림{(miss.Count > 0 ? " — 없음: " + string.Join(", ", miss) : "")}");
                Check(bad.Count == 0, $"새 적 이름이 글꼴(주아)에 있음{(bad.Count > 0 ? " — 없음: " + string.Join(" ", bad) : "")}");
                bool rank = true;
                foreach (var id in ExElite) if (!Tables.Enemies[id].IsElite) rank = false;
                foreach (var id in ExBoss) if (!Tables.Enemies[id].IsBoss) rank = false;
                Check(rank, "엘리트 4 · 보스 4 등급");
            }

            // ---------- 2) 스테이지 배치 ----------
            {
                string[] chBoss = { null, null, "frost_emperor", "mecha_crab", "ring_wyrm", "jelly_king" };
                bool bossOk = true; var seen = new HashSet<string>(); var seenEl = new HashSet<string>();
                for (int s = 1; s <= 100; s++)
                {
                    var st = Tables.Stage(s); int ch = World.ChapterOf(s);
                    if (ch == 1 && st.boss != "planet" && st.boss != "queen") bossOk = false;
                    if (ch >= 2 && st.boss != chBoss[ch]) bossOk = false;
                    foreach (var e in st.enemies) { seen.Add(e); if (!Tables.Enemies.ContainsKey(e)) bossOk = false; }
                    seenEl.Add(st.elite);
                    if (ch == 1) foreach (var e in st.enemies) if (System.Array.IndexOf(ExNormal, e) >= 0) bossOk = false;
                }
                Check(bossOk, "스테이지 1~20 = 예전 보스 · 21~40 서리 수정왕 · 41~60 집게 요새 게 · 61~80 토성 고리룡 · 81~100 대왕 젤리 황제 · 1~20 엔 새 적 없음");
                bool allNormal = true; foreach (var id in ExNormal) if (!seen.Contains(id)) allNormal = false;
                bool allElite = true; foreach (var id in ExElite) if (!seenEl.Contains(id)) allElite = false;
                Check(allNormal && allElite, "새 일반 8종 · 엘리트 4종이 모두 어느 스테이지엔가 나옴");
            }

            // ---------- 3) 챕터 하늘 · 행성 + 전투에서 소환 ----------
            Lobby.NoIntro = true; Popup.CloseAll();
            S.best = Mathf.Max(S.best, 100); S.stamina = 999;
            int[] stages = { 5, 25, 45, 65, 85 };
            string[][] show = {
                new[] { "pebble", "fire" },
                new[] { "shooter_ufo", "shield_bot", "heal_jelly", "frost_mite", "frost_emperor" },
                new[] { "bomb_rock", "mirror_cube", "lava_slime", "carrier_pod", "mecha_crab" },
                new[] { "blink_bug", "cryo_golem", "magma_titan", "ring_wyrm" },
                new[] { "prism_core", "heal_jelly", "lava_slime", "jelly_king" },
            };
            for (int i = 0; i < stages.Length; i++)
            {
                int s = stages[i];
                Popup.CloseAll(); Game.StartStage(s, 7); yield return Wait(0.5f);
                var w = World.I;
                int ch = World.ChapterOf(s);
                var skyName = GameObject.Find("Sky")?.GetComponent<SpriteRenderer>()?.sprite?.name ?? "";
                var moonName = GameObject.Find("Moon")?.GetComponent<SpriteRenderer>()?.sprite?.name ?? "";
                bool rabbit = GameObject.Find("MoonRabbit")?.GetComponent<SpriteRenderer>()?.enabled ?? false;
                bool skyOk = ch == 1 ? skyName.EndsWith("Bg/sky") : skyName.EndsWith("sky_ch" + ch);
                bool moonOk = ch <= 2 ? moonName.EndsWith("Bg/moon") && rabbit : moonName.EndsWith("planet_ch" + ch) && !rabbit;
                Check(w.Chapter == ch && skyOk && moonOk, $"스테이지 {s} = 챕터 {ch}: 하늘 {skyName} · 하늘 그림 {moonName} · 달토끼 {(rabbit ? "있음" : "없음")}");
                var bt = Battle.I;
                int k = 0; bool spawned = true;
                foreach (var id in show[i])
                {
                    var r = Tables.Enemies[id];
                    var pos = new Vector2(-3.2f + (k % 4) * 2.2f, r.IsBoss ? 5.2f : r.IsElite ? 3.2f : 7.2f - (k / 4) * 1.6f);
                    var e = bt.Spawn(id, pos); k++;
                    if (e == null || e.dead) spawned = false;
                }
                yield return Wait(1.2f);
                Check(spawned, $"스테이지 {s}: {string.Join(" · ", show[i])} 전투에 나옴 (오류 없음)");
                Capture($"ex_{i + 1}_ch{ch}_stage{s}");
            }
            // 예전 하늘 스위치 (Chapter_Bg 0)
            Tables.SetC("Chapter_Bg", 0);
            Popup.CloseAll(); Game.StartStage(65, 7); yield return Wait(0.3f);
            Check(World.I.Chapter == 1 && (GameObject.Find("Sky")?.GetComponent<SpriteRenderer>()?.sprite?.name ?? "").EndsWith("Bg/sky"), "Config Chapter_Bg 0 = 예전 하늘 하나로 되돌림");
            Tables.SetC("Chapter_Bg", 1);
            // 로비로 가면 로비 하늘 (챕터와 무관)
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.3f);
            Check((GameObject.Find("Sky")?.GetComponent<SpriteRenderer>()?.sprite?.name ?? "").EndsWith("sky_plain"), "로비 = 심플 하늘 그대로");

            // ---------- 4) 터렛 탭: 곧 열리는 터렛 ----------
            Popup.CloseAll(); Game.Lobby.SetTab(1); yield return Wait(0.4f);
            int soon = 0; bool pics = true;
            foreach (var t in Tables.TurretSoon) { var g = GameObject.Find("Soon_" + t.id); if (g) soon++; if (!Art.TurretUI(t.id)) pics = false; }
            Check(soon == 6 && pics && Tables.Turrets.Count == 12 && GameObject.Find("SoonHead"), $"터렛 탭 맨 아래 \"곧 열리는 터렛\" {soon}종 (그림만 — 전투 터렛 목록은 그대로 {Tables.Turrets.Count})");
            var sr = Object.FindAnyObjectByType<ScrollRect>();
            if (sr) { Canvas.ForceUpdateCanvases(); sr.verticalNormalizedPosition = 0f; }
            yield return Wait(0.3f);
            Capture("ex_6_turret_soon");
            running = false;
        }
    }
}
