// Echoes: Shattered Pantheon — Iteration N (Operation 1 prototype)
// Custom InlineJoystick + DirectButton via Input.GetTouch — NO EventSystem dependency.
// Goal: kill 6 hoplites -> spawn boss -> kill boss -> VICTORY.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrapper : MonoBehaviour
{
    // ---- Camera & world ----
    private Camera mainCam;
    private GameObject player;
    private GameObject boss;
    private List<GameObject> enemies = new List<GameObject>();
    private List<GameObject> vfxList = new List<GameObject>();
    private List<float> vfxTTL = new List<float>();

    // ---- HUD (UGUI Image-based, no Button) ----
    private RectTransform joyBgRT, joyHandleRT;
    private RectTransform slamBtnRT, infernoBtnRT, aegisBtnRT;
    private Image slamBtnImg, infernoBtnImg, aegisBtnImg;
    private Image hpFillImg, mpFillImg;
    private Image damageFlashImg, victoryOverlayImg, defeatOverlayImg;
    private Text hudCountText, hudActionText, diagText, victoryText, defeatText;
    private Canvas hudCanvas;

    // ---- Stats ----
    private float playerHP = 500f, playerHPMax = 500f;
    private float playerMP = 200f, playerMPMax = 200f;
    private float playerSpeed = 6f;
    private float bossHP = 1000f, bossHPMax = 1000f;
    private bool bossSpawned = false;
    private int enemiesKilled = 0;
    private const int ENEMIES_TO_BOSS = 6;
    private bool victory = false, defeat = false;
    private float damageFlashTimer = 0f;

    // ---- Skills ----
    private float slamCD = 0f, infernoCD = 0f, aegisCD = 0f;
    private float aegisActive = 0f;
    private float slamFlashTimer = 0f, infernoFlashTimer = 0f, aegisFlashTimer = 0f;

    // ---- Joystick state ----
    private int joyTouchId = -1;            // active touch finger id for joystick (-1 = none)
    private Vector2 joyCenterScreenPx;      // screen-space pixel center of joystick
    private Vector2 joyHandleOffsetPx;      // handle visual offset (-radius..+radius)
    private const float JOY_RADIUS_PX = 160f;
    private Vector2 joystickInput = Vector2.zero; // -1..+1

    // ---- Button rects in screen px ----
    private Rect slamRect, infernoRect, aegisRect;

    // ---- Spawn timers ----
    private float waveStartDelay = 1.0f;
    private float spawnedWave = 0f;

    // ---- Materials (shader fallback chain) ----
    private static Material baseLit;
    private static Material GetMat(Color c)
    {
        if (baseLit == null)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Mobile/Diffuse");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            baseLit = new Material(sh);
        }
        Material m = new Material(baseLit);
        m.color = c;
        return m;
    }

    void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;

        BuildCamera();
        BuildLight();
        BuildGround();
        BuildPlayer();
        BuildHUD();
        ComputeButtonRects();
    }

    // ===================================================================
    // BUILD SCENE
    // ===================================================================
    void BuildCamera()
    {
        GameObject g = new GameObject("MainCamera");
        g.tag = "MainCamera";
        mainCam = g.AddComponent<Camera>();
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.08f, 0.07f, 0.10f, 1f);
        mainCam.transform.position = new Vector3(0f, 16f, -12f);
        mainCam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        mainCam.fieldOfView = 60f;
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane = 200f;
        g.AddComponent<AudioListener>();
    }

    void BuildLight()
    {
        GameObject g = new GameObject("DirectionalLight");
        Light l = g.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.95f, 0.85f);
        l.intensity = 1.4f;
        g.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.4f, 0.35f, 0.3f);
    }

    void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50
        ground.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.36f, 0.27f, 0.20f));
    }

    void BuildPlayer()
    {
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "TitanWarrior";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        player.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.95f, 0.82f, 0.25f));
        Collider c = player.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }

    void SpawnEnemies(int n)
    {
        for (int i = 0; i < n; i++)
        {
            float ang = (i + Time.time) * (Mathf.PI * 2f / Mathf.Max(1, n));
            GameObject e = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            e.name = "Hoplite_" + i;
            float r = 12f;
            e.transform.position = new Vector3(Mathf.Cos(ang) * r, 1f, Mathf.Sin(ang) * r);
            e.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            e.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.7f, 0.18f, 0.18f));
            Collider col = e.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            enemies.Add(e);
        }
    }

    void SpawnBoss()
    {
        boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        boss.name = "TheFallenHoplite";
        boss.transform.position = new Vector3(0f, 1.8f, 12f);
        boss.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        boss.GetComponent<Renderer>().sharedMaterial = GetMat(new Color(0.28f, 0.10f, 0.55f));
        Collider c = boss.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
        bossSpawned = true;
        SetActionText("BOSS APPEARED!");
    }

    // ===================================================================
    // HUD (no Button, no EventSystem — Images only)
    // ===================================================================
    void BuildHUD()
    {
        GameObject canvasGO = new GameObject("HUDCanvas");
        hudCanvas = canvasGO.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform p = canvasGO.transform;

        // HP bar background + fill (top-left)
        MakeImage(p, "HPbg", new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(60f,-60f), new Vector2(700f,50f), new Color(0.15f,0.05f,0.05f,0.85f), out _);
        hpFillImg = MakeFillImage(p, "HPfill",
            new Vector2(0f,1f), new Vector2(60f,-60f), new Vector2(700f,50f),
            new Color(0.9f,0.15f,0.15f,1f));

        // MP bar
        MakeImage(p, "MPbg", new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(60f,-130f), new Vector2(700f,40f), new Color(0.05f,0.08f,0.18f,0.85f), out _);
        mpFillImg = MakeFillImage(p, "MPfill",
            new Vector2(0f,1f), new Vector2(60f,-130f), new Vector2(700f,40f),
            new Color(0.2f,0.45f,0.95f,1f));

        // Enemies counter / Diag
        diagText = MakeText(p, "Diag", new Vector2(1f,1f), new Vector2(-30f,-30f),
            new Vector2(600f,40f), TextAnchor.UpperRight, 28, "Echoes v0.8 Prototype Op1");
        hudCountText = MakeText(p, "Count", new Vector2(0.5f,1f), new Vector2(0f,-200f),
            new Vector2(800f,60f), TextAnchor.MiddleCenter, 40, "Enemies: 6/6");
        hudActionText = MakeText(p, "Action", new Vector2(0.5f,1f), new Vector2(0f,-280f),
            new Vector2(900f,50f), TextAnchor.MiddleCenter, 32, "");

        // Joystick (visual only; logic in Update via touches)
        joyBgRT = BuildJoystickVisual(p);

        // Skill buttons (visual only; logic via TouchInRect)
        slamBtnImg = BuildSkillBtnVisual(p, "SLAM",  new Vector2(-110f, 130f),
                                         new Color(0.95f,0.45f,0.10f,1f), out slamBtnRT);
        infernoBtnImg = BuildSkillBtnVisual(p, "INFERNO", new Vector2(-310f, 130f),
                                            new Color(0.95f,0.18f,0.10f,1f), out infernoBtnRT);
        aegisBtnImg = BuildSkillBtnVisual(p, "AEGIS", new Vector2(-110f, 340f),
                                          new Color(0.2f,0.55f,0.95f,1f), out aegisBtnRT);

        // Damage flash overlay (fullscreen, alpha=0)
        damageFlashImg = MakeImage(p, "DmgFlash", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(1f,0f,0f,0f), out _);
        // Stretch
        RectTransform dfrt = damageFlashImg.rectTransform;
        dfrt.anchorMin = Vector2.zero; dfrt.anchorMax = Vector2.one;
        dfrt.offsetMin = Vector2.zero; dfrt.offsetMax = Vector2.zero;

        // Victory overlay
        victoryOverlayImg = MakeImage(p, "VicOverlay", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(0.1f,0.6f,0.2f,0f), out _);
        RectTransform vrt = victoryOverlayImg.rectTransform;
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
        victoryText = MakeText(p, "VicText", new Vector2(0.5f,0.5f), Vector2.zero,
            new Vector2(900f,200f), TextAnchor.MiddleCenter, 90, "");

        // Defeat overlay
        defeatOverlayImg = MakeImage(p, "DefOverlay", Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(0.5f,0.05f,0.05f,0f), out _);
        RectTransform drt = defeatOverlayImg.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        defeatText = MakeText(p, "DefText", new Vector2(0.5f,0.5f), Vector2.zero,
            new Vector2(900f,200f), TextAnchor.MiddleCenter, 90, "");
    }

    Image MakeImage(Transform parent, string n, Vector2 aMin, Vector2 aMax,
                     Vector2 pos, Vector2 size, Color c, out GameObject go)
    {
        go = new GameObject(n);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = c;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    Image MakeFillImage(Transform parent, string n, Vector2 aMin, Vector2 pos, Vector2 size, Color c)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = c;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMin;
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    Text MakeText(Transform parent, string n, Vector2 anchor, Vector2 pos,
                   Vector2 size, TextAnchor align, int fontSize, string content)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = align;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    RectTransform BuildJoystickVisual(Transform parent)
    {
        // Background ring (circle approximation as colored square; visual hint only)
        GameObject bgGO = new GameObject("JoyBG");
        bgGO.transform.SetParent(parent, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.30f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(0f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.anchoredPosition = new Vector2(240f, 240f);
        bgRT.sizeDelta = new Vector2(340f, 340f);

        // Handle (smaller white square)
        GameObject hGO = new GameObject("JoyHandle");
        hGO.transform.SetParent(bgGO.transform, false);
        Image hImg = hGO.AddComponent<Image>();
        hImg.color = new Color(1f, 1f, 1f, 0.80f);
        joyHandleRT = hGO.GetComponent<RectTransform>();
        joyHandleRT.anchorMin = new Vector2(0.5f, 0.5f);
        joyHandleRT.anchorMax = new Vector2(0.5f, 0.5f);
        joyHandleRT.pivot = new Vector2(0.5f, 0.5f);
        joyHandleRT.anchoredPosition = Vector2.zero;
        joyHandleRT.sizeDelta = new Vector2(150f, 150f);

        return bgRT;
    }

    Image BuildSkillBtnVisual(Transform parent, string label, Vector2 pos,
                               Color c, out RectTransform rt)
    {
        GameObject bGO = new GameObject("Btn_" + label);
        bGO.transform.SetParent(parent, false);
        Image img = bGO.AddComponent<Image>();
        img.color = c;
        rt = bGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(190f, 190f);

        // Label child
        GameObject tGO = new GameObject("Lbl");
        tGO.transform.SetParent(bGO.transform, false);
        Text t = tGO.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = label;
        t.fontSize = 36;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        RectTransform tRT = t.rectTransform;
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;

        return img;
    }

    // ===================================================================
    // Convert anchored-bottom-right pixel offset -> screen rect for touch hit
    // ===================================================================
    void ComputeButtonRects()
    {
        // Reference 1080x1920; scaler matchWidthOrHeight=0.5 -> compute scale based on actual screen
        float sw = Screen.width;
        float sh = Screen.height;
        float refW = 1080f, refH = 1920f;
        // log average scale
        float scaleW = sw / refW;
        float scaleH = sh / refH;
        float scale = Mathf.Lerp(scaleW, scaleH, 0.5f); // matches Unity ScaleWithScreenSize match=0.5

        float btnSize = 190f * scale;
        float halfBtn = btnSize * 0.5f;

        // Slam: anchored bottom-right at (-110, 130) px in ref coords
        // World screen px from bottom-right: (-110*scale + sw - halfBtn, 130*scale - halfBtn)
        // Easier: compute center in screen px
        Vector2 slamCenter   = new Vector2(sw + (-110f) * scale, (130f) * scale);
        Vector2 infCenter    = new Vector2(sw + (-310f) * scale, (130f) * scale);
        Vector2 aegisCenter  = new Vector2(sw + (-110f) * scale, (340f) * scale);

        slamRect    = new Rect(slamCenter.x   - halfBtn, slamCenter.y   - halfBtn, btnSize, btnSize);
        infernoRect = new Rect(infCenter.x    - halfBtn, infCenter.y    - halfBtn, btnSize, btnSize);
        aegisRect   = new Rect(aegisCenter.x  - halfBtn, aegisCenter.y  - halfBtn, btnSize, btnSize);

        // Joystick center on screen: anchored bottom-left at (240, 240) in ref
        joyCenterScreenPx = new Vector2(240f * scale, 240f * scale);
    }

    // ===================================================================
    // UPDATE LOOP
    // ===================================================================
    void Update()
    {
        float dt = Time.deltaTime;

        if (victory || defeat)
        {
            // Restart on any tap
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == 0 /*Began*/)
            {
                RestartLevel();
            }
            UpdateOverlays(dt);
            return;
        }

        // ---- 1. Read inputs (touches + mouse fallback) ----
        ReadInput();

        // ---- 2. Apply movement ----
        if (player != null)
        {
            Vector3 mv = new Vector3(joystickInput.x, 0f, joystickInput.y) * playerSpeed * dt;
            player.transform.position += mv;
            // Clamp to arena 25 units
            Vector3 p = player.transform.position;
            p.x = Mathf.Clamp(p.x, -23f, 23f);
            p.z = Mathf.Clamp(p.z, -23f, 23f);
            player.transform.position = p;
        }

        // ---- 3. Wave spawn ----
        if (!bossSpawned && spawnedWave < waveStartDelay)
        {
            spawnedWave += dt;
            if (spawnedWave >= waveStartDelay && enemies.Count == 0 && !bossSpawned && enemiesKilled < ENEMIES_TO_BOSS)
                SpawnEnemies(ENEMIES_TO_BOSS);
        }

        // ---- 4. Enemy AI ----
        if (player != null)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (e == null) { enemies.RemoveAt(i); continue; }
                Vector3 dir = player.transform.position - e.transform.position;
                dir.y = 0f;
                float d = dir.magnitude;
                if (d > 1.6f)
                    e.transform.position += dir.normalized * 2.4f * dt;
                else if (aegisActive <= 0f)
                {
                    playerHP -= 12f * dt;
                    damageFlashTimer = 0.15f;
                }
            }
        }

        // ---- 5. Boss AI ----
        if (boss != null && player != null && !victory)
        {
            Vector3 dir = player.transform.position - boss.transform.position;
            dir.y = 0f;
            if (dir.magnitude > 2.8f)
                boss.transform.position += dir.normalized * 2.6f * dt;
            else if (aegisActive <= 0f)
            {
                playerHP -= 25f * dt;
                damageFlashTimer = 0.2f;
            }
        }

        // ---- 6. Spawn boss when wave done ----
        if (!bossSpawned && enemiesKilled >= ENEMIES_TO_BOSS)
        {
            SpawnBoss();
        }

        // ---- 7. MP regen ----
        playerMP = Mathf.Min(playerMPMax, playerMP + 8f * dt);

        // ---- 8. Aegis duration ----
        if (aegisActive > 0f) aegisActive -= dt;

        // ---- 9. CDs ----
        slamCD = Mathf.Max(0f, slamCD - dt);
        infernoCD = Mathf.Max(0f, infernoCD - dt);
        aegisCD = Mathf.Max(0f, aegisCD - dt);

        // ---- 10. Flash timers (button press feedback) ----
        if (slamFlashTimer > 0f)
        {
            slamFlashTimer -= dt;
            slamBtnImg.color = Color.Lerp(new Color(1f,1f,1f,1f), new Color(0.95f,0.45f,0.10f,1f), 1f - slamFlashTimer/0.3f);
        }
        if (infernoFlashTimer > 0f)
        {
            infernoFlashTimer -= dt;
            infernoBtnImg.color = Color.Lerp(new Color(1f,1f,1f,1f), new Color(0.95f,0.18f,0.10f,1f), 1f - infernoFlashTimer/0.3f);
        }
        if (aegisFlashTimer > 0f)
        {
            aegisFlashTimer -= dt;
            aegisBtnImg.color = Color.Lerp(new Color(1f,1f,1f,1f), new Color(0.2f,0.55f,0.95f,1f), 1f - aegisFlashTimer/0.3f);
        }

        // ---- 11. Damage flash ----
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= dt;
            damageFlashImg.color = new Color(1f, 0f, 0f, Mathf.Clamp(damageFlashTimer * 2f, 0f, 0.5f));
        }
        else damageFlashImg.color = new Color(1f, 0f, 0f, 0f);

        // ---- 12. VFX ----
        for (int i = vfxList.Count - 1; i >= 0; i--)
        {
            vfxTTL[i] -= dt;
            if (vfxTTL[i] <= 0f)
            {
                if (vfxList[i] != null) Object.Destroy(vfxList[i]);
                vfxList.RemoveAt(i);
                vfxTTL.RemoveAt(i);
            }
            else if (vfxList[i] != null)
            {
                vfxList[i].transform.localScale += new Vector3(8f, 0f, 8f) * dt;
            }
        }

        // ---- 13. HUD update ----
        if (hpFillImg != null)
        {
            float r = Mathf.Max(0f, playerHP / playerHPMax);
            hpFillImg.rectTransform.sizeDelta = new Vector2(700f * r, 50f);
        }
        if (mpFillImg != null)
        {
            float r = Mathf.Max(0f, playerMP / playerMPMax);
            mpFillImg.rectTransform.sizeDelta = new Vector2(700f * r, 40f);
        }
        if (hudCountText != null)
        {
            if (!bossSpawned)
                hudCountText.text = "Enemies: " + (ENEMIES_TO_BOSS - enemiesKilled) + "/" + ENEMIES_TO_BOSS;
            else
                hudCountText.text = "BOSS HP: " + Mathf.Max(0, (int)bossHP) + "/" + (int)bossHPMax;
        }
        if (diagText != null)
        {
            diagText.text = "HP " + (int)playerHP + "  MP " + (int)playerMP + "  Joy(" + joystickInput.x.ToString("0.0") + "," + joystickInput.y.ToString("0.0") + ")";
        }

        // ---- 14. Camera follow ----
        if (mainCam != null && player != null)
        {
            Vector3 target = player.transform.position + new Vector3(0f, 16f, -12f);
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, target, 6f * dt);
        }

        // ---- 15. Win/lose conditions ----
        if (playerHP <= 0f && !defeat) { defeat = true; SetDefeat(); }
        if (boss != null && bossHP <= 0f && !victory)
        {
            victory = true; SetVictory();
            Object.Destroy(boss); boss = null;
        }
    }

    // ===================================================================
    // INPUT (touches + mouse fallback for editor)
    // ===================================================================
    void ReadInput()
    {
        joystickInput = Vector2.zero;
        bool slamTapped = false, infernoTapped = false, aegisTapped = false;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 pos = t.position;
                int phase = t.phase; // 0=Began,1=Moved,2=Stationary,3=Ended,4=Canceled

                if (phase == 0) // Began
                {
                    // Check buttons first (they have priority)
                    if (slamRect.Contains(pos))     { slamTapped = true; continue; }
                    if (infernoRect.Contains(pos))  { infernoTapped = true; continue; }
                    if (aegisRect.Contains(pos))    { aegisTapped = true; continue; }
                    // Else, if touch in left half and no joystick active -> capture
                    if (joyTouchId < 0 && pos.x < Screen.width * 0.5f)
                    {
                        joyTouchId = t.fingerId;
                        UpdateJoyHandle(pos);
                    }
                }
                else if (phase == 1 || phase == 2) // Moved/Stationary
                {
                    if (t.fingerId == joyTouchId)
                        UpdateJoyHandle(pos);
                }
                else if (phase == 3 || phase == 4) // Ended/Cancelled
                {
                    if (t.fingerId == joyTouchId)
                    {
                        joyTouchId = -1;
                        ResetJoyHandle();
                    }
                }
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            // Editor mouse fallback
            Vector3 mp = Input.mousePosition;
            Vector2 pos = new Vector2(mp.x, mp.y);
            if (slamRect.Contains(pos))         slamTapped = true;
            else if (infernoRect.Contains(pos)) infernoTapped = true;
            else if (aegisRect.Contains(pos))   aegisTapped = true;
        }

        if (slamTapped)    CastSlam();
        if (infernoTapped) CastInferno();
        if (aegisTapped)   CastAegis();
    }

    void UpdateJoyHandle(Vector2 touchScreenPx)
    {
        Vector2 delta = touchScreenPx - joyCenterScreenPx;
        if (delta.magnitude > JOY_RADIUS_PX)
            delta = delta.normalized * JOY_RADIUS_PX;
        joyHandleOffsetPx = delta;
        joystickInput = delta / JOY_RADIUS_PX; // -1..+1
        if (joyHandleRT != null)
            joyHandleRT.anchoredPosition = delta;
    }

    void ResetJoyHandle()
    {
        joyHandleOffsetPx = Vector2.zero;
        joystickInput = Vector2.zero;
        if (joyHandleRT != null) joyHandleRT.anchoredPosition = Vector2.zero;
    }

    // ===================================================================
    // SKILLS
    // ===================================================================
    void CastSlam()
    {
        if (slamCD > 0f || playerMP < 8f) { SetActionText("Slam: not ready"); return; }
        playerMP -= 8f; slamCD = 2.5f;
        slamFlashTimer = 0.3f;
        SetActionText("SLAM!");
        // VFX: yellow growing ring centered on player
        if (player != null) SpawnVFX(player.transform.position, new Color(1f, 0.9f, 0.2f, 0.6f), 0.4f);
        // AoE damage
        Vector3 pp = player.transform.position;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null) { enemies.RemoveAt(i); continue; }
            if (Vector3.Distance(e.transform.position, pp) < 5.5f)
            {
                Object.Destroy(e);
                enemies.RemoveAt(i);
                enemiesKilled++;
            }
        }
        if (boss != null && Vector3.Distance(boss.transform.position, pp) < 5.5f)
        {
            bossHP -= 80f;
            FlashBoss();
        }
    }

    void CastInferno()
    {
        if (infernoCD > 0f || playerMP < 14f) { SetActionText("Inferno: not ready"); return; }
        playerMP -= 14f; infernoCD = 4f;
        infernoFlashTimer = 0.3f;
        SetActionText("INFERNO!");
        if (player != null) SpawnVFX(player.transform.position, new Color(1f, 0.4f, 0.1f, 0.65f), 0.5f);
        // Forward damage cone (any enemy/boss within 9 units)
        Vector3 pp = player.transform.position;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null) { enemies.RemoveAt(i); continue; }
            if (Vector3.Distance(e.transform.position, pp) < 9f)
            {
                Object.Destroy(e);
                enemies.RemoveAt(i);
                enemiesKilled++;
            }
        }
        if (boss != null && Vector3.Distance(boss.transform.position, pp) < 9f)
        {
            bossHP -= 160f;
            FlashBoss();
        }
    }

    void CastAegis()
    {
        if (aegisCD > 0f || playerMP < 12f) { SetActionText("Aegis: not ready"); return; }
        playerMP -= 12f; aegisCD = 8f;
        aegisFlashTimer = 0.3f;
        aegisActive = 3f;
        SetActionText("AEGIS!");
        if (player != null) SpawnVFX(player.transform.position, new Color(0.3f, 0.6f, 1f, 0.5f), 0.6f);
    }

    void FlashBoss()
    {
        if (boss == null) return;
        var r = boss.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = GetMat(new Color(1f, 0.5f, 0.5f));
    }

    void SpawnVFX(Vector3 pos, Color c, float ttl)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = "VFX";
        Collider col = g.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        g.transform.position = new Vector3(pos.x, 0.05f, pos.z);
        g.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        g.GetComponent<Renderer>().sharedMaterial = GetMat(c);
        vfxList.Add(g);
        vfxTTL.Add(ttl);
    }

    // ===================================================================
    // STATE
    // ===================================================================
    void SetActionText(string s)
    {
        if (hudActionText != null) hudActionText.text = s;
    }

    void SetVictory()
    {
        if (victoryOverlayImg != null) victoryOverlayImg.color = new Color(0.1f,0.6f,0.2f,0.55f);
        if (victoryText != null) victoryText.text = "VICTORY!\nTap to restart";
    }

    void SetDefeat()
    {
        if (defeatOverlayImg != null) defeatOverlayImg.color = new Color(0.5f,0.05f,0.05f,0.65f);
        if (defeatText != null) defeatText.text = "DEFEAT\nTap to restart";
    }

    void UpdateOverlays(float dt) {}

    void RestartLevel()
    {
        // Cheap restart: reset stats + respawn entities
        playerHP = playerHPMax;
        playerMP = playerMPMax;
        bossHP = bossHPMax;
        enemiesKilled = 0;
        bossSpawned = false;
        victory = false;
        defeat = false;
        spawnedWave = 0f;
        slamCD = infernoCD = aegisCD = 0f;
        aegisActive = 0f;

        // Destroy enemies & boss
        foreach (var e in enemies) if (e != null) Object.Destroy(e);
        enemies.Clear();
        if (boss != null) { Object.Destroy(boss); boss = null; }

        // Reset player position
        if (player != null) player.transform.position = new Vector3(0f, 1f, 0f);

        // Hide overlays
        if (victoryOverlayImg != null) victoryOverlayImg.color = new Color(0.1f,0.6f,0.2f,0f);
        if (defeatOverlayImg != null)  defeatOverlayImg.color  = new Color(0.5f,0.05f,0.05f,0f);
        if (victoryText != null) victoryText.text = "";
        if (defeatText != null) defeatText.text = "";

        SetActionText("Wave incoming...");
    }
}
