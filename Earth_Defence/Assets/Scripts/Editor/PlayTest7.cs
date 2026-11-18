using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-18 7차 수정안 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR7 --play
    /// 화면 = Logs/PlayTest/r7_*.png (보고서 사본은 캡쳐본 모음/7차_수정안_캡처/)
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator R7Steps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            yield return Wait(0.5f);
            var W = World.I;
            SpriteRenderer R(string n) => GameObject.Find(n)?.GetComponent<SpriteRenderer>();

            // 1) 타이틀 (로비와 같은 배경: 사령탑 없음 · 심플한 하늘 · 큰 지구 · 별똥별)
            Check(Title.I != null && Game.Lobby == null && W.Lobby, "켜면 타이틀 화면 (로비는 아직)");
            Check(!R("Tower").enabled && !R("Ground").enabled && !R("Moon").enabled && R("EarthBig").enabled && R("Sky").sprite.name == "Bg/sky_plain",
                  "타이틀 · 로비 배경 = 사령탑 · 달 바닥 · 달 없음 + 심플한 하늘 + 큰 지구");
            Check(FindDeep("Title/Start") != null && LobbyWalker.I != null, "[게임 시작] 단추 · 지구 위 대장 카피");
            int st0 = W.StarsShot; float w0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - w0 < 6f && W.StarsShot < st0 + 2) yield return null;
            Check(W.StarsShot >= st0 + 2, $"별똥별이 떨어짐 ({W.StarsShot - st0}개 / 6초)");
            Capture("r7_01_title");
            // 별똥별이 하늘에 있을 때 한 장
            w0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - w0 < 5f && !AnyStar()) yield return null;
            yield return Wait(0.12f);
            Capture("r7_02_title_star");

            // 2) [게임 시작] → 입장 방법 (구글 / 게스트)
            Title.I.OnStart(); yield return Wait(0.3f);
            Check(GameObject.Find("Google") != null && GameObject.Find("Guest") != null, "[게임 시작] → 구글 계정 연동 · 게스트 두 가지");
            Capture("r7_03_enter");
            // 게스트 → 데이터가 저장되지 않는다는 확인
            GameObject.Find("Guest").GetComponent<Button>().onClick.Invoke(); yield return Wait(0.3f);
            bool warn = false; foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) if (t.text.Contains("저장되지 않아요")) warn = true;
            Check(warn && GameObject.Find("GuestGo") != null, "게스트 → \"데이터가 저장되지 않아요\" 확인 창");
            Capture("r7_04_guest_warn");
            GameObject.Find("GuestGo").GetComponent<Button>().onClick.Invoke(); yield return Wait(0.3f);
            Check(Game.Lobby != null && Title.I == null && !Store.Linked && Title.Guest, "게스트로 입장 → 로비 (연동 안 됨)");
            Popup.CloseAll(); Lobby.NoIntro = true;
            // 구글 [초안] → 연동 + 로비 · 연동돼 있으면 다음엔 [게임 시작]만으로 바로
            Game.ToTitle(); yield return Wait(0.2f);
            Title.I.OnStart(); yield return Wait(0.2f);
            GameObject.Find("Google").GetComponent<Button>().onClick.Invoke(); yield return Wait(0.2f);
            Check(Game.Lobby != null && Store.Linked, "구글 계정으로 시작 [초안] → 연동 + 로비");
            Game.ToTitle(); yield return Wait(0.2f);
            Title.I.OnStart(); yield return Wait(0.2f);
            Check(Game.Lobby != null && Popup.Open == null, "연동돼 있으면 [게임 시작] → 바로 로비");
            Store.Unlink(); Popup.CloseAll();
            Tables.SetC("Title_On", 0); Game.ToTitle(); yield return null;
            Check(Title.I == null && Game.Lobby != null, "Config Title_On 0 → 타이틀 없이 바로 로비");
            Tables.SetC("Title_On", 1);

            // 3) 로비: 사령탑 없이 큰 지구 + 지구 위를 걷는 대장 카피 + 단추는 그 위 · 기지 외형은 전투에서만
            S.best = 30; S.dia = 100000; S.gold = 1e9;
            Store.BuyFrame(Tables.Frame("gold")); Store.EquipFrame("gold");
            Game.ToLobby(); yield return Wait(0.4f); Popup.CloseAll();
            Check(!R("Tower").enabled && R("Tower").sprite.name == "Bg/tower_gold" && R("EarthBig").enabled, "로비엔 사령탑(기지 외형) 안 보임 · 끼운 외형은 그대로 (황금)");
            var wk = LobbyWalker.I;
            Check(wk != null && Mathf.Abs(wk.transform.position.y - (World.EarthSurf(wk.X) - 0.06f)) < 0.1f, $"대장 카피가 지구 위에 섬 (발 {wk?.transform.position.y:0.00} · 지구 면 {World.EarthSurf(wk ? wk.X : 0):0.00})");
            {
                var st = FindDeep("Page_battle/Patrol")?.GetComponent<RectTransform>();
                var c = new Vector3[4]; if (st) st.GetWorldCorners(c);
                Check(st != null && c[0].y > World.EarthTop + 1.25f && Mathf.Abs((c[0].x + c[2].x) / 2) < 0.1f,
                      $"[전투 시작] · 순찰은 가운데 · 대장 카피 머리 위 (순찰 아래 끝 {c[0].y:0.00} · 지구 꼭대기 {World.EarthTop:0.00})");
            }
            wk.Go(1.5f); yield return Wait(2.5f);
            Check(Mathf.Abs(wk.transform.eulerAngles.z - 360f * (wk.transform.eulerAngles.z > 180 ? 1 : 0) - World.EarthTilt(wk.X)) < 9f && World.EarthTilt(1.5f) < -5f,
                  $"지구 둥근 면을 따라 몸이 기욺 (x {wk.X:0.0} · 기울기 {World.EarthTilt(wk.X):0.0}°)");
            Capture("r7_05_lobby");
            for (int f = 0; f < 16; f++) { yield return Wait(0.12f); Capture($"r7_lobby_{f:00}", true); }     // 보고서 GIF (별똥별 · 걷기)

            // 4) 장비 탭: 총 없이 2D 로 둥실둥실
            Game.Lobby.SetTab(2); yield return Wait(0.3f);
            var fl = Object.FindAnyObjectByType<Float2D>();
            var p0 = fl ? fl.Offset : Vector2.zero;
            Capture("r7_06_gear");
            for (int f = 0; f < 16; f++) { yield return Wait(0.25f); Capture($"r7_gear_{f:00}", true); }
            Check(fl != null && (fl.Offset - p0).magnitude > 8f && fl.GetComponent<Image>().sprite.name.EndsWith("guardian_float"),
                  $"장비 탭 카피바라: 총 없는 우주복 · 2D 로 떠다님 (움직임 {(fl ? (fl.Offset - p0).magnitude : 0):0})");
            Game.Lobby.SetTab(0); yield return null;

            // 5) 전투: 사령탑(황금 외형) · 컴팩트한 지지대 · 큰 지구 · 달토끼 자리 · 적 Fade in · 대장 카피 탄도
            Game.StartStage(10, 7); yield return Wait(0.3f);
            Check(!W.Lobby && R("Tower").enabled && R("Tower").sprite.name == "Bg/tower_gold" && !R("EarthBig").enabled && R("Moon").enabled, "전투에선 기지 외형(황금 사령탑) · 달토끼 보임 · 로비 지구 숨김");
            foreach (var id in new[] { "new_rail", "new_laser", "new_missile", "new_bolt" }) Force(id);
            yield return null;
            Check(B.units.Count == 5, $"터렛 4칸 다 참 ({B.units.Count - 1})");
            Bounds Union(IEnumerable<SpriteRenderer> rs) { Bounds b = default; bool first = true; foreach (var r in rs) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); } return b; }
            var turretB = new List<(TurretUnit u, Bounds b)>();
            foreach (var u in B.units) if (u != B.Guardian) turretB.Add((u, Union(u.GetComponentsInChildren<SpriteRenderer>())));
            float turretRight = -99; foreach (var u in B.units) if (u != B.Guardian) turretRight = Mathf.Max(turretRight, u.GetComponent<SpriteRenderer>().bounds.max.x);   // 받침 그림 (빛 · 대기 효과 조각 빼고)
            // 지지대 컴팩트: 칸 x · 사령탑 그림의 보이는 폭
            var tsr = R("Tower");
            Check(World.SlotXPos <= -3.85f && turretRight < World.GuardianPos.x - 0.3f, $"컴팩트: 터렛 줄 x {World.SlotXPos} (예전 -3.1) · 터렛 오른쪽 끝 {turretRight:0.00} < 대장 카피 {World.GuardianPos.x}");
            // 큰 지구: 땅 그림이 위로 더 올라옴 (예전 월드 -5.0 → -2.6)
            Check(R("Ground").bounds.max.y > -2.7f, $"전투 지구 크게 (땅 그림 위 끝 {R("Ground").bounds.max.y:0.0})");
            // 달토끼: 위 UI 아래 · 사령탑 · 터렛 · 방어선 오른쪽(적 · 탄 다니는 곳)과 안 겹침
            var mc = R("Moon").transform.position; var mb = new Bounds(mc, new Vector3(1.7f, 1.7f, 0));   // 달 원(반지름 0.78 + 테두리) — 그림 칸은 빛 번짐 여백까지라 더 큼
            bool moonFree = mb.max.x < World.ShieldX - 0.6f && mb.max.y <= World.FadeTop + 0.05f && mb.min.y > World.TowerTop + 0.8f;
            foreach (var (u, b) in turretB) if (mb.Intersects(b)) moonFree = false;
            if (mb.Intersects(Union(B.Guardian.GetComponentsInChildren<SpriteRenderer>()))) moonFree = false;
            Check(moonFree, $"달토끼가 다른 그림과 안 겹침 (달 {mb.min.x:0.0}~{mb.max.x:0.0}, {mb.min.y:0.0}~{mb.max.y:0.0} · 방어선 {World.ShieldX} · 위 UI {World.FadeTop:0.0})");
            // Lv 게이지 아래 끝 = 적이 나타나기 시작하는 높이
            {
                var lv = GameObject.Find("LvBadge")?.GetComponent<RectTransform>(); var c = new Vector3[4]; if (lv) lv.GetWorldCorners(c);
                Check(lv != null && Mathf.Abs(c[0].y - World.FadeTop) < 0.3f && World.SpawnY < World.FadeTop, $"적 나오는 줄은 Lv 게이지 아래 (게이지 아래 {c[0].y:0.00} · Fade 시작 {World.FadeTop:0.00} · 나오는 줄 {World.SpawnY:0.00})");
            }
            {   // 위에서 나온 운석: 처음엔 거의 안 보이다가 내려오며 서서히
                var e = B.Spawn("pebble", new Vector2(2f, World.SpawnY)); e.hp = e.maxHp = 1e12; e.Aim(new Vector2(World.ShieldX, World.GroundY + 3f));
                float a0 = 1, a1 = 0, maxA = 0;
                for (int i = 0; i < 30; i++) { B.Tick(Battle.StepSec); if (i == 16) a0 = e.Alpha; }
                float t = 0; while (t < 8f && !e.dead && e.Alpha < 1f) { B.Tick(Battle.StepSec); t += Battle.StepSec; maxA = Mathf.Max(maxA, e.Alpha); }
                a1 = e.Alpha;
                Check(a0 < 0.6f && a1 >= 0.99f && t > 0.3f, $"운석이 Lv 게이지 아래부터 서서히 나타남 (0.5초 뒤 {a0:0.00} → {t:0.0}초 뒤 {a1:0.00})");
            }
            // 대장 카피 탄도: 30초 동안 0.25초마다 겨누는 적까지의 선이 위 터렛(그림 범위)을 지나지 않음
            int samples = 0, cross = 0;
            for (int k = 0; k < 120; k++)
            {
                FF(0.25f);
                if (B == null || B.st == Battle.St.End) break;
                var g = B.Guardian; var tg = B.Nearest(g.Head, g.t.Range);
                if (tg == null) continue;
                samples++;
                foreach (var (u, b) in turretB) { var bb = b; bb.Expand(-0.1f); if (bb.IntersectRay(new Ray(g.Head, (tg.Pos - g.Head).normalized), out float d) && d < (tg.Pos - g.Head).magnitude) { cross++; break; } }
            }
            Check(samples > 20 && cross == 0, $"대장 카피 탄도가 터렛과 안 겹침 (겨눈 {samples}번 중 겹침 {cross})");
            Capture("r7_07_battle");
            yield return Wait(0.3f);
            Capture("r7_07b_battle");
            for (int f = 0; f < 12; f++) { FF(0.1f); yield return null; Capture($"r7_fade_{f:00}", true); }

            // 6) 자리 고르기: 안내는 가운데 크게 · [카드 다시 고르기] 없음
            Game.ToLobby(); yield return null; Popup.CloseAll();
            Game.StartStage(3, 5); yield return Wait(0.3f);
            Force("new_rail");
            Battle.NoSlotPick = false;
            B.offer = new List<CardRow> { Tables.CardById["new_laser"] }; B.st = Battle.St.Pick; B.pendingLevel++;
            B.Choose(0); yield return Wait(0.4f);
            Check(B.st == Battle.St.Slot && FindDeep("SlotPick/Tip") != null && FindDeep("SlotPick/Back") == null && FindDeep("SlotPick/Arrow") != null,
                  "자리 고르기: 가운데 큰 안내 + 칸 쪽 화살표 · [카드 다시 고르기] 없음");
            Capture("r7_09_slot_pick");
            B.PlaceAt(B.run.FreeSlotList[0]); yield return null;
            Check(B.st == Battle.St.Play && B.run.Owns("laser"), "칸을 누르면 놓이고 전투로");
            Battle.NoSlotPick = true;

            running = false;
        }

        /// <summary>"부모/자식" — 부모(이름)를 찾고 그 아래 어디서든 자식 이름</summary>
        static GameObject FindDeep(string path)
        {
            var parts = path.Split('/');
            var root = GameObject.Find(parts[0]); if (!root) return null;
            var t = root.transform;
            for (int i = 1; i < parts.Length && t; i++) t = Deep(t, parts[i]);
            return t ? t.gameObject : null;
        }
        static Transform Deep(Transform t, string n)
        {
            foreach (Transform c in t) { if (c.name == n) return c; var r = Deep(c, n); if (r) return r; }
            return null;
        }

        static bool AnyStar()
        {
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if (sr.enabled && sr.name == "ShootStar" && sr.transform.position.y > World.EarthTop + 2f && sr.color.a > 0.6f) return true;
            return false;
        }

    }
}
