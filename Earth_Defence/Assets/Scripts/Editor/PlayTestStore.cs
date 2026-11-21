using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-21 스토어 스크린샷 촬영 (카치건 RunStore 방식): bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunStore --play
    /// → Logs/PlayTest/store/01_battle … 08_story (1080x2340) → tools/store_graphics.py 가 1080x1920 문구 판 + 그래픽 이미지로
    /// 게임 내용이 바뀌면(적 · 보스 · 화면) 다시 찍을 것 — 스토어 그림은 그 빌드가 나간 날 바꿈
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator StoreSteps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir + "/store");
            SetupCapture();
            var S = SaveData.Cur;
            Lobby.NoIntro = true; Popup.CloseAll();
            S.best = Mathf.Max(S.best, 99); S.stamina = 999; S.gold = 2.4e7; S.dia = 12800; Battle.LastSpeed = 1f;
            Ads.HideDraftNote = true;      // 에디터 "[초안]" 글자는 스토어 그림에 안 나오게
            // 장비 탭이 비어 보이지 않게: 부위마다 영웅~전설 장비를 끼우고 가방에도 몇 개 · 회로 몇 개
            string[] parts = { "weapon", "helmet", "suit", "shield", "core", "boots" };
            for (int i = 0; i < parts.Length; i++) { var g = Gear.NewGear(4 + i % 3, parts[i]); if (g != null) Gear.Equip(g); }
            for (int i = 0; i < 10; i++) Gear.NewGear(2 + i % 5);
            for (int i = 0; i < 6; i++) Gear.NewChip(3 + i % 4);

            // 01 전투: Ch.2 보라 하늘 · 터렛 4 · 적이 쏟아짐
            yield return Fight(25, new[] { "laser", "missile", "hive", "bolt" }, new[] { "shooter_ufo", "fire", "moth", "heal_jelly", "frost_mite", "spider", "pebble", "jelly", "shield_bot", "moth" }, null, 2.2f);
            Capture("store/01_battle");
            // 02 보스: Ch.3 화성 석양 · 집게 요새 게 + 얼음 파동
            yield return Fight(45, new[] { "rail", "cannon", "focus", "tesla" }, new[] { "bomb_rock", "mirror_cube", "lava_slime", "fire", "beetle" }, "mecha_crab", 1.6f);
            Battle.I.skillCdD = 0; Battle.I.UseDome(); yield return Wait(0.35f);
            Capture("store/02_boss");
            // 03 카드: 레벨업 카드 3장
            yield return Fight(65, new[] { "vortex", "wheel", "drone", "laser" }, new[] { "blink_bug", "ice", "moth", "spider" }, null, 1.2f);
            var b = Battle.I;
            for (int i = 0; i < 30 && b.st != Battle.St.Pick; i++) { b.GainExp(1e6); yield return null; }
            yield return Wait(0.5f);
            Tutorial.CloseCur(); yield return Wait(0.2f);      // 카드 안내 말풍선은 빼고
            Capture("store/03_cards");
            // 04 특수 능력: Ch.5 젤리 은하 · 대왕 젤리 황제 · 버닝
            Popup.CloseAll();
            yield return Fight(85, new[] { "missile", "bolt", "hive", "focus" }, new[] { "heal_jelly", "lava_slime", "prism_core", "split", "jelly" }, "jelly_king", 1.4f);
            Battle.I.skillCdG = 0; Battle.I.UseBurn();
            var ev = Battle.I.Guardian; if (ev) ev.targetX = 1.2f;
            yield return Wait(1.0f);
            Capture("store/04_skill");
            // 05 장비 · 06 외형(코스튬 x 탈것) · 07 터렛
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.3f); Popup.CloseAll();
            foreach (var id in new[] { "wizard", "onsen", "king" }) if (!S.costumes.Contains(id)) S.costumes.Add(id);
            foreach (var id in new[] { "unicorn", "tub", "cloud" }) if (!S.mounts.Contains(id)) S.mounts.Add(id);
            Store.WearLook(Tables.Costume("king"), false); Store.WearLook(Tables.Mount("cloud"), true);
            Game.Lobby.SetTab(2); yield return Wait(0.6f);
            Capture("store/05_gear");
            Looks.Open(2); yield return Wait(0.6f);
            Capture("store/06_looks");
            Game.Lobby.SetTab(1); yield return Wait(0.5f);
            Capture("store/07_turret");
            // 08 스토리: 새 보스가 말하는 장면
            Popup.CloseAll(); Game.Lobby.SetTab(0); yield return Wait(0.3f);
            var v = StoryView.Play("s40", 40, null); yield return Wait(0.2f);
            for (int i = 0; i < 40 && v && v.CurWho != "frost_emperor"; i++) { v.Next(); if (v && v.Typing) v.Next(); yield return null; }
            yield return Wait(1.6f);
            Capture("store/08_story");
            if (StoryView.Cur) Object.Destroy(StoryView.Cur.gameObject);
            Store.WearLook(Tables.Costume("basic"), false); Store.WearLook(Tables.Mount("none"), true);
            Ads.HideDraftNote = false;
            Check(true, "스토어 촬영 8장");
            running = false;
        }

        /// <summary>스테이지 하나: 터렛 4칸(카드 A 단계) + 적 몇 마리 + (보스) → 몇 초 싸움</summary>
        IEnumerator Fight(int stage, string[] turrets, string[] enemies, string boss, float sec)
        {
            Popup.CloseAll(); Game.StartStage(stage, 7); yield return Wait(0.3f);
            var b = Battle.I; Tutorial.CloseCur();
            for (int i = 0; i < turrets.Length; i++) b.LabPlace(i + 1, turrets[i], "A");
            World.I.ShowSlots(false);
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = b.Spawn(enemies[i], new Vector2(-4f + (i % 5) * 2f, 2.2f + (i / 5) * 2.4f + (i % 2) * 0.8f));
                if (e != null) e.hp = e.maxHp = e.maxHp * 40;     // 촬영 동안 안 죽게
            }
            if (boss != null) { var bo = b.Spawn(boss, new Vector2(0.6f, 5.2f)); if (bo != null) bo.hp = bo.maxHp = bo.maxHp * 40; }
            b.skillCdG = b.skillCdD = 0;
            yield return Wait(sec);
        }
    }
}
