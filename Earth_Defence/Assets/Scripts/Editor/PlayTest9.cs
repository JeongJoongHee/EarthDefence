using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-20 9차 수정안 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR9 --play
    /// 화면 = Logs/PlayTest/r9_*.png (보고서 사본은 캡쳐본 모음/9차_수정안_캡처/)
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator R9Steps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            Lobby.NoIntro = true;
            StoryView.Off = false;
            yield return Wait(0.4f);
            Rect UiRect(GameObject go) { var c = new Vector3[4]; ((RectTransform)go.transform).GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
            SpriteRenderer R(string n) => GameObject.Find(n)?.GetComponent<SpriteRenderer>();
            var font = Object.FindAnyObjectByType<Text>()?.font;

            // ---------- 1) 표: 처음 시작 스토리 · 스테이지 100개 · 로비 말풍선 · 글꼴 ----------
            var pro = Tables.StoryOf("prologue");
            Check(pro != null && pro.Count >= 6, $"처음 시작 스토리 {pro?.Count ?? 0}줄 (Story 표 prologue)");
            int stN = 0, minL = 99;
            for (int s = 1; s <= Tables.Stages.Count; s++) { var l = Tables.StoryOf("s" + s); if (l != null && l.Count > 0) { stN++; minL = Mathf.Min(minL, l.Count); } }
            Check(stN == Tables.Stages.Count && minL >= 2, $"스테이지 스토리 {stN} / {Tables.Stages.Count} (가장 짧은 것 {minL}줄)");
            Check(Tables.LobbyTalk.Count >= 15, $"로비 말풍선 대사 {Tables.LobbyTalk.Count}개");
            {
                var miss = new HashSet<char>(); var who = new HashSet<string>();
                void Scan(string s) { foreach (char ch in s) if (ch > ' ' && font != null && !font.HasCharacter(ch)) miss.Add(ch); }
                foreach (var l in Tables.Story.Values) foreach (var r in l) { Scan(r.text); who.Add(r.who); }
                foreach (var r in Tables.LobbyTalk) Scan(r.text);
                Check(font != null && miss.Count == 0, $"스토리 · 말풍선 글자가 모두 글꼴(주아)에 있음{(miss.Count > 0 ? " — 없음: " + string.Join(" ", miss) : "")}");
                bool whoOk = true; foreach (var w in who) if (w != "narr" && !Tables.Strings.ContainsKey("who_" + w)) whoOk = false;
                Check(whoOk, $"인물 이름이 글자 표에 있음 ({string.Join(", ", who)})");
            }

            // ---------- 2) 처음 시작 스토리 (타이틀 [게임 시작] → 스토리 → 로비) ----------
            S.seen.RemoveAll(x => x.StartsWith("story:")); S.storySkip = false;
            Check(StoryView.Want("prologue"), "처음이면 처음 시작 스토리를 보여 줌");
            Title.I.Enter(); yield return Wait(0.5f);
            var v = StoryView.Cur;
            Check(v != null && Game.Lobby == null, "타이틀 → 스토리 (로비보다 먼저)");
            if (v != null)
            {
                Check(v.CurWho == "narr" && !FindDeep("Story_prologue/Portrait").activeSelf, "첫 줄 = 해설 (얼굴 없이 가운데)");
                yield return Wait(2.5f); Capture("r9_01_prologue_narr");
                v.Next(); yield return Wait(0.05f);
                bool typing = v.Typing; v.Next(); yield return null;
                Check(typing && !v.Typing && v.Index == 1, "글자가 나오는 중에 누르면 한 번에 다 보임 (다음 줄로 안 넘어감)");
                while (v != null && v.CurWho != "rabbit" && v.Index < v.LineCount - 1) { v.Next(); v.Next(); yield return null; }
                yield return Wait(3f);
                var face = FindDeep("Story_prologue/Portrait/Face")?.GetComponent<Image>();
                Check(v.CurWho == "rabbit" && face && face.sprite && face.sprite.name.StartsWith("Bg/moon_rabbit") && GameObject.Find("NameTag").GetComponentInChildren<Text>().text == L.T("who_rabbit"),
                      "달토끼 줄: 왼쪽 동그라미 얼굴(달토끼) · 이름표");
                Capture("r9_02_prologue_rabbit");
                while (v != null && v.CurWho != "queen" && v.Index < v.LineCount - 1) { v.Next(); v.Next(); yield return null; }
                yield return Wait(3f); Capture("r9_03_prologue_queen");
                while (v != null && v.CurWho != "capy" && v.Index < v.LineCount - 1) { v.Next(); v.Next(); yield return null; }
                yield return Wait(2.5f); Capture("r9_04_prologue_capy");
                for (int i = 0; i < 40 && StoryView.Cur != null; i++) { StoryView.Cur.Next(); yield return null; }
                yield return Wait(0.3f);
            }
            Check(StoryView.Cur == null && Game.Lobby != null, "마지막 [다음] → 로비");
            Check(!StoryView.Want("prologue") && S.seen.Contains("story:prologue"), "처음 시작 스토리는 한 번만");
            Popup.CloseAll();

            // ---------- 3) 로비 말풍선 ----------
            S.best = 12; S.stage = 13; S.stamina = 99; S.gold = 1e9; S.dia = 100000;
            Game.ToLobby(); yield return Wait(0.5f); Popup.CloseAll(); yield return null;
            var lt = LobbyTalk.I;
            Check(lt != null, "로비 전투 탭에 대장 카피 말풍선");
            if (lt != null)
            {
                Tables.SetC("Lobby_Talk_Gap_Min", 0.3f); Tables.SetC("Lobby_Talk_Gap_Max", 0.4f);
                int c0 = lt.Count; float w0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - w0 < 8f && lt.Count == c0) yield return null;
                Check(lt.Count > c0, "몇 초마다 저절로 한마디");
                lt.Say("다음은 스테이지 {stage}! 준비됐어."); yield return Wait(0.4f);
                Check(lt.Showing && lt.Said == "다음은 스테이지 13! 준비됐어.", $"{{stage}} = 다음 스테이지 ({lt.Said})");
                var bub = GameObject.Find("Bubble"); var walker = LobbyWalker.I;
                if (bub && walker)
                {
                    var r = UiRect(bub); var cam = World.I.cam; float halfW = cam.orthographicSize * cam.aspect;
                    Check(r.yMin > walker.transform.position.y + 0.8f && r.xMin > cam.transform.position.x - halfW && r.xMax < cam.transform.position.x + halfW,
                          $"말풍선이 대장 카피 머리 위 · 화면 안 (아래 끝 {r.yMin:0.0} · 카피 {walker.transform.position.y:0.0})");
                }
                Capture("r9_05_lobby_talk");
                Settings.Open(); yield return Wait(lt.Count > 0 ? 0.1f : 0f);
                c0 = lt.Count; yield return Wait(1.2f);
                Check(!lt.Showing && lt.Count == c0, "창이 떠 있으면 말풍선 쉼");
                Popup.CloseAll();
                Tables.SetC("Lobby_Talk_Gap_Min", 6); Tables.SetC("Lobby_Talk_Gap_Max", 12);
                for (int f = 0; f < 16; f++) { if (f == 2) lt.Say(); yield return Wait(0.12f); Capture($"r9_talk_{f:00}", true); }
            }

            // ---------- 4) 설정: 스토리 건너뛰기 ----------
            Settings.Open(); yield return Wait(0.3f);
            {
                bool row = false; foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) if (t.text == L.T("set_story_skip")) row = true;
                Check(row, "설정 창에 \"스토리 건너뛰기\"");
                Capture("r9_06_settings");
            }
            Popup.CloseAll();

            // ---------- 5) 전투 시작 → 스테이지 스토리 → 전투 ----------
            S.seen.Remove("story:s13");
            int stam0 = S.stamina;
            Game.TryStart(13, false); yield return Wait(0.6f);
            v = StoryView.Cur;
            Check(v != null && B == null && GameObject.Find("StageCard") != null && S.stamina < stam0, "[전투 시작] → 스태미나 냄 → 스테이지 스토리 (위에 스테이지 카드)");
            if (v != null)
            {
                var st = Tables.Stage(13); bool nameOk = false, weakOk = GameObject.Find("Elem_" + st.weak) != null;
                foreach (var t in GameObject.Find("StageCard").GetComponentsInChildren<Text>()) if (t.text == st.name) nameOk = true;
                Check(nameOk && weakOk && GameObject.Find("Boss") != null, $"스테이지 카드: 번호 · 이름({st.name}) · 약점({Elem.Name(st.weak)}) · 보스 그림");
                yield return Wait(2.5f); Capture("r9_07_stage_story");
                while (v.Index < v.LineCount - 1) { v.Next(); v.Next(); yield return null; }
                yield return Wait(2.5f);
                { var nb = FindDeep("Story_s13/Box/Next"); Check(nb && UIKit.LabelOf(nb.GetComponent<Button>()).text == L.T("story_start"), "마지막 줄 단추 = \"전투 시작!\""); }
                Capture("r9_08_stage_story_last");
                v.Next(); v.Next(); yield return Wait(0.4f);
            }
            Check(StoryView.Cur == null && B != null && B.stageNo == 13, "스토리 끝 → 스테이지 13 전투");
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f); Popup.CloseAll();
            Check(!StoryView.Want("s13"), "같은 스테이지 두 번째부터는 바로 전투 (Story_Stage_Repeat 0)");
            Tables.SetC("Story_Stage_Repeat", 1);
            Check(StoryView.Want("s13"), "Story_Stage_Repeat 1 = 전투 시작마다");
            Game.TryStart(13, false); yield return Wait(0.3f);
            GameObject.Find("Skip")?.GetComponent<Button>().onClick.Invoke(); yield return Wait(0.4f);
            Check(StoryView.Cur == null && B != null, "[건너뛰기] → 바로 전투");
            Tables.SetC("Story_Stage_Repeat", 0);
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f); Popup.CloseAll();
            S.seen.Remove("story:s14"); S.storySkip = true;
            Check(!StoryView.Want("s14") && !StoryView.Want("prologue"), "설정 \"스토리 건너뛰기\" 켬 → 스토리 안 보임");
            S.storySkip = false;
            Game.TryStart(14, false); yield return Wait(0.3f);
            BackKey.Press(); yield return Wait(0.4f);
            Check(StoryView.Cur == null && B != null, "스토리 중 폰 뒤로 가기 = 건너뛰기");
            StoryView.Off = true;

            // ---------- 6) 전투: 아래(돔) → 위(적) 방식 복구 ----------
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f); Popup.CloseAll();
            Check(World.V, "Config Battle_Vertical 1 (세로 전장)");
            Game.StartStage(10, 7); yield return Wait(0.3f);
            {
                var tw = R("Tower"); var gr = R("Ground");
                Check(tw && tw.enabled && tw.sprite.name.StartsWith("Bg/dome") && Mathf.Abs(tw.transform.position.x) < 0.01f, $"기지 = 아래 가운데 돔 ({(tw ? tw.sprite.name : "-")})");
                Check(gr && gr.sprite.name.EndsWith("ground_earth_v"), $"바닥 = 가운데 지구 윗면 ({(gr ? gr.sprite.name : "-")})");
                var g = B.Guardian;
                Check(g && Mathf.Abs(g.transform.position.x) < 0.05f && g.transform.position.y < World.ShieldY, $"대장 카피 = 돔 앞 가운데 ({(g ? g.transform.position.ToString("0.00") : "-")})");
                bool slotsOk = true;
                for (int s = 1; s <= 4; s++) { var p = World.SlotPos(s); if (Mathf.Abs(Mathf.Abs(p.x) - (s <= 2 ? 2.72f : 4.3f)) > 0.01f || p.y > World.GroundY || p.y < World.GroundY - 0.4f) slotsOk = false; }
                Check(slotsOk, $"터렛 칸 4개 = 땅 위 한 줄 (x ±2.72 · ±4.3 · y {World.SlotPos(1).y:0.00} ~ {World.SlotPos(3).y:0.00})");
            }
            {
                var dirs = new List<Enemy>();
                for (int i = 0; i < 12; i++) { var p = B.SpawnPoint(); var e = B.Spawn("pebble", p); if (e) { e.hp = e.maxHp = 1e12; dirs.Add(e); } }
                bool down = true, top = true; foreach (var e in dirs) { if (e.Dir.y > -0.99f) down = false; if (e.Pos.y < World.SpawnY - 1.2f) top = false; }
                Check(down && top, "적은 위 가장자리에서 나와 곧게 아래로");
                var near = B.Spawn("pebble", new Vector2(1f, World.ShieldY + 0.6f)); near.hp = near.maxHp = 1e12;
                double hp0 = B.baseHp; for (int i = 0; i < 120; i++) B.Tick(Battle.StepSec);
                Check(B.baseHp < hp0, $"방어막 높이(Shield_Y {World.ShieldY:0.0})에 닿은 적이 기지를 때림 ({hp0:0} → {B.baseHp:0})");
                foreach (var e in dirs) e.dead = true; near.dead = true;
            }
            Force("new_laser"); Force("new_bolt"); Force("new_drone"); Force("new_missile"); yield return null;
            {
                bool rowOk = true; foreach (var t in B.run.turrets) if (t.slot > 0 && t.unit && Mathf.Abs(t.unit.transform.position.y - World.SlotPos(t.slot).y) > 0.2f) rowOk = false;
                Check(B.run.turrets.Count == 5 && rowOk, $"터렛 4개가 땅 위 칸에 놓임 ({B.run.turrets.Count - 1}개)");
                var hd = B.run.turrets.Find(t => t.unit && t.row.id == "laser")?.unit;
                Check(hd == null || Mathf.Abs(hd.HeadDeg - 90f) < 1f, $"터렛 머리는 쉴 때 위(90°) ({(hd ? hd.HeadDeg : -1):0})");
                var f = new Field(B.run.Get("drone"), 1.5f, 0.9f, 0.5f, 0.4f, 1); B.Add(f);
                Check(!f.Vertical && f.Area.width > 9f, $"끈끈이 장판 = 예전 가로 띠 (폭 {f.Area.width:0.0})");
            }
            FF(6f, 2); yield return Wait(0.3f);
            Capture("r9_09_battle");
            FF(25f, 2); yield return Wait(0.3f);
            Capture("r9_10_battle_more");
            for (int f2 = 0; f2 < 16; f2++) { FF(0.12f, 2); yield return null; Capture($"r9_battle_{f2:00}", true); }
            // 되돌리기 스위치: Battle_Vertical 0 = 사령탑
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f); Popup.CloseAll();
            Tables.SetC("Battle_Vertical", 0);
            Game.StartStage(10, 7); yield return Wait(0.3f);
            Check(!World.V && R("Tower").sprite.name.StartsWith("Bg/tower") && Mathf.Abs(World.SlotPos(1).x - World.SlotXPos) < 0.01f, "Battle_Vertical 0 → 사령탑으로 되돌아감");
            Capture("r9_11_battle_tower_switch");
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f); Popup.CloseAll();
            Tables.SetC("Battle_Vertical", 1);
            Game.StartStage(10, 7); yield return Wait(0.3f);
            Check(World.V && R("Tower").sprite.name.StartsWith("Bg/dome"), "다시 1 → 돔");
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f); Popup.CloseAll();
            {
                bool names = true; foreach (var fr in Tables.Frames) if (fr.name.Contains("사령탑")) names = false;
                var ic = Art.FrameIcon(Tables.Frame("gold"));
                Check(names && ic && ic.name.Contains("framed_gold"), $"기지 외형 이름 · 목록 그림 = 돔 ({Tables.Frame("gold").name} · {(ic ? ic.name : "-")})");
            }

            // ---------- 7) 도감 ----------
            Game.Lobby.SetTab(2); yield return Wait(0.3f);
            Check(GameObject.Find("Codex") && GameObject.Find("Preset"), "장비 탭에 [도감] · [회로 프리셋]");
            Capture("r9_12_gear_tab");
            {
                var g = Gear.NewGear(5, "weapon"); var ch = Gear.NewChip(4, Tables.Chips[0].id);
                Check(Codex.HasGear("weapon", 5) && Codex.ChipBest(Tables.Chips[0].id) >= 4, "새로 얻은 장비 · 회로가 도감에 기록");
                Gear.Scrap(g);
                Check(Codex.HasGear("weapon", 5), "분해해도 도감엔 남음");
                for (int q = 1; q <= 4; q++) Gear.NewGear(q, Tables.GearParts[q % Tables.GearParts.Count].part);
                for (int i = 1; i < 12; i++) Gear.NewChip(1 + i % 6, Tables.Chips[i * 3 % Tables.Chips.Count].id);
            }
            CodexUI.view = 0; CodexUI.Open(); yield return Wait(0.4f);
            string topG = "G_weapon_" + Tables.GearTop;     // 11-22 오픈 스펙: 신화(6)까지
            Check(GameObject.Find("G_weapon_5") != null && GameObject.Find(topG) != null, "장비 도감: 부위 x 등급 칸");
            Check(GameObject.Find(topG)?.transform.Find("Lock") != null && GameObject.Find("G_weapon_5").transform.Find("Lock") == null, "못 얻은 칸은 흐리게 + 자물쇠");
            Capture("r9_13_codex_gear");
            CodexUI.GearInfo(Tables.Part("weapon"), 5); yield return Wait(0.3f); Capture("r9_14_codex_gear_info");
            Popup.CloseAll(); CodexUI.view = 1; CodexUI.Open(); yield return Wait(0.4f);
            Check(GameObject.Find("C_" + Tables.Chips[0].id) != null, $"회로 도감: 종류마다 한 줄 ({Tables.Chips.Count}종)");
            Capture("r9_15_codex_chip");
            CodexUI.ChipInfo(Tables.Chips[0]); yield return Wait(0.3f); Capture("r9_16_codex_chip_info");
            Popup.CloseAll();

            // ---------- 8) 회로 프리셋 3개 ----------
            {
                Check(ChipPreset.Count == 3, $"회로 프리셋 {ChipPreset.Count}개");
                S.best = Mathf.Max(S.best, 60);
                foreach (var c in S.chips) c.at = -1;
                int put = 0;
                foreach (var c in S.chips) { var (p, s) = Gear.FreeSlotFor(c); if (p >= 0 && put < 4) { Gear.PutChip(c, p, s); put++; } }
                var before = new Dictionary<int, int>(); foreach (var c in S.chips) if (c.at >= 0) before[c.uid] = c.at;
                ChipPreset.Save(0);
                Check(ChipPreset.Size(0) == before.Count && before.Count >= 2, $"프리셋 1 저장 = 지금 끼운 회로 {before.Count}개");
                foreach (var c in S.chips) c.at = -1;
                int miss = ChipPreset.Load(0);
                bool same = miss == 0; foreach (var c in S.chips) if ((before.TryGetValue(c.uid, out var at) ? at : -1) != c.at) same = false;
                Check(same, "불러오기 → 같은 회로를 같은 자리에");
                ChipPreset.Save(1);
                foreach (var c in S.chips) c.at = -1;
                var gone = S.chips.Find(c => before.ContainsKey(c.uid)); Gear.ScrapChip(gone);
                miss = ChipPreset.Load(0);
                Check(miss == 1, $"없어진 회로는 빼고 끼움 (빠짐 {miss})");
                Check(ChipPreset.Load(2) == -1, "빈 프리셋 불러오기 = 아무것도 안 바뀜");
                CodexUI.Presets(null); yield return Wait(0.3f);
                Check(GameObject.Find("Preset3") != null, "프리셋 창: 3줄 (저장 · 불러오기)");
                Capture("r9_17_presets");
                Popup.CloseAll();
            }

            running = false;
        }
    }
}
