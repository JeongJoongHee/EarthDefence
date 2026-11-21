using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EarthDef
{
    /// <summary>
    /// 씬 시작점: 표·세이브를 읽고 전장 · 효과 · UI 를 코드로 만든다 (씬에는 카메라와 Boot 만).
    /// 흐름: 로비 → [전투 시작] → 전투 → 결과 → 로비.
    /// </summary>
    public class Boot : MonoBehaviour
    {
        /// <summary>시작에 걸린 시간 (성능 측정용): 표 · 세이브 / 소리 만들기 / 전체</summary>
        public static double BootTablesMs, BootAudioMs, BootMs;
        void Awake()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Tables.Load();
            SaveData.Load();
            Settings.Apply();
            BootTablesMs = sw.Elapsed.TotalMilliseconds;
            Audio.Ensure();
            BootAudioMs = sw.Elapsed.TotalMilliseconds - BootTablesMs;
            var cam = Camera.main;
            cam.backgroundColor = Art.Night;
            new GameObject("World").AddComponent<World>().Build(cam);
            new GameObject("Fx").AddComponent<Fx>();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            UIRoot.Build(cam);
            gameObject.AddComponent<BackKey>();     // 8차 "종료 기능": 폰 뒤로 가기
            Game.ToTitle();     // 7차 수정안: 타이틀 → [게임 시작] → 구글 연동 / 게스트 → 로비
            BootMs = sw.Elapsed.TotalMilliseconds;
        }

        void OnApplicationPause(bool p) { if (p) SaveData.Cur?.Save(); }
        void OnApplicationQuit() { SaveData.Cur?.Save(); }
    }

    /// <summary>화면 바꾸기</summary>
    public static class Game
    {
        public static Lobby Lobby;
        public static BattleUI BattleUI;

        /// <summary>7차 수정안: 타이틀 (Config Title_On 0 이면 바로 로비)</summary>
        public static void ToTitle()
        {
            if (Tables.Ci("Title_On", 1) != 1) { ToLobby(); return; }
            World.I.SetLobby(true);
            World.I.ShowSlots(0, false);
            LobbyWalker.Show(true);
            Title.Show();
            Audio.Music("lobby");
        }

        public static void ToLobby()
        {
            if (Title.I) UnityEngine.Object.Destroy(Title.I.gameObject);
            Popup.CloseAll();
            Tutorial.CloseCur();
            if (BattleUI) { UnityEngine.Object.Destroy(BattleUI.gameObject); BattleUI = null; }
            Battle.I?.Close();
            if (Lobby) UnityEngine.Object.Destroy(Lobby.gameObject);
            Lobby = UIKit.Rect(UIRoot.I.pages, "Lobby").Fill().gameObject.AddComponent<Lobby>();
            World.I.SetLobby(true);       // 7차: 로비 = 사령탑 없이 심플한 하늘 + 큰 지구 ("기지 외형은 게임 안에서만")
            Lobby.Build();
            World.I.ShowSlots(0, false);
            LobbyWalker.Show(true);       // 11-16 저녁: 기지 앞을 대장 카피가 어슬렁어슬렁
            Audio.Music("lobby");
        }

        /// <summary>로비 · 결과 창에서: 스태미나를 내고 시작 (모자라면 스태미나 창)</summary>
        public static bool TryStart(int stage, bool elite)
        {
            if (elite && !Meta.EliteOpen(stage)) elite = false;
            if (!Meta.Pay(elite)) { Popup.CloseAll(); if (!Lobby) ToLobby(); EarthDef.Lobby.ShowStamina(true); return false; }
            // 9차 수정안 "전투 시작을 누르면 이 스테이지는 어느 스테이지고 각오 · 간단한 정보 스토리 → 게임에 진입" (스태미나는 먼저 냄)
            string sid = StoryView.StageId(stage);
            if (StoryView.Want(sid)) { Popup.CloseAll(); StoryView.Play(sid, stage, () => StartStage(stage, 0, elite)); return true; }
            StartStage(stage, 0, elite);
            return true;
        }

        /// <summary>도전 모드 (3차): 하루 입장 횟수를 쓰고 시작 — 난이도 = 깬 최고 스테이지</summary>
        public static bool StartMode(string mode, int seed = 0)
        {
            if (Store.Tickets(mode) <= 0) { Toast.Show(L.T("ch_no_ticket")); return false; }
            SaveData.Cur.DailyAdd("mode:" + mode);
            StartStage(Mathf.Max(1, SaveData.Cur.best), seed, false, mode);
            return true;
        }

        public static void StartStage(int stage, int seed = 0, bool elite = false, string mode = "stage")
        {
            Popup.CloseAll();
            if (Lobby) { UnityEngine.Object.Destroy(Lobby.gameObject); Lobby = null; }
            if (BattleUI) UnityEngine.Object.Destroy(BattleUI.gameObject);
            Battle.I?.Close();
            if (mode == "stage") SaveData.Cur.stage = stage;
            Tutorial.CloseCur();
            LobbyWalker.Show(false);
            if (Title.I) UnityEngine.Object.Destroy(Title.I.gameObject);
            World.I.SetLobby(false);
            var b = Battle.Begin(stage, seed, elite, mode);
            Audio.Music("battle");
            BattleUI = UIKit.Rect(UIRoot.I.pages, "BattleUI").Fill().gameObject.AddComponent<BattleUI>();
            BattleUI.Build(b);
        }
    }

    /// <summary>캔버스 · 안전 영역 · 층(화면 · 창)</summary>
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot I;
        public Canvas canvas;
        public RectTransform safe, pages, popups;

        public static void Build(Camera cam)
        {
            var cgo = new GameObject("Canvas", typeof(RectTransform));
            I = cgo.AddComponent<UIRoot>();
            var canvas = I.canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = cam.aspect > 1080f / 1920f ? 1f : 0f;   // 폰 = 폭 기준, 태블릿처럼 넓으면 높이 기준
            cgo.AddComponent<GraphicRaycaster>();
            I.safe = UIKit.Rect(cgo.transform, "Safe").Fill();
            I.safe.gameObject.AddComponent<SafeArea>();
            I.pages = UIKit.Rect(I.safe, "Pages").Fill();
            I.popups = UIKit.Rect(I.safe, "Popups").Fill();
        }

        /// <summary>월드 위치 → 캔버스(Safe 안) 위치</summary>
        public Vector2 WorldToUI(Vector2 w)
        {
            var sp = World.I.cam.WorldToScreenPoint(w);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(pages, sp, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var lp);
            return lp;
        }
    }

    /// <summary>노치·둥근 모서리를 피하도록 UI 영역을 줄인다</summary>
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        void Update()
        {
            var r = Screen.safeArea;
            if (r == last || Screen.width == 0) return;
            last = r;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            rt.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    // 11-21 보상형 광고(Ads)는 Core/AdService.cs 로 옮김 (실제 AdMob)
}
