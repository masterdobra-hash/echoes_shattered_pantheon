using UnityEngine;
using Echoes.Entities;

namespace Echoes.Core
{
    /// <summary>
    /// Procedural scene builder for RuinedOlympus. Wrapped in try/catch
    /// per-section so a single failure doesn't kill the whole boot.
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        public int   enemiesToSpawn = 4;
        public float arenaSize      = 18f;

        private void Awake()
        {
            BootStatusLabel.Message = "Echoes: building scene...";
            SafeRun("Ground",   BuildGround);
            SafeRun("Light",    BuildLight);
            SafeRun("Player",   BuildPlayer);
            SafeRun("Boss",     BuildBoss);
            SafeRun("Enemies",  BuildEnemies);
            SafeRun("Audio",    BuildAudio);
            SafeRun("HUD",      BuildHud);
            BootStatusLabel.Message = "";   // hide once HUD is up
            Debug.Log("[Echoes] SceneSetup DONE");
        }

        private void SafeRun(string label, System.Action a)
        {
            try { a(); Debug.Log($"[Echoes] built: {label}"); }
            catch (System.Exception ex) {
                Debug.LogError($"[Echoes] build '{label}' FAILED: {ex}");
                BootStatusLabel.Message = $"build '{label}' failed: {ex.Message}";
            }
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(arenaSize/5f, 1f, arenaSize/5f);
            int layer = LayerMask.NameToLayer("Ground");
            if (layer < 0) layer = 0;
            ground.layer = layer;
            var r = ground.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.32f, 0.27f, 0.20f);
        }

        private void BuildLight()
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            l.color = new Color(1f, 0.95f, 0.85f);
            sun.transform.rotation = Quaternion.Euler(55f, 45f, 0f);
            RenderSettings.ambientLight = new Color(0.30f, 0.30f, 0.35f);
        }

        private void BuildPlayer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Titan Warrior";
            go.tag = "Player";
            go.transform.position = Vector3.up * 1f;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.85f, 0.75f, 0.40f);
            var pc = go.AddComponent<PlayerController>();

            // Replace FallbackCamera with the proper IsometricCamera
            var fallback = GameObject.Find("FallbackCamera");
            if (fallback != null) Destroy(fallback);

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.08f, 0.12f);
            cam.fieldOfView = 50f;
            var iso = camGo.AddComponent<Echoes.Systems.IsometricCamera>();
            iso.target = go.transform;

            var inp = new GameObject("TapToMove");
            var tap = inp.AddComponent<Echoes.Input.TapToMoveInput>();
            tap.cam = cam;
            tap.player = pc;
            // Use Default for ground if Ground layer is missing
            int g = LayerMask.NameToLayer("Ground");
            tap.groundMask = g >= 0 ? (LayerMask)(1 << g) | LayerMask.GetMask("Default") : (LayerMask)~0;
        }

        private void BuildBoss()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "The Fallen Hoplite";
            go.tag = "Boss";
            go.transform.position = new Vector3(0f, 1.4f, arenaSize/2.2f);
            go.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.55f, 0.10f, 0.10f);
            go.AddComponent<BossAI>();
        }

        private void BuildEnemies()
        {
            string[] ids = { "broken_hoplite", "temple_archer", "marble_guardian", "titan_spawn" };
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Enemy_" + ids[i % ids.Length];
                go.tag = "Enemy";
                float ang = (360f / enemiesToSpawn) * i * Mathf.Deg2Rad;
                go.transform.position = new Vector3(Mathf.Cos(ang) * 6f, 1f, Mathf.Sin(ang) * 6f);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.40f, 0.50f, 0.40f);
                var ai = go.AddComponent<EnemyAI>();
                ai.defId = ids[i % ids.Length];
            }
        }

        private void BuildAudio()  { Echoes.Systems.AudioManager.EnsureExists(); }

        private void BuildHud()
        {
            var go = new GameObject("HUD");
            go.AddComponent<Echoes.UI.HUDController>();
        }
    }
}
