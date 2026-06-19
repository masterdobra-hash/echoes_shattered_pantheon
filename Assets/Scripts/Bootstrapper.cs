using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Echoes
{
    /// <summary>
    /// THE single entry point. Attached to GameObject "[Bootstrapper]" in scene
    /// via m_Script reference (guid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb).
    /// No RuntimeInitializeOnLoadMethod. No GameObject.Find checks. Just runs in Awake.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        public Player Player;
        public Boss   BossInst;

        // HUD widgets (created in code)
        public Image HpFill, MpFill;
        public Text  HpText, MpText, DiagText;

        private void Awake()
        {
            Debug.Log("[Echoes] Bootstrapper.Awake");
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // Try to find a working shader
            MaterialFactory.Resolve();
            Debug.Log("[Echoes] shader = " + MaterialFactory.CurrentShaderName);

            BuildLight();
            BuildGround();
            BuildPlayer();
            BuildBoss();
            BuildEnemies();
            BuildHud();
            UpdateDiag("ready");
            Debug.Log("[Echoes] Bootstrapper.Awake DONE");
        }

        private void Update()
        {
            if (Player == null) return;
            if (HpFill != null) HpFill.fillAmount = Player.hp / Mathf.Max(1f, Player.maxHp);
            if (MpFill != null) MpFill.fillAmount = Player.mana / Mathf.Max(1f, Player.maxMana);
            if (HpText != null) HpText.text = "HP " + (int)Player.hp + "/" + (int)Player.maxHp;
            if (MpText != null) MpText.text = "MP " + (int)Player.mana + "/" + (int)Player.maxMana;
        }

        private void UpdateDiag(string s)
        {
            if (DiagText == null) return;
            DiagText.text = "Echoes v0.5 | sh=" + MaterialFactory.CurrentShaderName + " | " + s;
        }

        private void BuildLight()
        {
            var sun = new GameObject("Sun");
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.95f, 0.85f);
            l.intensity = 1.4f;
            sun.transform.rotation = Quaternion.Euler(55f, 45f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.60f);
        }

        private void BuildGround()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
            g.name = "Ground";
            g.transform.localScale = new Vector3(4f, 1f, 4f);
            g.transform.position = Vector3.zero;
            var r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Make(new Color(0.33f, 0.26f, 0.18f));
        }

        private void BuildPlayer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.tag = "Player";
            go.transform.position = new Vector3(0f, 1f, 0f);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Make(new Color(1.0f, 0.84f, 0.30f));
            Player = go.AddComponent<Player>();
            go.AddComponent<TapInput>();
        }

        private void BuildBoss()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Boss";
            go.tag = "Boss";
            go.transform.position = new Vector3(0f, 1.6f, 8f);
            go.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialFactory.Make(new Color(0.85f, 0.12f, 0.12f));
            BossInst = go.AddComponent<Boss>();
        }

        private void BuildEnemies()
        {
            Color[] cols = {
                new Color(0.40f, 0.55f, 0.35f),
                new Color(0.50f, 0.40f, 0.65f),
                new Color(0.75f, 0.75f, 0.78f),
                new Color(0.55f, 0.30f, 0.18f),
            };
            for (int i = 0; i < 4; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Enemy_" + i;
                go.tag = "Enemy";
                float ang = (360f / 4) * i * Mathf.Deg2Rad;
                go.transform.position = new Vector3(Mathf.Cos(ang) * 5f, 1f, Mathf.Sin(ang) * 5f);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = MaterialFactory.Make(cols[i % cols.Length]);
                go.AddComponent<Enemy>();
            }
        }

        private void BuildHud()
        {
            var canvasGo = new GameObject("HudCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            DiagText = HudFactory.Text(canvasGo.transform, "Diag",
                new Vector2(20, -20), new Vector2(900, 40),
                HudFactory.Anchor.TopLeft, 18, Color.white);

            HudFactory.Panel(canvasGo.transform, "HpBg",
                new Vector2(20, 20), new Vector2(260, 36),
                HudFactory.Anchor.BottomLeft, new Color(0, 0, 0, 0.6f));
            HpFill = HudFactory.Bar(canvasGo.transform, "HpFill",
                new Vector2(22, 22), new Vector2(256, 32),
                HudFactory.Anchor.BottomLeft, new Color(0.75f, 0.1f, 0.1f));
            HpText = HudFactory.Text(canvasGo.transform, "HpText",
                new Vector2(28, 24), new Vector2(260, 36),
                HudFactory.Anchor.BottomLeft, 18, Color.white);

            HudFactory.Panel(canvasGo.transform, "MpBg",
                new Vector2(-280, 20), new Vector2(260, 36),
                HudFactory.Anchor.BottomRight, new Color(0, 0, 0, 0.6f));
            MpFill = HudFactory.Bar(canvasGo.transform, "MpFill",
                new Vector2(-278, 22), new Vector2(256, 32),
                HudFactory.Anchor.BottomRight, new Color(0.1f, 0.3f, 0.85f));
            MpText = HudFactory.Text(canvasGo.transform, "MpText",
                new Vector2(-274, 24), new Vector2(260, 36),
                HudFactory.Anchor.BottomRight, 18, Color.white);

            string[] skillIds = { "titan_slam", "inferno_strike", "aegis_ward" };
            string[] labels = { "Slam", "Inferno", "Aegis" };
            for (int i = 0; i < 3; i++)
            {
                float x = -180f + i * 120f;
                int idx = i;
                var btn = HudFactory.Button(canvasGo.transform, "Btn_" + i,
                    new Vector2(x, 20), new Vector2(110, 60),
                    HudFactory.Anchor.BottomCenter, labels[idx]);
                string sid = skillIds[idx];
                btn.onClick.AddListener(() => { if (Player != null) Player.TryCast(sid); });
            }

            if (GameObject.Find("EventSystem") == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }

    // ==== MaterialFactory ====================================================

    public static class MaterialFactory
    {
        private static Shader _cached;
        public static string CurrentShaderName = "<none>";

        public static Shader Resolve()
        {
            if (_cached != null) return _cached;
            string[] names = {
                "Standard", "Mobile/Diffuse", "Legacy Shaders/Diffuse",
                "Legacy Shaders/VertexLit", "Unlit/Color", "Sprites/Default",
                "Hidden/InternalErrorShader"
            };
            for (int i = 0; i < names.Length; i++)
            {
                var s = Shader.Find(names[i]);
                if (s != null) { _cached = s; CurrentShaderName = names[i]; break; }
            }
            return _cached;
        }

        public static Material Make(Color color)
        {
            var sh = Resolve();
            var m = new Material(sh != null ? sh : Shader.Find("Hidden/InternalErrorShader"));
            if (m.HasProperty("_Color"))     m.SetColor("_Color", color);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_MainColor")) m.SetColor("_MainColor", color);
            return m;
        }
    }

    // ==== Player =============================================================

    public class Player : MonoBehaviour
    {
        public float maxHp = 500f, hp;
        public float maxMana = 200f, mana;
        public float moveSpeed = 5f;
        public float manaRegen = 6f, hpRegen = 1f;
        private Vector3 _dest;
        private bool _hasDest;
        private readonly Dictionary<string, float> _cd = new Dictionary<string, float>();

        private void Awake() { hp = maxHp; mana = maxMana; _dest = transform.position; }

        private void Update()
        {
            if (_hasDest)
            {
                var flat = new Vector3(_dest.x, transform.position.y, _dest.z);
                transform.position = Vector3.MoveTowards(transform.position, flat, moveSpeed * Time.deltaTime);
                if ((transform.position - flat).sqrMagnitude < 0.04f) _hasDest = false;
            }
            mana = Mathf.Min(maxMana, mana + manaRegen * Time.deltaTime);
            hp   = Mathf.Min(maxHp,   hp   + hpRegen   * Time.deltaTime);
            var keys = new List<string>(_cd.Keys);
            foreach (var k in keys) _cd[k] = Mathf.Max(0f, _cd[k] - Time.deltaTime);
        }

        public void MoveTo(Vector3 p) { _dest = p; _hasDest = true; }

        public bool TryCast(string skillId)
        {
            float cdCur; _cd.TryGetValue(skillId, out cdCur);
            if (cdCur > 0f) return false;
            float manaCost = 8f, cooldown = 2.5f, dmg = 36f, radius = 3f;
            if (skillId == "inferno_strike") { manaCost = 14; cooldown = 4f; dmg = 28; radius = 3.5f; }
            else if (skillId == "aegis_ward") { manaCost = 18; cooldown = 12f; dmg = 0; radius = 0; }
            if (mana < manaCost) return false;
            mana -= manaCost;
            _cd[skillId] = cooldown;
            if (dmg > 0f && radius > 0f)
            {
                var hits = Physics.OverlapSphere(transform.position, radius);
                if (hits != null) for (int i = 0; i < hits.Length; i++)
                {
                    var e = hits[i].GetComponent<Enemy>();
                    if (e != null) { e.TakeDamage(dmg); continue; }
                    var b = hits[i].GetComponent<Boss>();
                    if (b != null) b.TakeDamage(dmg);
                }
            }
            Debug.Log("[Player] cast " + skillId);
            return true;
        }

        public void TakeDamage(float d) { hp = Mathf.Max(0f, hp - d); }
    }

    // ==== Enemy ==============================================================

    public class Enemy : MonoBehaviour
    {
        public float hp = 120f, damage = 12f, speed = 2.2f, attackRange = 1.4f;
        public float attackCooldown = 1.2f;
        private float _atkTimer;
        private Transform _target;

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _target = p.transform;
        }

        private void Update()
        {
            if (_target == null || hp <= 0f) return;
            float d = Vector3.Distance(transform.position, _target.position);
            if (d > attackRange)
            {
                var dir = (_target.position - transform.position).normalized;
                transform.position += dir * speed * Time.deltaTime;
            }
            else
            {
                _atkTimer -= Time.deltaTime;
                if (_atkTimer <= 0f)
                {
                    var pc = _target.GetComponent<Player>();
                    if (pc != null) pc.TakeDamage(damage);
                    _atkTimer = attackCooldown;
                }
            }
        }

        public void TakeDamage(float d)
        {
            hp -= d;
            if (hp <= 0f) Destroy(gameObject);
        }
    }

    // ==== Boss ===============================================================

    public class Boss : MonoBehaviour
    {
        public float maxHp = 1400f, hp;
        public float damage = 35f, speed = 2.4f;
        private float _abilityTimer = 2f;
        private int _phase = 0;
        private Transform _target;

        private void Start()
        {
            hp = maxHp;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _target = p.transform;
        }

        private void Update()
        {
            if (_target == null || hp <= 0f) return;
            float r = hp / maxHp;
            if      (r <= 0.33f && _phase < 2) _phase = 2;
            else if (r <= 0.66f && _phase < 1) _phase = 1;
            float d = Vector3.Distance(transform.position, _target.position);
            if (d > 2f)
            {
                var dir = (_target.position - transform.position).normalized;
                float spd = _phase == 2 ? speed * 1.4f : speed;
                transform.position += dir * spd * Time.deltaTime;
            }
            _abilityTimer -= Time.deltaTime;
            if (_abilityTimer <= 0f)
            {
                if (d < 3.5f)
                {
                    var pc = _target.GetComponent<Player>();
                    float dmg = damage * (_phase == 2 ? 1.5f : 1.0f);
                    if (pc != null) pc.TakeDamage(dmg);
                }
                _abilityTimer = _phase == 2 ? 2f : 3.5f;
            }
        }

        public void TakeDamage(float d)
        {
            hp -= d;
            if (hp <= 0f) Destroy(gameObject);
        }
    }

    // ==== TapInput ===========================================================

    public class TapInput : MonoBehaviour
    {
        private Player _player;
        private Camera _cam;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _cam = Camera.main;
        }

        private void Update()
        {
            if (_player == null) return;
            if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
            bool tapped = false;
            Vector3 screen = Vector3.zero;
            if (UnityEngine.Input.GetMouseButtonDown(0))
            { tapped = true; screen = UnityEngine.Input.mousePosition; }
            else if (UnityEngine.Input.touchCount > 0)
            {
                var t = UnityEngine.Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { tapped = true; screen = new Vector3(t.position.x, t.position.y, 0f); }
            }
            if (!tapped) return;
            var ray = _cam.ScreenPointToRay(screen);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f)) _player.MoveTo(hit.point);
        }
    }

    // ==== HudFactory =========================================================

    public static class HudFactory
    {
        public enum Anchor { TopLeft, TopRight, BottomLeft, BottomRight, BottomCenter, Center }

        private static void ApplyAnchor(RectTransform rt, Anchor a, Vector2 pos, Vector2 size)
        {
            Vector2 amin = Vector2.zero, amax = Vector2.zero, pivot = Vector2.zero;
            if (a == Anchor.TopLeft)     { amin = new Vector2(0, 1); amax = new Vector2(0, 1); pivot = new Vector2(0, 1); }
            else if (a == Anchor.TopRight)    { amin = new Vector2(1, 1); amax = new Vector2(1, 1); pivot = new Vector2(1, 1); }
            else if (a == Anchor.BottomLeft)  { amin = new Vector2(0, 0); amax = new Vector2(0, 0); pivot = new Vector2(0, 0); }
            else if (a == Anchor.BottomRight) { amin = new Vector2(1, 0); amax = new Vector2(1, 0); pivot = new Vector2(1, 0); }
            else if (a == Anchor.BottomCenter){ amin = new Vector2(0.5f, 0); amax = new Vector2(0.5f, 0); pivot = new Vector2(0.5f, 0); }
            else                              { amin = new Vector2(0.5f, 0.5f); amax = new Vector2(0.5f, 0.5f); pivot = new Vector2(0.5f, 0.5f); }
            rt.anchorMin = amin; rt.anchorMax = amax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static Image Panel(Transform parent, string name, Vector2 pos, Vector2 size,
                                  Anchor a, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            ApplyAnchor(go.GetComponent<RectTransform>(), a, pos, size);
            return img;
        }

        public static Image Bar(Transform parent, string name, Vector2 pos, Vector2 size,
                                Anchor a, Color color)
        {
            var img = Panel(parent, name, pos, size, a, color);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            return img;
        }

        public static Text Text(Transform parent, string name, Vector2 pos, Vector2 size,
                                Anchor a, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = "";
            t.color = color;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleLeft;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ApplyAnchor(go.GetComponent<RectTransform>(), a, pos, size);
            return t;
        }

        public static Button Button(Transform parent, string name, Vector2 pos, Vector2 size,
                                    Anchor a, string label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);
            var btn = go.AddComponent<Button>();
            ApplyAnchor(go.GetComponent<RectTransform>(), a, pos, size);

            var lbl = Text(go.transform, "Label", Vector2.zero, size, Anchor.Center, 22, Color.white);
            lbl.text = label;
            lbl.alignment = TextAnchor.MiddleCenter;
            return btn;
        }
    }
}
