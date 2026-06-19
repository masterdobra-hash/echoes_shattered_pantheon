using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// Runtime-injected bootstrap. Runs BEFORE any scene load — guarantees
    /// the game self-assembles even if no .unity scenes are valid.
    /// Also installs a fallback Camera + UI label IMMEDIATELY so the
    /// screen is never black.
    /// </summary>
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void EarlyBoot()
        {
            // Force visible fallback frame ASAP — solid colour, no black.
            var camGo = new GameObject("FallbackCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.05f, 0.05f);   // dark red, NEVER black
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(0f, 12f, -10f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camGo.tag = "MainCamera";
            Object.DontDestroyOnLoad(camGo);

            // Status label so user sees something even before SceneSetup runs
            var status = new GameObject("BootStatus");
            status.AddComponent<BootStatusLabel>();
            Object.DontDestroyOnLoad(status);

            Debug.Log("[Echoes] EarlyBoot OK (fallback camera + status label)");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LateBoot()
        {
            if (GameObject.Find("GameBootstrap") != null) return;
            try
            {
                var boot = new GameObject("GameBootstrap");
                boot.AddComponent<GameBootstrap>();
                Object.DontDestroyOnLoad(boot);

                var setup = new GameObject("SceneSetup");
                setup.AddComponent<SceneSetup>();

                Debug.Log("[Echoes] LateBoot injected GameBootstrap + SceneSetup");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Echoes] LateBoot failed: {ex}");
            }
        }
    }

    /// <summary>Minimal IMGUI status label — guarantees visible text on screen.</summary>
    public class BootStatusLabel : MonoBehaviour
    {
        private GUIStyle _style;
        public static string Message = "Echoes: Booting...";

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) {
                    fontSize = 28, fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.85f, 0.4f) }
                };
            }
            GUI.Label(new Rect(20, 20, Screen.width - 40, 80), Message, _style);
        }
    }
}
