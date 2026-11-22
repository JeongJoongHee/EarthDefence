using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-20 코스튬 · 탈것 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR10 --play
    /// 화면 = Logs/PlayTest/r10_*.png (보고서 사본은 캡쳐본 모음/코스튬_탈것_캡처/)
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator R10Steps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            Lobby.NoIntro = true;
            yield return Wait(0.4f);
            Popup.CloseAll();
            var font = Object.FindAnyObjectByType<Text>()?.font;
            string[] fronts = { "plane", "seahorse", "unicorn", "hoverbike", "trex", "tub", "cloud", "ufo", "duckboat", "whale", "corgi" };

            // ---------- 1) 표 · 그림 ----------
            Check(Tables.Costumes.Count == 27 && Tables.Costumes[0].id == "basic", $"코스튬 {Tables.Costumes.Count}종 (첫 줄 = 기본 우주 대원)");
            Check(Tables.Mounts.Count == 17 && Tables.Mounts[0].id == "none", $"탈것 {Tables.Mounts.Count}종 + 없음 (첫 줄 = 탈것 없음)");
            {
                var miss = new List<string>();
                foreach (var c in Tables.Costumes)
                {
                    if (!Art.CostumeFloat(c.id) || !Art.CostumeBody(c.id) || !Art.CostumeGun(c.id)) miss.Add(c.id);
                    if (c.id != "basic" && (!Art.CostumeBody(c.id).name.Contains("costume/" + c.id) || !Resources.Load<Texture2D>("Art/Char/costume/" + c.id + "_ui"))) miss.Add(c.id + "(몸/ui)");
                }
                foreach (var m in Tables.Mounts) if (m.id != "none" && !Art.Mount(m.id)) miss.Add(m.id);
                bool fr = true; foreach (var f in fronts) if (!Art.MountFront(f)) fr = false;
                Check(miss.Count == 0, $"코스튬 몸 · 총 · 둥실 그림 · 탈것 그림이 모두 있음{(miss.Count > 0 ? " — 없음: " + string.Join(", ", miss) : "")}");
                Check(fr && Art.MountFront("surf") == null && Art.Mount("none") == null, "올라타는 탈것(비행기 · 해마 · 유니콘 · 바이크 · 티라노)은 앞 층(몸 앞)이 있음");
                bool liftOk = true; foreach (var m in Tables.Mounts) if (m.id != "none" && (m.lift <= 0.1f || m.lift > 1.5f)) liftOk = false;
                Check(liftOk, "탈것마다 올라타는 높이(Lift) 0.1 ~ 1.5칸");
                var bad = new HashSet<char>();
                void Scan(string s) { foreach (char ch in s) if (ch > ' ' && font != null && !font.HasCharacter(ch)) bad.Add(ch); }
                foreach (var r in Tables.Costumes) Scan(r.name); foreach (var r in Tables.Mounts) Scan(r.name);
                foreach (var k in new[] { "looks_costume", "looks_mount", "look_now", "look_preview", "look_back_now", "look_tap_tip", "look_stat_none", "look_no_mount", "look_got" }) Scan(L.T(k));
                Check(font != null && bad.Count == 0, $"코스튬 · 탈것 이름 · 글자가 글꼴(주아)에 있음{(bad.Count > 0 ? " — 없음: " + string.Join(" ", bad) : "")}");
            }

            // ---------- 2) 세이브: 옛 세이브(칸 없음) → 기본 우주복 · 탈것 없음 ----------
            {
                var old = JsonUtility.FromJson<SaveData>("{\"gold\":5,\"best\":3}");
                old.costume = null; old.mount = null; old.costumes = null; old.mounts = null;
                typeof(SaveData).GetMethod("Fix", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(old, null);
                Check(old.costume == "basic" && old.mount == "none" && old.costumes != null && old.mounts != null && old.HasCostume("basic") && old.HasMount("none") && !old.HasCostume("wizard"),
                      "옛 세이브 = 기본 우주 대원 · 탈것 없음 (세이브 판 번호 그대로)");
            }

            // ---------- 3) 사기 · 착용 · 능력치 ----------
            S.dia = 100000; S.best = Mathf.Max(S.best, 20); S.stage = 10; S.stamina = 99; S.gold = 1e9;
            var wiz = Tables.Costume("wizard"); var uni = Tables.Mount("unicorn");
            Check(!wiz.HasStat && !uni.HasStat && Looks.StatLine(wiz) == L.T("look_stat_none"), "능력치 칸이 비어 있으면 \"능력치 준비 중\" (아직 미정)");
            var lo0 = Loadout.Build(S);
            double d0 = S.dia;
            Check(Store.BuyLook(wiz, false) && S.HasCostume("wizard") && S.costume == "wizard" && S.dia == d0 - wiz.price && !Store.BuyLook(wiz, false),
                  $"코스튬 사기 → 가짐 · 바로 착용 · 다이아 -{wiz.price} · 두 번 못 삼");
            Check(Store.BuyLook(uni, true) && S.mount == "unicorn" && S.HasMount("unicorn"), "탈것 사기 → 바로 착용");
            Store.WearLook(Tables.Costume("dino"), false);
            Check(S.costume == "wizard", "안 가진 코스튬은 착용 안 됨");
            {
                var lo1 = Loadout.Build(S);
                Check(lo1.atkPct == lo0.atkPct && lo1.hp == lo0.hp, "능력치가 비어 있으면 착용해도 전투 값 그대로");
                wiz.stat1 = "atk_pct"; wiz.val1 = 10; uni.stat1 = "hp"; uni.val1 = 500; uni.stat2 = "crit"; uni.val2 = 5;
                var lo2 = Loadout.Build(S);
                Check(System.Math.Abs(lo2.atkPct - lo0.atkPct - 10) < 1e-6 && System.Math.Abs(lo2.hp - lo0.hp - 500) < 1e-6 && System.Math.Abs(lo2.TS("*", "crit") - lo0.TS("*", "crit") - 5) < 1e-6,
                      $"표에 능력치를 넣으면 착용 중일 때 더해짐 (공격력 % +{lo2.atkPct - lo0.atkPct} · 체력 +{lo2.hp - lo0.hp} · 치명 +{lo2.TS("*", "crit") - lo0.TS("*", "crit")})");
                Check(Looks.StatLine(uni).Contains("+500"), $"능력치 글자: {Looks.StatLine(uni)}");
                Store.WearLook(Tables.Costume("basic"), false); Store.WearLook(Tables.Mount("none"), true);
                var lo3 = Loadout.Build(S);
                Check(lo3.atkPct == lo0.atkPct && lo3.hp == lo0.hp, "벗으면 능력치도 빠짐");
                // 보고서 캡처용으로 능력치를 넣은 채 외형 탭을 한 번 찍고 비움
                Store.WearLook(wiz, false); Store.WearLook(uni, true);
            }

            // ---------- 4) 외형 탭: 코스튬 · 탈것 칸 · 미리 보기 ----------
            Game.ToLobby(); yield return Wait(0.3f); Popup.CloseAll();
            Looks.Open(2); yield return Wait(0.5f);
            int cRows = 0; foreach (var c in Tables.Costumes) if (GameObject.Find("C_" + c.id)) cRows++;
            Check(GameObject.Find("Sub3") && cRows == Tables.Costumes.Count(r => Store.ShowLook(r, false)) && GameObject.Find("Preview"), $"외형 탭: [터렛 · 기지 · 코스튬 · 탈것] · 코스튬 {cRows}줄(숨김 빼고 · 가진 것은 보임) · 위 미리 보기");
            {
                var body = GameObject.Find("Preview/Sky/Look/Body")?.GetComponent<Image>(); var mb = GameObject.Find("Preview/Sky/Look/MountBack")?.GetComponent<Image>();
                var mf = GameObject.Find("Preview/Sky/Look/MountFront")?.GetComponent<Image>();
                Check(body && body.sprite.name.EndsWith("wizard_float") && mb && mb.sprite.name.EndsWith("mount/unicorn") && mf,
                      "미리 보기 = 지금 코스튬(마법사) + 탈것(유니콘 뒤 · 앞 층) 겹침");
                if (body && mb)
                {   // 발(몸 피벗)과 자리(탈것 피벗)가 같은 점
                    var a = body.rectTransform.position; var b2 = mb.rectTransform.position;
                    Check((a - b2).magnitude < 0.5f, $"몸 발 = 탈것 자리 (어긋남 {(a - b2).magnitude:0.00})");
                }
            }
            Capture("r10_01_looks_costume");
            yield return ScrollLooksEnd("r10_01b_looks_costume_end");
            Looks.previewCostume = "pirate"; Game.Lobby.SetTab(6); yield return Wait(0.4f);
            var pb = GameObject.Find("Preview/Sky/Look/Body")?.GetComponent<Image>();
            Check(pb && pb.sprite.name.EndsWith("pirate_float") && GameObject.Find("BackNow") && S.costume == "wizard", "목록을 누르면 미리 보기만 (착용은 그대로) · [지금 모습으로]");
            Capture("r10_02_looks_preview");
            Looks.Open(3); yield return Wait(0.5f);
            int mRows = 0; foreach (var m in Tables.Mounts) if (GameObject.Find("M_" + m.id)) mRows++;
            var rowIcon = GameObject.Find("M_surf/Icon/Look/Body")?.GetComponent<Image>();
            Check(mRows == Tables.Mounts.Count(r => Store.ShowLook(r, true)) && rowIcon && rowIcon.sprite.name.EndsWith("wizard_float"), $"탈것 {mRows}줄 · 목록 그림 = 지금 코스튬을 태운 모습");
            Capture("r10_03_looks_mount");
            yield return ScrollLooksEnd("r10_03b_looks_mount_end");
            wiz.stat1 = ""; wiz.val1 = 0; uni.stat1 = ""; uni.val1 = 0; uni.stat2 = ""; uni.val2 = 0;
            Game.Lobby.SetTab(6); yield return Wait(0.3f);
            Capture("r10_04_looks_mount_nostat");

            // ---------- 5) 장비 탭 ----------
            Game.Lobby.SetTab(2); yield return Wait(0.6f);
            {
                var g = GameObject.Find("Guardian");
                var gb = g ? g.transform.Find("Body")?.GetComponent<Image>() : null; var gm = g ? g.transform.Find("MountBack")?.GetComponent<Image>() : null;
                Check(gb && gb.sprite.name.EndsWith("wizard_float") && gm && gm.sprite.name.EndsWith("mount/unicorn") && g.GetComponent<Float2D>(),
                      "장비 탭: 코스튬 + 탈것 겹친 모습이 둥실");
                // 장비 칸과 안 겹침 (가운데 빈 곳)
                bool clear = true;
                if (g)
                {
                    var c = new Vector3[4]; ((RectTransform)g.transform).GetWorldCorners(c); var rr = Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
                    foreach (var n in new[] { "Slot_weapon", "Slot_suit" })
                    {
                        var sl = GameObject.Find(n); if (!sl) continue;
                        ((RectTransform)sl.transform).GetWorldCorners(c); var sr2 = Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
                        if (rr.Overlaps(sr2)) clear = false;
                    }
                }
                Check(clear, "장비 탭 대장 카피가 장비 칸과 안 겹침");
            }
            Capture("r10_05_gear");
            Store.WearLook(Tables.Costume("basic"), false); Store.WearLook(Tables.Mount("none"), true);
            Game.Lobby.SetTab(2); yield return Wait(0.4f);
            Capture("r10_06_gear_basic");

            // ---------- 6) 전투: 코스튬 몸 · 총 · 탈것 · 높이 ----------
            Game.StartStage(10, 7); yield return Wait(0.5f);
            var g0 = B.run.turrets[0].unit; float y0 = g0 ? g0.transform.position.y : 0;
            Check(g0 && g0.GetComponent<SpriteRenderer>().sprite.name == "Char/guardian" && g0.Mount == null, "기본 = 예전 우주복 그림 · 탈것 없음");
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.3f);
            foreach (var id in new[] { "dino", "knight", "ninja", "bee", "shark", "chef", "onsen", "angel", "duck", "winter" }) { if (!S.costumes.Contains(id)) S.costumes.Add(id); }
            foreach (var id in new[] { "trex", "plane", "hoverbike", "broom", "seahorse", "star", "carpet", "ladybug", "surf", "tub", "cloud", "ufo", "duckboat", "whale", "corgi" }) { if (!S.mounts.Contains(id)) S.mounts.Add(id); }
            Store.WearLook(wiz, false); Store.WearLook(uni, true);
            Game.StartStage(10, 7); yield return Wait(0.5f);
            {
                var u = B.run.turrets[0].unit; var sr = u.GetComponent<SpriteRenderer>();
                var gun = u.transform.Find("GunRoot/Gun")?.GetComponent<SpriteRenderer>();
                Check(sr.sprite.name.Contains("costume/wizard") && gun && gun.sprite.name.Contains("wizard_gun"), $"전투: 코스튬 몸 · 손 색 맞춘 총 ({sr.sprite.name})");
                Check(u.Mount && u.Mount.back.sprite.name.Contains("mount/unicorn") && u.Mount.front && u.Mount.back.sortingOrder < sr.sortingOrder && u.Mount.front.sortingOrder > sr.sortingOrder && gun.sortingOrder > u.Mount.front.sortingOrder,
                      "전투: 탈것 뒤 층 < 몸 < 앞 층 < 총");
                float dy = u.transform.position.y - y0;
                Check(Mathf.Abs(dy - uni.lift * Tables.Cf("Mount_Battle_Scale", 1f) * Tables.Cf("Mount_Lift_Mul", 1f)) < 0.05f, $"탈것 높이만큼 올라탐 ({dy:0.00}칸 · 표 Lift {uni.lift})");
                yield return Wait(2.5f);
                Check(u.Mount.back.flipX == sr.flipX && Mathf.Abs(Mathf.DeltaAngle(u.Mount.transform.eulerAngles.z, 0)) < 0.5f, "몸 방향을 따라 탈것도 뒤집힘 · 몸을 젖혀도 탈것은 수평");
                Capture("r10_10_battle_wizard_unicorn");
            }
            var combos = new[] { ("dino", "trex"), ("knight", "plane"), ("ninja", "hoverbike"), ("bee", "broom"), ("shark", "seahorse"), ("basic", "star"), ("chef", "ladybug"), ("wizard", "carpet"), ("basic", "surf"),
                ("onsen", "tub"), ("angel", "cloud"), ("basic", "ufo"), ("duck", "duckboat"), ("shark", "whale"), ("winter", "corgi") };
            int ci = 11;
            foreach (var (c, m) in combos)
            {
                Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f);
                Store.WearLook(Tables.Costume(c), false); Store.WearLook(Tables.Mount(m), true);
                Game.StartStage(10, 7); yield return Wait(2.2f);
                var u = B.run.turrets[0].unit;
                bool ok = u && u.Mount && u.Mount.back.sprite.name.EndsWith("mount/" + m) && u.GetComponent<SpriteRenderer>().sprite.name.Contains(c == "basic" ? "guardian" : "costume/" + c);
                if (!ok) Check(false, $"전투 조합 {c} + {m}");
                if (Tables.Mount(m).bob)
                {
                    // 11-21: 두 순간만 비교하면 사인 곡선에서 우연히 같은 높이가 나옴 → 0.2초마다 6번 재서 위아래 폭으로
                    float lo = 99f, hi = -99f;
                    for (int k = 0; k < 6; k++) { float yy = u.transform.position.y; lo = Mathf.Min(lo, yy); hi = Mathf.Max(hi, yy); yield return Wait(0.2f); }
                    if (hi - lo < 0.01f) Check(false, $"떠다니는 탈것 둥실 ({m}, 폭 {hi - lo:0.000})");
                }
                Capture($"r10_{ci++}_battle_{c}_{m}");
            }
            Check(true, $"전투 조합 {combos.Length}가지 (몸 · 탈것 그림 · 떠다니는 탈것 둥실)");

            // ---------- 7) 로비: 걸어 다니는 대장 카피도 같은 모습 ----------
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.3f);
            Store.WearLook(Tables.Costume("dino"), false); Store.WearLook(Tables.Mount("trex"), true);
            yield return Wait(0.6f);
            var w = LobbyWalker.I;
            Check(w && w.GetComponent<SpriteRenderer>().sprite.name.Contains("costume/dino") && w.Mount && w.Mount.back.sprite.name.EndsWith("mount/trex"), "로비: 바꾸면 걸어 다니는 대장 카피도 바로 그 모습");
            Popup.CloseAll(); Game.Lobby.SetTab(0); yield return Wait(1f);
            Capture("r10_30_lobby");

            // ---------- 8) 대장 카피 카드 · 스토리 얼굴 = 지금 코스튬 ----------
            Check(Art.TurretUI("guardian").name.Contains("costume/dino_ui"), "터렛 카드 · 스토리 얼굴 = 지금 코스튬");
            Store.WearLook(Tables.Costume("basic"), false); Store.WearLook(Tables.Mount("none"), true);
            Check(Art.TurretUI("guardian").name == "Char/guardian_ui", "기본이면 예전 그림");

            running = false;
        }

        /// <summary>11-21: 외형 목록을 맨 아래로 내려 찍고 다시 위로 (새 코스튬 · 탈것은 목록 끝)</summary>
        IEnumerator ScrollLooksEnd(string shot)
        {
            var sr = GameObject.Find("Looks")?.GetComponentInChildren<ScrollRect>();
            Check(sr, "외형 목록 스크롤");
            if (!sr) yield break;
            Canvas.ForceUpdateCanvases(); sr.verticalNormalizedPosition = 0f; yield return Wait(0.3f);
            Capture(shot);
            sr.verticalNormalizedPosition = 1f; yield return Wait(0.1f);
        }
    }
}
