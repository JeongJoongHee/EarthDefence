using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-21 비공개 테스트 1번 빌드 준비 시험: bash tools/unity_batch.sh EarthDef.EditorTools.PlayTest.RunClosed --play
    /// 실제 광고(AdService) · 결제(Iap) · 폰에서 [초안] 숨김(Store.SimulateDevice · Iap.SimulateNoStore) · 앱 아이콘 · 새 글자
    /// 화면 = Logs/PlayTest/cl_*.png
    /// </summary>
    public partial class PlayTestRunner
    {
        IEnumerator ClosedSteps()
        {
            yield return null;
            Directory.CreateDirectory(OutDir);
            SetupCapture();
            var S = SaveData.Cur;
            var font = Object.FindAnyObjectByType<Text>()?.font;

            // ---------- 1) 광고 ----------
            Check(Ads.Draft && !AdIds.UseTest, "에디터 = 광고 없이 바로 보상 (Draft) · 시험 광고 스위치는 개발용 APK 에서만");
            Check(AdIds.RewardedUnit == (AdIds.Configured ? AdIds.Rewarded : AdIds.TestRewarded),
                  $"광고 단위: 실제 번호를 넣기 전엔 구글 시험 단위 (지금 {(AdIds.Configured ? "실제 번호" : "시험 번호 — set_admob.py 대기")})");
            S.iapOnce.Remove("noads");
            bool got = false; Ads.Show(ok => got = ok);
            Check(got, "Ads.Show → 보상 (에디터)");

            // ---------- 2) 결제 (에디터 가짜 결제 = Iap.Buy → Store.Grant) ----------
            Tables.Load();
            var dia1 = Tables.Products.Find(p => p.productId == "earth_dia_80");
            var noads = Tables.Products.Find(p => p.kind == "noads");
            var month = Tables.Products.Find(p => p.kind == "monthly");
            var boost = Tables.Products.Find(p => p.kind == "boost");
            Check(dia1 != null && noads != null && month != null && boost != null && Tables.Products.TrueForAll(p => !string.IsNullOrEmpty(p.productId)),
                  $"상품 {Tables.Products.Count}개 모두 Play Console 상품 ID 있음");
            S.iapOnce.RemoveAll(k => k.StartsWith("first2x:") || k.StartsWith("month:") || k.StartsWith("goldboost:"));
            double d0 = S.dia; int got1 = -1, got2 = -1;
            Iap.Buy(dia1, n => got1 = n);
            Iap.Buy(dia1, n => got2 = n);
            Check(got1 == dia1.dia * 2 && got2 == dia1.dia && S.dia == d0 + dia1.dia * 3, $"다이아 상품: 첫 구매 2배 {got1} → 다음 {got2} (다이아 +{S.dia - d0})");
            int gotN = -1; Iap.Buy(noads, n => gotN = n);
            int gotN2 = -1; Iap.Buy(noads, n => gotN2 = n);
            Check(gotN == 1 && Store.NoAds && gotN2 == -1 && Iap.Owned(noads) && Iap.Permanent(noads), "광고 제거: 사면 영구 · 두 번 못 삼");
            bool adCalled = false; Ads.Show(ok => adCalled = ok);
            Check(adCalled, "광고 제거를 사면 광고 없이 바로 보상");
            S.iapOnce.Remove("noads");
            double dm = S.dia; int gm = -1, gm2 = -1;
            Iap.Buy(month, n => gm = n); Iap.Buy(month, n => gm2 = n);
            Check(gm == month.dia && gm2 == -1 && Store.MonthDone(month) && S.dia == dm + month.dia, "월 1회 패키지: 이번 달 한 번만");
            int gb = -1; Iap.Buy(boost, n => gb = n);
            Check(gb == 1 && Store.GoldBoostOn && Store.GoldBoostDaysLeft == Tables.Ci("Gold_Boost_Days", 30), $"골드 부스터 {Store.GoldBoostDaysLeft}일");
            S.iapOnce.RemoveAll(k => k.StartsWith("goldboost:") || k.StartsWith("month:"));
            // 상점 화면: 에디터 = 초록 단추 · 결제 키 없는 폰처럼(SimulateNoStore) = 회색 + 사도 안 줌
            Popup.CloseAll(); Lobby.NoIntro = true; Game.ToLobby(); yield return Wait(0.2f);
            ShopTab.sub = 0; Game.Lobby.SetTab(5); yield return Wait(0.4f);
            var buy = GameObject.Find("P_" + dia1.id + "/Buy");
            Check(buy && UIKit.LabelOf(buy.GetComponent<Button>()).text == Iap.PriceText(dia1) && Iap.PriceText(dia1) == "1,200원", $"단추 가격 글자 = 스토어 가격 → 없으면 표 원화 ({Iap.PriceText(dia1)})");
            Capture("cl_01_shop_editor");
            Iap.SimulateNoStore = true;
            Game.Lobby.SetTab(5); yield return Wait(0.3f);
            double dn = S.dia; Iap.Buy(dia1, n => { });
            bool offNote = false; foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) if (t.text == L.T("iap_off")) offNote = true;
            Check(S.dia == dn && !Iap.CanSell(dia1), "결제 키 없는 폰: 원화 상품을 사도 지급 없음 (\"결제는 준비 중\")");
            Capture("cl_02_shop_nostore");
            Iap.SimulateNoStore = false;

            // ---------- 3) 폰에서 [초안] 숨김 (구글 로그인 · 클라우드 — Play 게임즈 붙기 전) ----------
            Store.SimulateDevice = true;
            Store.Unlink();
            Popup.CloseAll(); Game.ToTitle(); yield return Wait(0.3f);
            Title.I.OnStart(); yield return Wait(0.3f);
            Check(Game.Lobby != null && Title.I == null && GameObject.Find("Google") == null, "폰: [게임 시작] → 고르기 창 없이 바로 로비 (가짜 구글 연동 없음)");
            Popup.CloseAll(); Lobby.NoIntro = true;
            Settings.Open(); yield return Wait(0.3f);
            bool draft = false; foreach (var t in Popup.Open.win.GetComponentsInChildren<Text>()) if (t.text.Contains("[초안]")) draft = true;   // 설정 창 안만 (뒤 상점의 [초안] 글은 에디터 전용)
            Check(GameObject.Find("Account") == null && GameObject.Find("CloudSave") == null && GameObject.Find("Quit") != null && !draft,
                  "폰: 설정에 계정 · 클라우드 줄 없음 · [초안] 글자 없음 · [게임 종료] 그대로");
            Check(Cheat.Enabled == true && GameObject.Find("Cheat") != null, $"치트 단추 (Cheat_On {Tables.Ci("Cheat_On", 0)} — 비공개 테스트 빌드는 켬)");
            Capture("cl_03_settings_device");
            Popup.CloseAll();
            Store.SimulateDevice = false;
            Settings.Open(); yield return Wait(0.2f);
            Check(GameObject.Find("Account") != null, "에디터: 계정 · 클라우드 [초안] 줄은 그대로 (예전 시험용)");
            Popup.CloseAll();

            // ---------- 4) 글자 · 아이콘 ----------
            {
                var bad = new System.Collections.Generic.HashSet<char>();
                string[] keys = { "ad_loading", "ad_fail", "iap_done", "iap_restored", "iap_connecting", "iap_deferred", "iap_invalid", "iap_cancel", "iap_pending", "iap_fail", "iap_off" };
                bool all = true;
                foreach (var k in keys) { string s = L.T(k); if (string.IsNullOrEmpty(s) || s == k) all = false; foreach (char ch in s) if (ch > ' ' && font != null && !font.HasCharacter(ch)) bad.Add(ch); }
                Check(all && bad.Count == 0, $"광고 · 결제 안내 글자 {keys.Length}개 · 글꼴(주아)에 있음{(bad.Count > 0 ? " — 없음: " + string.Join(" ", bad) : "")}");
            }
            {
                var icons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Application);
                bool files = File.Exists("Assets/Art/AppIcon/app_icon.png") && File.Exists("Assets/Art/AppIcon/app_icon_fg.png") && File.Exists("Assets/Art/AppIcon/app_icon_bg.png") && File.Exists("Assets/Art/AppIcon/app_icon_round.png");
                Check(files, "앱 아이콘 4장 (tools/app_icon.py)");
            }

            // ---------- 5) 11-21 치트 "처음부터 초기화" ----------
            {
                Popup.CloseAll(); Lobby.NoIntro = true; Game.ToLobby(); yield return Wait(0.2f);
                Cheat.Open(); yield return Wait(0.3f);
                Check(GameObject.Find("CheatReset") != null, "치트 창 맨 아래 [처음부터 다시 (초기화)] 단추");
                Capture("cl_04_cheat_reset");
                Cheat.ResetAsk(); yield return Wait(0.3f);
                Check(GameObject.Find("CheatResetOk") != null, "누르면 확인 창 ([취소] / [초기화])");
                Capture("cl_05_cheat_reset_ask");
                Popup.CloseAll();
                // 진행을 쌓고 전투 중에 초기화
                S = SaveData.Cur;
                S.best = 30; S.stage = 31; S.gold = 9999999; S.dia = 77777; S.baseLv = 20; S.parts = 5000; S.tLv[0] = 10;
                S.costumes.Add("x"); S.tut.Add("x"); S.storySkip = true; Store.Link(); S.Save();
                Lobby.lastTab = 3; Battle.LastSpeed = 2f;
                Game.StartStage(1); yield return Wait(0.5f);
                Time.timeScale = 2f;
                Cheat.ResetNow(); yield return Wait(0.5f);
                var N = SaveData.Cur;
                bool fresh = N != S && N.best == 0 && N.stage == 1 && N.gold == Tables.C("Start_Gold") && N.baseLv == 1 && N.parts == 0 && N.tLv[0] == 1
                             && N.costumes.Count == 0 && N.tut.Count == 0 && !N.storySkip && !Store.Linked && N.costume == "basic";
                Check(fresh, $"초기화 → 새 세이브 (최고 {N.best} · 스테이지 {N.stage} · 골드 {N.gold} · 기지 Lv {N.baseLv} · 부품 {N.parts} · 연동 {Store.Linked})");
                bool titleOn = Tables.Ci("Title_On", 1) == 1;
                Check(Game.BattleUI == null && Battle.I == null, "전투 화면 닫힘");
                Check(titleOn ? Title.I != null && Game.Lobby == null : Game.Lobby != null, "타이틀부터 다시 (Title_On 0 이면 로비)");
                Check(Time.timeScale == 1f && Battle.LastSpeed == 1f && Lobby.lastTab == 0, "속도 x1 · 탭 기억도 처음으로");
                Check(!PlayerPrefs.HasKey(SaveData.BackupKey) || JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveData.BackupKey)).best == 0, "옛 세이브 백업도 지워짐 (깨진 세이브 되살리기로 옛 판이 돌아오지 않게)");
                Capture("cl_06_after_reset");
                if (Title.I) { Title.I.OnStart(); yield return Wait(0.3f); }
                Popup.CloseAll();
            }
            running = false;
        }
    }
}
