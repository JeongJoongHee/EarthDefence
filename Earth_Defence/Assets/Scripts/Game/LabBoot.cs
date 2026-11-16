using UnityEngine;
using UnityEngine.EventSystems;

namespace EarthDef
{
    /// <summary>터렛 시험장 씬의 시작점 (Boot 와 같은 준비 + 시험장 전투 + 시험장 화면)</summary>
    public class LabBoot : MonoBehaviour
    {
        public static Battle B;
        void Awake()
        {
            Application.targetFrameRate = 60;
            Tables.Load();
            SaveData.Load();
            // 위쪽 시험장 단추 아래에서 적이 나오게 (표는 그대로 · 이 씬에서만)
            Tables.SetC("Hud_H", Tables.Cf("Lab_Hud_H", 760f));
            var cam = Camera.main;
            cam.backgroundColor = Art.Night;
            new GameObject("World").AddComponent<World>().Build(cam);
            new GameObject("Fx").AddComponent<Fx>();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();
            }
            UIRoot.Build(cam);
            Audio.Ensure();
            Audio.Music("battle");
            B = Battle.Begin(10, 1, false);
            B.lab = new Lab();
            World.I.ShowSlots(0, true);
            UIKit.Rect(UIRoot.I.pages, "LabUI").Fill().gameObject.AddComponent<LabUI>().Build(B);
        }
    }
}
