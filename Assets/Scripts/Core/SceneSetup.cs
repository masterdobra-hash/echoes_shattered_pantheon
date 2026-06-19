using UnityEngine;
using Echoes.Entities;

namespace Echoes.Core
{
    /// <summary>
    /// Procedural scene builder for RuinedOlympus — keeps Unity Editor optional
    /// (we have no committed .unity scene contents; this script self-assembles
    /// the arena at runtime so CI build doesn't need scene serialisation).
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        public int   enemiesToSpawn = 4;
        public float arenaSize      = 18f;

        private void Awake()
        {
            BuildGround();
            BuildLight();
            BuildPlayer();
            BuildBoss();
            BuildEnemies();
            BuildAudio();
            BuildHud();
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(arenaSize/5f, 1f, arenaSize/5f);
            ground.layer = LayerMask.NameToLayer("Ground");
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

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            var iso = camGo.AddComponent<Echoes.Systems.IsometricCamera>();
            iso.target = go.transform;

            var inp = new GameObject("TapToMove");
            var tap = inp.AddComponent<Echoes.Input.TapToMoveInput>();
            tap.cam = cam;
            tap.player = pc;
            tap.groundMask = LayerMask.GetMask("Ground", "Default");
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
