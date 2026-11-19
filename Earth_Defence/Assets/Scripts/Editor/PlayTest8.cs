using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-19 8차 수정안 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR8 --play
    /// 화면 = Logs/PlayTest/r8_*.png (보고서 사본은 캡쳐본 모음/8차_수정안_캡처/)
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator R8Steps()
        {
            yield return null;
            Tables.SetC("Battle_Vertical", 0);      // 9차: 이 시험은 11-18 사령탑 배치 기준 → 되돌리기 스위치(0)가 그대로 맞는지 확인하는 시험으로
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            Lobby.NoIntro = true;
            yield return Wait(0.5f);
            var W = World.I;
            SpriteRenderer R(string n) => GameObject.Find(n)?.GetComponent<SpriteRenderer>();
            Rect UiRect(GameObject go) { var c = new Vector3[4]; ((RectTransform)go.transform).GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }

            // 1) 타이틀: [게임 시작] 아래 · 0.7배
            var start = FindDeep("Title/Start");
            Check(start != null, "타이틀 [게임 시작] 단추");
            if (start)
            {
                var rt = (RectTransform)start.transform; var r = UiRect(start);
                float screenBottom = W.cam.transform.position.y - W.cam.orthographicSize;
                Check(Mathf.Abs(rt.sizeDelta.x - 640 * 0.7f) < 1 && Mathf.Abs(rt.sizeDelta.y - 170 * 0.7f) < 1, $"[게임 시작] 0.7배 ({rt.sizeDelta.x:0} x {rt.sizeDelta.y:0}, 예전 640 x 170)");
                Check(r.yMin - screenBottom < 3.0f && r.yMin - screenBottom > 1.5f, $"[게임 시작] 화면 아래 (아래 끝이 화면 아래에서 {(r.yMin - screenBottom) * 100:0}px · 예전 ~1000px)");
            }
            Capture("r8_01_title");

            // 2) 별똥별이 더 천천히 (타이틀 · 로비 같은 코드): 하늘에 있는 별똥별의 빠르기를 잼
            float speed = -1;
            for (int tryN = 0; tryN < 6 && speed < 0; tryN++)
            {
                SpriteRenderer star = null; float w0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - w0 < 5f && star == null)
                {
                    foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                        if (sr.enabled && sr.name == "ShootStar" && sr.color.a > 0.2f) { star = sr; break; }
                    if (star == null) yield return null;
                }
                if (star == null) break;
                Vector2 a = star.transform.position; float ta = Time.time;
                yield return Wait(0.2f);
                if (!star.enabled || Time.time - ta < 0.05f) continue;
                speed = ((Vector2)star.transform.position - a).magnitude / (Time.time - ta);
            }
            Check(speed >= 4f && speed <= 7f, $"별똥별 빠르기 {speed:0.0}칸/초 (예전 9~13 → {Tables.Cf("Shoot_Star_Speed_Min", 0):0.#}~{Tables.Cf("Shoot_Star_Speed_Max", 0):0.#})");
            {
                float w0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - w0 < 5f && !AnyStar()) yield return null;
                yield return Wait(0.3f);
                Capture("r8_02_title_star");
            }
            for (int f = 0; f < 20; f++) { yield return Wait(0.1f); Capture($"r8_title_{f:00}", true); }     // 보고서 GIF (별똥별 빠르기)

            // 3) 로비: [전투 시작] · 순찰이 아래 탭 바로 위
            Title.I.Enter(); yield return Wait(0.4f); Popup.CloseAll();
            S.best = 30; S.dia = 100000; S.gold = 1e9;
            Game.ToLobby(); yield return Wait(0.4f); Popup.CloseAll();
            {
                var pat = FindDeep("Page_battle/Patrol"); var st = FindDeep("Page_battle/Start"); var nav = FindDeep("Lobby/Nav");
                bool ok = pat && st && nav;
                Rect pr = ok ? UiRect(pat) : default, sr2 = ok ? UiRect(st) : default, nr = ok ? UiRect(nav) : default;
                Check(ok && pr.yMin - nr.yMax > 0.05f && pr.yMin - nr.yMax < 0.6f && sr2.yMin > pr.yMax && Mathf.Abs(pr.center.x) < 0.1f,
                      $"로비 [전투 시작] · 순찰이 아래 탭 바로 위 (탭 위 끝 ↔ 순찰 {(pr.yMin - nr.yMax) * 100:0}px · 지구 꼭대기 {World.EarthTop:0.0} · 순찰 위 끝 {pr.yMax:0.0})");
                Check(ok && sr2.yMax < World.EarthTop + 0.2f, $"단추가 지구 위 대장 카피를 가리지 않음 ([전투 시작] 위 끝 {sr2.yMax:0.00} ≤ 지구 꼭대기 {World.EarthTop:0.0})");
            }
            Capture("r8_03_lobby");
            for (int f = 0; f < 20; f++) { yield return Wait(0.1f); Capture($"r8_lobby_{f:00}", true); }

            // 4) 장비 탭: 제자리에서 둥실둥실 · 좌우로 안 뒤집힘
            Game.Lobby.SetTab(2); yield return Wait(0.3f);
            var fl = Object.FindAnyObjectByType<Float2D>();
            {
                var rt = fl ? (RectTransform)fl.transform : null;
                float xMin = 1e9f, xMax = -1e9f, yMin = 1e9f, yMax = -1e9f; bool flip = false;
                for (int f = 0; f < 24; f++)
                {
                    yield return Wait(0.15f);
                    if (!rt) break;
                    var p = rt.anchoredPosition; xMin = Mathf.Min(xMin, p.x); xMax = Mathf.Max(xMax, p.x); yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y);
                    if (rt.localScale.x < 0.99f) flip = true;
                    Capture($"r8_gear_{f:00}", true);
                }
                Check(rt && !flip && xMax - xMin < 1f && yMax - yMin > 15f,
                      $"장비 탭 대장 카피: 제자리에서 위아래로 둥실 (좌우 이동 {xMax - xMin:0} · 위아래 {yMax - yMin:0} · 뒤집힘 {(flip ? "있음" : "없음")})");
            }
            Capture("r8_04_gear");

            // 5) 외형: 설명 짧게
            Looks.sub = 0; Game.Lobby.SetTab(6); yield return Wait(0.3f);
            {
                bool longDesc = false; int lines = 0;
                foreach (var k in Tables.Skins)
                {
                    var row = GameObject.Find("S_" + k.id); if (!row) continue;
                    foreach (var t in row.GetComponentsInChildren<Text>()) { if (t.text.Contains(k.ultDesc)) longDesc = true; if (t.text.Length > 0) lines++; }
                }
                Check(!longDesc, $"터렛 외형: 필살기 긴 설명 뺌 (이름 · 필살기 이름 · 보유 효과)");
            }
            Capture("r8_05_looks_turret");
            Looks.sub = 1; Game.Lobby.SetTab(6); yield return Wait(0.3f);
            {
                int longest = 0; foreach (var f in Tables.Frames) longest = Mathf.Max(longest, f.Desc(5).Length);
                Check(longest <= 20, $"기지 외형 설명 짧게 (가장 긴 설명 {longest}글자, 예전 30글자 넘음)");
            }
            Capture("r8_06_looks_base");
            Game.Lobby.SetTab(0); yield return null;

            // 6) 종료: 설정 [게임 종료] → 확인 창 → 종료 · 폰 뒤로 가기
            Settings.Open(); yield return Wait(0.3f);
            var qb = GameObject.Find("Quit");
            Check(qb != null, "설정 창에 [게임 종료]");
            Capture("r8_07_settings");
            if (qb) qb.GetComponent<Button>().onClick.Invoke();
            yield return Wait(0.3f);
            Check(GameObject.Find("QuitOk") != null, "[게임 종료] → \"게임을 종료할까요?\" 확인 창");
            Capture("r8_08_quit");
            Quit.Requested = false;
            GameObject.Find("QuitOk")?.GetComponent<Button>().onClick.Invoke(); yield return null;
            Check(Quit.Requested, "확인 창 [게임 종료] → 저장하고 종료 (에디터에선 끄지 않음)");
            Quit.Requested = false;
            Popup.CloseAll(); yield return null;
            // 뒤로 가기: 다른 탭 → 전투 탭 · 전투 탭 → 종료 확인 · 창이 있으면 닫기
            Game.Lobby.SetTab(3); yield return null;
            BackKey.Press(); yield return null;
            Check(Game.Lobby.tab == 0 && Popup.Open == null, "뒤로 가기: 다른 탭이면 전투 탭으로");
            BackKey.Press(); yield return Wait(0.2f);
            Check(GameObject.Find("QuitOk") != null, "뒤로 가기: 로비 전투 탭이면 종료 확인 창");
            BackKey.Press(); yield return Wait(0.1f);
            Check(Popup.Open == null && !Quit.Requested, "뒤로 가기: 창이 있으면 닫기 (종료 안 함)");

            // 7) 전투: 지구 바닥 · 일반 행성 · 달토끼 오른쪽 위 · 스테이지 옆 약점
            Game.StartStage(10, 7); yield return Wait(0.3f);
            var gr = R("Ground");
            Check(gr && gr.enabled && gr.sprite.name.EndsWith("ground_earth"), $"전투 바닥 = 지구 그림 ({(gr ? gr.sprite.name : "-")})");
            Check(World.FloorY(World.SlotXPos) > World.GroundY - 0.05f && World.FloorY(5f) < World.GroundY - 0.8f,
                  $"지구 윗면: 사령탑 자리 평평 ({World.FloorY(World.SlotXPos):0.00}) · 오른쪽은 둥글게 낮음 ({World.FloorY(5f):0.00})");
            {
                var mc = R("Moon").transform.position;
                Check(mc.x > 3.5f && mc.y < World.FadeTop && mc.y > World.FadeTop - 2f && R("Moon").sortingOrder < 0,
                      $"달토끼 오른쪽 위 (x {mc.x:0.0} · y {mc.y:0.0} · 위 UI 아래 {World.FadeTop:0.0}) · 운석보다 뒤");
                Check(Vector2.Distance(R("MoonRabbit").transform.position, mc) < 0.01f, "달토끼가 달 위");
            }
            {
                var st = Tables.Stage(10); var wv = GameObject.Find("Weak"); var stg = FindDeep("BattleUI/Stage");
                var wt = wv ? wv.GetComponent<Text>() : null;
                Check(wt != null && wt.text == Elem.Name(st.weak) && GameObject.Find("WeakLabel") != null,
                      $"전투 위 줄 스테이지 옆에 약점 \"{(wt ? wt.text : "-")}\" (스테이지 10 약점 {Elem.Name(st.weak)})");
                var clock = FindDeep("BattleUI/Clock");
                if (wv && clock)
                {   // 글자 실제 폭으로: 약점 글자 오른쪽 끝 < 시계 글자 왼쪽 끝 (시계는 오른쪽 맞춤, "04:00" 기준)
                    float wEnd = UiRect(wv).xMin + wt.preferredWidth * 0.01f;
                    var ct = clock.GetComponent<Text>(); ct.text = "04:00";
                    float cStart = UiRect(clock).xMax - ct.preferredWidth * 0.01f;
                    Check(wEnd < cStart - 0.1f, $"약점 글자가 시계와 안 겹침 (약점 끝 {wEnd:0.00} · 시계 시작 {cStart:0.00})");
                }
            }
            Capture("r8_09_battle");
            yield return Wait(0.2f);

            // 8) 끈끈이 장판 세로 띠 (적 무리 x · 땅 ~ 위 UI 아래)
            Force("new_drone"); yield return null;
            var drone = B.run.Get("drone");
            Check(drone != null, "끈끈이 드론 놓음");
            if (drone != null)
            {
                {   // 비교용: 예전 가로 띠 (Field_Vertical 0) — 이 사이 드론이 쏴도 가로
                    Tables.SetC("Field_Vertical", 0);
                    var o = new Field(drone, 1.5f, 0.9f, 0.7f, 0.4f, 1); B.Add(o);
                    for (int i = 0; i < 8; i++) B.Tick(Battle.StepSec);
                    yield return null; Capture("r8_10a_field_old");
                    FF(0.9f, 2);
                    Tables.SetC("Field_Vertical", 1);
                }
                var e = B.Spawn("pebble", new Vector2(2.2f, 1f)); e.hp = e.maxHp = 1e12; e.Aim(new Vector2(World.ShieldX, World.GroundY + 2f));
                float cx = B.CrowdX();
                var f = new Field(drone, cx, 0.9f, 3f, 0.4f, 100);
                B.Add(f);
                double hp0 = e.hp;
                for (int i = 0; i < 20; i++) B.Tick(Battle.StepSec);
                var a = f.Area;
                Check(f.Vertical && a.height > 8f && a.width < 3f && a.yMin < World.GroundY + 0.1f && a.yMax <= World.FadeTop + 0.01f,
                      $"끈끈이 장판 = 세로 띠 (x {f.Center:0.0} · 폭 {a.width:0.0} · 높이 {a.yMin:0.0}~{a.yMax:0.0})");
                Check(Mathf.Abs(cx - 2.2f) < 1f && e.hp < hp0, $"장판이 적이 있는 x 에 깔리고 안의 적이 맞음 (적 x 2.2 · 장판 x {cx:0.0} · 피해 {hp0 - e.hp:0})");
                var old = new Field(drone, 0f, 0.9f, 0.5f, 0.4f, 1, false); B.Add(old);
                Check(!old.Vertical && old.Area.width > 6f && old.Area.height < 3f, "Config Field_Vertical 0 이면 예전 가로 띠");
                yield return null;
                // 실제 드론이 쏜 장판도 세로인지 (적을 여럿 두고 빨리 감기)
                for (int i = 0; i < 6; i++) { var x = B.Spawn("pebble", new Vector2(1.5f + i * 0.3f, 2f + i)); x.hp = x.maxHp = 1e12; x.Aim(new Vector2(World.ShieldX, World.GroundY + 2f)); }
                FF(2.5f, 2); yield return null;
                Capture("r8_10_field");
                int vf = 0, hf = 0;
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                    if (sr.enabled && sr.gameObject.name.Contains("field") && sr.drawMode == SpriteDrawMode.Sliced) { if (sr.size.y > sr.size.x) vf++; else hf++; }
                Check(vf > 0 && hf <= 1, $"드론이 쏜 장판이 세로 (세로 {vf} · 가로 {hf} — 가로 1개는 위에서 일부러 깐 예전 띠)");
                for (int f2 = 0; f2 < 10; f2++) { FF(0.12f, 2); yield return null; Capture($"r8_field_{f2:00}", true); }
            }

            // 9) 결과 창: 보상 칸 테두리 (성공 · 실패)
            B.End(true); yield return Wait(0.5f);
            {
                var box = GameObject.Find("RewardBox"); var coin = FindDeep("Popup_" + L.T("win") + "/Coin") ?? GameObject.Find("Coin"); var shard = GameObject.Find("Shard");
                bool inBox = box && coin && shard && UiRect(box).Contains(UiRect(coin).center) && UiRect(box).Contains(UiRect(shard).center);
                Check(inBox, "성공 창: 골드 · 부품 둘레에 보상 칸 테두리");
            }
            Capture("r8_11_result_win");
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f); Popup.CloseAll();
            Game.StartStage(10, 7); yield return Wait(0.2f);
            B.End(false); yield return Wait(0.5f);
            Check(GameObject.Find("RewardBox") != null, "실패 창에도 보상 칸 테두리");
            Capture("r8_12_result_lose");
            Popup.CloseAll(); Game.ToLobby(); yield return Wait(0.2f);

            running = false;
        }
    }
}
