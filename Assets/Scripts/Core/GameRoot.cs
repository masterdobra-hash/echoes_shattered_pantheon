using UnityEngine;
using UnityEngine.UI;
using Echoes.Entities;

namespace Echoes.Core
{
    /// <summary>
    /// Builds the entire playable scene at runtime: camera, light, ground,
    /// player, boss, enemies, UGUI HUD. Single Awake; explicit ordering.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public PlayerController Player;
        public BossAI           Boss;
        public Camera           GameCamera;
        public Canvas           HudCanvas;

        // HUD widgets (created in code, no prefab needed)
        public Image HpFill;
        public Image MpFill;
        public Text  HpText;
        public Text  MpText;
        public Text  DiagText;     // top-left: shader/errors

        private const int LAYER_GROUND = 8;
        private const int LAYER_PLAYER = 9;
        private const int LAYER_ENEMY  = 10;
        private const int LAYER_BOSS   = 11;
        private const int LAYER_LOOT   = 12;

        private void Awake()
        {
            Debug.Log("[Echoes] GameRoot.Awake");

            // Load JSON content first
            ContentRegistry.Instance.LoadAllSync();

            BuildCamera();
            BuildLight();
            BuildGround();
            BuildPlayer();
            BuildBoss();
            BuildEnemies();
            BuildHud();
            Diag("ready");
            Debug.Log("[Echoes] GameRoot.Awake DONE");
        }

        private void Diag(string s)
        {
            string txt = "Echoes v0.3 | shader=" + MaterialFactory.CurrentShaderName + " | " + s;
            if (SelfTest.Errors.Count > 0)
                txt += " | ERRORS: " + string.Join(" ; ", SelfTest.Errors.ToArray());
            if (DiagText != null) DiagText.text = txt;
            Debug.Log("[Echoes] " + txt);
        }

