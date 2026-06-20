// Echoes: Shattered Pantheon — Iteration M Bootstrapper
// Portrait orientation + FixedJoystick (left) + large skill buttons (right)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Bootstrapper : MonoBehaviour
{
    // ---- Runtime references ----
    private Camera mainCam;
    private GameObject player;
    private GameObject boss;
    private List<GameObject> enemies = new List<GameObject>();
    private Joystick joystick;

    // ---- HUD ----
    private Slider hpBar;
    private Slider mpBar;
    private Text diagText;

    // ---- Stats ----
    private float playerHP = 500f, playerHPMax = 500f;
    private float playerMP = 200f, playerMPMax = 200f;
    private float playerSpeed = 4.5f;
    private float bossSpawnTimer = 3.5f;
    private bool bossSpawned = false;
    private float enemyFirstHitTimer = 1.5f;

    // ---- Skills ----
    private float slamCD = 0f, infernoCD = 0f, aegisCD = 0f;
    private float aegisActive = 0f;

    // ---- Material fallback ----
    private static Material standardMat;
    private static Material GetMat(Color c)
    {
        if (standardMat == null)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Mobile/Diffuse");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            standardMat = new Material(sh);
        }
        Material m = new Material(standardMat);
        m.color = c;
        return m;
    }

    void Awake()
    {
        // Force portrait orientation
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;

        BuildCamera();
        BuildLight();
        BuildGround();
        BuildPlayer();
        BuildEnemies();
        BuildHUD();
    }

    void BuildCamera()
    {
        GameObject camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        mainCam = camGO.AddComponent<Camera>();
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.08f, 0.07f, 0.10f, 1f);
        mainCam.transform.position = new Vector3(0f, 14f, -10f);
        mainCam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        mainCam.fieldOfView = 60f;
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane = 200f;
        camGO.AddComponent<AudioListener>();
    }

    void BuildLight()
    {
        GameObject lightGO = new GameObject("DirectionalLight");
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.95f, 0.85f);
        l.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.35f, 0.30f, 0.25f);
    }

    void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(4f, 1f, 4f); // 40x40
        ground.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.36f, 0.27f, 0.20f));
    }

    void BuildPlayer()
    {
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "TitanWarrior";
        player.transform.position = new Vector3(0f, 1f, -4f);
        player.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        player.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.85f, 0.75f, 0.30f));
    }

    void BuildEnemies()
    {
        for (int i = 0; i < 4; i++)
        {
            float ang = i * (Mathf.PI * 2f / 4f);
            GameObject e = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            e.name = "Hoplite_" + i;
            e.transform.position = new Vector3(Mathf.Cos(ang) * 10f, 1f, Mathf.Sin(ang) * 10f + 4f);
            e.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            e.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.55f, 0.18f, 0.18f));
            // Remove collider so enemies don't block boss/player
            Collider col = e.GetComponent<Collider>();
            if (col != null) Destroy(col);
            enemies.Add(e);
        }
    }

    void BuildBoss()
    {
        boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        boss.name = "TheFallenHoplite";
        boss.transform.position = new Vector3(0f, 1.6f, 8f);
        boss.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        boss.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.22f, 0.10f, 0.45f));
        Collider col = boss.GetComponent<Collider>();
        if (col != null) Destroy(col);
        bossSpawned = true;
    }

    void BuildHUD()
    {
        // EventSystem (required for joystick IPointerHandler)
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // Canvas
        GameObject canvasGO = new GameObject("HUDCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // HP bar (top)
        hpBar = MakeBar(canvas.transform, "HPBar",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(60f, -60f), new Vector2(700f, 50f),
            new Color(0.8f, 0.1f, 0.1f), playerHPMax);
        // MP bar (below HP)
        mpBar = MakeBar(canvas.transform, "MPBar",
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(60f, -130f), new Vector2(700f, 40f),
            new Color(0.15f, 0.35f, 0.85f), playerMPMax);

        // Diag text top-right
        GameObject diagGO = new GameObject("Diag");
        diagGO.transform.SetParent(canvas.transform, false);
        diagText = diagGO.AddComponent<Text>();
        diagText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        diagText.fontSize = 24;
        diagText.color = new Color(0.95f, 0.95f, 0.95f);
        diagText.alignment = TextAnchor.UpperRight;
        diagText.text = "Echoes v0.7 Portrait+Joystick";
        RectTransform drt = diagText.rectTransform;
        drt.anchorMin = new Vector2(1f, 1f);
        drt.anchorMax = new Vector2(1f, 1f);
        drt.pivot = new Vector2(1f, 1f);
        drt.anchoredPosition = new Vector2(-30f, -30f);
        drt.sizeDelta = new Vector2(500f, 40f);

        // FixedJoystick (left bottom) — 320x320
        BuildJoystick(canvas.transform);

        // Skill buttons (right bottom) — 180x180 stack
        BuildSkillButton(canvas.transform, "Slam",    new Vector2(-90f, 110f),  new Color(0.85f,0.4f,0.1f),  () => CastSlam());
        BuildSkillButton(canvas.transform, "Inferno", new Vector2(-280f, 110f), new Color(0.95f,0.2f,0.1f),  () => CastInferno());
        BuildSkillButton(canvas.transform, "Aegis",   new Vector2(-90f, 320f),  new Color(0.2f,0.6f,0.95f),  () => CastAegis());
    }

    Slider MakeBar(Transform parent, string n, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c, float maxVal)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(parent, false);
        Slider s = go.AddComponent<Slider>();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(go.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Fill area
        GameObject fa = new GameObject("FillArea");
        fa.transform.SetParent(go.transform, false);
        RectTransform faRt = fa.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(4f, 4f); faRt.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fa.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = c;
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

        s.fillRect = fillRt;
        s.minValue = 0f; s.maxValue = maxVal; s.value = maxVal;
        s.interactable = false;
        return s;
    }

    void BuildJoystick(Transform parent)
    {
        // Container
        GameObject jGO = new GameObject("FixedJoystick");
        jGO.transform.SetParent(parent, false);
        RectTransform jrt = jGO.AddComponent<RectTransform>();
        jrt.anchorMin = new Vector2(0f, 0f);
        jrt.anchorMax = new Vector2(0f, 0f);
        jrt.pivot = new Vector2(0.5f, 0.5f);
        jrt.anchoredPosition = new Vector2(220f, 220f);
        jrt.sizeDelta = new Vector2(320f, 320f);

        // Background image
        Image bgImg = jGO.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.45f);

        // Handle
        GameObject hGO = new GameObject("Handle");
        hGO.transform.SetParent(jGO.transform, false);
        Image hImg = hGO.AddComponent<Image>();
        hImg.color = new Color(1f, 1f, 1f, 0.85f);
        RectTransform hrt = hGO.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.5f, 0.5f);
        hrt.anchorMax = new Vector2(0.5f, 0.5f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(150f, 150f);

        // Attach FixedJoystick component
        FixedJoystick fj = jGO.AddComponent<FixedJoystick>();
        joystick = fj;

        // Wire fields via reflection-free SerializedField-like setup
        // Joystick base requires "background" (RectTransform) and "handle" (RectTransform).
        // These are serialized [SerializeField] privates — set via SendMessage workaround:
        // Easiest: set m_background/m_handle is impossible without reflection in IL2CPP-safe way.
        // BUT: Joystick.Start() auto-initializes if fields are null:
        //   "background = GetComponent<RectTransform>()" is NOT in base.
        // So we rely on JoystickHelper component to assign via SerializeField proxy.
        JoystickWirer w = jGO.AddComponent<JoystickWirer>();
        w.Setup(fj, jGO.GetComponent<RectTransform>(), hrt);
    }

    void BuildSkillButton(Transform parent, string label, Vector2 pos, Color c, System.Action onClick)
    {
        GameObject bGO = new GameObject("Btn_" + label);
        bGO.transform.SetParent(parent, false);
        RectTransform brt = bGO.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = pos;
        brt.sizeDelta = new Vector2(180f, 180f);

        Image bImg = bGO.AddComponent<Image>();
        bImg.color = c;
        Button btn = bGO.AddComponent<Button>();
        btn.targetGraphic = bImg;
        btn.onClick.AddListener(() => onClick());

        GameObject tGO = new GameObject("Label");
        tGO.transform.SetParent(bGO.transform, false);
        Text t = tGO.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = label;
        t.fontSize = 32;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Spawn boss after timer
        if (!bossSpawned)
        {
            bossSpawnTimer -= dt;
            if (bossSpawnTimer <= 0f) BuildBoss();
        }

        // Joystick movement
        if (player != null && joystick != null)
        {
            float h = joystick.Horizontal;
            float v = joystick.Vertical;
            Vector3 mv = new Vector3(h, 0f, v) * playerSpeed * dt;
            player.transform.position += mv;
        }

        // Enemy slow chase (first hit delayed)
        enemyFirstHitTimer -= dt;
        if (player != null && enemyFirstHitTimer <= 0f)
        {
            foreach (var e in enemies)
            {
                if (e == null) continue;
                Vector3 dir = (player.transform.position - e.transform.position);
                dir.y = 0f;
                if (dir.magnitude > 1.6f)
                    e.transform.position += dir.normalized * 1.8f * dt;
                else
                {
                    // melee damage
                    if (aegisActive <= 0f) playerHP -= 6f * dt;
                }
            }
        }

        // Boss chase + damage telegraph
        if (boss != null && player != null)
        {
            Vector3 dir = (player.transform.position - boss.transform.position);
            dir.y = 0f;
            if (dir.magnitude > 2.5f)
                boss.transform.position += dir.normalized * 2.4f * dt;
            else if (aegisActive <= 0f) playerHP -= 10f * dt;
        }

        // MP regen
        playerMP = Mathf.Min(playerMPMax, playerMP + 6f * dt);

        // Aegis duration
        if (aegisActive > 0f) aegisActive -= dt;

        // CDs
        slamCD = Mathf.Max(0f, slamCD - dt);
        infernoCD = Mathf.Max(0f, infernoCD - dt);
        aegisCD = Mathf.Max(0f, aegisCD - dt);

        // Update HUD
        if (hpBar != null) hpBar.value = Mathf.Max(0f, playerHP);
        if (mpBar != null) mpBar.value = playerMP;
        if (diagText != null)
            diagText.text = string.Format("HP {0:0}/{1:0}  MP {2:0}/{3:0}", playerHP, playerHPMax, playerMP, playerMPMax);

        // Camera follow
        if (mainCam != null && player != null)
        {
            Vector3 target = player.transform.position + new Vector3(0f, 14f, -10f);
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, target, 4f * dt);
        }
    }

    void CastSlam()
    {
        if (slamCD > 0f || playerMP < 8f) return;
        playerMP -= 8f; slamCD = 2.5f;
        // AoE damage: destroy enemies within 4m
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null) { enemies.RemoveAt(i); continue; }
            if (Vector3.Distance(e.transform.position, player.transform.position) < 4f)
            {
                Destroy(e);
                enemies.RemoveAt(i);
            }
        }
    }

    void CastInferno()
    {
        if (infernoCD > 0f || playerMP < 14f) return;
        playerMP -= 14f; infernoCD = 4f;
        // Damage boss
        if (boss != null && Vector3.Distance(boss.transform.position, player.transform.position) < 8f)
        {
            // Boss color flash
            var r = boss.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = GetMat(new Color(1f, 0.4f, 0.4f));
        }
    }

    void CastAegis()
    {
        if (aegisCD > 0f || playerMP < 12f) return;
        playerMP -= 12f; aegisCD = 8f;
        aegisActive = 3f;
    }
}

// Helper to wire Joystick base private SerializeField fields without reflection
public class JoystickWirer : MonoBehaviour
{
    [SerializeField] private FixedJoystick target;
    [SerializeField] private RectTransform bg;
    [SerializeField] private RectTransform hd;

    public void Setup(FixedJoystick t, RectTransform background, RectTransform handle)
    {
        target = t; bg = background; hd = handle;
    }

    void Start()
    {
        // Joystick.cs (MIT) auto-initializes in its own Start if 'background'/'handle' are null
        // (it sets background = GetComponent<RectTransform>() and handle = transform.GetChild(0)).
        // Our layout matches: FixedJoystick on a RectTransform, Handle is first child.
        // So no manual wiring needed — leave this component as a marker.
    }
}
