using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-18 폰 시험용 APK 굽기 (카치건 BuildAab 의 APK 부분을 가져옴).
    /// 메뉴 지구 방위대 > APK 빌드 (폰에 바로 설치)... = 창 — [APK 빌드] · [폰에 설치 (USB)] · [폴더 열기].
    /// 배치: bash tools/unity_batch.sh EarthDef.EditorTools.BuildApk.Build      (일반)
    ///       bash tools/unity_batch.sh EarthDef.EditorTools.BuildApk.BuildDev   (개발용 빌드 — 치트 · 오류 표시)
    /// 서명 = Unity 개발용 키 (비밀번호 필요 없음) · 광고 = 늘 구글 시험 광고(EARTH_TEST_ADS). 스토어용 AAB · 업로드 키는 BuildAab.cs.
    /// 결과 = Earth_Defence/Builds/earthdefence-{버전}[-dev].apk (git 제외).
    /// </summary>
    public static class BuildApk
    {
        /// <summary>패키지 이름 — 11-19 사용자 결정 "이대로" (첫 AAB 를 올리면 영영 못 바꿈)</summary>
        public const string PackageName = "com.zkfpf.earthdefence";
        const string FilePrefix = "earthdefence";

        [MenuItem("지구 방위대/APK 빌드 (폰에 바로 설치)...", priority = 30)]
        public static void Menu() => ApkBuildWindow.Open();

        [MenuItem("지구 방위대/안드로이드 설정 맞추기", priority = 32)]
        public static void SetupMenu() => Setup();

        /// <summary>배치용 — 일반 APK</summary>
        public static void Build() => ExitAfter(() => Make(false));

        /// <summary>배치용 — 개발용 빌드 APK (치트 단추 · 화면 구석 Development Build 표시 · 오류 로그 자세히)</summary>
        public static void BuildDev() => ExitAfter(() => Make(true));

        /// <summary>마지막으로 만든 파일 (창의 [폰에 설치] · [폴더 열기] 용)</summary>
        public static string LastOutput;

        static void ExitAfter(Func<bool> build)
        {
            int code = 1;
            try { code = build() ? 0 : 2; }
            catch (Exception e) { Debug.LogError("[BuildApk] " + e); code = 3; }
            EditorApplication.Exit(code);
        }

        /// <summary>안드로이드 설정 — 패키지 · 버전 이름 1.0.버전코드 · API 24~36 · IL2CPP ARM64 · 세로 고정</summary>
        public static void Setup()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.bundleVersion = VersionName(PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            AppIconSetup.Apply();   // 11-21 앱 아이콘 (tools/app_icon.py)
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildApk] 설정 — {PackageName} · 버전 {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}) · API 24~36 · IL2CPP ARM64 · 세로");
        }

        public static string VersionName(int code) => $"1.0.{code}";

        public static void BumpVersionCode()
        {
            PlayerSettings.Android.bundleVersionCode += 1;
            PlayerSettings.bundleVersion = VersionName(PlayerSettings.Android.bundleVersionCode);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildApk] 버전 코드 → {PlayerSettings.Android.bundleVersionCode}");
        }

        /// <summary>창에서 부름</summary>
        public static bool MakeSafe(bool dev)
        {
            try { return Make(dev); }
            catch (Exception e) { Debug.LogError("[BuildApk] " + e); return false; }
        }

        static bool Make(bool dev)
        {
            Setup();
            if (!BuildAab.PrepareAndroidDependencies()) return false;   // 11-21 광고 · 결제 라이브러리 (gradle 줄)
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[BuildApk] 빌드 대상을 Android 로 바꿈 (처음 한 번은 오래 걸림)");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, $"{FilePrefix}-{PlayerSettings.bundleVersion}{(dev ? "-dev" : "")}.apk");

            bool wasAab = EditorUserBuildSettings.buildAppBundle;
            bool wasCustom = PlayerSettings.Android.useCustomKeystore;
            string keepName = PlayerSettings.productName;
            BuildReport report;
            try
            {
                PlayerSettings.productName = BuildAab.AppName;      // 11-21 홈 화면 이름 (빌드 때만 — BuildAab 설명)
                EditorUserBuildSettings.buildAppBundle = false;
                PlayerSettings.Android.useCustomKeystore = false;   // 개발용 키 (비밀번호 없음)
                Debug.Log($"[BuildApk] APK 시작 — {PackageName} · 버전 {PlayerSettings.bundleVersion} · {(dev ? "개발용 빌드" : "일반")} · 씬 {string.Join(", ", scenes)} · 서명 개발용 키");
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = dev ? BuildOptions.Development : BuildOptions.None,
                    extraScriptingDefines = new[] { "EARTH_TEST_ADS" },   // 11-21 개발용 키 APK = 늘 구글 시험 광고 (눌러도 됨)
                });
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = wasAab;
                PlayerSettings.Android.useCustomKeystore = wasCustom;
                PlayerSettings.productName = keepName;
            }

            var s = report.summary;
            if (s.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type == LogType.Error || m.type == LogType.Exception) Debug.Log("[BuildApk] 오류: " + m.content);
                Debug.LogError($"[BuildApk] APK 실패 — {s.result} · 오류 {s.totalErrors}개");
                return false;
            }
            LastOutput = outPath;
            Debug.Log($"[BuildApk] APK 성공 — {outPath} · {new FileInfo(outPath).Length / 1048576f:0.0} MB · {s.totalTime.TotalMinutes:0.0}분");
            return true;
        }

        // ------------------------------------------------------------------ 폰에 설치 (USB 디버깅)

        static string Adb => Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe");

        /// <summary>USB 로 연결된 폰에 설치하고 켬. 돌려주는 글 = 창에 보일 결과</summary>
        public static string Install(string apk, out bool ok)
        {
            ok = false;
            if (string.IsNullOrEmpty(apk) || !File.Exists(apk)) return "APK 파일이 없습니다 — 먼저 [APK 빌드] 를 누르세요.";
            if (!File.Exists(Adb)) return "adb 를 못 찾음: " + Adb;

            var dev = Run("devices", 15000);
            var lines = dev.output.Split('\n').Select(l => l.Trim()).Where(l => l.EndsWith("device") || l.EndsWith("unauthorized")).ToArray();
            if (lines.Length == 0)
                return "연결된 폰이 없습니다 — 폰 설정 › 개발자 옵션 › USB 디버깅을 켜고 케이블을 다시 꽂으세요. (파일 전송만 보이면 USB 디버깅이 꺼진 것)";
            if (lines.All(l => l.EndsWith("unauthorized")))
                return "폰 화면에 \"USB 디버깅을 허용하시겠습니까?\" 창이 떠 있습니다 — [허용] 을 누른 뒤 다시 누르세요.";

            EditorUtility.DisplayProgressBar("폰에 설치", Path.GetFileName(apk), 0.5f);
            try
            {
                var r = Run($"install -r \"{apk}\"", 180000);
                string all = r.output + r.error;
                if (!all.Contains("Success"))
                {
                    if (all.Contains("INSTALL_FAILED_UPDATE_INCOMPATIBLE"))
                        return "폰에 서명이 다른 같은 앱이 있습니다 — 폰에서 앱을 지운 뒤 다시 설치하세요 (세이브도 지워짐).\n" + all.Trim();
                    return "설치 실패:\n" + all.Trim();
                }
                Run($"shell monkey -p {PackageName} -c android.intent.category.LAUNCHER 1", 15000);
                ok = true;
                return "설치하고 켰습니다 — " + Path.GetFileName(apk);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        static (string output, string error) Run(string args, int timeoutMs)
        {
            var info = new ProcessStartInfo(Adb, args)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
            };
            using (var p = Process.Start(info))
            {
                var o = p.StandardOutput.ReadToEndAsync();
                var e = p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(timeoutMs)) { try { p.Kill(); } catch { } return ("", "시간 초과"); }
                o.Wait(2000); e.Wait(2000);
                return (o.Result, e.Result);
            }
        }

        /// <summary>Builds 폴더에서 가장 최근 APK (Unity 를 다시 켜면 LastOutput 이 비어서)</summary>
        public static string Latest()
        {
            if (!string.IsNullOrEmpty(LastOutput) && File.Exists(LastOutput)) return LastOutput;
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            if (!Directory.Exists(dir)) return null;
            return new DirectoryInfo(dir).GetFiles("*.apk").OrderByDescending(f => f.LastWriteTime).Select(f => f.FullName).FirstOrDefault();
        }
    }

    /// <summary>APK 빌드 창</summary>
    public class ApkBuildWindow : EditorWindow
    {
        bool dev;
        string message = "";
        MessageType messageType = MessageType.Info;

        public static void Open()
        {
            var w = GetWindow<ApkBuildWindow>(true, "APK 빌드 (폰에 바로 설치)", true);
            w.minSize = new Vector2(460f, 330f);
            w.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("패키지 : " + BuildApk.PackageName + "  (임시 — 스토어 올리기 전에 확정)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("서명 : 개발용 키 (비밀번호 필요 없음)", EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);

            dev = EditorGUILayout.ToggleLeft("개발용 빌드 (치트 단추 · 화면 구석 \"Development Build\" 글자)", dev);
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"버전 {PlayerSettings.bundleVersion}  (버전 코드 {PlayerSettings.Android.bundleVersionCode})");
                if (GUILayout.Button("버전 코드 +1", GUILayout.Width(110f))) BuildApk.BumpVersionCode();
            }
            EditorGUILayout.LabelField("폰 시험용이라 버전 코드를 안 올려도 덮어 설치됩니다.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8f);

            if (GUILayout.Button("APK 빌드 (처음 10분쯤 · 다음부터 몇 분)", GUILayout.Height(30f)))
            {
                bool d = dev;
                message = "빌드 중…"; messageType = MessageType.Info;
                EditorApplication.delayCall += () =>   // 창 그리기가 끝난 다음 차례에 빌드
                {
                    bool ok = BuildApk.MakeSafe(d);
                    message = ok ? "빌드 끝 — " + Path.GetFileName(BuildApk.LastOutput) + " (Earth_Defence/Builds 폴더)"
                                 : "빌드하지 못했습니다 — Console 의 [BuildApk] 빨간 줄을 보세요";
                    messageType = ok ? MessageType.Info : MessageType.Error;
                    Repaint();
                };
                GUIUtility.ExitGUI();
            }

            string latest = BuildApk.Latest();
            using (new EditorGUI.DisabledScope(latest == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("폰에 설치 (USB)", GUILayout.Height(24f)))
                {
                    message = BuildApk.Install(latest, out bool ok);
                    messageType = ok ? MessageType.Info : MessageType.Warning;
                }
                if (GUILayout.Button("폴더 열기", GUILayout.Height(24f))) EditorUtility.RevealInFinder(latest);
            }
            if (latest != null) EditorGUILayout.LabelField("설치할 파일 : " + Path.GetFileName(latest), EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(message))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(message, messageType);
            }
        }
    }
}