        private void BuildCamera()
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            GameCamera = camGo.AddComponent<Camera>();
            GameCamera.clearFlags = CameraClearFlags.SolidColor;
            GameCamera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            GameCamera.fieldOfView = 55f;
            GameCamera.nearClipPlane = 0.1f;
            GameCamera.farClipPlane = 200f;
            // Diablo-II angle: high pitch, ~45deg yaw, sit back and above origin.
            camGo.transform.position = new Vector3(8f, 12f, -8f);
            camGo.transform.rotation = Quaternion.Euler(50f, -45f, 0f);
            camGo.AddComponent<AudioListener>();
            Debug.Log("[Echoes] camera built");
        }

        private void BuildLight()
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.95f, 0.85f);
            l.intensity = 1.4f;
            sun.transform.rotation = Quaternion.Euler(55f, 45f, 0f);
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.50f);
            Debug.Log("[Echoes] light built");
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);   // 40x40 units
            ground.transform.position   = Vector3.zero;
            ground.layer = LAYER_GROUND;
            var r = ground.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Make(new Color(0.33f, 0.26f, 0.18f));
            Debug.Log("[Echoes] ground built");
        }

        private void BuildPlayer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "TitanWarrior";
            go.tag = "Player";
            go.layer = LAYER_PLAYER;
            go.transform.position = new Vector3(0f, 1f, 0f);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Make(new Color(1.0f, 0.84f, 0.30f));   // gold
            Player = go.AddComponent<PlayerController>();
            go.AddComponent<Echoes.Input.TapToMoveInput>();
            Debug.Log("[Echoes] player built");
        }

        private void BuildBoss()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "TheFallenHoplite";
            go.tag = "Boss";
            go.layer = LAYER_BOSS;
            go.transform.position = new Vector3(0f, 1.6f, 8f);
            go.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Make(new Color(0.85f, 0.12f, 0.12f));
            Boss = go.AddComponent<BossAI>();
            Debug.Log("[Echoes] boss built");
        }

        private void BuildEnemies()
        {
            string[] ids = { "broken_hoplite", "temple_archer", "marble_guardian", "titan_spawn" };
            Color[]  cols = {
                new Color(0.40f, 0.55f, 0.35f),
                new Color(0.50f, 0.40f, 0.65f),
                new Color(0.75f, 0.75f, 0.78f),
                new Color(0.55f, 0.30f, 0.18f),
            };
            for (int i = 0; i < ids.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Enemy_" + ids[i];
                go.tag = "Enemy";
                go.layer = LAYER_ENEMY;
                float ang = (360f / ids.Length) * i * Mathf.Deg2Rad;
                go.transform.position = new Vector3(Mathf.Cos(ang) * 5f, 1f, Mathf.Sin(ang) * 5f);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = MaterialFactory.Make(cols[i]);
                var ai = go.AddComponent<EnemyAI>();
                ai.defId = ids[i];
            }
            Debug.Log("[Echoes] enemies built");
        }

        private void BuildHud()
        {
            var canvasGo = new GameObject("HudCanvas");
            HudCanvas = canvasGo.AddComponent<Canvas>();
            HudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Diag label top-left
            DiagText = HudFactory.Text(HudCanvas.transform, "diag", new Vector2(20, -20),
                                       new Vector2(900, 40), AnchorPreset.TopLeft, 18, Color.white);

            // HP bar bottom-left
            HudFactory.Panel(HudCanvas.transform, "HpBg", new Vector2(20, 20),
                             new Vector2(260, 36), AnchorPreset.BottomLeft, new Color(0,0,0,0.6f));
            HpFill = HudFactory.Image(HudCanvas.transform, "HpFill", new Vector2(22, 22),
                                      new Vector2(256, 32), AnchorPreset.BottomLeft, new Color(0.7f,0.1f,0.1f));
            HpText = HudFactory.Text(HudCanvas.transform, "HpText", new Vector2(28, 24),
                                     new Vector2(260, 36), AnchorPreset.BottomLeft, 18, Color.white);

            // MP bar bottom-right
            HudFactory.Panel(HudCanvas.transform, "MpBg", new Vector2(-280, 20),
                             new Vector2(260, 36), AnchorPreset.BottomRight, new Color(0,0,0,0.6f));
            MpFill = HudFactory.Image(HudCanvas.transform, "MpFill", new Vector2(-278, 22),
                                      new Vector2(256, 32), AnchorPreset.BottomRight, new Color(0.1f,0.3f,0.85f));
            MpText = HudFactory.Text(HudCanvas.transform, "MpText", new Vector2(-274, 24),
                                     new Vector2(260, 36), AnchorPreset.BottomRight, 18, Color.white);

            // Skill buttons row (centered)
            string[] skills = { "titan_slam", "inferno_strike", "aegis_ward" };
            string[] labels = { "Slam", "Inferno", "Aegis" };
            for (int i = 0; i < skills.Length; i++)
            {
                float x = -180f + i * 120f;
                var btn = HudFactory.Button(HudCanvas.transform, "Btn_" + skills[i], new Vector2(x, 20),
                                            new Vector2(110, 60), AnchorPreset.BottomCenter, labels[i]);
                string id = skills[i];
                btn.onClick.AddListener(() => { if (Player != null) Player.TryCast(id); });
            }

            // Add EventSystem so buttons receive clicks
            if (GameObject.Find("EventSystem") == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("[Echoes] HUD built (UGUI)");
        }

        private void Update()
        {
            if (Player == null) return;
            float hpPct = Player.hp / Mathf.Max(1f, Player.maxHp);
            float mpPct = Player.mana / Mathf.Max(1f, Player.maxMana);
            if (HpFill != null) HpFill.fillAmount = hpPct;
            if (MpFill != null) MpFill.fillAmount = mpPct;
            if (HpText != null) HpText.text = "HP " + (int)Player.hp + "/" + (int)Player.maxHp;
            if (MpText != null) MpText.text = "MP " + (int)Player.mana + "/" + (int)Player.maxMana;
        }
    }
}
