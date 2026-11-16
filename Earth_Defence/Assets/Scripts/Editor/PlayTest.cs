using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 배치 모드 자동 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.Run --play
    /// 깨끗한 세이브로 시작 → 로비 → 스테이지 1 전투 · 레벨업 카드 · 새 터렛 · 별 4개 → 돌파 → 연계 · 콤보 · 새로고침(광고 1번)
    /// · 터렛 6종이 모두 쏨 · 중간 보스 → 카드 1장 더 · 마지막 보스 → 승리 · 보상 · 로비 강화 · 순찰 · 세이브 · 패배.
    /// 화면은 Earth_Defence/Logs/PlayTest/*.png (1080x2340). 끝나면 시험 세이브는 지움.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayTest
    {
        const string Key = "EarthDef.PlayTest";
        public const string ModeKey = "EarthDef.PlayTest.Mode";

        static PlayTest()
        {
            if (!SessionState.GetBool(Key, false)) return;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode) new GameObject("PlayTestRunner").AddComponent<PlayTestRunner>();
            };
        }

        public static void Run() => Start("");
        /// <summary>11-16 밤 터렛 대기 효과 촬영: Logs/PlayTest/idle/p0_*.png(효과 끔) · p1_*.png(켬) → tools/idle_gif.py 가 GIF 비교 HTML</summary>
        public static void RunIdle() => Start("idle");

        static void Start(string mode)
        {
            SessionState.SetString(ModeKey, mode);
            TableConverter.Convert(true);
            SaveData.ResetAll();
            SessionState.SetBool(Key, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
        }

        public static void Finish(int errors)
        {
            SaveData.ResetAll();
            SessionState.SetBool(Key, false);
            SessionState.SetString(ModeKey, "");
            EditorApplication.Exit(errors == 0 ? 0 : 3);
        }
    }

    public class PlayTestRunner : MonoBehaviour
    {
        int errors;
        RenderTexture rt;
        const string OutDir = "Logs/PlayTest";
        static SaveData S => SaveData.Cur;
        static Battle B => Battle.I;

        void Awake()
        {
            Application.logMessageReceived += (msg, st, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception) { errors++; Debug.Log("[TEST] ERR " + msg + "\n" + st); }
            };
        }

        IEnumerator Start()
        {
            float timeout = Time.realtimeSinceStartup + 420f;
            var main = StartCoroutine(SessionState.GetString(PlayTest.ModeKey, "") == "idle" ? IdleShots() : Steps());
            while (running && Time.realtimeSinceStartup < timeout) yield return null;
            if (running) { errors++; Log("FAIL 시간 초과"); StopCoroutine(main); }
            Log($"DONE errors={errors}");
            PlayTest.Finish(errors);
        }

        bool running = true;

        /// <summary>대기 효과 촬영: 같은 판을 효과 끔(p0) · 켬(p1) 으로 3초씩, 1/30초 고정 간격 · 묶음 s0 = 레이저 · 벼락 · 관통포 · 드론, s1 = 벌집 · 미사일</summary>
        IEnumerator IdleShots()
        {
            yield return null;
            Directory.CreateDirectory(OutDir + "/idle");
            SetupCapture();
            yield return Wait(0.5f);
            var sets = new[] { new[] { "new_laser", "new_bolt", "new_rail", "new_drone" }, new[] { "new_hive", "new_missile" } };
            for (int run = 0; run < 4; run++)
            {
                int set = run / 2, pass = run % 2;
                Tables.SetC("Idle_Fx", pass);
                Game.StartStage(1, 77); yield return null;
                foreach (var id in sets[set]) Force(id);
                for (int i = 0; i < Mathf.CeilToInt(6f / Battle.StepSec); i++) { B.exp = 0; B.Tick(Battle.StepSec); }   // 레벨업 창이 안 뜨게 (뜨면 화면이 어두워짐)
                Time.captureDeltaTime = 1f / 30f;
                for (int f = 0; f < 30; f++) { B.exp = 0; yield return null; }       // 놓일 때 효과가 끝나게
                for (int f = 0; f < 90; f++)
                {
                    B.exp = 0;                                                        // 촬영 중 레벨업 창이 안 뜨게
                    yield return null;
                    Capture($"idle/s{set}p{pass}_{f:000}", true);
                }
                Time.captureDeltaTime = 0;
                if (pass == 1)
                    foreach (var u in B.units) if (u != B.Guardian) Check(u.idle != null && u.idle.Parts >= 4, $"{u.t.row.name} 대기 효과 조각 {u.idle?.Parts}");
                Game.ToLobby(); yield return null;
            }
            Tables.SetC("Idle_Fx", 1);
            running = false;
        }

        IEnumerator Steps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            yield return Wait(0.5f);

            // 1) 로비
            Check(Game.Lobby != null && World.I != null && UIRoot.I != null, "로비 · 전장 · 화면 만들어짐");
            Check(S.stage == 1 && S.best == 0 && Tables.Stages.Count == 30, $"스테이지 1 부터 (표 {Tables.Stages.Count}개)");
            Check(Tables.Turrets.Count == 7 && Tables.Cards.Count >= 90 && Tables.Enemies.Count >= 18, $"표: 터렛 {Tables.Turrets.Count} · 카드 {Tables.Cards.Count} · 적 {Tables.Enemies.Count}");
            foreach (var e in Tables.Enemies.Values) if (Art.Enemy(e) == null) Check(false, "적 그림 없음 " + e.id);
            foreach (var t in Tables.Turrets) if (Art.TurretUI(t.art) == null) Check(false, "터렛 그림 없음 " + t.id);
            foreach (var c in Tables.Cards) foreach (var (k, _) in c.stats) if (!KnownStats.Contains(k)) Check(false, $"카드 {c.id} 의 모르는 키 {k}");
            // 11-16 1차 수정안(아트 퀄업): 네온 탄 · 빛 더하기 · 새 가디언 그림
            Check(Art.AddMat != null, "빛 더하기 셰이더(EarthDef/SpriteAdd) 찾음");
            Check(Art.AddMat != null && Art.AddMat.HasProperty("_RendererColor") && Art.AddMat.HasProperty("_Flip"),
                  "빛 더하기 셰이더가 스프라이트 색 · 뒤집기를 받음 (_RendererColor · _Flip — 없으면 빛이 다 흰색)");
            foreach (var n in new[] { "bullet", "bullet_charge", "shell", "missile", "mini_missile" })
            { var sp = Art.Shot(n); Check(sp != null && sp.pivot.x / sp.rect.width > 0.7f, $"네온 탄 {n} (머리 피벗 {(sp ? sp.pivot.x / sp.rect.width : 0):0.00})"); }
            foreach (var n in new[] { "spark", "glow", "muzzle", "beam_core", "bolt2" }) if (Art.Fx(n) == null) Check(false, "효과 그림 없음 " + n);
            Check(Art.Guardian != null && Art.Guardian.rect.width == 324 && Art.GuardianGun != null && Art.TurretUI("guardian") != null,
                  $"가디언 = 카치건 형태 몸 {Art.Guardian?.rect.width}x{Art.Guardian?.rect.height} + 따로 도는 소총 + 카드용 한 장");
            Capture("01_lobby");

            // 2) 전투 시작 → 가디언 연사로 적을 잡음
            Game.StartStage(1, 1234); yield return null;
            Check(B != null && B.run.turrets.Count == 1 && B.units.Count == 1, "전투 시작 = 가디언만");
            FF(14f, 2);
            Check(B.kills >= 3 && B.enemies.Count > 0, $"14초: 처치 {B.kills} · 적 {B.enemies.Count} · Lv {B.level}");
            Check(B.Guardian.shotsFired > 20, $"가디언 연사 {B.Guardian.shotsFired}번");
            Check(GameObject.Find("spark") != null && GameObject.Find("muzzle") != null, "맞으면 불꽃 튐 · 총구 섬광 나옴");
            Check(World.SpawnY <= World.I.PlayTop - 0.5f, $"적은 위쪽 UI 아래에서 나옴 (나오는 줄 {World.SpawnY:0.0} · UI 아래 끝 {World.I.PlayTop:0.0})");
            {   // 피해 숫자: 같은 적이 바로 또 맞으면 합침 (카치건 방식)
                int n0 = Fx.NumCount; var key = new object();
                Fx.Number(Vector2.zero, 10, false, key); Fx.Number(Vector2.zero, 15, false, key); Fx.Number(Vector2.zero, 5, true, key);
                Check(Fx.NumCount == n0 + 1, $"같은 적 피해 숫자 3번 → 1개로 합침 ({Fx.NumCount - n0}개)");
            }
            {   // 적이 왼쪽 → 가디언이 왼쪽을 봄(그림 뒤집기 · 총구도 왼쪽), 오른쪽 → 다시 오른쪽
                var saved = new List<Enemy>(B.enemies);
                var e0 = saved.Find(e => !e.dead);
                var p0 = e0.transform.position;
                float gx = B.Guardian.transform.position.x;
                B.enemies.Clear(); B.enemies.Add(e0);
                e0.transform.position = new Vector3(gx - 3f, 0f, 0);
                for (int i = 0; i < 150 && !B.Guardian.FaceLeft; i++) B.Guardian.Step(Battle.StepSec);
                bool l = B.Guardian.FaceLeft && B.Guardian.GetComponent<SpriteRenderer>().flipX;
                e0.transform.position = new Vector3(gx + 3f, 0f, 0);
                for (int i = 0; i < 150 && B.Guardian.FaceLeft; i++) B.Guardian.Step(Battle.StepSec);
                Check(l && !B.Guardian.FaceLeft, "적 쪽으로 몸을 돌림 (왼쪽 → 오른쪽)");
                var hd = (Vector2)e0.transform.position - (Vector2)B.Guardian.transform.position;
                float want = Mathf.Atan2(hd.y - 0.7f, hd.x - 0.55f) * Mathf.Rad2Deg;
                Check(Mathf.Abs(Mathf.DeltaAngle(B.Guardian.GunAngle, want)) < 12f, $"총이 적 쪽으로 돎 (총 {B.Guardian.GunAngle:0}° · 적 {want:0}°)");
                // 서서 쏠 때 몸이 튀지 않음 (11-16 "방방 거림")
                B.Guardian.targetX = B.Guardian.transform.position.x;
                for (int i = 0; i < 30; i++) B.Guardian.Step(Battle.StepSec);
                float y0 = B.Guardian.transform.position.y, ymax = 0; int f0 = B.Guardian.shotsFired;
                for (int i = 0; i < 90; i++) { B.Guardian.Step(Battle.StepSec); ymax = Mathf.Max(ymax, Mathf.Abs(B.Guardian.transform.position.y - y0)); }
                Check(B.Guardian.shotsFired > f0 && ymax < 0.005f, $"서서 쏠 때 몸이 안 튐 (쏨 {B.Guardian.shotsFired - f0} · 흔들림 {ymax:0.000})");
                e0.transform.position = p0;
                B.enemies.Clear(); B.enemies.AddRange(saved);
            }
            yield return Wait(0.3f);
            Capture("02_battle_start");

            // 3) 레벨업 → 카드 3장
            B.GainExp(B.need + 0.1); B.Tick(Battle.StepSec);
            Check(B.st == Battle.St.Pick && B.offer.Count == 3, $"레벨업 → 카드 {B.offer.Count}장 ({string.Join(", ", B.offer.ConvertAll(c => c.name))})");
            // 시험용으로 카드 고정: 새 터렛 관통포 · 벼락탑 · 가디언 강화
            B.offer = new List<CardRow> { Tables.CardById["g_dmg"], Tables.CardById["new_bolt"], Tables.CardById["l_dmg"] };
            Check(!B.run.Eligible(Tables.CardById["l_dmg"]), "없는 터렛(레이저)의 강화 카드는 안 나옴");
            Game.BattleUI.ShowPick();
            yield return Wait(0.5f);
            Capture("03_levelup");
            Check(B.CanRefresh && B.run.refreshAd == 1 && B.run.refreshFree == 0, "새로고침 = 광고 1번 (원작대로)");
            B.Refresh();
            Check(!B.CanRefresh && B.offer.Count > 0, "새로고침 1번 뒤엔 못 함");
            B.offer[0] = Tables.CardById["new_rail"];
            B.Choose(0);
            Check(B.run.Owns("rail") && B.units.Count == 2 && B.st == Battle.St.Play, "새 터렛 관통포 → 칸 1");
            Check(B.units[1].transform.Find("Pad") == null, "터렛 아래 빛 고리 없음 (대기 효과를 넣어 뺌)");
            Check(B.units[1].idle != null && B.units[1].idle.Parts >= 4, $"관통포 대기 효과 (조각 {B.units[1].idle?.Parts})");

            // 4) 터렛 4칸 채우기
            foreach (var id in new[] { "new_laser", "new_drone", "new_bolt" }) Force(id);
            Check(B.run.turrets.Count == 5 && B.run.FreeSlots == 0, $"가디언 + 터렛 4칸 ({B.run.turrets.Count - 1})");
            Check(!B.run.Eligible(Tables.CardById["new_hive"]), "칸이 차면 새 터렛 카드 안 나옴");

            // 5) 별 4개 → 돌파 → 연계
            var rail = B.run.Get("rail");
            Check(rail.stars == 1 && rail.tier == 1, "새 터렛 = 별 1");
            Force("r_dmg"); Force("r_dmg"); Force("r_dmg");
            Check(rail.stars == 4, $"강화 3번 → 별 {rail.stars}");
            Check(!B.run.Eligible(Tables.CardById["r_cd"]) && B.run.Eligible(Tables.CardById["r_bA"]), "별이 다 차면 강화 카드 대신 돌파 카드");
            Check(!B.run.Eligible(Tables.CardById["r_cA1"]), "돌파 전엔 연계 카드 안 나옴");
            Force("r_bA");
            Check(rail.tier == 2 && rail.stars == 0 && rail.Has("explode"), "돌파 → 2단계 · 별 0 · 폭발탄");
            Check(B.run.Eligible(Tables.CardById["r_cA1"]) && !B.run.Eligible(Tables.CardById["r_cB1"]), "돌파한 줄의 연계만 열림");
            Check(rail.S("dmg") == 150, $"탄약 강화 3번 = 피해 +{rail.S("dmg")}%");
            Check(B.run.Eligible(Tables.CardById["cb_r_l"]) && !B.run.Eligible(Tables.CardById["cb_g_m"]), "콤보: 관통포+레이저 열림 · 미사일 없으면 안 열림");
            Force("cb_r_l"); Force("l_bB"); Force("b_bB"); Force("d_bA"); Force("g_bB");

            // 6) 피해 공식: 약점 x1.5 · 저항 x0.6
            var g = B.run.turrets[0];
            double weak = MinCalc(g, "pebble"), neutral = MinCalc(g, "moth"), resist = MinCalc(B.run.Get("drone"), "jelly");
            double neutralL = MinCalc(B.run.Get("drone"), "moth");
            Check(System.Math.Abs(weak / neutral - 1.5) < 0.02, $"약점 배율 {weak / neutral:0.00}");
            Check(System.Math.Abs(resist / neutralL - 0.6) < 0.02, $"저항 배율 {resist / neutralL:0.00}");

            // 7) 40초 진행 → 모든 터렛이 쏨
            int k0 = (int)B.kills;
            FF(40f);
            foreach (var u in B.units) Check(u.fired > 0, $"{u.t.row.name} 쏨 ({u.fired}번)");
            {   // 대기 효과: 벼락탑 · 레이저 포에 조각이 붙고, 쏜 직후보다 쏘기 직전이 더 셈
                var lu0 = B.run.Get("laser").unit;
                foreach (var u in B.units) if (u != B.Guardian) Check(u.idle != null && u.idle.Parts >= 4, $"{u.t.row.name} 대기 효과 조각 {u.idle?.Parts}");
                Check(lu0.Charge01 >= 0f && lu0.Charge01 <= 1f, $"충전 0~1 ({lu0.Charge01:0.00})");
            }
            var lu = B.run.Get("laser").unit;
            {   // 빛 쪼개기(관통포가 맞히면 20% 작은 레이저)를 확률에 안 기대고: 산 적에게 40번 맞힘 → 안 나올 확률 0.8^40
                Beam.LastMiniOrigin = null;
                var tgt = B.enemies.Find(e => !e.dead && e.Pos.y < World.SpawnY - 1f);
                if (tgt != null) { tgt.hp = tgt.maxHp = 1e12; for (int i = 0; i < 40 && !Beam.LastMiniOrigin.HasValue; i++) Combat.Hit(B.run.Get("rail"), tgt, 1); }
            }
            Check(Beam.LastMiniOrigin.HasValue && Vector2.Distance(Beam.LastMiniOrigin.Value, lu.Head) < 0.3f,
                  $"작은 레이저(빛 쪼개기)가 레이저 포에서 나감 (거리 {(Beam.LastMiniOrigin.HasValue ? Vector2.Distance(Beam.LastMiniOrigin.Value, lu.Head) : -1):0.00})");
            Check(B.kills > k0 + 20, $"40초 동안 처치 {B.kills - k0}");
            Capture("04_battle_full");
            yield return Wait(0.4f);
            Capture("04b_battle_full");

            // 8) 중간 보스 (2분) → 잡으면 카드 1장 더
            B.time = Tables.Cf("Mid_Boss_Time") - 0.1f; FF(0.5f, 1);
            var elite = B.enemies.Find(e => e.row.IsElite);
            Check(elite != null, $"2분: 중간 보스 {elite?.row.name}");
            yield return Wait(0.3f);
            Capture("05_elite");
            if (elite != null) { Combat.Raw(elite, elite.hp + 1, false, g); B.Tick(Battle.StepSec); }
            for (int i = 0; i < 25 && B.st == Battle.St.Pick && !B.offerIsBoss; i++) { B.Choose(0); B.Tick(Battle.StepSec); }   // 같이 오른 레벨업 카드 먼저
            Check(B.st == Battle.St.Pick && B.offerIsBoss, "중간 보스 처치 → 카드 1장 더");
            yield return Wait(0.3f);
            Capture("06_elite_card");
            B.Choose(0);

            // 9) 마지막 보스 → 승리
            B.time = Tables.Cf("Boss_Time") - 0.1f; FF(3f);
            Check(B.boss != null && !B.boss.dead, $"3분 30초: 보스 {B.boss?.row.name}");
            FF(4f);
            yield return Wait(0.3f);
            Capture("07_boss");
            double gold0 = S.gold;
            Combat.Raw(B.boss, B.boss.hp + 1, false, g); B.Tick(Battle.StepSec);
            Check(B.st == Battle.St.End && B.won, "보스 처치 → 승리");
            Check(S.best == 1 && S.stage == 2 && S.gold > gold0 && B.FirstClear, $"보상 골드 +{B.RewardGold} · 조각 +{B.RewardShards} · 다음 스테이지 열림");
            yield return Wait(0.5f);
            Capture("08_result_win");

            // 10) 로비 강화
            Game.ToLobby(); yield return null;
            S.gold += 100000; for (int i = 0; i < S.shards.Count; i++) S.shards[i] += 50;
            int lv0 = S.TurretLv("rail");
            Check(Meta.UpgradeTurret("rail") && S.TurretLv("rail") == lv0 + 1, "관통포 레벨 업 (조각 + 골드)");
            double hp0 = S.BaseHp;
            Check(Meta.UpgradeBase() && S.BaseHp > hp0, $"기지 레벨 업 → 체력 {hp0} → {S.BaseHp}");
            var rt2 = new TurretInst { row = Tables.TurretById["rail"], metaLv = 10 };
            Check(rt2.Cooldown < Tables.TurretById["rail"].cd && System.Math.Abs(rt2.Ordinary - 0.72) < 0.001, $"터렛 Lv10 = 피해 +{rt2.Ordinary:P0} · 쿨 {rt2.Cooldown:0.00}초");

            // 11) 순찰 2시간
            S.patrolFrom = SaveData.Clock; SaveData.NowOffset = 7200;
            double exp = 2 * (Tables.C("Patrol_Gold_Hour") + Tables.C("Patrol_Gold_Per_Stage") * (S.best - 1));
            double g1 = S.gold;
            Game.Lobby.Refresh(); yield return Wait(0.3f);
            Capture("09_lobby_after");
            var (pg, ps) = Meta.ClaimPatrol();
            Check(System.Math.Abs(pg - exp) < 1 && System.Math.Abs(S.gold - g1 - exp) < 1 && ps == 2 * Tables.Ci("Patrol_Shards_Hour"), $"순찰 2시간 → 골드 {pg} · 조각 {ps}");
            Check(!Meta.CanClaimPatrol, "받은 뒤엔 다시 0부터");
            SaveData.NowOffset = 0;

            // 12) 세이브 → 다시 읽기
            S.Save();
            var back = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveData.Key));
            Check(back.best == S.best && back.baseLv == S.baseLv && back.tLv[1] == S.tLv[1] && System.Math.Abs(back.gold - S.gold) < 1, "세이브 → 다시 읽기 같음");

            // 13) 패배
            Game.StartStage(2, 99); yield return null;
            FF(3f);
            B.HurtBase(B.baseHp + 1, Vector2.zero); B.Tick(Battle.StepSec);
            Check(B.st == Battle.St.End && !B.won && S.best == 1, $"기지 체력 0 → 패배 (보상 {B.RewardGold})");
            yield return Wait(0.4f);
            Capture("10_result_lose");

            // 14) 손대지 않고 한 판 (자동으로 첫 카드) — 레벨 · 적 수 · 오류
            Game.StartStage(1, 7); yield return null;
            int peak = 0;
            for (float t = 0; t < 200f && B.st != Battle.St.End; t += 1f) { FF(1f); peak = Mathf.Max(peak, B.enemies.Count); }
            Log($"200초: Lv {B.level} · 처치 {B.kills} · 기지 {Num.Comma(B.baseHp)}/{Num.Comma(B.baseMax)} · 적 최대 {peak} · 터렛 {B.run.turrets.Count - 1}");
            Check(B.level >= 5 && peak <= Tables.Ci("Max_Alive") + 30, $"한 판 진행 (Lv {B.level})");
            yield return Wait(0.4f);
            Capture("11_auto_run");
            running = false;
        }

        // ---------- 시험 도우미 ----------
        static readonly HashSet<string> KnownStats = new()
        {
            "dmg", "shots", "shots_free", "pierce", "cd", "dur", "area", "range", "count", "count_free", "hits", "crit", "slow", "slow_max", "tick",
            "explode", "explode_dmg", "explode_area", "split", "split_dmg", "burn", "burn_amp", "fire_zone", "paralyze", "para_time", "vuln",
            "chain", "chain_dmg", "beam_split", "sweep", "dmg_by_slow", "field_explode", "pull", "pulse", "pulse_area", "pulse_end", "echo",
            "echo_hits", "charge", "charge_speed", "charge_shots", "dmg_vs_para", "hit_laser", "kill_bolt", "kill_bee", "kill_explode", "fire_add", "hit_chain",
        };

        /// <summary>카드 고르기를 건너뛰고 바로 (시험용)</summary>
        static void Force(string cardId)
        {
            B.offer = new List<CardRow> { Tables.CardById[cardId] };
            B.st = Battle.St.Pick; B.pendingLevel++; B.offerIsBoss = false;
            B.Choose(0);
        }

        /// <summary>sec 초 빨리 감기. 카드 창이 뜨면 mode 0 = 첫 카드 · 1 = 멈춤 · 2 = 고르지 않고 넘김</summary>
        static void FF(float sec, int mode = 0)
        {
            int n = Mathf.CeilToInt(sec / Battle.StepSec);
            for (int i = 0; i < n; i++)
            {
                if (B == null || B.st == Battle.St.End) return;
                if (B.st == Battle.St.Pick)
                {
                    if (mode == 0) B.Choose(0);
                    else if (mode == 1) return;
                    else { B.pendingLevel = 0; B.pendingBoss = 0; B.st = Battle.St.Play; }
                }
                if (B.st == Battle.St.Pause) B.Pause(false);
                B.Tick(Battle.StepSec);
            }
        }

        static double MinCalc(TurretInst t, string enemyId)
        {
            var e = B.Spawn(enemyId, new Vector2(0, 2));
            double best = double.MaxValue;
            for (int i = 0; i < 40; i++) { double d = Combat.Calc(t, e, 100, out bool crit); if (!crit) best = System.Math.Min(best, d); }
            e.dead = true;
            return best;
        }

        void Check(bool ok, string what)
        {
            if (ok) Log("OK " + what);
            else { errors++; Log("FAIL " + what); }
        }
        static void Log(string s) => Debug.Log("[TEST] " + s);

        static IEnumerator Wait(float s)
        {
            float t = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < t) yield return null;
        }

        void SetupCapture()
        {
            rt = new RenderTexture(1080, 2340, 24);
            var cam = Camera.main;
            cam.targetTexture = rt;
            cam.ResetAspect();
            World.I.Fit();
            var safe = Object.FindFirstObjectByType<SafeArea>();
            if (safe) { safe.enabled = false; ((RectTransform)safe.transform).Fill(); }
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.isRootCanvas || c.renderMode == RenderMode.WorldSpace) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 5;
                c.sortingOrder = 32767;
                var sc = c.GetComponent<CanvasScaler>();
                if (sc) sc.matchWidthOrHeight = 0;
            }
        }

        readonly HashSet<char> glyphMiss = new();

        void Capture(string name, bool quiet = false)
        {
            var cam = Camera.main;
            // 배치 모드는 처음 화면이 작아서 그때 만든 글자가 흐린 채로 남음 → 찍기 전에 글자를 다시 만들게 (폰에선 생기지 않음)
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                t.SetAllDirty();
                if (!t.isActiveAndEnabled || t.font == null) continue;
                foreach (char ch in t.text)
                    if (ch > ' ' && !t.font.HasCharacter(ch) && glyphMiss.Add(ch)) Check(false, $"글꼴에 없는 글자 '{ch}' (U+{(int)ch:X4}) — {t.text}");
            }
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"{OutDir}/{name}.png", tex.EncodeToPNG());
            Destroy(tex);
            if (!quiet) Log("찍음 " + name);
        }
    }
}
