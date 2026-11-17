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
        /// <summary>11-16 저녁 소리 검토: Logs/Sound/ 에 배경음 · 효과음 wav + 전투 장면 효과음 기록(json) → tools/sound_review.py</summary>
        public static void RunSound() => Start("sound");
        /// <summary>11-16 밤 터렛 시험장 씬 시험 (Assets/Scenes/TurretLab.unity)</summary>
        public static void RunLab() => Start("lab");
        /// <summary>3차 시험 (11-16 밤): 상점 · 다이아 · 결제 [초안] · 기지 외형 · 스킨 · 필살기 · 무한 모드 · 엘리트 군단 · 광고 2배 · 클라우드 [초안]</summary>
        public static void RunP3() => Start("p3");
        /// <summary>11-17 성능 측정 (Editor/PlayTestPerf.cs) → Logs/Perf/perf_<이름>.json</summary>
        public static void RunPerf() => Start("perf");
        /// <summary>11-17 밸런스 측정 (Editor/PlayTestBal.cs) → Logs/Bal/bal_<이름>.json</summary>
        public static void RunBal() => Start("bal");

        static void Start(string mode)
        {
            SessionState.SetString(ModeKey, mode);
            TableConverter.Convert(true);
            SaveData.ResetAll();
            SessionState.SetBool(Key, true);
            if (mode == "lab") SceneBuilder.BuildLab();
            EditorSceneManager.OpenScene(mode == "lab" ? "Assets/Scenes/TurretLab.unity" : "Assets/Scenes/Main.unity");
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

    public partial class PlayTestRunner : MonoBehaviour
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
            string mode = SessionState.GetString(PlayTest.ModeKey, "");
            float timeout = Time.realtimeSinceStartup + (mode == "bal" ? 7000f : 420f);
            var main = StartCoroutine(mode == "idle" ? IdleShots() : mode == "sound" ? SoundDump() : mode == "lab" ? LabSteps() : mode == "p3" ? P3Steps() : mode == "perf" ? PerfSteps() : mode == "bal" ? BalSteps() : Steps());
            while (running && Time.realtimeSinceStartup < timeout) yield return null;
            if (running) { errors++; Log("FAIL 시간 초과"); StopCoroutine(main); }
            Log($"DONE errors={errors}");
            PlayTest.Finish(errors);
        }

        bool running = true;

        // ---------- 3차 (RunP3) ----------
        IEnumerator P3Steps()
        {
            yield return null;
            Lobby.NoIntro = true; Popup.CloseAll();
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            yield return Wait(0.5f);
            Check(Tables.Frames.Count == 7 && Tables.Skins.Count == 11 && Tables.Products.FindAll(x => x.kind == "dia").Count == 5 && Tables.ShopItems.Count >= 8, $"3차 표: 외형 {Tables.Frames.Count} · 스킨 {Tables.Skins.Count} · 상품 {Tables.Products.Count} · 상점 {Tables.ShopItems.Count}");
            double rsum = 0; foreach (var (_, r) in Tables.BoxRates) rsum += r;
            Check(System.Math.Abs(rsum - 100) < 0.01, $"상자 확률 합 {rsum}%");
            Check(Art.HueMat != null && Art.HueMat.HasProperty("_Hue") && Art.HueMat.HasProperty("_RendererColor"), "스킨 색 돌리기 셰이더 (EarthDef/SpriteHue)");
            foreach (var k in Tables.Skins) if (Art.TurretSkinUI(Tables.TurretById[k.turret].art) == null) Check(false, "스킨 그림 없음 " + k.id);
            Check(S.dia == Tables.C("Start_Dia"), $"처음 다이아 {S.dia}");
            S.best = 30; S.gold = 1e8;

            // --- 상점
            var free = Tables.ShopItems.Find(i => i.kind == "free_box");
            int inv0 = S.gear.Count + S.chips.Count;
            Check(Store.Buy(free) != null && S.gear.Count + S.chips.Count == inv0 + 1 && Store.Left(free) == 0 && Store.Buy(free) == null, "무료 상자 하루 1번");
            S.dia = 100000;
            var gb10 = Tables.ShopItems.Find(i => i.id == "i_gear10"); int g0 = S.gear.Count; double d0 = S.dia;
            Check(Store.Buy(gb10) != null && S.gear.Count == g0 + 11 && S.dia == d0 - gb10.costDia, $"장비 상자 10+1 → 장비 +{S.gear.Count - g0} · 다이아 -{d0 - S.dia}");
            {   // 상자 등급이 표 확률대로 (2만 번)
                var rnd = new System.Random(5); var cnt = new int[8];
                for (int i = 0; i < 100000; i++) cnt[Store.BoxGrade(rnd)]++;      // 6차: 2만 → 10만 번 (표본 오차로 1% 넘게 벗어난 적이 있음)
                bool ok = true; string log = "";
                foreach (var (g, r) in Tables.BoxRates) { double got = cnt[g] / 1000.0; log += $"{g}:{got:0.0} "; if (System.Math.Abs(got - r) > 0.8) ok = false; }
                Check(ok, "상자 등급 확률 = 표 (" + log + ")");
            }
            var stItem = Tables.ShopItems.Find(i => i.kind == "stamina"); int st0 = Meta.Stamina;
            Check(Store.Buy(stItem) != null && Meta.Stamina == st0 + 30, "스태미나 30 사기");
            // --- 결제 [초안]
            var p1 = Tables.Products[0]; d0 = S.dia;
            int n1 = Store.Purchase(p1), n2 = Store.Purchase(p1);
            Check(n1 == p1.dia * 2 && n2 == p1.dia && S.dia == d0 + n1 + n2, $"[초안] 결제: 첫 구매 2배 {n1} → 다음 {n2}");

            // ---- 2차 수정안 (11-17)
            Check(L.T("equip") == "착용" && L.T("equipped") == "착용 중" && L.T("frame_change") == "변경", "글자: 착용 · 착용 중 · 변경");
            Check(Tables.Grade(3).color.b > Tables.Grade(3).color.r + 0.3f && Tables.Grade(4).color.b > Tables.Grade(4).color.g + 0.2f, "등급 색: 희귀 파랑 · 영웅 보라");
            {
                var got = Store.Buy(Tables.ShopItems.Find(i => i.id == "i_chip10"));
                Check(got != null && got.Count == 11 && got.TrueForAll(g => g.icon.StartsWith("chip_") && g.grade > 0), "상자 → 아이콘 11개 (등급)");
                Game.ToLobby(); yield return null; Popup.CloseAll();
                ShopTab.GotPopup(got); yield return Wait(2.2f);
                Capture("40_box_icons");
                Popup.CloseAll();
                for (int i = 0; i < 3; i++) Gear.NewGear(2, "helmet"); for (int i = 0; i < 3; i++) Gear.NewChip(1, "ch_crit");
                Check(Gear.GearMergeCount() >= 1 && Gear.ChipMergeCount() >= 1, $"일괄 합성: 장비 {Gear.GearMergeCount()} · 칩 {Gear.ChipMergeCount()}");
                Game.Lobby.SetTab(2); yield return Wait(0.3f);
                Capture("41_gear_stats");
                Game.Lobby.gearTab.MergeAll(); yield return Wait(0.3f);
                Capture("42_merge_popup");
                Popup.CloseAll();
                var mg = Gear.MergeAllGear(); var mc = Gear.MergeAllChipList();
                Check(mg.Count >= 1 && mc.Count >= 1 && Gear.GearMergeCount() == 0, $"일괄 합성 → 새 장비 {mg.Count} · 칩 {mc.Count}");
                Game.Lobby.SetTab(1); yield return null;
                Game.Lobby.UpPopup(Tables.TurretById["rail"]); yield return Wait(0.3f);
                Capture("43_up_popup");
                int lv0 = S.TurretLv("rail"); S.shards[S.TIdx("rail")] += 999;
                Check(Meta.UpgradeTurret("rail") && S.TurretLv("rail") == lv0 + 1, "강화 창에서 강화");
                Popup.CloseAll();
                Game.Lobby.SetTab(0); yield return Wait(0.3f);
                Capture("44_lobby_small");
                Check(World.I != null && GameObject.Find("MoonRabbit") != null, "달토끼");
                bool rd0 = World.I.RabbitDown, changed = false; float w0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - w0 < 2f) { if (World.I.RabbitDown != rd0) changed = true; yield return null; }
                Check(changed, "달토끼가 방아를 찧음 (그림이 바뀜)");
                Game.StartStage(5, 11); yield return null;
                var met = B.Spawn("fire", new Vector2(0, 5)); var el = B.Spawn("big", new Vector2(0, 6)); var bo = B.Spawn("queen", new Vector2(0, 7));
                Check(System.Math.Abs(met.size - 1.06f) < 0.01f && System.Math.Abs(el.size - 1.35f * 3f) < 0.01f && System.Math.Abs(bo.size - 1.9f * 3.5f) < 0.01f,
                      $"크기: 운석 {met.size} · 중간 보스 {el.size} · 보스 {bo.size}");
                foreach (var id in new[] { "new_rail", "r_dmg", "r_dmg", "r_dmg", "r_bA" }) Force(id);
                var ru = B.run.Get("rail").unit; var mpb2 = new MaterialPropertyBlock(); ru.GetComponent<SpriteRenderer>().GetPropertyBlock(mpb2);
                Check(ru.Broken && ru.GetComponent<SpriteRenderer>().sharedMaterial == Art.HueMat && mpb2.GetFloat("_SetHue") >= 0, "돌파한 관통포 = 붉은색");
                B.Tick(Battle.StepSec);
                Game.BattleUI.CardList(B.run.Get("rail")); yield return Wait(0.3f);
                Capture("45_card_list");
                Popup.CloseAll();
                B.time = Tables.Cf("Mid_Boss_Time") - 0.1f; FF(0.3f, 2); yield return Wait(0.4f);
                Capture("46_elite_toast");
                B.time = Tables.Cf("Boss_Time") - 0.1f; FF(1.5f, 2); yield return Wait(0.5f);
                Capture("47_boss_big");
            }
            {   // ---- 3차 수정안 (11-17)
                Check(L.T("chip") == "회로" && Tables.ChipById["ch_atk"].name == "공격" && L.T("skin") == "외형", "글자: 칩 → 회로 · 스킨 → 외형");
                var gw = Gear.NewGear(4, "weapon"); var cw = Gear.NewChip(3, "ch_atk");
                Check(gw.Name == "영웅 칼날 소총" && gw.Icon == "g_weapon_4" && cw.Name == "공격" && cw.Icon == "chip_3" && Art.Icon(gw.Icon) != null && Art.Icon(cw.Icon) != null,
                      $"등급별 이름 · 아이콘: {gw.Name} · {cw.Name}");
                Store.BuySkin(Tables.SkinOf("rail"));
                Check(Art.TurretIcon(Tables.TurretById["rail"]) == Art.TurretSkinUI("rail"), "외형 착용 → 터렛 아이콘도 외형 그림");
                Game.ToLobby(); yield return null; Popup.CloseAll();
                Game.Lobby.SetTab(1); yield return Wait(0.3f);
                Check(GameObject.Find("Skin") == null && GameObject.Find("Frame") == null, "터렛 탭에서 외형 단추 없앰");
                Capture("50_turret_tab_skinicon");
                Gear.Equip(gw); Gear.Equip(Gear.NewGear(6, "core")); Gear.Equip(Gear.NewGear(7, "helmet"));
                { var (pp, ss) = Gear.FreeSlotFor(cw); Gear.PutChip(cw, pp, ss); }
                foreach (var q in new[] { 5, 7, 2 }) { var cx = Gear.NewChip(q, "ch_atk"); var (pp, ss) = Gear.FreeSlotFor(cx); if (pp >= 0) Gear.PutChip(cx, pp, ss); }
                Game.Lobby.SetTab(2); yield return Wait(0.3f);
                Check(GameObject.Find("MiniSlot") != null, "장비 탭 회로 작은 칸");
                Capture("51_gear_minislots");
                Game.Lobby.gearTab.ChipPlace(cw); yield return Wait(0.3f);
                Capture("52_chip_place");
                Popup.CloseAll();
                var got = new List<GotItem> { GotItem.Of(Gear.NewGear(4, "suit")), GotItem.Of(Gear.NewGear(6, "core")), GotItem.Of(Gear.NewChip(7, "ch_dmg")), GotItem.Of(Gear.NewChip(2, "ch_hp")) };
                ShopTab.GotPopup(got); yield return Wait(1.2f);
                int glows = 0; foreach (var gl in Object.FindObjectsByType<GlowPulse>(FindObjectsSortMode.None)) glows++;
                Check(glows == 3, $"영웅 이상 획득 테두리 빛 {glows}개 (4개 중 영웅 · 신화 · 궁극)");
                Capture("53_got_glow");
                Popup.CloseAll();
            }
            {   // ---- 4차 수정안 (11-17)
                var anyLabel = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None);
                bool soft = false, oldOutline = false;
                foreach (var tx in anyLabel) { if (tx.GetComponent<SoftOutline>()) soft = true; if (tx.GetComponent<UnityEngine.UI.Outline>()) oldOutline = true; }
                Check(soft && !oldOutline, "글자 테두리 8방향 (찢김 고침)");
                Check(Art.Icon("g_weapon_7") != null && Art.Icon("g_weapon_7").rect.width >= 224 && Art.Icon("chip_7").rect.width >= 224 && Art.Icon("info") != null, "등급 아이콘 224px · 정보 아이콘");
                { int bk = S.best; S.best = 1;     // 밸런스 3차: 보상 등급이 진행에 맞춰 오르므로 이름 검사는 스테이지 1 기준
                  Check(Meta.RewardText("chip", 3) == "희귀 회로" && Meta.RewardText("gear", 1) == "일반 장비", $"등급 이름 통일: {Meta.RewardText("chip", 3)}"); S.best = bk; }
                Check(Lobby.Tabs.Length == 7 && GameObject.Find("Tab_looks") != null, "아래 탭에 외형");
                Game.Lobby.SetTab(6); yield return Wait(0.3f);
                Check(GameObject.Find("Sub0") != null && GameObject.Find("S_sk_rail") != null, "외형 탭 = 터렛 외형 목록");
                Capture("60_looks_tab");
                // 전투 탭: 스테이지 칸 작게 · 순찰은 [전투 시작] 아래 · 광고 2배 · 토스트는 사이 빈 곳
                Lobby.enemiesOpen = false; Game.Lobby.SetTab(0);
                // ---- 밸런스 3차 (11-17): 골드 부스터 · 골드 금고 · 회로 % · 방어력 비율 · 엘리트 난이도 · 무한 모드 골드 · 미션 보상
                {
                    var boost = Tables.Products.Find(x => x.kind == "boost");
                    long c0 = SaveData.Clock;
                    Check(boost != null && Store.Purchase(boost) == 1 && Store.GoldBoostOn && Store.GoldBoostDaysLeft == Tables.Ci("Gold_Boost_Days", 30), $"골드 부스터: {Store.GoldBoostDaysLeft}일");
                    Store.Purchase(boost);
                    Check(Store.GoldBoostDaysLeft == 2 * Tables.Ci("Gold_Boost_Days", 30), $"골드 부스터 다시 사면 이어 붙음 ({Store.GoldBoostDaysLeft}일)");
                    Game.StartStage(2, 41); yield return null;
                    B.goldGot = 0; B.time = 200; double gb0 = S.gold; B.End(true); double withBoost = S.gold - gb0; Popup.CloseAll();
                    S.iapOnce.RemoveAll(k => k.StartsWith("goldboost:"));
                    Game.StartStage(2, 41); yield return null;
                    B.goldGot = 0; B.time = 200; gb0 = S.gold; B.End(true); double noBoost = S.gold - gb0; Popup.CloseAll();
                    Check(!Store.GoldBoostOn && System.Math.Abs(withBoost - noBoost * Tables.C("Gold_Boost_Mul", 2)) <= 2, $"골드 부스터: 판 골드 {Num.F(noBoost)} → {Num.F(withBoost)}");
                    var vault = Tables.ShopItems.Find(x => x.id == "i_gold3");
                    S.dia += 5000; double gv = S.gold;
                    Check(vault != null && Store.Buy(vault) != null && System.Math.Abs(S.gold - gv - System.Math.Floor(vault.value * S.PatrolGoldHour)) < 1, $"골드 금고 = 순찰 {vault?.value:0}시간치 (+{Num.F(S.gold - gv)})");
                    // 회로 %: 기지 공격력 +% · 기지 체력 +% · 보호막 = 체력의 %
                    foreach (var c in S.chips) c.at = -1;
                    var ca = Gear.NewChip(7, "ch_atk"); var ch = Gear.NewChip(7, "ch_hp"); var cs = Gear.NewChip(7, "ch_shield");
                    foreach (var c in new[] { ca, ch, cs }) { var (pp, ss) = Gear.FreeSlotFor(c); Gear.PutChip(c, pp, ss); }
                    var lo3b = Loadout.Build(S);
                    Check(System.Math.Abs(lo3b.baseAtkPct - ca.Val) < 0.01 && System.Math.Abs(lo3b.baseHpPct - ch.Val) < 0.01 && lo3b.ShieldOf(S) > lo3b.HpOf(S) * 0.2,
                          $"회로 %: 기지 공격력 +{lo3b.baseAtkPct}% · 체력 +{lo3b.baseHpPct}% · 보호막 {Num.F(lo3b.ShieldOf(S))}");
                    lo3b.reduce = Tables.C("Reduce_K", 240);
                    Check(System.Math.Abs(lo3b.ReduceMul - 0.5) < 0.001, $"방어력 = K 이면 받는 피해 절반 ({lo3b.ReduceMul:0.00})");
                    // 엘리트 난이도 = Elite_Mode_Offset 스테이지 뒤의 적
                    S.best = 30; S.eliteBest = 0;
                    Game.StartStage(5, 42, true); yield return null; var ee = B.Spawn("moth", new Vector2(0, 5)); double eh = ee.maxHp;
                    Game.StartStage(5 + Tables.Ci("Elite_Mode_Offset", 10), 42); yield return null; var ne = B.Spawn("moth", new Vector2(0, 5));
                    Check(System.Math.Abs(eh / ne.maxHp - 1) < 0.01, $"엘리트 5 의 적 = 보통 {5 + Tables.Ci("Elite_Mode_Offset", 10)} 의 적 ({Num.F(eh)})");
                    // 무한 모드 골드: 같은 시간 버텨도 높은 스테이지일수록 많이
                    double EndG(int best) { S.best = best; Game.StartStage(best, 43, false, "endless"); B.time = 240; B.goldGot = 0; double g = S.gold; B.baseHp = 0; B.End(false); Popup.CloseAll(); return S.gold - g; }
                    double e10 = EndG(10), e60 = EndG(60);
                    Check(e60 > e10 * 10, $"무한 모드 4분 골드: 스테이지 10 {Num.F(e10)} · 60 {Num.F(e60)}");
                    S.best = 30;
                    // 미션 · 출석 보상이 진행에 맞춰 커짐
                    double rg = Meta.RewardValue("gold", 1500), rs = Meta.RewardValue("shards", 10);
                    Check(rg > 1500 * 3 && rs > 10, $"미션 보상(스테이지 30): 골드 1,500 → {Num.F(rg)} · 조각 10 → {rs:0}");
                    Game.ToLobby(); yield return null; Popup.CloseAll();
                    ShopTab.sub = 0; Game.Lobby.SetTab(5); yield return Wait(0.3f);
                    Store.Purchase(boost); Game.Lobby.SetTab(5); yield return Wait(0.3f);
                    Check(GameObject.Find("P_p_boost") != null, "상점 다이아 패널에 골드 부스터");
                    Capture("92_shop_boost");
                    S.iapOnce.RemoveAll(k => k.StartsWith("goldboost:"));
                    Game.Lobby.SetTab(0);
                } yield return Wait(0.3f);
                var card = GameObject.Find("StageCard").GetComponent<RectTransform>();
                var pat = GameObject.Find("Patrol").GetComponent<RectTransform>(); var st = GameObject.Find("Start").GetComponent<RectTransform>();
                Check(card.sizeDelta.y <= 360 && pat.position.y < st.position.y, $"스테이지 칸 {card.sizeDelta.y} · 순찰이 [전투 시작] 아래");
                S.patrolFrom = SaveData.Clock - 7200;
                double pg = System.Math.Floor(S.PatrolGold), gb = S.gold;
                pat.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.3f);     // 6차: 순찰 단추 → 안내 창 → [광고 보고 2배]
                GameObject.Find("ClaimAd").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.3f);
                var ts = Object.FindAnyObjectByType<Toast>();
                var cc = new Vector3[4]; card.GetWorldCorners(cc); var sc = new Vector3[4]; st.GetWorldCorners(sc);
                Check(System.Math.Abs(S.gold - gb - pg * 2) <= System.Math.Max(1, pg * 0.01) && ts != null && ts.transform.position.y < cc[0].y && ts.transform.position.y > sc[1].y,
                      $"순찰 광고 2배 (+{Num.F(S.gold - gb)}) · 토스트가 스테이지 칸과 [전투 시작] 사이");
                Capture("61_lobby_patrol_toast");
                Lobby.enemiesOpen = true; Game.Lobby.SetTab(0); yield return Wait(0.3f);
                Check(GameObject.Find("WeakHint") != null && GameObject.Find("EnemiesToggle").GetComponentInChildren<UnityEngine.UI.Text>().text == L.T("enemies_show"), "적 정보 · 약점 안내 강조");
                Capture("62_lobby_enemies");
                Lobby.enemiesOpen = false; Game.Lobby.SetTab(0);
                // 장비 강화 창 · 회로 창 · 레벨별 능력
                S.gold += 1e7; S.parts += 500;
                Game.Lobby.SetTab(2); yield return Wait(0.3f);
                var gu = Gear.NewGear(5, "shield"); int lv0 = gu.lv;
                Game.Lobby.gearTab.GearUpPopup(gu); yield return Wait(0.3f);
                Capture("63_gear_up");
                GameObject.Find("U_Up").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.2f);
                Check(gu.lv == lv0 + 1 && GameObject.Find("U_Up") != null, $"장비 강화 창: Lv {lv0} → {gu.lv} (창 그대로)");
                Popup.CloseAll();
                var cx = Gear.NewChip(6, "tc_rail_dmg");
                Game.Lobby.gearTab.ChipWindow(cx); yield return Wait(0.3f);
                int fits = 0; foreach (var gp in Tables.GearParts) if (GameObject.Find("Fit_" + gp.part) != null) fits++;
                Check(GameObject.Find("Ability") != null && fits >= 1, $"회로 창: 능력 칸 · 착용 가능 부위 {fits}");
                Capture("64_chip_window");
                Popup.CloseAll();
                Game.Lobby.SetTab(1); yield return Wait(0.2f);
                Game.Lobby.UpPopup(Tables.TurretById["rail"]); yield return Wait(0.3f);
                Check(GameObject.Find("U_Info") != null, "터렛 강화 창에 레벨별 능력 단추");
                Capture("65_turret_up");
                Lobby.LvTable(Tables.TurretById["rail"]); yield return Wait(0.3f);
                Check(GameObject.Find("L1") != null && GameObject.Find("L" + Tables.MaxTurretLv) != null, $"레벨별 능력 표 Lv 1~{Tables.MaxTurretLv}");
                Capture("66_lv_table");
                Popup.CloseAll();
                Game.Lobby.SetTab(2); yield return Wait(0.2f); Game.Lobby.gearTab.MergeAll(); yield return Wait(0.3f);
                Capture("67_merge");
                Popup.CloseAll();
                // 11-17 치트: 설정 창 [치트] → 장비 · 회로 · 조각 · 부품
                Settings.Open(); yield return Wait(0.2f);
                Check(GameObject.Find("Cheat") != null, "설정 창에 [치트] (에디터)");
                Capture("68_settings_cheat");
                Popup.CloseAll(); Cheat.Open(); yield return Wait(0.3f);
                int ng = S.gear.Count, nc = S.chips.Count, sh0 = S.shards[S.TIdx("rail")]; double pa0 = S.parts;
                void Press(string row, int n) => GameObject.Find("Row_" + row).transform.Find("Add" + n).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                Press(L.T("r_gear"), 10); Press(L.T("r_chip"), 1); Press(L.T("cheat_shards"), 100); Press(L.T("r_parts"), 1000);
                yield return Wait(0.2f);
                Check(S.gear.Count == ng + 10 && S.gear.FindAll(x => x.grade == 4).Count >= 10 && S.chips.Count == nc + 1 && S.shards[S.TIdx("rail")] == sh0 + 100 && S.parts == pa0 + 1000,
                      $"치트: 장비 +10 (영웅) · 회로 +1 · 조각 +100 · 부품 +1000");
                Capture("69_cheat");
                Popup.CloseAll();
            }
            {   // ---- 요청 사항_05 (11-17)
                Lobby.enemiesOpen = false; Game.Lobby.SetTab(0); yield return Wait(0.3f);
                Toast.Show("토스트 시험"); yield return null;
                var tt = Object.FindAnyObjectByType<Toast>();
                Check(tt != null && tt.GetComponent<UnityEngine.UI.Image>() == null && GameObject.Find("StageCard").GetComponent<RectTransform>().sizeDelta.y <= 300
                      && GameObject.Find("StageCard").transform.Find("EnemyBox/Weak") != null, "토스트 = 글자만 · 스테이지 칸 300 · 적 약점은 [적 정보] 안");
                Capture("70_lobby");
                var navBg = GameObject.Find("NavBg").GetComponent<RectTransform>(); var nb = new Vector3[4]; navBg.GetWorldCorners(nb);
                bool inside = true;
                foreach (var id in Lobby.Tabs) { var tb = new Vector3[4]; GameObject.Find("Tab_" + id).GetComponent<RectTransform>().GetWorldCorners(tb); if (tb[0].x < nb[0].x || tb[2].x > nb[2].x || tb[0].y < nb[0].y) inside = false; }
                Check(inside, "아래 탭이 바탕 판 테두리 안");
                Lobby.TurretInfo(Tables.TurretById["drone"]); yield return Wait(0.3f);
                Check(GameObject.Find("Sec_" + L.T("ti_stat")) != null && GameObject.Find("Sec_" + L.T("ti_desc")) != null && GameObject.Find("Sec_" + L.T("ti_break")) != null, "터렛 정보 = 능력 · 설명 · 돌파 칸");
                Capture("71_turret_info");
                Popup.CloseAll();
                // 장비 탭: 능력치 칸은 판 아래 · 강화 재료 글자 · 부위 창 겹침 없음
                Game.Lobby.SetTab(2); yield return Wait(0.3f);
                Check(GameObject.Find("Stat0").transform.parent.name != "Top", "능력치 칸을 위 판 아래로");
                Capture("72_gear_tab");
                Game.Lobby.gearTab.PartWindow(Tables.PartIdx("weapon")); yield return Wait(0.3f);
                var up = new Vector3[4]; GameObject.Find("Up").GetComponent<RectTransform>().GetWorldCorners(up);
                var ln5 = new Vector3[4]; GameObject.Find("ChipLine").GetComponent<RectTransform>().GetWorldCorners(ln5);
                Check(up[0].y > ln5[1].y, "부위 창: [강화] 단추가 회로 칸 위 (안 겹침)");
                Capture("73_part_window");
                Popup.CloseAll();
                // 회로 착용 확인 → 취소 = 이전 단계 · 확인 = 착용되었습니다
                var cc5 = Gear.NewChip(5, "ch_crit");
                Game.Lobby.gearTab.ChipPlace(cc5); yield return Wait(0.3f);
                var slot5 = GameObject.Find("P_helmet").transform.Find("S0") ?? GameObject.Find("P_weapon").transform.Find("S0");
                slot5.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.3f);
                Capture("74_chip_confirm");
                GameObject.Find("Btn_" + L.T("cancel")).GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.2f);
                Check(GameObject.Find("P_weapon") != null && cc5.at < 0, "회로 확인 [취소] → 착용 안내 창으로");
                slot5.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.2f);
                GameObject.Find("Btn_" + L.T("ok")).GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.4f);
                Check(cc5.at >= 0 && Popup.Open != null && Popup.Open.name.Contains(L.T("equip")), "회로 확인 [확인] → 착용 + \"착용되었습니다\" 창");
                Capture("75_equipped");
                Popup.CloseAll();
                // 상점: 광고 제거 · 강화 재료 상자 · 터렛 조각 상자
                var pa = Tables.ShopItems.Find(x => x.kind == "parts_box"); var sb = Tables.ShopItems.Find(x => x.kind == "shard_box");
                double p0 = S.parts; int s0 = 0; foreach (var v in S.shards) s0 += v;
                S.dia += 1000;
                Store.Buy(pa); Store.Buy(sb);
                int s1 = 0; foreach (var v in S.shards) s1 += v;
                var na = Tables.Products.Find(x => x.kind == "noads");
                Check(pa != null && sb != null && pa.perDay == 10 && S.parts == p0 + pa.value && s1 == s0 + (int)sb.value && na != null && Store.Purchase(na) == 1 && Store.NoAds,
                      "상점: 강화 재료 상자 · 터렛 조각 상자 (하루 10번) · 광고 제거 [초안]");
                ShopTab.sub = 0; Game.Lobby.SetTab(5); yield return Wait(0.3f);
                Capture("76_shop");
                Game.Lobby.shopTab.GetComponentInChildren<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition = 0f; yield return Wait(0.3f);
                Capture("76b_shop_bottom");
                // 돌파 카드: 붉은 빛 테두리 · Lv 직사각형 · 마지막 보석 붉게
                Game.StartStage(3, 1); yield return null;
                var gt = B.run.Get("guardian"); gt.stars = Tables.Ci("Stars_Per_Tier", 4);
                var brk = Tables.Cards.Find(x => x.turret == "guardian" && x.kind == "break");
                B.offer = new List<CardRow> { brk, Tables.CardById["new_bolt"], Tables.CardById["g_dmg"] }; B.st = Battle.St.Pick; B.pendingLevel++;
                Game.BattleUI.ShowPick(); yield return Wait(0.6f);
                Check(GameObject.Find("BreakGlow") != null && GameObject.Find("Tier").GetComponent<UnityEngine.UI.Image>().sprite == Art.UI("badge"), "돌파 카드 붉은 빛 · Lv 직사각형");
                Capture("77_break_card");
                Popup.CloseAll(); B.st = Battle.St.Play; gt.tier = 2; gt.stars = 3;
                Game.BattleUI.ShowCards(); yield return Wait(0.4f);
                Capture("78_cards_list");
                Popup.CloseAll(); Game.ToLobby(); yield return null; Popup.CloseAll();
            }
            {   // ---- 6차 수정안 (11-17)
                // 1) 재화 숫자: 100만부터 K · 칸 안에 들어감 · 오른쪽 정렬
                Check(Num.Res(999999) == "999,999" && Num.Res(1000000) == "1,000K" && Num.Res(110000000) == "110,000K" && Num.Res(1e9) == "1,000M" && Num.Res(999999999999999) == "999,999B",
                      $"재화 숫자: {Num.Res(999999)} · {Num.Res(1000000)} · {Num.Res(110000000)} · {Num.Res(1e9)}");
                Game.ToLobby(); yield return null; Popup.CloseAll(); Game.Lobby.SetTab(0);
                double gKeep = S.gold, dKeep = S.dia;
                bool fit = true; string fitLog = "";
                foreach (var v in new[] { 999999.0, 999999999.0, 999999999999.0, 999999999999999.0 })
                {
                    S.gold = v; S.dia = v; Game.Lobby.Refresh(); yield return null;
                    foreach (var nm in new[] { "Gold", "Dia" })
                    {
                        var tx = GameObject.Find("TopBar/" + nm).GetComponentInChildren<UnityEngine.UI.Text>();
                        if (tx.alignment != TextAnchor.MiddleRight || tx.preferredWidth > tx.rectTransform.rect.width + 1) fit = false;
                        fitLog = tx.text;
                    }
                }
                Check(fit, $"금화 · 다이아 글자가 칸 안 (가장 긴 {fitLog}) · 오른쪽 정렬");
                S.gold = 123456789; S.dia = 2345678; Game.Lobby.Refresh(); yield return Wait(0.2f);
                Capture("80_topbar");
                S.gold = gKeep; S.dia = dKeep; Game.Lobby.Refresh();
                // 2) 순찰: [전투 시작]과 같은 크기 · 누르면 안내 창 · [받기] = 그대로
                var pat6 = GameObject.Find("Patrol").GetComponent<RectTransform>(); var st6 = GameObject.Find("Start").GetComponent<RectTransform>();
                Check(pat6.sizeDelta == st6.sizeDelta, $"순찰 단추 크기 = 전투 시작 ({pat6.sizeDelta.x}x{pat6.sizeDelta.y})");
                S.patrolFrom = SaveData.Clock - 5400; Game.Lobby.Refresh(); yield return null;
                double pg6 = System.Math.Floor(S.PatrolGold), gb6 = S.gold;
                pat6.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.5f);
                Check(Popup.Open != null && GameObject.Find("PatrolClaim") != null && GameObject.Find("ClaimAd") != null && GameObject.Find("Got_coin") != null,
                      "순찰 안내 창: 순찰 시간 · 골드 · 조각 · [받기] · [광고 보고 2배]");
                Capture("81_patrol_popup");
                GameObject.Find("PatrolClaim").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.2f);
                Check(Popup.Open == null && System.Math.Abs(S.gold - gb6 - pg6) <= System.Math.Max(1, pg6 * 0.005), $"[받기] → 그대로 +{Num.F(S.gold - gb6)} (창 열 때 {Num.F(pg6)})");   // 밸런스 1차: 순찰 골드가 커져 누르는 사이 쌓이는 몫이 1 을 넘음
                // 3) 토스트: 사라질 때 흐려짐
                Toast.Show("흐려지기 시험", 1.0f); yield return Wait(0.82f);
                var tf = Object.FindAnyObjectByType<Toast>(); float ta = tf ? tf.Alpha : -1;
                yield return Wait(0.4f);
                Check(ta > 0.05f && ta < 0.95f && Object.FindAnyObjectByType<Toast>() == null, $"토스트 흐려지며 사라짐 (끝나기 전 투명도 {ta:0.00})");
                // 4) 설정: 프레임 60 · 도움말 다시 보기 없음
                Settings.Open(); yield return Wait(0.3f);
                bool oldRow = false;
                foreach (var tx in Popup.Open.GetComponentsInChildren<UnityEngine.UI.Text>()) if (tx.text == L.T("set_fps") || tx.text == L.T("set_tutorial")) oldRow = true;
                Check(!oldRow && Application.targetFrameRate == 60, "설정: 프레임 60 · 도움말 다시 보기 제거 (프레임은 늘 60)");
                Capture("82_settings");
                Popup.CloseAll();
                // 5) 상점 패널 3개: 다이아 · 장비 · 재화
                ShopTab.sub = 0; Game.Lobby.SetTab(5); yield return Wait(0.3f);
                bool pDia = GameObject.Find("P_p_month") != null && GameObject.Find("P_p_dia1") != null && GameObject.Find("I_i_gear") == null;
                Capture("83_shop_dia");
                GameObject.Find("ShopSub1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.3f);
                bool pGear = GameObject.Find("I_i_gear") != null && GameObject.Find("I_i_free") != null && GameObject.Find("Rates") != null && GameObject.Find("P_p_dia1") == null && GameObject.Find("I_i_stamina") == null;
                bool pityT = false; foreach (var tx in Game.Lobby.shopTab.GetComponentsInChildren<UnityEngine.UI.Text>()) if (tx.text == L.F("pity_left", Store.PityLeft("gear_box"))) pityT = true;
                Capture("84_shop_gear");
                GameObject.Find("ShopSub2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); yield return Wait(0.3f);
                bool pRes = GameObject.Find("I_i_stamina") != null && GameObject.Find("I_i_parts") != null && GameObject.Find("I_i_gear") == null;
                Capture("85_shop_res");
                Check(pDia && pGear && pRes && pityT, $"상점 패널: 다이아 {pDia} · 장비 {pGear} · 재화 {pRes} · 천장 표시 {pityT}");
                // 6) 월 1회 다이아 폭탄 패키지
                var mp = Tables.Products.Find(x => x.kind == "monthly"); double dm = S.dia;
                int m1 = mp != null ? Store.Purchase(mp) : 0, m2 = mp != null ? Store.Purchase(mp) : -1;
                Check(mp != null && mp.krw == 9900 && m1 == 10000 && m2 == 0 && Store.MonthDone(mp) && S.dia == dm + 10000, $"월 1회 패키지: {m1} → 같은 달 다시 {m2}");
                // 7) 상자: 전설까지 · 55번 천장
                {
                    var rnd = new System.Random(9); int top = 0;
                    for (int i = 0; i < 20000; i++) top = Mathf.Max(top, Store.BoxGrade(rnd));
                    int pc = 0, maxGap = 0, gap = 0, forced = 0, legends = 0;
                    for (int i = 0; i < 6000; i++)
                    {
                        int before = pc, g = Store.PityGrade(ref pc, rnd); gap++;
                        if (g > Store.BoxMax) top = 99;
                        if (g >= 5) { legends++; maxGap = Mathf.Max(maxGap, gap); if (gap == Store.PityN) forced++; gap = 0; if (pc != 0) maxGap = 999; }
                    }
                    Check(top <= 5 && Store.BoxMax == 5, $"상자 등급은 전설까지 (2만 번 중 최고 {top})");
                    Check(Store.PityN == 55 && maxGap <= 55 && forced > 0 && legends > 6000 / 55, $"천장 55: 전설 사이 최대 {maxGap}번 · 천장으로 {forced}번 · 전설 {legends}번 (뽑으면 셈 0)");
                    S.pityGear = 54; S.dia += 1000;
                    var got1 = Store.Buy(Tables.ShopItems.Find(i => i.id == "i_gear"));
                    Check(got1 != null && got1[0].grade >= 5 && S.pityGear == 0, $"54번 쌓인 뒤 55번째 = {Tables.Grade(got1?[0].grade ?? 1).name} · 셈 다시 0");
                    var fb = Tables.ShopItems.Find(i => i.kind == "ad_box"); S.best = 300; int bestKeep = 30;
                    int hi = 0; for (int i = 0; i < 300; i++) hi = Mathf.Max(hi, Mathf.Min(Store.BoxMax, Gear.DropGrade(300, false, rnd)));
                    S.best = bestKeep;
                    Check(hi <= 5, $"무료 · 광고 상자도 전설까지 (스테이지 300 에서 최고 {hi})");
                }
                ShopTab.RateOpen(); yield return Wait(0.3f);
                Capture("86_rates");
                Popup.CloseAll();
                // 8) 외형: 터렛 외형은 바뀐 그림만 · 기지 외형은 외형마다 다른 돔
                Game.Lobby.SetTab(6); Looks.sub = 0; Game.Lobby.SetTab(6); yield return Wait(0.3f);
                var srow = GameObject.Find("S_sk_rail");
                Check(srow != null && srow.transform.Find("After") != null && srow.transform.Find("Before") == null, "터렛 외형 = 바뀐 그림 한 장");
                Capture("87_looks_turret");
                bool domes = true; foreach (var f in Tables.Frames) if (Art.Get("Bg/dome_" + f.id, 100f) == null || Art.Icon("frame_" + f.id) == null) domes = false;
                Looks.sub = 1; Game.Lobby.SetTab(6); yield return Wait(0.3f);
                Capture("88_looks_base");
                S.dia += 2000; var gf = Tables.Frame("gold"); Store.BuyFrame(gf); Store.EquipFrame("gold");
                var domeSr = GameObject.Find("Dome").GetComponent<SpriteRenderer>();
                Check(domes && domeSr.sprite.name == "Bg/dome_gold" && domeSr.color == Color.white, $"기지 외형마다 다른 돔 그림 ({Tables.Frames.Count}종) · 끼우면 전장 돔도 ({domeSr.sprite.name})");
                Game.Lobby.SetTab(0); yield return Wait(0.3f);
                Capture("89_lobby_gold_dome");
                foreach (var fid in new[] { "prism", "frost", "thorn", "bloom" }) { Store.BuyFrame(Tables.Frame(fid)); Store.EquipFrame(fid); yield return null; Capture("89_dome_" + fid); }
                Store.EquipFrame("basic");
                // 9) 장비 탭 대장 카피: 둥실둥실 + 몸 돌리기
                Game.Lobby.SetTab(2); yield return Wait(0.2f);
                var hv = Object.FindAnyObjectByType<Hover3D>(); var hrt = hv ? (RectTransform)hv.transform : null;
                Vector2 hvP0 = hrt ? hrt.anchoredPosition : Vector2.zero; float hy0 = hrt ? hrt.localEulerAngles.y : 0;
                Capture("90_gear_capy_a");
                for (int f = 0; f < 12; f++) { yield return Wait(0.3f); Capture($"90_capy_{f:00}"); }    // 보고서 GIF 용 (0.3초 간격)
                Capture("90_gear_capy_b");
                Check(hv != null && hv.shadow != null && hv.lit != null && (hrt.anchoredPosition - hvP0).magnitude > 4f && Mathf.Abs(Mathf.DeltaAngle(hrt.localEulerAngles.y, hy0)) > 3f,
                      $"장비 탭 카피바라 둥실둥실 (움직임 {(hrt ? (hrt.anchoredPosition - hvP0).magnitude : 0):0} · 몸 돌림 {(hrt ? Mathf.DeltaAngle(hrt.localEulerAngles.y, hy0) : 0):0}°)");
                // 10) 무한 모드: 레벨업 창이 덜 자주 (3분 동안 연 창 수 예전 ↔ 지금)
                double mulNew = Tables.C("Endless_Exp_Mul", 1), gapNew = Tables.C("Endless_Pick_Gap", 0);
                // 몇 가지 조합을 재서 로그에 남김 (마지막 = 지금 표 값). 같은 씨앗 · 스테이지 15 (시험 세이브 전력에 맞춤 — 30 은 대장 카피 혼자 겨우 잡아서 결과가 흔들림)
                var combos = new List<(double mul, double gap)> { (1, 0), (1, 8), (1.6, 6), (2, 6), (2, 8), (mulNew, gapNew) };
                var opened = new int[combos.Count]; var lvs = new int[combos.Count]; var min1 = new int[combos.Count];
                for (int k = 0; k < combos.Count; k++)
                {
                    Tables.SetC("Endless_Exp_Mul", combos[k].mul); Tables.SetC("Endless_Pick_Gap", combos[k].gap);
                    Game.StartStage(5, 77, false, "endless"); yield return null;      // 밸런스 2차: 15 는 시험 세이브에게 너무 셈
                    string marks = "";
                    for (int sec = 1; sec <= 180; sec++)
                    {
                        B.baseHp = B.baseMax; FF(1f, 0);
                        if (sec % 30 == 0) marks += $" {sec}초:Lv{B.level}/창{B.picksOpened}/처치{B.kills}";
                        if (sec == 60) min1[k] = B.picksOpened;
                    }
                    opened[k] = B.picksOpened; lvs[k] = B.level;
                    Log($"무한 모드 경험치 x{combos[k].mul} · 창 간격 {combos[k].gap}초 →{marks}");
                    yield return null;
                }
                Tables.SetC("Endless_Exp_Mul", mulNew); Tables.SetC("Endless_Pick_Gap", gapNew);
                int last = combos.Count - 1;
                Check(min1[last] <= min1[0] * 0.6f && opened[last] < opened[0] && lvs[last] >= 10, $"무한 모드 레벨업 창: 처음 1분 {min1[0]} → {min1[last]}번 · 3분 {opened[0]} → {opened[last]}번 (Lv {lvs[0]} → {lvs[last]})");
                // 밸런스 2차: 보스 분노 (체력 절반 아래로 처음 → 부하 몰려옴 · 돌진) · 로비 스테이지 약점 안내
                Game.StartStage(3, 9); yield return null;
                var boss2 = B.Spawn(B.stage.boss, new Vector2(0, 5f)); B.Tick(Battle.StepSec);
                int en0 = B.enemies.Count; boss2.hp = boss2.maxHp * 0.49; B.Tick(Battle.StepSec); B.Tick(Battle.StepSec);
                Check(B.bossRages == 1 && B.enemies.Count >= en0 + Tables.Ci("Boss_Rage_Summon", 6), $"보스 분노: 부하 {B.enemies.Count - en0}마리 · 화남 {B.bossRages}번");
                Game.ToLobby(); yield return null; Popup.CloseAll();
                S.stage = 2; Lobby.enemiesOpen = true; Game.Lobby.SetTab(0); yield return Wait(0.3f);
                var wtx = GameObject.Find("WeakHint").GetComponentInChildren<UnityEngine.UI.Text>().text;
                Check(!string.IsNullOrEmpty(Tables.Stage(2).weak) && wtx.Contains(Elem.Name(Tables.Stage(2).weak)), $"로비 스테이지 약점 안내: {wtx}");
                Capture("91_stage_weak");
                Lobby.enemiesOpen = false; Game.Lobby.SetTab(0);
                Game.ToLobby(); yield return null; Popup.CloseAll();
            }
            Game.ToLobby(); yield return null; Popup.CloseAll();
            Game.Lobby.SetTab(5); yield return Wait(0.4f);
            Capture("30_shop");
            Game.Lobby.shopTab.GetComponentInChildren<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition = 0f; yield return Wait(0.3f);
            Capture("30b_shop_bottom");
            Check(GameObject.Find("S_sk_rail") == null, "상점엔 외형이 없음 (외형 메뉴로 옮김)");
            Looks.Open(0); yield return Wait(0.4f);
            Check(Game.Lobby.tab == 6, "외형 열기 = 외형 탭");
            Capture("30c_looks_turret");
            Popup.CloseAll();
            Game.Lobby.SetTab(4); yield return Wait(0.3f);
            Capture("31_challenge");

            // --- 기지 외형
            var star = Tables.Frame("starsea");
            Check(Store.BuyFrame(star) && S.FrameStar("starsea") == 1, "별바다 장막 사기");
            Check(Store.UpFrame("starsea") && S.FrameStar("starsea") == 2, $"별 올리기 → 별 {S.FrameStar("starsea")} (효과 {star.Val(2)})");
            Store.EquipFrame("starsea");
            Game.Lobby.SetTab(1); yield return null;
            Looks.Open(1); yield return Wait(0.3f);
            Capture("32_frames");
            Popup.CloseAll();
            Game.StartStage(10, 3); yield return null;
            Check(B.frame?.id == "starsea" && System.Math.Abs(B.frameVal - star.Val(2)) < 0.01, "전투에 외형 들어감");
            FF(45f);
            Check(B.frameFired >= 1, $"별똥별 비 {B.frameFired}번 (40초마다)");
            Store.BuyFrame(Tables.Frame("prism")); Store.EquipFrame("prism");
            Game.StartStage(10, 4); yield return null;
            B.HurtBase(B.baseHp * 5, Vector2.zero); B.Tick(Battle.StepSec);
            double hr = B.baseHp;
            B.HurtBase(B.baseMax * 5, Vector2.zero); B.Tick(Battle.StepSec);
            Check(B.st == Battle.St.Play && hr > 0 && B.baseHp == hr, $"무지개 돔: 무너질 때 체력 {hr / B.baseMax:P0} 로 버티고 3초 무적");
            Store.BuyFrame(Tables.Frame("thorn")); Store.EquipFrame("thorn");
            Game.StartStage(10, 5); yield return null;
            var at = B.Spawn("beetle", new Vector2(0, World.ShieldY + 0.3f)); B.Tick(Battle.StepSec);
            B.HurtBase(10, at.Pos, at);
            Check(B.frameFired == 1, "가시 돔: 때린 적에게 반격");

            // --- 스킨 · 필살기
            foreach (var k in Tables.Skins) Store.BuySkin(k);
            Check(S.skins.Count == 11 && S.skinOn.Count == 11, $"스킨 11개 사고 끼움 (다이아 {Num.Comma(S.dia)} 남음)");
            Game.StartStage(3, 6); yield return null;      // 밸런스 2차: 12 는 시험 세이브에게 너무 셈 → 3
            foreach (var id in new[] { "new_rail", "new_laser", "new_missile", "new_vortex" }) Force(id);
            var rail = B.run.Get("rail");
            var mpb = new MaterialPropertyBlock(); rail.unit.GetComponent<SpriteRenderer>().GetPropertyBlock(mpb);
            Check(rail.skin != null && rail.late > 0 && rail.unit.GetComponent<SpriteRenderer>().sharedMaterial == Art.HueMat && System.Math.Abs(mpb.GetFloat("_Hue") - rail.skin.hue / 360f) < 0.001f,
                  $"관통포 스킨: 색 돌리기 {mpb.GetFloat("_Hue") * 360:0}° · 피해 +{rail.late:P0}");
            for (int i = 0; i < 12 && !B.units.TrueForAll(u => u == B.Guardian || u.ults > 0); i++)
            {   // 소용돌이포는 사거리가 짧음 → 필살기가 안 찼으면 사거리 안에 적을 둠 (시험이 가끔 실패했음)
                foreach (var u in B.units) if (u != B.Guardian && u.ults == 0) B.Spawn("beetle", (Vector2)u.transform.position + new Vector2(0, 1.2f));
                B.baseHp = B.baseMax; FF(5f);
            }
            foreach (var u in B.units) if (u != B.Guardian) Check(u.ults > 0, $"{u.t.row.name} 필살기 {u.ults}번 ({u.t.skin.ultName})");
            yield return Wait(0.3f);
            Capture("33_skins_battle");
            {   // 나머지 7종 필살기도 한 번씩 (오류 없이 공격이 나감)
                var cast = typeof(TurretUnit).GetMethod("CastUlt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var id in new[] { "drone", "hive", "bolt", "cannon", "focus", "wheel", "tesla" })
                {
                    var t = B.LabPlace(1, id, "base");
                    var e = B.enemies.Find(x => !x.dead);
                    int before = B.units.Count; cast.Invoke(t.unit, new object[] { e });
                    for (int i = 0; i < 30; i++) B.Tick(Battle.StepSec);
                    Check(t.unit != null, $"{t.row.name} 필살기 ({t.skin?.ultName})");
                }
                yield return Wait(0.3f);
                Capture("33b_ults");
            }

            // --- 무한 모드
            Game.ToLobby(); yield return null; Popup.CloseAll();
            int tk0 = Store.Tickets("endless");
            Check(Game.StartMode("endless") && B.mode == "endless" && Store.Tickets("endless") == tk0 - 1, $"무한 모드 시작 (입장 {tk0} → {Store.Tickets("endless")})");
            var e0 = B.Spawn("moth", new Vector2(0, 5)); double hp0 = e0.maxHp;
            B.immune = 1 << 30;   // 시험: 기지 레벨 1 로 스테이지 30 난이도라 1초 안에 무너짐 → 막아 둠
            for (int i = 0; i < 70; i++) { FF(1f); B.baseHp = B.baseMax; }
            var e1 = B.Spawn("moth", new Vector2(0, 5));
            Check(e1.maxHp / hp0 > 1.4 && B.st == Battle.St.Play, $"70초 뒤 적 체력 x{e1.maxHp / hp0:0.00} · 끝나지 않음");
            yield return Wait(0.3f);
            Capture("34_endless");
            double dia0 = S.dia;
            B.immune = 0; B.HurtBase(B.baseMax * 10, Vector2.zero); B.Tick(Battle.StepSec);
            Check(B.st == Battle.St.End && S.endlessBest >= 70 && B.rewardDia >= 4 && S.dia == dia0 + B.rewardDia, $"무한 모드 끝: 버틴 {S.endlessBest:0}초 · 다이아 +{B.rewardDia}");
            double gold1 = S.gold;
            Check(B.AdDouble() && S.gold > gold1 && !B.AdDouble(), "[초안] 광고 보고 보상 2배 (1번만)");
            yield return Wait(0.4f);
            Capture("35_endless_result");

            // --- 엘리트 군단
            Check(Game.StartMode("survival") && B.mode == "survival", "엘리트 군단 시작");
            B.immune = 1 << 30;
            int elites = 0;
            for (int i = 0; i < 200 && B.st != Battle.St.End; i++) { FF(1f); B.baseHp = B.baseMax; elites = System.Math.Max(elites, B.enemies.FindAll(x => !x.dead && x.row.IsElite).Count); }
            Check(B.st == Battle.St.End && B.won && S.survWins == 1 && B.drops.Count == Tables.Ci("Survival_Drops") && B.rewardDia == Tables.C("Survival_Dia"),
                  $"3분 버팀 → 승리 · 장비/칩 {B.drops.Count} · 다이아 {B.rewardDia} (중간 보스 동시 최대 {elites}) [st {B.st} won {B.won} t {B.time:0} hp {B.baseHp:0} imm {B.immune}]");
            yield return Wait(0.4f);
            Capture("36_survival_result");
            Game.StartMode("survival");
            Game.ToLobby(); yield return null; Popup.CloseAll();
            Check(!Game.StartMode("survival") && Store.Tickets("survival") == 0, "엘리트 군단 하루 2번까지");

            // --- 구글 계정 · 클라우드 [초안]
            Store.Link(); Store.CloudSave(); double gs = S.gold;
            S.gold = 1; S.Save();
            Check(Store.Linked && Store.CloudPeek() != null && System.Math.Abs(Store.CloudPeek().gold - gs) < 1, "[초안] 계정 연결 · 클라우드에 저장");
            Settings.CloudCompare(); yield return Wait(0.3f);
            Capture("37_cloud");
            Popup.CloseAll();
            Check(Store.CloudLoad() && System.Math.Abs(SaveData.Cur.gold - gs) < 1, "[초안] 클라우드에서 불러오기 → 골드 되돌아옴");
            Settings.Open(); yield return Wait(0.3f);
            Capture("38_settings");
            Popup.CloseAll();
            PlayerPrefs.DeleteKey("EarthDef.Cloud.Draft");
            running = false;
        }

        // ---------- 터렛 시험장 (RunLab) ----------
        IEnumerator LabSteps()
        {
            yield return null; yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            var b = LabBoot.B; var ui = Object.FindFirstObjectByType<LabUI>();
            Check(b != null && b.lab != null && ui != null, "시험장 씬: 전투(시험장 규칙) + 화면");
            for (int i = 0; i < 90; i++) b.Tick(Battle.StepSec);
            int dummies = b.enemies.FindAll(e => !e.dead && e.hold).Count;
            Check(dummies == 15, $"허수아비 3줄 x 5 ({dummies})");
            ui.Place(1, "cannon"); ui.Place(3, "vortex"); ui.Place(4, "tesla");
            {   // 끌어 놓기: 집중 빔 칸을 칸 2 자리로 끌어 놓음
                var drag = System.Array.Find(Object.FindObjectsByType<LabDrag>(FindObjectsSortMode.None), d => d.name == "T_focus");
                var pe = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { position = Camera.main.WorldToScreenPoint(new Vector3(World.SlotX[1], World.GroundY + 0.5f, 0)) };
                drag.OnBeginDrag(pe); drag.OnEndDrag(pe);
            }
            Check(b.run.turrets.Find(t => t.slot == 2)?.row.id == "focus" && b.units.Count == 5, $"끌어서 칸 2 에 집중 빔 · 터렛 {b.units.Count - 1}");
            double h0 = b.baseHp;
            for (int i = 0; i < 60 * 12; i++) b.Tick(Battle.StepSec);
            foreach (var t in b.run.turrets) if (t.slot > 0) Check(t.unit.fired > 0 && t.dmgDone > 0, $"{t.row.name} 쏨 {t.unit.fired} · 피해 {Num.F(t.dmgDone)}");
            Check(b.st == Battle.St.Play && b.baseHp == h0 && b.enemies.TrueForAll(e => e.dead || e.hold), "끝나지 않음 · 기지 그대로 · 허수아비는 제자리");
            Check(b.run.Get("guardian").unit.fired == 0, "대장 카피 쏘기 끔 (처음)");
            ui.SetPreset("all");
            Check(b.run.turrets.TrueForAll(t => t.slot == 0 || t.tier == 3), "카드 모두 → 3단계");
            ui.SetSpeed(2f);
            Check(b.speed == 2f, "속도 x2");
            yield return Wait(2.5f);
            Capture("23_lab_dummy");
            ui.SetMode("boss"); ui.SetSpeed(1f);
            for (int i = 0; i < 90; i++) b.Tick(Battle.StepSec);
            var boss = b.enemies.Find(e => !e.dead && e.row.IsBoss);
            Check(boss != null && boss.hold, $"보스 모드 ({boss?.row.name})");
            ui.SetMode("swarm");
            for (int i = 0; i < 60 * 6; i++) b.Tick(Battle.StepSec);
            Check(b.enemies.Exists(e => !e.dead && !e.hold), "무리 모드: 적이 내려옴");
            yield return Wait(1.5f);
            Capture("23b_lab_swarm");
            running = false;
        }

        // ---------- 소리 검토 (RunSound) ----------
        static void Wav(string path, float[] d)
        {
            using var f = new BinaryWriter(File.Create(path));
            int n = d.Length;
            f.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); f.Write(36 + n * 2); f.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            f.Write(16); f.Write((short)1); f.Write((short)1); f.Write(Audio.SR); f.Write(Audio.SR * 2); f.Write((short)2); f.Write((short)16);
            f.Write(System.Text.Encoding.ASCII.GetBytes("data")); f.Write(n * 2);
            foreach (var v in d) f.Write((short)Mathf.RoundToInt(Mathf.Clamp(v, -1f, 1f) * 32767));
        }

        static void SaveEvents(string path, List<(float t, Audio.Sfx s, float vol, float pitch)> ev, float t0)
        {
            var sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < ev.Count; i++)
                sb.Append(i > 0 ? "," : "").Append($"{{\"t\":{(ev[i].t - t0).ToString(System.Globalization.CultureInfo.InvariantCulture)},\"s\":\"{ev[i].s}\",\"v\":{ev[i].vol.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"p\":{ev[i].pitch.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
            File.WriteAllText(path, sb.Append("]").ToString());
        }

        IEnumerator SoundDump()
        {
            yield return null;
            Lobby.NoIntro = true; Popup.CloseAll();
            string dir = "Logs/Sound"; Directory.CreateDirectory(dir);
            while (!Audio.SfxReady) yield return null;      // 효과음은 일꾼 스레드에서 만들어짐 (11-17)
            foreach (var kv in Audio.SfxData) Wav($"{dir}/sfx_{kv.Key}.wav", kv.Value);
            foreach (var n in new[] { "lobby", "battle", "boss" }) { var d = Audio.Song(n); Wav($"{dir}/bgm_{n}.wav", d); Log($"배경음 {n} {d.Length / (float)Audio.SR:0.0}초"); }
            Check(Audio.SfxData.Count == System.Enum.GetValues(typeof(Audio.Sfx)).Length, $"효과음 {Audio.SfxData.Count}개 wav");
            // 전투 장면: 스테이지 8 · 터렛 4칸(관통포 · 레이저 · 벌집 · 쾅쾅 대포) · 30초 / 보스 장면: 보스 2초 전부터 20초
            S.best = 30;
            Game.StartStage(8, 808); yield return null;
            foreach (var id in new[] { "new_rail", "new_laser", "new_hive", "new_cannon", "g_shot", "r_bA" }) Force(id);
            Audio.Record = new List<(float, Audio.Sfx, float, float)>();
            float t0 = B.time; FF(30f);
            SaveEvents($"{dir}/events_battle.json", Audio.Record, t0);
            Log($"전투 30초 효과음 {Audio.Record.Count}번");
            Audio.Record = new List<(float, Audio.Sfx, float, float)>();
            B.time = Tables.Cf("Boss_Time") - 2f; t0 = B.time; FF(20f);
            SaveEvents($"{dir}/events_boss.json", Audio.Record, t0);
            Log($"보스 20초 효과음 {Audio.Record.Count}번");
            Audio.Record = null;
            running = false;
        }

        /// <summary>대기 효과 촬영: 같은 판을 효과 끔(p0) · 켬(p1) 으로 3초씩, 1/30초 고정 간격 · 묶음 s0 = 레이저 · 벼락 · 관통포 · 드론, s1 = 벌집 · 미사일</summary>
        IEnumerator IdleShots()
        {
            yield return null;
            Lobby.NoIntro = true; Popup.CloseAll();
            Directory.CreateDirectory(OutDir + "/idle");
            SetupCapture();
            yield return Wait(0.5f);
            var sets = new[] { new[] { "new_laser", "new_bolt", "new_rail", "new_drone" }, new[] { "new_hive", "new_missile" },
                               new[] { "new_cannon", "new_focus", "new_vortex", "new_wheel" }, new[] { "new_tesla" } };
            S.best = 30;   // 2차 터렛 열기
            for (int run = 0; run < sets.Length * 2; run++)
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
                    foreach (var u in B.units) if (u != B.Guardian) Check(u.idle != null && u.idle.Parts >= 2, $"{u.t.row.name} 대기 효과 조각 {u.idle?.Parts}");
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
            Check(S.stage == 1 && S.best == 0 && Tables.Stages.Count == 100, $"스테이지 1 부터 (표 {Tables.Stages.Count}개)");
            Check(Tables.Turrets.Count == 12 && Tables.Cards.Count >= 180 && Tables.Enemies.Count >= 18, $"표: 터렛 {Tables.Turrets.Count} · 카드 {Tables.Cards.Count} · 적 {Tables.Enemies.Count}");
            Check(Tables.GearParts.Count == 6 && Tables.Grades.Count == 7 && Tables.Chips.Count >= 60 && Tables.Missions.Count >= 6 && Tables.Attend.Count == 7,
                  $"2차 표: 부위 {Tables.GearParts.Count} · 등급 {Tables.Grades.Count} · 칩 {Tables.Chips.Count} · 미션 {Tables.Missions.Count} · 출석 {Tables.Attend.Count}");
            foreach (var c in Tables.Chips)
            {
                bool known = c.scope is "turret" or "all" ? KnownStats.Contains(c.stat) || c.stat == "start_stars" : BoardStats.Contains(c.stat);
                if (!known) Check(false, $"칩 {c.id} 의 모르는 키 {c.stat} ({c.scope})");
                if (c.scope == "turret" && !Tables.TurretById.ContainsKey(c.turret)) Check(false, $"칩 {c.id} 의 터렛 없음 {c.turret}");
                foreach (var pt in c.parts) if (pt != "all" && Tables.Part(pt) == null) Check(false, $"칩 {c.id} 의 부위 없음 {pt}");
            }
            foreach (var gp in Tables.GearParts) if (Art.Icon(gp.icon) == null) Check(false, "장비 아이콘 없음 " + gp.icon);
            foreach (var n in new[] { "cannonball", "mine", "vortex", "wheel" }) if (Art.Fx(n) == null) Check(false, "2차 효과 그림 없음 " + n);
            foreach (var n in new[] { "cannon_base", "cannon_barrel", "focus_base", "vortex_base", "wheel_base", "tesla_base", "tesla_orb" }) if (Art.Turret(n) == null) Check(false, "2차 터렛 조각 없음 " + n);
            // 처음 켜면 출석 (숨은 조건 없이 첫날부터) — 4차 수정안: 창 없이 자동으로 받고 토스트 (Config Attend_Auto)
            yield return Wait(0.2f);
            var at = Object.FindAnyObjectByType<Toast>();
            Check(Popup.Open == null && !Meta.AttendReady && S.attendN == 1 && at != null && S.stamina == Meta.StaminaMax,
                  $"처음 로비 = 출석 자동 받기 + 토스트 · 스태미나 {S.stamina}/{Meta.StaminaMax}");
            Capture("01a_lobby_attend");
            Popup.CloseAll(); yield return null;
            Lobby.NoIntro = true;   // 이 뒤로는 로비에 들어올 때 창을 안 띄움 (출석 · 미션은 2차 단계에서 직접 시험)
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
                e0.Teleport(new Vector2(gx - 3f, 0f));
                for (int i = 0; i < 150 && !B.Guardian.FaceLeft; i++) B.Guardian.Step(Battle.StepSec);
                bool l = B.Guardian.FaceLeft && B.Guardian.GetComponent<SpriteRenderer>().flipX;
                e0.Teleport(new Vector2(gx + 3f, 0f));
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
                e0.Teleport(p0);
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
            string sw0 = B.stage.weak; B.stage.weak = "";      // 밸런스 2차: 적 자기 약점만 볼 때는 스테이지 약점을 잠깐 비움
            double weak = MinCalc(g, "pebble"), neutral = MinCalc(g, "moth"), resist = MinCalc(B.run.Get("drone"), "jelly");
            double neutralL = MinCalc(B.run.Get("drone"), "moth");
            Check(System.Math.Abs(weak / neutral - Tables.C("Weak_Mul")) < 0.02, $"약점 배율 {weak / neutral:0.00} (Config {Tables.C("Weak_Mul")})");
            B.stage.weak = g.row.elem; double stageWeak = MinCalc(g, "moth"); B.stage.weak = sw0;
            Check(System.Math.Abs(stageWeak / neutral - Tables.C("Weak_Mul")) < 0.02, $"스테이지 약점: 약점이 다른 적도 x{stageWeak / neutral:0.00}");
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
                if (tgt == null) { tgt = B.Spawn("pebble", new Vector2(0, 2f)); B.Tick(Battle.StepSec); }   // 밸런스 1차: 적이 세져 40초 뒤 남은 적이 없을 수 있음 → 하나 만들어 맞힘
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
            double exp = 2 * S.PatrolGoldHour;   // 밸런스 2차: 순찰 · 골드 주머니 같은 식
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
            yield return Phase2();
            running = false;
        }

        // =============================================================== 2차
        IEnumerator Phase2()
        {
            Game.ToLobby(); yield return null; Popup.CloseAll();
            // --- 스태미나
            S.stamina = 3; S.staminaAt = SaveData.Clock;
            Check(!Game.TryStart(1, false) && Popup.Open != null && Battle.I == null, "스태미나 3 < 5 → 시작 못 함 · 스태미나 창");
            yield return Wait(0.3f);
            Capture("15_stamina_lack");
            Popup.CloseAll();
            S.staminaAt -= 360 * 2;
            Check(Meta.Stamina == 5, $"6분마다 1 → 12분 뒤 {Meta.Stamina}");
            int ad0 = Meta.StaminaAdLeft; Meta.StaminaAd();
            Check(Meta.Stamina == 25 && Meta.StaminaAdLeft == ad0 - 1, $"[초안] 광고 +20 → {Meta.Stamina} (오늘 남은 {Meta.StaminaAdLeft})");
            Check(Game.TryStart(1, false) && Battle.I != null && Meta.Stamina == 20, $"시작하면 -5 → {Meta.Stamina}");
            Game.ToLobby(); yield return null; Popup.CloseAll();

            // --- 출석 · 미션 (날짜를 넘김)
            if (!Meta.AttendReady) SaveData.NowOffset += 86400;   // 첫 로비에서 이미 받았음 (4차 수정안 자동 출석)
            int an = S.attendN; string got = Meta.AttendReady ? Meta.AttendClaim() : null;
            Check(got != null && !Meta.AttendReady && S.attendN == an + 1, $"출석 1일 받음 ({got})");
            SaveData.NowOffset += 86400;
            Check(Meta.AttendReady && Meta.AttendIdx == S.attendN % Meta.AttendDays, $"다음 날 → 출석 {Meta.AttendIdx + 1}일");   // 첫 로비에서 1일을 자동으로 받아 한 칸 밀림
            Meta.MissionAdd("play", 3);
            int mi = Tables.Missions.FindIndex(m => m.kind == "play");
            double g0 = S.gold;
            Check(Meta.MissionReady(mi) && Meta.MissionClaim(mi) != null && S.gold > g0 && Meta.MissionGot(mi), "미션 전투 3판 → 받기 → 골드");
            SaveData.NowOffset += 86400;
            Check(!Meta.MissionGot(mi) && Meta.MissionProg(mi) == 0, "다음 날 미션 새로");

            // --- 터렛 열림 (스테이지 5 · 10 · 15 · 20 · 25)
            var cannonRow = Tables.TurretById["cannon"];
            S.best = 1;
            Game.StartStage(1, 5); yield return null;
            Check(!S.Unlocked(cannonRow) && !B.run.Eligible(Tables.CardById["new_cannon"]), "스테이지 5 전엔 쾅쾅 대포 카드 안 나옴");
            Game.ToLobby(); yield return null; Popup.CloseAll();
            S.best = 30;
            Check(Meta.NewUnlock() != null, $"새로 열린 터렛 알림 ({Meta.NewUnlock()?.name})");

            // --- 장비 · 칩
            S.gold += 1e7; S.parts += 500;
            var w = Gear.NewGear(3, "weapon"); Gear.Equip(w);
            var lo = Loadout.Build(S);
            Check(System.Math.Abs(lo.atk - w.V1) < 0.01 && lo.critDmg > 0, $"희귀 무기 → 공격력 +{lo.atk:0} · 치명 피해 +{lo.critDmg:0.#}%");
            int wl = w.lv; Check(Gear.Up(w) && w.lv == wl + 1, $"장비 강화 Lv {w.lv}");
            for (int i = 0; i < 3; i++) Gear.NewGear(2, "boots");
            var bt = S.gear.Find(x => x.part == "boots" && x.grade == 2);
            var merged = Gear.Merge(bt);
            Check(merged != null && merged.grade == 3 && S.gear.FindAll(x => x.part == "boots" && x.grade == 2).Count == 0, "같은 부위 · 등급 3개 합성 → 한 등급 위");
            Gear.Equip(merged);
            foreach (var p in new[] { "helmet", "suit", "shield", "core" }) Gear.Equip(Gear.NewGear(4, p));
            double pr0 = S.parts; var sc = Gear.NewGear(1, "helmet"); Gear.Scrap(sc);
            Check(S.parts > pr0, $"분해 → 부품 +{S.parts - pr0}");
            int core = Tables.PartIdx("core"), shield = Tables.PartIdx("shield"), suit = Tables.PartIdx("suit"), weapon = Tables.PartIdx("weapon");
            var cd = Gear.NewChip(7, "ch_dmg");
            Check(!Gear.CanPut(cd, weapon, 0) && Gear.PutChip(cd, core, 0), "피해 칩: 무기엔 못 끼고 코어엔 낌");
            Check(Gear.SlotOpen(4) && !Gear.SlotOpen(5) == (S.best < Gear.SlotNeed(5)), $"칩 칸 6 = 스테이지 {Gear.SlotNeed(5)}");
            for (int i = 0; i < 3; i++) Gear.NewChip(2, "ch_atk");
            var ca = S.chips.Find(x => x.id == "ch_atk" && x.q == 2 && x.at < 0);
            var cm = Gear.MergeChip(ca);
            Check(cm != null && cm.q == 3, "같은 칩 3개 합성 → 등급 3");
            Gear.PutChip(Gear.NewChip(7, "tc_rail_star"), suit, 0);
            Gear.PutChip(Gear.NewChip(7, "ch_immune"), suit, 1);
            Gear.PutChip(Gear.NewChip(7, "ch_refresh"), shield, 0);
            Gear.PutChip(Gear.NewChip(7, "ch_choice"), shield, 1);
            Gear.PutChip(Gear.NewChip(7, "ch_elite"), core, 1);
            Gear.PutChip(Gear.NewChip(7, "ch_shield"), shield, 2);
            lo = Loadout.Build(S);
            Check(System.Math.Abs(lo.C("dmg") - 14) < 0.01 && lo.TS("rail", "start_stars") == 2 && lo.B("refresh") == 6, $"칩 합계: 피해 +{lo.C("dmg")}% · 관통포 별 +{lo.TS("rail", "start_stars")} · 새로고침 +{lo.B("refresh")}");
            Game.ToLobby(); yield return null; Popup.CloseAll();
            GearTab.showChips = false; Game.Lobby.SetTab(2); yield return Wait(0.4f);
            Capture("13_gear_tab");
            Game.Lobby.gearTab.PartWindow(shield); yield return Wait(0.4f);
            Capture("14_gear_part");
            Popup.CloseAll(); GearTab.showChips = true; Game.Lobby.SetTab(2); yield return Wait(0.3f);
            Capture("14b_chip_bag");

            // 장비 · 칩이 전투에 들어감
            Game.StartStage(3, 31); yield return null;
            Check(System.Math.Abs(B.atk - (S.Atk + lo.AtkTotal)) < 0.01 && B.baseMax > S.BaseHp, $"전투 공격력 {B.atk:0} (기지 {S.Atk:0} + 장비 · 칩) · 기지 체력 {B.baseMax:0}");
            Check(B.immune == 7 && B.shield > 0 && B.run.refreshFree == 6, $"반사막 {B.immune}번 · 보호막 {B.shield:0} · 새로고침 무료 {B.run.refreshFree}");
            double h0 = B.baseHp; for (int i = 0; i < 7; i++) B.HurtBase(500, Vector2.zero);
            Check(B.baseHp == h0 && B.immune == 0, "처음 7번은 피해 없음");
            B.HurtBase(100, Vector2.zero);
            Check(B.baseHp == h0 && B.shield < B.shieldMax, "그다음은 보호막이 먼저 받음");
            B.GainExp(B.need + 0.1); B.Tick(Battle.StepSec);
            Check(B.st == Battle.St.Pick && B.offer.Count == 4, $"첫 카드 칩 → 첫 레벨업 카드 {B.offer.Count}장");
            Game.BattleUI.ShowPick(); yield return Wait(0.4f);
            Capture("16_four_cards");
            B.offer[0] = Tables.CardById["new_rail"]; B.Choose(0);
            Check(B.run.Get("rail")?.stars == 3, $"관통포 별 칩 → 놓자마자 별 {B.run.Get("rail")?.stars}");
            {
                var g = B.run.turrets[0];
                var el = B.Spawn("big", new Vector2(0, 3)); var nm = B.Spawn("moth", new Vector2(1, 3)); B.Tick(Battle.StepSec);
                double de = 0, dn = 0;
                for (int i = 0; i < 40; i++) { de = System.Math.Max(de, Combat.Calc(g, el, 100, out bool c1) / (c1 ? 2 : 1)); dn = System.Math.Max(dn, Combat.Calc(g, nm, 100, out bool c2) / (c2 ? 2 : 1)); }
                Check(de > dn * 1.5, $"보스 사냥 칩: 중간 보스에게 피해 {de / dn:0.00}배");
                el.dead = true; nm.dead = true;
            }
            Game.ToLobby(); yield return null; Popup.CloseAll();

            // --- 2차 터렛 5종이 실제로 쏨
            Enemy.Knocks = 0; FireWheel.LastBounces = 0; Vortex.LastPulled = 0; TeslaBolt.LastJumps = 0; FocusBeam.LastRefract = 0;
            Game.StartStage(12, 4321); yield return null;
            foreach (var id in new[] { "new_cannon", "new_focus", "new_vortex", "new_wheel" }) Force(id);
            foreach (var id in new[] { "c_dmg", "c_dmg", "c_cd", "c_bB", "f_dmg", "f_cd", "f_dur", "f_bA", "v_dmg", "v_area", "v_cd", "v_bA", "w_dmg", "w_size", "w_cd", "w_bA" }) Force(id);
            Check(B.run.Get("cannon").tier == 2 && B.run.Get("focus").Has("focus_ramp") && B.run.Get("vortex").Has("blackhole") && B.run.Get("wheel").Has("wheel_trail"), "2차 터렛 4종 돌파");
            // 쾅쾅 대포 · 소용돌이포는 사거리가 짧아(9 · 8칸) 장비를 낀 대장 카피가 멀리서 다 잡으면 못 쏨 → 사거리 안에 단단한 적 무리를 둠
            for (int i = 0; i < 12; i++) { var bt2 = B.Spawn("beetle", new Vector2(-4f + (i % 6) * 1.6f, -1.5f + (i / 6) * 1.2f)); bt2.hp = bt2.maxHp = bt2.maxHp * 400; }
            FF(30f);
            foreach (var u in B.units) Check(u.fired > 0, $"{u.t.row.name} 쏨 ({u.fired}번)");
            Check(Enemy.Knocks > 0, $"쾅쾅 대포가 적을 밀어냄 ({Enemy.Knocks}번)");
            Check(FireWheel.LastBounces > 0, $"불바퀴가 벽에 튕김 ({FireWheel.LastBounces}번)");
            Check(Vortex.LastPulled > 0, $"소용돌이가 적을 끌어당김 (최대 {Vortex.LastPulled}마리)");
            foreach (var u in B.units) if (u != B.Guardian) Check(u.idle != null && u.idle.Parts >= 2, $"{u.t.row.name} 대기 효과 조각 {u.idle?.Parts}");
            {   // 집중 누적: 한 적에 붙어 있으면 피해가 오름
                var tg = B.enemies.Find(e => !e.dead && e.Pos.y < World.SpawnY - 1f);
                if (tg != null)
                {
                    tg.hp = tg.maxHp = 1e15;
                    var fb = new FocusBeam(B.run.Get("focus"), tg, 4f, 0.22f);
                    for (int i = 0; i < 200; i++) fb.Step(Battle.StepSec);
                    Check(FocusBeam.LastRamp > 0.5f, $"집중 누적: 3초 붙으면 +{FocusBeam.LastRamp * 100:0}%");
                    fb.Kill();
                }
            }
            yield return Wait(0.3f);
            Capture("12_turrets2");
            Game.BattleUI.ShowCards(); yield return Wait(0.4f);
            Capture("12d_cards");
            Popup.CloseAll(); B.Pause(false);
            yield return Wait(0.3f);
            Capture("12b_turrets2");
            Game.ToLobby(); yield return null; Popup.CloseAll();
            Game.StartStage(25, 99); yield return null;
            foreach (var id in new[] { "new_tesla", "t_dmg", "t_dmg", "t_bounce", "t_bA", "new_focus", "f_dmg", "f_dmg", "f_cd", "f_bB" }) Force(id);
            for (int i = 0; i < 12; i++) { var bt3 = B.Spawn("beetle", new Vector2(-4f + (i % 6) * 1.6f, -0.5f + (i / 6) * 1.2f)); bt3.hp = bt3.maxHp = bt3.maxHp * 400; }   // 굴절 · 튐을 운에 안 기대게
            FF(25f);
            Check(B.run.Get("tesla").unit.fired > 0 && TeslaBolt.LastJumps >= 3, $"찌릿 코일 번개가 튐 (최대 {TeslaBolt.LastJumps}번)");
            Check(FocusBeam.LastRefract > 0, $"굴절 빔 ({FocusBeam.LastRefract}줄)");
            yield return Wait(0.3f);
            Capture("12c_tesla");

            // --- 엘리트 난이도
            Game.ToLobby(); yield return null; Popup.CloseAll();
            Game.StartStage(2, 77, false); yield return null;
            var en = B.Spawn("moth", new Vector2(0, 5)); double hpN = en.maxHp;
            Game.StartStage(2, 77, true); yield return null;
            var ee = B.Spawn("moth", new Vector2(0, 5));
            Check(ee.maxHp / hpN > 2.5, $"엘리트 난이도 적 체력 x{ee.maxHp / hpN:0.0}");
            int g1 = S.gear.Count + S.chips.Count; double gold1 = S.gold;
            FF(2f);
            Combat.Raw(B.Spawn(Tables.Stage(2).boss, new Vector2(0, 4)), 1e30, false, B.run.turrets[0]);
            B.boss = null; B.Tick(Battle.StepSec);
            int want = Tables.Ci("Drop_Win_N") + Tables.Ci("Drop_Elite_N") + Tables.Ci("Drop_First_N");
            Check(B.st == Battle.St.End && B.won && S.eliteBest == 2 && B.drops.Count == want && S.gear.Count + S.chips.Count == g1 + want,
                  $"엘리트 승리 → 기록 {S.eliteBest} · 장비/칩 {B.drops.Count}개 ({string.Join(", ", B.drops)})");
            yield return Wait(0.5f);
            Capture("17_result_elite");

            // --- 스테이지 100
            Game.StartStage(100, 5, true); yield return null;
            FF(5f);
            Check(B.st == Battle.St.Play && B.enemies.Count > 0 && B.stage.stage == 100, $"스테이지 100 (엘리트) 적 {B.enemies.Count} · 이름 {B.stage.name}");

            // --- 로비 탭 · 설정 · 도움말
            Game.ToLobby(); yield return null; Popup.CloseAll();
            Game.Lobby.SetTab(0); yield return Wait(0.3f);
            Capture("18_lobby_battle");
            Lobby.enemiesOpen = true; Game.Lobby.SetTab(0); yield return Wait(0.3f);
            Capture("18d_lobby_enemies_open");
            Lobby.enemiesOpen = false; Game.Lobby.SetTab(0);
            S.elite = true; Game.Lobby.Refresh(); yield return Wait(0.2f);
            Capture("18b_lobby_elite");
            S.elite = false;
            {   // 11-16 저녁 의견: 스태미나 최대 = 30 + 깬 스테이지 (최대 80) · 기지 앞을 걷는 대장 카피
                int b0 = S.best;
                S.best = 0; int m0 = Meta.StaminaMax; S.best = 30; int m30 = Meta.StaminaMax; S.best = 99; int m99 = Meta.StaminaMax; S.best = b0;
                Check(m0 == 30 && m30 == 60 && m99 == 80, $"스태미나 최대: 처음 {m0} · 30스테이지 {m30} · 끝 {m99}");
                Check(LobbyWalker.I != null, "로비에 대장 카피가 있음");
                float wx0 = LobbyWalker.I.X; LobbyWalker.I.Go(wx0 > 0 ? -1.4f : 1.4f); yield return Wait(4f);
                Check(LobbyWalker.I != null && Mathf.Abs(LobbyWalker.I.X - wx0) > 0.05f, $"대장 카피가 어슬렁 걸음 ({wx0:0.00} > {LobbyWalker.I?.X:0.00})");
                Capture("18c_lobby_walk");
            }
            Game.Lobby.SetTab(1); yield return Wait(0.3f);
            Capture("19_lobby_turret");
            Lobby.TurretInfo(Tables.TurretById["vortex"]); yield return Wait(0.3f);
            Capture("19b_turret_info");
            Popup.CloseAll();
            Meta.MissionAdd("kill", 9999);
            Game.Lobby.SetTab(3); yield return Wait(0.3f);
            Capture("20_lobby_mission");
            Settings.Open(); yield return Wait(0.3f);
            Capture("21_settings");
            Popup.CloseAll();
            S.tut.Clear(); S.runs = 1;
            Check(Tutorial.Want("t_turret") && Tutorial.Show("t_turret", L.T("tut_turret"), null) != null && !Tutorial.Want("t_turret"), "도움말 한 번만");
            yield return Wait(0.3f);
            Capture("22_tutorial");
            Tutorial.CloseCur();
            // 세이브 → 다시 읽기 (장비 · 칩 · 출석 · 미션)
            S.Save();
            var back = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveData.Key));
            Check(back.gear.Count == S.gear.Count && back.chips.Count == S.chips.Count && back.chips.Exists(c => c.at >= 0) && back.eliteBest == S.eliteBest && back.attendN == S.attendN,
                  $"세이브 → 다시 읽기: 장비 {back.gear.Count} · 칩 {back.chips.Count} · 엘리트 {back.eliteBest}");
            SaveData.NowOffset = 0;
            // 소리 (11-16 저녁)
            Check(Audio.I != null && Audio.SfxData.Count == System.Enum.GetValues(typeof(Audio.Sfx)).Length, $"효과음 {Audio.SfxData.Count}개 만들어짐");
            Check(Audio.Played > 100, $"시험 동안 효과음 {Audio.Played}번 재생");
            Game.ToLobby(); yield return null;
            float w0 = Time.realtimeSinceStartup;
            while (Audio.Playing != "lobby" && Time.realtimeSinceStartup - w0 < 20f) yield return null;
            Check(Audio.Playing == "lobby", $"로비 배경음 ({Audio.Playing}, {Time.realtimeSinceStartup - w0:0.0}초)");
            Game.StartStage(1, 3); yield return null;
            w0 = Time.realtimeSinceStartup;
            while (Audio.Playing != "battle" && Time.realtimeSinceStartup - w0 < 20f) yield return null;
            Check(Audio.Playing == "battle", "전투 배경음");
            B.time = Tables.Cf("Boss_Time") - 0.1f; FF(0.5f, 1);
            w0 = Time.realtimeSinceStartup;
            while (Audio.Playing != "boss" && Time.realtimeSinceStartup - w0 < 20f) yield return null;
            Check(Audio.Playing == "boss", "보스가 나오면 보스 배경음");
        }

        // ---------- 시험 도우미 ----------
        static readonly HashSet<string> KnownStats = new()
        {
            "dmg", "shots", "shots_free", "pierce", "cd", "dur", "area", "range", "count", "count_free", "hits", "crit", "slow", "slow_max", "tick",
            "explode", "explode_dmg", "explode_area", "split", "split_dmg", "burn", "burn_amp", "fire_zone", "paralyze", "para_time", "vuln",
            "chain", "chain_dmg", "beam_split", "sweep", "dmg_by_slow", "field_explode", "pull", "pulse", "pulse_area", "pulse_end", "echo",
            "echo_hits", "charge", "charge_speed", "charge_shots", "dmg_vs_para", "hit_laser", "kill_bolt", "kill_bee", "kill_explode", "fire_add", "hit_chain",
            // 2차 터렛 5종 · 콤보
            "knock", "scatter", "scatter_dmg", "mines", "mine_dmg", "mine_knock", "beams", "focus_ramp", "focus_keep", "refract", "refract_dmg",
            "pull_pow", "blackhole", "bh_area", "bh_dmg", "vortex_burst", "burst_area", "burst_dmg", "hits_pct", "speed", "wheel_trail", "trail_dmg",
            "trail_dur", "mini_wheel", "mini_dmg", "bounce", "bounce_free", "shockwave", "trap", "trap_dmg", "trap_dur", "trap_bolt", "overcharge", "kill_chain",
        };
        /// <summary>칩 board · card · cond 키 (Loadout · Combat · Battle 이 읽음)</summary>
        static readonly HashSet<string> BoardStats = new()
        {
            "atk", "atk_pct", "base_atk", "base_hp", "shield_pct", "crit", "crit_dmg", "hp", "reduce", "shield", "immune", "heal_elite", "heal_break", "crit_hp", "extra_shot", "refresh", "choices", "break_rate",
            "dmg", "el_fire", "el_bolt", "el_grav", "el_light", "el_phys", "hp70", "elite_dmg", "early", "low_hp", "debuff", "near", "boss_on", "per_turret", "per_card", "cd",
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
