using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// 11-21 Play Console 경고 "필수 하드웨어 기능 선언으로 인해 앱의 사용 가능 여부가 제한됨 — android.hardware.screen.portrait"
    /// (카치건 AndroidAppName 11-20 과 같은 고침): 세로 고정(screenOrientation="portrait") 때문에 붙는 암묵적 필수 하드웨어 선언을
    /// 안드로이드 빌드 때 만들어지는 unityLibrary AndroidManifest.xml 에 uses-feature required="false" 로 넣어 "선택사항"으로 바꿈.
    /// 게임은 그대로 세로 고정 — 설치 가능한 기기만 넓어짐. tools/aab_check.py 가 확인함.
    /// </summary>
    public class AndroidManifestFix : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest)) { Debug.LogWarning("[ManifestFix] AndroidManifest.xml 없음: " + manifest); return; }
            string xml = File.ReadAllText(manifest);
            if (xml.Contains("android.hardware.screen.portrait")) return;
            const string tag = "<uses-feature android:name=\"android.hardware.screen.portrait\" android:required=\"false\" />\n  ";
            int idx = xml.IndexOf("<application", StringComparison.Ordinal);
            if (idx < 0) { Debug.LogWarning("[ManifestFix] AndroidManifest.xml 에서 <application 을 못 찾음"); return; }
            File.WriteAllText(manifest, xml.Insert(idx, tag), new System.Text.UTF8Encoding(false));
            Debug.Log("[ManifestFix] AndroidManifest.xml 에 android.hardware.screen.portrait (required=false) 추가");
        }
    }
}
