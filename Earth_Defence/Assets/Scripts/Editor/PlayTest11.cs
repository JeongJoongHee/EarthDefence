using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-21 전투 요청 3개 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunR11 --play
    /// ① 대장 카피 좌우 이동 ② 특수 능력 단추(버닝 · 얼음 파동) ③ 속도 x0.5 ~ x2 — 화면 = Logs/PlayTest/r11_*.png
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator R11Steps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            var S = SaveData.Cur;
            var font = Object.FindAnyObjectByType<Text>()?.font;
            Lobby.NoIntro = true; Popup.CloseAll();
            S.best = Mathf.Max(S.best, 30); S.stamina = 999;
            Battle.LastSpeed = 1f;
            Game.StartStage(12, 7); yield return Wait(0.4f);
            var b = Battle.I; var ui = Object.FindAnyObjectByType<BattleUI>();
            Tutorial.CloseCur();

            // ---------- ③ 속도 ----------
            {
                var btn = GameObject.Find("Speed")?.GetComponent<Button>();
                var seq = new System.Text.StringBuilder(); bool ok = btn != null && b.speed == 1f;
                float[] want = { 1.5f, 2f, 0.5f, 1f };
                foreach (var w in want)
                {
                    btn.onClick.Invoke(); yield return null;
                    string lbl = btn.GetComponentInChildren<Text>().text; seq.Append(lbl).Append(' ');
                    if (Mathf.Abs(b.speed - w) > 0.01f || Mathf.Abs(Time.timeScale - w) > 0.01f) ok = false;
                }
                Check(ok && seq.ToString().Trim() == "x1.5 x2 x0.5 x1", $"속도 단추: {seq.ToString().Trim()} (누를 때마다 · 게임 시간도 같이)");
                btn.onClick.Invoke(); btn.onClick.Invoke(); yield return null;     // x2
                Popup.CloseAll(); Game.StartStage(12, 7); yield return Wait(0.2f);
                Check(Battle.I.speed == 2f && GameObject.Find("Speed").GetComponentInChildren<Text>().text == "x2", "고른 속도는 다음 전투에도 (앱을 끌 때까지)");
                Battle.LastSpeed = 1f;
                Popup.CloseAll(); Game.StartStage(12, 7); yield return Wait(0.3f);      // x1 로 다시 시작 (단추 글자도)
                b = Battle.I; ui = Object.FindAnyObjectByType<BattleUI>(); Tutorial.CloseCur();
            }

            // ---------- ① 좌우 이동 ----------
            {
                var g = b.Guardian; float x0 = g.transform.position.x;
                var ev = new PointerEventData(EventSystem.current) { position = World.I.cam.WorldToScreenPoint(new Vector3(4f, 0f, 0f)) };
                ui.OnPointerDown(ev); yield return Wait(0.8f);
                float xr = g.transform.position.x;
                ev.position = World.I.cam.WorldToScreenPoint(new Vector3(-0.8f, 0f, 0f)); ui.OnDrag(ev); yield return Wait(0.8f);
                float xm = g.transform.position.x;
                float max = Tables.Cf("Guardian_X_Max", 1.5f);
                Check(Tables.Ci("Guardian_Fixed", 1) == 0 && Mathf.Abs(x0) < 0.05f && Mathf.Abs(xr - max) < 0.08f && Mathf.Abs(xm + 0.8f) < 0.08f,
                      $"화면을 끌면 대장 카피가 좌우로 (가운데 {x0:0.00} → 오른쪽 끝 {xr:0.00} · 범위 ±{max} → {xm:0.00})");
                Check(max + 0.6f < World.SlotXV[1] - 0.6f, $"움직이는 범위(±{max})가 옆 터렛 칸(±{World.SlotXV[1]})과 안 겹침");
                ev.position = World.I.cam.WorldToScreenPoint(new Vector3(1.2f, 0f, 0f)); ui.OnDrag(ev); yield return Wait(0.6f);
                Capture("r11_1_move");
            }

            // ---------- ② 특수 능력 ----------
            {
                var bb = GameObject.Find("SkillBurn"); var bd = GameObject.Find("SkillDome");
                Check(bb && bd && !b.CanBurn && !b.CanDome && b.skillCdG > 5f, $"단추 2개 (왼쪽 버닝 · 오른쪽 얼음 파동) · 시작 {Tables.Ci("Skill_First_Wait", 10)}초는 기다림");
                // 버닝
                b.skillCdG = 0; yield return null;
                var gt = b.run.Get("guardian");
                float cd0 = gt.Cooldown * b.BurnCdMul;
                bb.transform.Find("Face").GetComponent<Button>().onClick.Invoke(); yield return null;
                float cd1 = gt.Cooldown * b.BurnCdMul;
                Check(b.Burning && b.burnUsed == 1 && Mathf.Abs(cd1 - cd0 / Tables.Cf("Burn_Rate_Mul", 2f)) < 1e-4f && System.Math.Abs(b.BurnDmgMul - Tables.C("Burn_Dmg_Mul", 1.5)) < 1e-9
                      && Mathf.Abs(b.skillCdG - b.BurnCdMax) < 0.2f,
                      $"버닝: {Tables.Cf("Burn_Time", 8)}초 동안 공격 간격 {cd0:0.000} → {cd1:0.000}초 · 피해 x{b.BurnDmgMul} · 쿨타임 {b.skillCdG:0}초");
                bb.transform.Find("Face").GetComponent<Button>().onClick.Invoke(); yield return null;
                Check(b.burnUsed == 1, "쿨타임 중엔 다시 안 됨");
                // 얼음 파동: 적 4 (일반 3 · 보스 1)
                foreach (var e in b.enemies) e.dead = true;
                yield return null;
                var es = new System.Collections.Generic.List<Enemy>();
                string[] ids = { "fire", "moth", "jelly", "planet" };
                for (int i = 0; i < ids.Length; i++) es.Add(b.Spawn(ids[i], new Vector2(-3f + i * 2f, ids[i] == "planet" ? 4.5f : 2.5f)));
                yield return Wait(0.3f);
                var hp0 = es.ConvertAll(e => e.hp);
                b.skillCdD = 0; yield return null;
                bd.transform.Find("Face").GetComponent<Button>().onClick.Invoke(); yield return null;
                bool bossFreeze = Tables.Ci("Dome_Freeze_Boss", 1) == 1;   // 11-22 의견 "모두 이동 정지" → 보스도 얼림 + 받는 피해 증가
                bool frozen = es.TrueForAll(e => e.dead || ((e.row.IsBoss && !bossFreeze ? e.slow > 0.01f && !e.Paralyzed : e.Paralyzed) && (Tables.C("Dome_Vuln", 30) <= 0 || e.vuln > 0)));
                double pn = Tables.C("Dome_Dmg_Pct", 10) / 100.0, pb = Tables.C("Dome_Dmg_Boss_Pct", 3) / 100.0;
                bool dmg = true; var lost = new System.Text.StringBuilder();
                for (int i = 0; i < es.Count; i++)
                {
                    var e = es[i]; double want = e.maxHp * (e.row.IsBoss ? pb : pn), got = hp0[i] - e.hp;
                    lost.Append($"{e.row.name} -{got / e.maxHp * 100:0.#}% ");
                    if (!e.dead && System.Math.Abs(got - want) > want * 0.05 + 1) dmg = false;
                }
                Check(frozen && dmg && b.domeUsed == 1 && Mathf.Abs(b.skillCdD - b.DomeCdMax) < 0.2f,
                      $"얼음 파동: 모두 얼림(보스 포함) · 받는 피해 증가 · 체력 감소 ({lost.ToString().Trim()}) · 쿨타임 {b.skillCdD:0}초");
                yield return Wait(0.35f);
                Capture("r11_2_skills");
                yield return Wait(Tables.Cf("Dome_Freeze_Time", 3f) + 0.4f);
                Check(es.TrueForAll(e => e.dead || !e.Paralyzed), "얼림은 시간이 지나면 풀림");
                yield return Wait(Tables.Cf("Burn_Time", 8f));
                Check(!b.Burning && Mathf.Abs(b.BurnCdMul - 1f) < 1e-6f, "버닝 시간이 끝나면 원래대로");
                Capture("r11_3_cooldown");
            }
            {
                var bad = new System.Collections.Generic.HashSet<char>();
                foreach (var k in new[] { "skill_burn", "skill_dome", "skill_burn_on", "skill_dome_on" }) foreach (char ch in L.T(k)) if (ch > ' ' && font != null && !font.HasCharacter(ch)) bad.Add(ch);
                Check(bad.Count == 0 && Art.Icon("skill_burn") && Art.Icon("skill_frost"), "단추 글자 · 아이콘 (글꼴에 있음)");
            }
            Battle.LastSpeed = 1f;
            running = false;
        }
    }
}
