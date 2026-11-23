using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-22 오픈 스펙 숨김 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunOpen --play
    /// 「오픈_스펙_기획서.html」 v0.2 — 코스튬 10 · 탈것 10 · 돔 기본 + 4 만 보임 · 회로 전설(5) · 장비 신화(6) 까지 · 상자 최대 회로 영웅 · 장비 전설
    /// 화면 = Logs/PlayTest/op_*.png
    /// </summary>
    public partial class PlayTestRunner
    {
        static readonly string[] OpCostume = { "basic", "duck", "sleepy", "rain", "dino", "ninja", "lion", "panda", "king", "onsen" };
        static readonly string[] OpMount = { "none", "duckboat", "star", "ladybug", "broom", "seahorse", "corgi", "surf", "unicorn", "tub" };
        static readonly string[] OpFrame = { "basic", "prism", "thorn", "bloom", "frost" };

        IEnumerator OpenSteps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            var S = SaveData.Cur;
            var font = Object.FindAnyObjectByType<Text>()?.font;
            Lobby.NoIntro = true; Popup.CloseAll();
            S.best = Mathf.Max(S.best, 30); S.stamina = 999;

            // ---------- 1) 표 ----------
            {
                string On(System.Collections.Generic.IEnumerable<string> ids) => string.Join(",", ids);
                var c = Tables.Costumes.Where(r => r.enabled).Select(r => r.id).ToArray();
                var m = Tables.Mounts.Where(r => r.enabled).Select(r => r.id).ToArray();
                var f = Tables.Frames.Where(r => r.enabled).Select(r => r.id).ToArray();
                Check(c.OrderBy(x => x).SequenceEqual(OpCostume.OrderBy(x => x)) && Tables.Costumes.Count == 27,
                      $"코스튬 보임 {c.Length} / 전체 {Tables.Costumes.Count} (숨김 {Tables.Costumes.Count - c.Length}) — {On(c)}");
                Check(m.OrderBy(x => x).SequenceEqual(OpMount.OrderBy(x => x)) && Tables.Mounts.Count == 17,
                      $"탈것 보임 {m.Length} / 전체 {Tables.Mounts.Count} (숨김 {Tables.Mounts.Count - m.Length}) — {On(m)}");
                Check(f.OrderBy(x => x).SequenceEqual(OpFrame.OrderBy(x => x)) && Tables.Frames.Count == 7,
                      $"기지 돔 보임 {f.Length} / 전체 {Tables.Frames.Count} — {On(f)} (숨김 {On(Tables.Frames.Where(r => !r.enabled).Select(r => r.id))})");
                Check(Tables.Turrets.Count == 18 && Tables.Skins.Count == 17 && Tables.Chips.Count == 86, $"표 줄은 그대로: 터렛 {Tables.Turrets.Count} · 스킨 {Tables.Skins.Count} · 회로 {Tables.Chips.Count}");
                Check(Tables.GearTop == 6 && Tables.ChipTop == 5 && Store.BoxMaxFor(true) == 5 && Store.BoxMaxFor(false) == 4,
                      $"등급 상한: 장비 {Tables.Grade(Tables.GearTop).name} · 회로 {Tables.Grade(Tables.ChipTop).name} / 상자 최대: 장비 {Tables.Grade(Store.BoxMaxFor(true)).name} · 회로 {Tables.Grade(Store.BoxMaxFor(false)).name}");
            }

            // 숨긴 외형을 하나도 안 가진 상태로 (시험 세이브는 끝나면 되돌림)
            S.costumes.RemoveAll(id => Tables.Costume(id) is { enabled: false }); S.mounts.RemoveAll(id => Tables.Mount(id) is { enabled: false });
            S.costume = "basic"; S.mount = "none";
            foreach (var fr in Tables.Frames) if (!fr.enabled) S.SetFrameStar(fr.id, 0);
            if (Tables.Frame(S.frame) is { enabled: false }) S.frame = "basic";

            // ---------- 2) 외형 탭: 숨긴 것은 목록에 없음 ----------
            Game.ToLobby(); yield return Wait(0.3f); Popup.CloseAll();
            {
                Looks.Open(2); yield return Wait(0.5f);
                int on = Tables.Costumes.Count(r => GameObject.Find("C_" + r.id) && r.enabled), off = Tables.Costumes.Count(r => GameObject.Find("C_" + r.id) && !r.enabled);
                Check(on == 10 && off == 0, $"코스튬 목록 {on}줄 · 숨긴 것 {off}줄");
                Capture("op_01_costume");
                yield return ScrollLooksEnd("op_01b_costume_end");
                Looks.Open(3); yield return Wait(0.5f);
                on = Tables.Mounts.Count(r => GameObject.Find("M_" + r.id) && r.enabled); off = Tables.Mounts.Count(r => GameObject.Find("M_" + r.id) && !r.enabled);
                Check(on == 10 && off == 0, $"탈것 목록 {on}줄 · 숨긴 것 {off}줄");
                Capture("op_02_mount");
                yield return ScrollLooksEnd("op_02b_mount_end");
                Looks.Open(1); yield return Wait(0.5f);
                on = Tables.Frames.Count(r => GameObject.Find("F_" + r.id) && r.enabled); off = Tables.Frames.Count(r => GameObject.Find("F_" + r.id) && !r.enabled);
                Check(on == 5 && off == 0, $"기지 돔 목록 {on}줄 · 숨긴 것 {off}줄");
                Capture("op_03_dome");
                Looks.Open(0); yield return Wait(0.4f);
                Capture("op_04_skin");
            }
            // 이미 가진 숨긴 외형은 보임 (테스트 때 산 것을 잃지 않게)
            {
                S.dia += 10000;
                var wiz = Tables.Costume("wizard"); var ufo = Tables.Mount("ufo");
                S.costumes.Add("wizard"); S.mounts.Add("ufo"); S.SetFrameStar("gold", 1);
                Looks.Open(2); yield return Wait(0.4f);
                bool c1 = GameObject.Find("C_wizard") && !GameObject.Find("C_pirate");
                Looks.Open(3); yield return Wait(0.4f);
                bool m1 = GameObject.Find("M_ufo") && !GameObject.Find("M_cloud");
                Looks.Open(1); yield return Wait(0.4f);
                bool f1 = GameObject.Find("F_gold") && !GameObject.Find("F_starsea");
                Check(c1 && m1 && f1 && !wiz.enabled && !ufo.enabled, "이미 가진 숨긴 외형(마법사 · UFO · 황금 돔)은 목록에 보임, 안 가진 숨긴 것은 그대로 숨김");
                S.costumes.Remove("wizard"); S.mounts.Remove("ufo"); S.SetFrameStar("gold", 0);
                Popup.CloseAll();
            }

            // ---------- 3) 등급 상한 · 합성 ----------
            {
                var g7 = Gear.NewGear(7, "weapon"); var c7 = Gear.NewChip(7, Tables.Chips[0].id);
                Check(g7.grade == 6 && c7.q == 5, $"숨긴 등급은 안 생김: 장비 7 → {Tables.Grade(g7.grade).name} · 회로 7 → {Tables.Grade(c7.q).name}");
                S.gear.Remove(g7); S.chips.Remove(c7);
                var gs = Enumerable.Range(0, 3).Select(_ => Gear.NewGear(6, "helmet")).ToList();
                var cs = Enumerable.Range(0, 3).Select(_ => Gear.NewChip(5, Tables.Chips[1].id)).ToList();
                Check(Gear.MergeSet(gs[0]) == null && Gear.ChipMergeSet(cs[0]) == null, "맨 위 등급(장비 신화 · 회로 전설)은 더 합성 안 됨");
                foreach (var x in gs) S.gear.Remove(x); foreach (var x in cs) S.chips.Remove(x);
                var g5 = Enumerable.Range(0, 3).Select(_ => Gear.NewGear(5, "boots")).ToList();
                var c4 = Enumerable.Range(0, 3).Select(_ => Gear.NewChip(4, Tables.Chips[2].id)).ToList();
                var gm = Gear.Merge(g5[0]); var cm = Gear.MergeChip(c4[0]);
                Check(gm != null && gm.grade == 6 && cm != null && cm.q == 5, $"합성으로만: 장비 전설 x3 → {(gm != null ? Tables.Grade(gm.grade).name : "-")} · 회로 영웅 x3 → {(cm != null ? Tables.Grade(cm.q).name : "-")}");
                if (gm != null) S.gear.Remove(gm); if (cm != null) S.chips.Remove(cm);
            }

            // ---------- 4) 상자 · 드롭 · 보상 ----------
            {
                var rnd = new System.Random(22);
                int gTop = 0, cTop = 0;
                for (int i = 0; i < 20000; i++) { gTop = Mathf.Max(gTop, Store.BoxGrade(rnd, true)); cTop = Mathf.Max(cTop, Store.BoxGrade(rnd, false)); }
                Check(gTop == 5 && cTop == 4, $"상자 2만 번: 장비 최고 {Tables.Grade(gTop).name} · 회로 최고 {Tables.Grade(cTop).name}");
                S.chipPity = Store.PityN - 1; S.dia += 2000;
                var got = Store.Buy(Tables.ShopItems.Find(i => i.kind == "chip_box"));
                Check(got != null && got[0].grade == 4 && S.chipPity == 0, $"회로 상자 천장 {Store.PityN}번째 = {Tables.Grade(got?[0].grade ?? 1).name} 확정 · 셈 다시 0");
                int best0 = S.best; S.best = 300; int dg = 0, dc = 0;
                for (int i = 0; i < 80; i++)
                {
                    int g = Gear.DropGrade(300, true, rnd);
                    dg = Mathf.Max(dg, Gear.NewGear(Mathf.Min(g, Store.BoxMaxFor(true)), null, rnd).grade);
                    dc = Mathf.Max(dc, Gear.NewChip(Mathf.Min(g, Store.BoxMaxFor(false)), null, rnd).q);
                }
                Gear.Give("gear", 7, rnd); int rg = S.gear[S.gear.Count - 1].grade;
                Gear.Give("chip", 7, rnd); int rc = S.chips[S.chips.Count - 1].q;
                S.best = best0;
                Check(dg == 5 && dc == 4 && rg == 5 && rc == 4, $"전투 드롭(엘리트 스테이지 300): 장비 최고 {Tables.Grade(dg).name} · 회로 최고 {Tables.Grade(dc).name} · 출석/미션 보상 7등급 → 장비 {Tables.Grade(rg).name} · 회로 {Tables.Grade(rc).name}");
                S.gear.RemoveAll(x => x.grade >= 4 && !x.on); S.chips.RemoveAll(x => x.q >= 4 && x.at < 0);
            }

            // ---------- 5) 상점 글자 · 확률 창 ----------
            {
                ShopTab.sub = 1; Game.Lobby.SetTab(5); yield return Wait(0.4f);
                bool pg = false, pc = false;
                foreach (var tx in Game.Lobby.shopTab.GetComponentsInChildren<Text>())
                {
                    if (tx.text == Store.PityText("gear_box") && tx.text.Contains(Tables.Grade(5).name)) pg = true;
                    if (tx.text == Store.PityText("chip_box") && tx.text.Contains(Tables.Grade(4).name)) pc = true;
                }
                Check(pg && pc, $"상자 줄 천장 글자: \"{Store.PityText("gear_box")}\" · \"{Store.PityText("chip_box")}\"");
                Capture("op_05_shop_box");
                ShopTab.RateOpen(); yield return Wait(0.3f);
                double sg = 0, sc = 0; bool dash5 = false, none6 = GameObject.Find("RG6") == null;
                for (int g = 1; g <= 7; g++)
                {
                    var a = GameObject.Find("RG" + g)?.GetComponent<Text>(); var b = GameObject.Find("RC" + g)?.GetComponent<Text>();
                    if (a && a.text.EndsWith("%")) sg += double.Parse(a.text.TrimEnd('%'), System.Globalization.CultureInfo.InvariantCulture);
                    if (b && b.text.EndsWith("%")) sc += double.Parse(b.text.TrimEnd('%'), System.Globalization.CultureInfo.InvariantCulture);
                    if (g == 5 && b && b.text == "-") dash5 = true;
                }
                Check(System.Math.Abs(sg - 100) < 0.1 && System.Math.Abs(sc - 100) < 0.1 && dash5 && none6,
                      $"확률 창: 장비 상자 합 {sg:0.##}% · 회로 상자 합 {sc:0.##}% (회로 전설 = \"-\" · 신화 줄 없음)");
                Capture("op_06_rates");
                Popup.CloseAll();
            }

            // ---------- 6) 도감 ----------
            {
                CodexUI.view = 0; CodexUI.Open(); yield return Wait(0.4f);
                bool g6 = GameObject.Find("G_weapon_6") != null, g7 = GameObject.Find("G_weapon_7") != null;
                Check(g6 && !g7 && Codex.GearAll == 36, $"장비 도감: 신화까지 (부위마다 6칸 · 모두 {Codex.GearAll}칸)");
                Capture("op_07_codex_gear");
                Popup.CloseAll(); CodexUI.ChipInfo(Tables.Chips[0]); yield return Wait(0.3f);
                bool q5 = GameObject.Find("Q5") != null, q6 = GameObject.Find("Q6") != null;
                Check(q5 && !q6, "회로 도감 설명: 전설까지만 (신화 · 궁극 줄 없음)");
                Capture("op_08_codex_chip");
                Popup.CloseAll();
            }

            // ---------- 7) 글자가 글꼴에 있는지 ----------
            {
                var bad = new System.Collections.Generic.HashSet<char>();
                foreach (var s in new[] { Store.PityText("gear_box"), Store.PityText("chip_box"), ShopTab.BoxMaxNote, L.T("rate_gear"), L.T("rate_chip"),
                                          L.F("pity_note", 55, "전설", "영웅") })
                    foreach (char ch in s) if (ch > ' ' && font != null && !font.HasCharacter(ch)) bad.Add(ch);
                Check(bad.Count == 0, $"새 글자 글꼴 확인 ({ShopTab.BoxMaxNote})" + (bad.Count > 0 ? " 없는 글자: " + string.Join("", bad) : ""));
            }

            // ---------- 8) 11-23 터렛은 찌릿 코일까지만 (스테이지 30 = 숨기지 않았다면 다 열렸을 진행) ----------
            {
                string[] openT = { "guardian", "rail", "laser", "drone", "hive", "missile", "bolt", "cannon", "focus", "vortex", "wheel", "tesla" };
                string[] hideT = { "boomerang", "frost_cannon", "flamethrower", "plasma_saw", "meteor_mortar", "prism_beacon" };
                var onT = Tables.Turrets.Where(t => t.enabled).Select(t => t.id).ToArray();
                Check(onT.SequenceEqual(openT), $"터렛 보임 {onT.Length} / 전체 {Tables.Turrets.Count} — 마지막 {onT.LastOrDefault()}");
                Check(hideT.All(id => !S.Unlocked(Tables.TurretById[id])) && openT.All(id => S.Unlocked(Tables.TurretById[id])), "스테이지 30 에서도 숨긴 6종은 안 열림 (조각 · 강화 대상 아님)");

                Popup.CloseAll(); Game.Lobby.SetTab(1); yield return Wait(0.4f);
                int rowOn = openT.Count(id => GameObject.Find("Row_" + id)), rowOff = hideT.Count(id => GameObject.Find("Row_" + id));
                Check(rowOn == 12 && rowOff == 0, $"터렛 탭 {rowOn}줄 · 숨긴 것 {rowOff}줄");
                Capture("op_09_turret");
                var trs = GameObject.Find("Row_tesla")?.GetComponentInParent<ScrollRect>();
                if (trs) { trs.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases(); yield return Wait(0.2f); }
                Capture("op_09b_turret_end");

                Looks.Open(0); yield return Wait(0.4f);
                int skOn = Tables.Skins.Count(k => GameObject.Find("S_" + k.id) && Tables.TurretOpen(k.turret)), skOff = Tables.Skins.Count(k => GameObject.Find("S_" + k.id) && !Tables.TurretOpen(k.turret));
                Check(skOn == 11 && skOff == 0, $"터렛 외형 {skOn}줄 · 숨긴 것 {skOff}줄");
                Popup.CloseAll();

                CodexUI.view = 1; CodexUI.Open(); yield return Wait(0.4f);
                int cdOn = Tables.Chips.Count(x => GameObject.Find("C_" + x.id) && Tables.TurretOpen(x.turret)), cdOff = Tables.Chips.Count(x => GameObject.Find("C_" + x.id) && !Tables.TurretOpen(x.turret));
                Check(cdOn == 68 && cdOff == 0 && Codex.ChipAll == 68, $"회로 도감 {cdOn}줄 · 숨긴 것 {cdOff}줄 · 모두 {Codex.ChipAll}종");
                Popup.CloseAll(); CodexUI.view = 0;

                var chipRnd = new System.Random(23); int badChip = 0;
                for (int i = 0; i < 2000; i++) { var x = Gear.NewChip(1, null, chipRnd); if (!Tables.TurretOpen(Tables.ChipById[x.id].turret)) badChip++; S.chips.Remove(x); }
                Check(badChip == 0, $"회로 2천 번: 숨긴 터렛 회로 {badChip}개");

                foreach (var id in hideT) S.seen.Remove("t:" + id);
                var nu = Meta.NewUnlock();
                Check(nu == null || nu.enabled, $"새 터렛 알림에 숨긴 터렛 없음 ({nu?.name ?? "없음"})");

                Game.StartStage(30, 2311); yield return null; Tutorial.CloseCur();
                var newCards = Tables.Cards.Where(x => x.kind == "new").ToList();
                bool hideOff = newCards.Where(x => !Tables.TurretOpen(x.turret)).All(x => !B.run.Eligible(x));
                bool teslaOn = B.run.Eligible(Tables.CardById["new_tesla"]);
                int badCard = 0;
                for (int i = 0; i < 500; i++) foreach (var x in B.run.Roll(3)) if (!Tables.TurretOpen(x.turret)) badCard++;
                Check(hideOff && teslaOn && badCard == 0, $"전투 카드: 숨긴 터렛 안 나옴 · 찌릿 코일은 나옴 (500번 뽑기에 숨긴 카드 {badCard}장)");
            }
            Game.ToLobby(); yield return Wait(0.2f);
            running = false;
        }
    }
}
