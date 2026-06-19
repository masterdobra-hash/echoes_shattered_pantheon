using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// Single, simple entry point. No IMGUI. No fallback cameras.
    /// Just: warm up materials, run self-test, hand off to GameRoot.
    /// </summary>
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (GameObject.Find("[GameRoot]") != null) return;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Debug.Log("[Echoes] AutoBootstrap.Boot start");

            MaterialFactory.Resolve();
            SelfTest.Run();

            var root = new GameObject("[GameRoot]");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameRoot>();
        }
    }
}
