using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EarthDef.EditorTools
{
    /// <summary>
    /// Main 씬 만들기 (메뉴: 지구 방위대 > 씬 다시 만들기 · 배치: EarthDef.EditorTools.SceneBuilder.BuildAll).
    /// 씬엔 카메라와 Boot 만 있고 나머지는 실행할 때 코드가 만든다. 표 변환도 같이.
    /// </summary>
    public static class SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("지구 방위대/씬 다시 만들기")]
        public static void BuildAll()
        {
            Debug.Log("[SceneBuilder] 표: " + TableConverter.Convert(true));
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.4f);
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            var urp = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urp != null) camGo.AddComponent(urp);

            new GameObject("Boot").AddComponent<Boot>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "카피바라 지구를 지켜라!";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] 만들었음: " + ScenePath);
        }
    }
}
