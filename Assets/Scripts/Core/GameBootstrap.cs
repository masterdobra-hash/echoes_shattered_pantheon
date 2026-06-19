using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Core
{
    /// <summary>
    /// First MonoBehaviour spawned in Bootstrap scene. Loads content registry,
    /// sets target frame rate, then transitions to RuinedOlympus.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string firstScene = "RuinedOlympus";
        [SerializeField] private int    targetFps  = 60;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = targetFps;
            QualitySettings.vSyncCount  = 0;
            Debug.Log("[Echoes] GameBootstrap.Awake");
        }

        private IEnumerator Start()
        {
            Debug.Log("[Echoes] Loading ContentRegistry...");
            yield return ContentRegistry.Instance.LoadAllAsync();
            Debug.Log($"[Echoes] Loaded skills={ContentRegistry.Instance.Skills.Count}, " +
                      $"enemies={ContentRegistry.Instance.Enemies.Count}, " +
                      $"items={ContentRegistry.Instance.Items.Count}");

            Debug.Log($"[Echoes] Loading scene '{firstScene}'");
            var op = SceneManager.LoadSceneAsync(firstScene);
            while (op != null && !op.isDone) yield return null;
            Debug.Log("[Echoes] Scene loaded");
        }
    }
}
