using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// Runtime-injected bootstrap. With BeforeSceneLoad, this builds a tiny
    /// "Bootstrap" scene programmatically so the project boots even if no
    /// .unity scene assets are committed.
    /// </summary>
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (GameObject.Find("GameBootstrap") != null) return;
            var boot = new GameObject("GameBootstrap");
            boot.AddComponent<GameBootstrap>();
            var setup = new GameObject("SceneSetup");
            setup.AddComponent<SceneSetup>();
            Object.DontDestroyOnLoad(boot);
            Debug.Log("[Echoes] AutoBootstrap injected");
        }
    }
}
