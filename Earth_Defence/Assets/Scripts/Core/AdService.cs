using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace EarthDef
{
    /// <summary>
    /// AdMob 번호 (11-21 비공개 테스트 준비 — 카치건 AdService 를 가져옴).
    /// 실제 번호는 사용자가 AdMob 에서 만든 뒤 `python tools/set_admob.py <앱 ID> <보상형 단위 ID>` 로 넣음
    ///   → 이 파일 · Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset · tools/aab_check.py 를 같이 고침.
    /// 아직 안 넣었으면(Configured false) 구글이 공개한 시험용 번호 = 시험 광고만 나옴 → AAB 점검(aab_check)이 실패로 막음.
    /// 개발용 키 APK(BuildApk) 는 EARTH_TEST_ADS 로 늘 시험 광고 — 내 광고를 내가 누르면 AdMob 계정이 막힐 수 있어서.
    /// </summary>
    public static class AdIds
    {
        public const string AppId = "ca-app-pub-5299482082775448~4098932532";
        public const string Rewarded = "ca-app-pub-5299482082775448/1611780664";
        /// <summary>구글이 공개한 안드로이드 시험용 앱 ID · 보상형 단위 (누구나 써도 됨)</summary>
        public const string TestAppId = "ca-app-pub-3940256099942544~3347511713";
        public const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";
        public static bool Configured => AppId != TestAppId && Rewarded != TestRewarded;
#if EARTH_TEST_ADS
        public const bool UseTest = true;
#else
        public const bool UseTest = false;
#endif
        public static string RewardedUnit => UseTest || !Configured ? TestRewarded : Rewarded;
    }

    /// <summary>
    /// 보상형 광고. 게임 속 광고 = 카드 새로고침 · 결과 2배 · 순찰 2배 · 스태미나 · 광고 상자 (모두 Ads.Show 하나로).
    ///  - "광고 제거"를 샀으면 광고 없이 바로 보상 (Store.NoAds)
    ///  - 에디터 · 자동 시험 = 광고 없이 바로 보상 (Draft — 화면에 "[초안]" 글자는 에디터에서만)
    ///  - 폰 = 켤 때 동의 확인(유럽 등만 창) → SDK 시작 → 광고 한 편 미리 불러 둠. 준비 안 됐으면 "불러오는 중" 토스트, 게임은 안 막힘
    ///  - 보상은 끝까지 봤을 때만 (중간에 닫으면 done(false))
    /// </summary>
    public static class Ads
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        public static bool Draft => false;
#else
        /// <summary>실제 광고 SDK 가 없는 곳(에디터) — 바로 보상</summary>
        public static bool Draft => true;
#endif

        /// <summary>11-21 스토어 촬영(RunStore) 때 에디터의 "[초안] 광고 없이 바로" 글자를 숨김</summary>
        public static bool HideDraftNote;
        /// <summary>화면에 "[초안]" 안내를 붙일지 (에디터에서만 · 촬영 땐 숨김)</summary>
        public static bool DraftNote => Draft && !HideDraftNote;

        public static void Show(Action<bool> done)
        {
            if (Store.NoAds) { done?.Invoke(true); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            AdMob.ShowRewarded(done);
#else
            done?.Invoke(true);
#endif
        }
    }

    /// <summary>광고 · 결제 콜백을 게임 흐름(메인 스레드)으로 옮기고, 늦게 다시 하기를 돌려 주는 작은 물건 (씬이 바뀌어도 남음)</summary>
    public class MainThread : MonoBehaviour
    {
        static MainThread instance;
        static readonly Queue<Action> queue = new();

        public static MainThread Get()
        {
            if (instance == null)
            {
                var go = new GameObject("MainThread");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<MainThread>();
            }
            return instance;
        }

        public static void Post(Action a) { lock (queue) queue.Enqueue(a); }
        public static void After(float sec, Action a) => Get().StartCoroutine(Later(sec, a));
        static IEnumerator Later(float sec, Action a) { yield return new WaitForSecondsRealtime(sec); a?.Invoke(); }

        void Update()
        {
            while (true)
            {
                Action a;
                lock (queue) { if (queue.Count == 0) break; a = queue.Dequeue(); }
                try { a(); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>AdMob 보상형 광고 (Google Mobile Ads 11.5.0 — 카치건 · 점프점프와 같은 버전)</summary>
    public static class AdMob
    {
        static readonly float[] RetryDelays = { 10f, 30f, 60f, 120f, 300f };
        static RewardedAd ad;
        static bool started, initialized, loading, consentBusy;
        static int failures;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Store.NoAds) return;   // 광고 제거를 샀으면 SDK 도 안 켬
            Start();
        }

        static void Start()
        {
            if (started) return;
            started = true;
            MainThread.Get();
            Debug.Log("[Ads] 시작 — " + (AdIds.UseTest || !AdIds.Configured ? "시험 광고" : "실제 광고") + " " + AdIds.RewardedUnit);
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            // 대상 연령 13세 이상 (앱 콘텐츠 타겟층) → 아동 대상 표시 안 함 · 광고 등급 상한 PG
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
                MaxAdContentRating = MaxAdContentRating.PG,
            });
            if (ConsentInformation.CanRequestAds()) InitSdk();   // 지난번에 동의를 받았으면 바로
            RequestConsent();
        }

        /// <summary>동의 정보 확인 → (필요한 나라만) 동의 창 → 광고 시작. 처음 켤 때 인터넷이 없었으면 광고 단추를 누를 때 다시 함 (카치건 11-12)</summary>
        static void RequestConsent()
        {
            if (consentBusy) return;
            consentBusy = true;
            ConsentInformation.Update(new ConsentRequestParameters(), updateError => MainThread.Post(() =>
            {
                if (updateError != null) Debug.LogWarning("[Ads] 동의 정보 확인 실패: " + updateError.Message);
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => MainThread.Post(() =>
                {
                    consentBusy = false;
                    if (formError != null) Debug.LogWarning("[Ads] 동의 창 실패: " + formError.Message);
                    if (ConsentInformation.CanRequestAds()) InitSdk();
                }));
            }));
        }

        static void InitSdk()
        {
            if (initialized) return;
            initialized = true;
            MobileAds.Initialize(_ => MainThread.Post(Load));
        }

        static bool Ready => ad != null && ad.CanShowAd();

        static void Load()
        {
            if (!initialized || loading || Ready) return;
            loading = true;
            RewardedAd.Load(AdIds.RewardedUnit, new AdRequest(), (loaded, error) => MainThread.Post(() =>
            {
                loading = false;
                if (error != null || loaded == null)
                {
                    float delay = RetryDelays[Mathf.Min(failures, RetryDelays.Length - 1)];
                    failures++;
                    Debug.LogWarning($"[Ads] 광고 불러오기 실패 ({(error != null ? error.GetMessage() : "빈 응답")}) → {delay}초 뒤 다시");
                    MainThread.After(delay, Load);
                    return;
                }
                failures = 0;
                ad = loaded;
            }));
        }

        public static void ShowRewarded(Action<bool> done)
        {
            if (!started) Start();
            if (!Ready)
            {
                Toast.Show(L.T("ad_loading"));
                if (!initialized) RequestConsent();   // 아직 시작 전(처음 켤 때 통신이 안 됐음) → 동의 확인부터 다시
                else if (!loading) { failures = 0; Load(); }
                done?.Invoke(false);
                return;
            }
            var a = ad;
            ad = null;
            bool rewarded = false, finished = false;
            void Finish()
            {
                if (finished) return;
                finished = true;
                AudioListener.pause = false;
                a.Destroy();
                Load();   // 다음 광고를 미리
                if (!rewarded) done?.Invoke(false);
            }
            a.OnAdFullScreenContentClosed += () => MainThread.Post(Finish);
            a.OnAdFullScreenContentFailed += e => MainThread.Post(() =>
            {
                Debug.LogWarning("[Ads] 광고를 띄우지 못함: " + e.GetMessage());
                Toast.Show(L.T("ad_fail"));
                Finish();
            });
            AudioListener.pause = true;   // 광고 소리와 겹치지 않게
            Debug.Log("[Ads] 보여 줌");
            a.Show(_ => MainThread.Post(() =>
            {
                if (rewarded) return;
                rewarded = true;
                done?.Invoke(true);
                SaveData.Cur?.Save();
            }));
        }
    }
#endif
}
