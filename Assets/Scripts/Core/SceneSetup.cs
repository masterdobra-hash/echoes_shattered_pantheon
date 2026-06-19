using UnityEngine;
using Echoes.Entities;

namespace Echoes.Core
{
    /// <summary>
    /// Procedural scene builder. ALL primitives get an explicit shader assigned
    /// via shared MaterialFactory so we never fall into "pink shader of doom".
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        public int   enemiesToSpawn = 4;
        public float arenaSize      = 18f;

        private void Awake()
        {
            BootStatusLabel.Message = "Echoes: building scene...";
            MaterialFactory.WarmUp();
            SafeRun("Ground",   BuildGround);
            SafeRun("Light",    BuildLight);
            SafeRun("Player",   BuildPlayer);
            SafeRun("Boss",     BuildBoss);
            SafeRun("Enemies",  BuildEnemies);
            SafeRun("Audio",    BuildAudio);
            SafeRun("HUD",      BuildHud);
            BootStatusLabel.Message = "";
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

        private void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Make(c);
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(arenaSize/5f, 1f, arenaSize/5f);
            int layer = LayerMask.NameToLayer("Ground");
            if (layer < 0) layer = 0;
            ground.layer = layer;
            Paint(ground, new Color(0.32f, 0.27f, 0.20f));
        }

        private void BuildLight()
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            l.color = new Color(1f, 0.95f, 0.85f);
            sun.transform.rotation = Quaternion.Euler(55f, 45f, 0f);
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.40f);
        }

        private void BuildPlayer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Titan Warrior";
            go.tag = "Player";
            go.transform.position = Vector3.up * 1f;
            Paint(go, new Color(0.95f, 0.78f, 0.30f));
            var pc = go.AddComponent<PlayerController>();

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
            Paint(go, new Color(0.75f, 0.10f, 0.10f));
            go.AddComponent<BossAI>();
        }

        private void BuildEnemies()
        {
            string[] ids = { "broken_hoplite", "temple_archer", "marble_guardian", "titan_spawn" };
            Color[] cols = {
                new Color(0.40f, 0.55f, 0.35f),
                new Color(0.45f, 0.40f, 0.55f),
                new Color(0.70f, 0.70f, 0.75f),
                new Color(0.55f, 0.35f, 0.20f),
            };
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Enemy_" + ids[i % ids.Length];
                go.tag = "Enemy";
                float ang = (360f / enemiesToSpawn) * i * Mathf.Deg2Rad;
                go.transform.position = new Vector3(Mathf.Cos(ang) * 6f, 1f, Mathf.Sin(ang) * 6f);
                Paint(go, cols[i % cols.Length]);
                var ai = go.AddComponent<EnemyAI>();
                ai.defId = ids[i % ids.Length];
            }
        }

        private void BuildAudio() { Echoes.Systems.AudioManager.EnsureExists(); }

        private void BuildHud()
        {
            var go = new GameObject("HUD");
            go.AddComponent<Echoes.UI.HUDController>();
        }
    }

    /// <summary>
    /// Tries Standard (BiRP), Universal Render Pipeline/Lit (URP), HDRP Lit, then
    /// finally bullet-proof Unlit/Color. Result is a guaranteed-coloured material.
    /// </summary>
    public static class MaterialFactory
    {
        private static Shader _shader;

        public static void WarmUp()
        {
            // Try in priority order
            _shader = Shader.Find("Standard");
            if (_shader == null) _shader = Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = Shader.Find("HDRP/Lit");
            if (_shader == null) _shader = Shader.Find("Unlit/Color");
            if (_shader == null) _shader = Shader.Find("Mobile/Diffuse");
            if (_shader == null) _shader = Shader.Find("Legacy Shaders/Diffuse");
            if (_shader == null) _shader = Shader.Find("Hidden/InternalErrorShader");
            Debug.Log($"[Echoes] MaterialFactory shader = {(_shader != null ? _shader.name : "NULL")}");
        }

        public static Material Make(Color color)
        {
            if (_shader == null) WarmUp();
            var sh = _shader != null ? _shader : Shader.Find("Unlit/Color");
            var m = new Material(sh);
            if (m.HasProperty("_Color"))     m.SetColor("_Color", color);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_MainColor")) m.SetColor("_MainColor", color);
            return m;
        }
    }
}
