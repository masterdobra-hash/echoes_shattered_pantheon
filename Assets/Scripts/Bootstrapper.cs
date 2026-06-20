// =============================================================================
// Echoes: Shattered Pantheon — Episode 1: Fall of Olympus
// =============================================================================
// Single-file monolithic game root for Iteration O.
// Architecture by regions:
//   #region Constants/Lore     - story canon constants
//   #region SaveSystem         - PlayerPrefs save/load
//   #region GameRoot           - MonoBehaviour entry point, runs everything
//   #region WorldBuilder       - ground/sky/lights/spawns per zone
//   #region InputController    - touches read directly (no EventSystem)
//   #region PlayerController   - movement, HP regen, damage flash
//   #region SkillController    - 4 skills with real MP cost + CD + VFX + SFX
//   #region EnemyAI            - 6 types with distinct behaviors
//   #region BossAI             - Fallen Hoplite, 3 phases, telegraph
//   #region VFXController      - skill rings, damage flashes, hit FX
//   #region HUDController      - HP/MP bars, quest log, dialog box, minimap
//   #region DialogSystem       - typewriter NPC dialogs
//   #region QuestSystem        - flags, progression
//   #region AudioController    - BGM loop + SFX one-shots
//   #region GameStateMachine   - Title/Zone1/Zone2/Zone3/Victory/Defeat
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrapper : MonoBehaviour
{
    // ============================================================
    #region Constants/Lore
    // ============================================================
    const string GAME_TITLE = "ECHOES: SHATTERED PANTHEON";
    const string EP1_TITLE  = "Episode 1: Fall of Olympus";
    const string GAME_VERSION = "v1.0.0";

    // Zones
    enum Zone { None, AwakeningGrove, BrokenAgora, AltarArena, Victory, Defeat }

    // Game state
    enum State { Boot, InZone, Dialog, Pause, Won, Lost }
    State state = State.Boot;
    Zone currentZone = Zone.None;

    // Skills
    enum Skill { Slam, Inferno, Aegis }

    // Quest flags
    [System.Serializable]
    public class QuestFlags {
        public bool talkedPythia;
        public int  killedScoutsZ1;     // 0..2
        public bool unlockedZone2;
        public bool talkedHermes;
        public bool unlockedAegis;
        public int  killedAgoraZ2;      // 0..6
        public bool unlockedZone3;
        public bool bossDefeated;
    }
    QuestFlags Q = new QuestFlags();
    #endregion

    // ============================================================
    #region SaveSystem
    // ============================================================
    const string SAVE_KEY = "echoes_ep1_save_v1";

    void SaveGame() {
        PlayerPrefs.SetInt("Q_talkedPythia",   Q.talkedPythia?1:0);
        PlayerPrefs.SetInt("Q_killedScoutsZ1", Q.killedScoutsZ1);
        PlayerPrefs.SetInt("Q_unlockedZone2",  Q.unlockedZone2?1:0);
        PlayerPrefs.SetInt("Q_talkedHermes",   Q.talkedHermes?1:0);
        PlayerPrefs.SetInt("Q_unlockedAegis",  Q.unlockedAegis?1:0);
        PlayerPrefs.SetInt("Q_killedAgoraZ2",  Q.killedAgoraZ2);
        PlayerPrefs.SetInt("Q_unlockedZone3",  Q.unlockedZone3?1:0);
        PlayerPrefs.SetInt("Q_bossDefeated",   Q.bossDefeated?1:0);
        PlayerPrefs.SetInt("Q_currentZone",    (int)currentZone);
        PlayerPrefs.Save();
    }

    void LoadGame() {
        Q.talkedPythia   = PlayerPrefs.GetInt("Q_talkedPythia",   0) == 1;
        Q.killedScoutsZ1 = PlayerPrefs.GetInt("Q_killedScoutsZ1", 0);
        Q.unlockedZone2  = PlayerPrefs.GetInt("Q_unlockedZone2",  0) == 1;
        Q.talkedHermes   = PlayerPrefs.GetInt("Q_talkedHermes",   0) == 1;
        Q.unlockedAegis  = PlayerPrefs.GetInt("Q_unlockedAegis",  0) == 1;
        Q.killedAgoraZ2  = PlayerPrefs.GetInt("Q_killedAgoraZ2",  0);
        Q.unlockedZone3  = PlayerPrefs.GetInt("Q_unlockedZone3",  0) == 1;
        Q.bossDefeated   = PlayerPrefs.GetInt("Q_bossDefeated",   0) == 1;
        currentZone      = (Zone)PlayerPrefs.GetInt("Q_currentZone", (int)Zone.AwakeningGrove);
    }

    void ResetSave() {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Q = new QuestFlags();
        currentZone = Zone.AwakeningGrove;
    }
    #endregion

    // ============================================================
    #region GameRoot - entry
    // ============================================================
    // World objects
    Camera mainCam;
    GameObject player;
    GameObject pythia;
    GameObject hermes;
    GameObject boss;
    GameObject altarHammer;
    GameObject zoneTrigger; // visible portal to next zone
    List<EnemyAgent> enemies = new List<EnemyAgent>();
    List<GameObject> vfxList = new List<GameObject>();
    List<float>      vfxTTL  = new List<float>();

    // Audio sources
    AudioSource bgmSource;
    AudioSource sfxSource;
    AudioClip clipBGM, clipSlam, clipInferno, clipHit;

    // Sprites loaded from Resources
    Sprite spriteGround, spriteSky, spriteSlam, spriteInferno, spriteAegis;

    // Materials
    Material matStandard;

    // Player stats
    float playerHP = 500f, playerHPMax = 500f;
    float playerMP = 200f, playerMPMax = 200f;
    float playerSpeed = 6.5f;
    float aegisActive = 0f;
    float damageFlashTimer = 0f;

    // Skill state
    float[] skillCD = new float[3];       // Slam, Inferno, Aegis
    float[] skillFlashT = new float[3];

    // Boss stats
    float bossHP = 1500f, bossHPMax = 1500f;
    int   bossPhase = 1;
    float bossTelegraph = 0f;             // 0 = no telegraph, > 0 = ticking down
    Vector3 bossTelegraphPos;
    GameObject bossTelegraphRing;
    int bossAddsSpawned = 0;

    // Input state
    int joyTouchId = -1;
    Vector2 joyCenterScreenPx;
    Vector2 joystickInput = Vector2.zero;  // -1..+1, ZERO when no touch (constraint 39)
    const float JOY_RADIUS_PX_REF = 160f;
    float joyRadiusPx;
    Rect rectSlam, rectInferno, rectAegis;

    // HUD refs
    Canvas hudCanvas;
    RectTransform joyHandleRT;
    Image hpFillImg, mpFillImg, damageFlashImg;
    Image victoryOverlayImg, defeatOverlayImg;
    Image slamBtnImg, infernoBtnImg, aegisBtnImg;
    Image slamIconImg, infernoIconImg, aegisIconImg;
    Text txtQuestLog, txtAction, txtDiag, txtZoneName;
    Text txtVictory, txtDefeat;
    GameObject dialogPanelGO;
    Image dialogPanelImg;
    Text dialogSpeakerText, dialogBodyText;
    Image dialogContinueHint;

    // Dialog state
    enum DialogId { None, PythiaIntro, PythiaAfterScouts, HermesIntro, HermesGiveAegis, HermesBeforeBoss }
    DialogId activeDialog = DialogId.None;
    int dialogLine = 0;
    string[][] dialogScript;       // [DialogId int][line]
    float dialogTypeT = 0f;
    int   dialogTypeChar = 0;
    bool  dialogTouchPrev = false;

    // NPC dialog triggers
    System.Action onDialogClose;

    // Action log
    string actionText = "";
    float actionTextT = 0f;

    void Awake() {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        LoadAllResources();
        LoadGame();
        BuildHUD();
        ComputeButtonRects();
        SetupAudio();
        InitDialogScripts();

        if (currentZone == Zone.None) currentZone = Zone.AwakeningGrove;
        EnterZone(currentZone);
        state = State.InZone;
    }

    void LoadAllResources() {
        spriteGround   = LoadSpriteFromResources("ground_marble");
        spriteSky      = LoadSpriteFromResources("sky_olympus");
        spriteSlam     = LoadSpriteFromResources("icon_slam");
        spriteInferno  = LoadSpriteFromResources("icon_inferno");
        spriteAegis    = LoadSpriteFromResources("icon_aegis");
        clipBGM        = Resources.Load<AudioClip>("bgm_olympus");
        clipSlam       = Resources.Load<AudioClip>("sfx_slam");
        clipInferno    = Resources.Load<AudioClip>("sfx_inferno");
        clipHit        = Resources.Load<AudioClip>("sfx_hit");
    }

    Sprite LoadSpriteFromResources(string name) {
        Texture2D t = Resources.Load<Texture2D>(name);
        if (t == null) return null;
        return Sprite.Create(t, new Rect(0,0,t.width,t.height), new Vector2(0.5f,0.5f), 100f);
    }

    Material GetStandardMat(Color c) {
        if (matStandard == null) {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Mobile/Diffuse");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            matStandard = new Material(sh);
        }
        Material m = new Material(matStandard);
        m.color = c;
        return m;
    }

    Material GetTexturedMat(Texture2D tex, Color c) {
        Material m = GetStandardMat(c);
        if (tex != null) m.mainTexture = tex;
        return m;
    }
    #endregion

    // ============================================================
    #region WorldBuilder
    // ============================================================
    void ClearWorld() {
        foreach (var e in enemies) if (e != null && e.go != null) Object.Destroy(e.go);
        enemies.Clear();
        if (boss != null)         { Object.Destroy(boss); boss = null; }
        if (pythia != null)       { Object.Destroy(pythia); pythia = null; }
        if (hermes != null)       { Object.Destroy(hermes); hermes = null; }
        if (altarHammer != null)  { Object.Destroy(altarHammer); altarHammer = null; }
        if (zoneTrigger != null)  { Object.Destroy(zoneTrigger); zoneTrigger = null; }
        if (bossTelegraphRing != null) { Object.Destroy(bossTelegraphRing); bossTelegraphRing = null; }
        foreach (var v in vfxList) if (v != null) Object.Destroy(v);
        vfxList.Clear(); vfxTTL.Clear();
    }

    void EnterZone(Zone z) {
        ClearWorld();
        currentZone = z;
        SaveGame();

        if (mainCam == null) BuildCamera();
        BuildLighting(z);
        BuildGround();
        BuildSkybox();

        if (player == null) BuildPlayer();
        else player.transform.position = new Vector3(0f, 1f, -8f);

        playerHP = playerHPMax;
        playerMP = playerMPMax;

        switch (z) {
            case Zone.AwakeningGrove:
                SetZoneName("Awakening Grove");
                SpawnPythia(new Vector3(0f, 1f, 6f));
                SpawnScouts();
                if (!Q.talkedPythia) {
                    StartDialog(DialogId.PythiaIntro, null);
                }
                break;

            case Zone.BrokenAgora:
                SetZoneName("Broken Agora");
                SpawnHermes(new Vector3(8f, 1f, 6f));
                SpawnAgoraEnemies();
                if (!Q.talkedHermes) {
                    StartDialog(DialogId.HermesIntro, null);
                }
                break;

            case Zone.AltarArena:
                SetZoneName("Altar of Echo");
                SpawnAltarHammer(new Vector3(0f, 1f, 8f));
                SpawnBoss(new Vector3(0f, 1.8f, 12f));
                StartDialog(DialogId.HermesBeforeBoss, null);
                break;
        }

        UpdateQuestLog();
    }

    void BuildCamera() {
        GameObject g = new GameObject("MainCamera");
        g.tag = "MainCamera";
        mainCam = g.AddComponent<Camera>();
        mainCam.clearFlags = CameraClearFlags.Skybox;
        mainCam.backgroundColor = new Color(0.06f, 0.04f, 0.10f, 1f);
        mainCam.transform.position = new Vector3(0f, 16f, -12f);
        mainCam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        mainCam.fieldOfView = 58f;
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane = 250f;
        g.AddComponent<AudioListener>();
    }

    void BuildLighting(Zone z) {
        // Find or create directional light
        GameObject lightGO = GameObject.Find("DirectionalLight");
        if (lightGO == null) {
            lightGO = new GameObject("DirectionalLight");
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.85f, 0.65f);
            l.intensity = 1.3f;
            lightGO.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        }
        if (z == Zone.AltarArena)
            RenderSettings.ambientLight = new Color(0.20f, 0.10f, 0.30f);
        else if (z == Zone.BrokenAgora)
            RenderSettings.ambientLight = new Color(0.30f, 0.22f, 0.18f);
        else
            RenderSettings.ambientLight = new Color(0.35f, 0.30f, 0.25f);
    }

    void BuildGround() {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null) {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60x60
        }
        Texture2D groundTex = Resources.Load<Texture2D>("ground_marble");
        Renderer r = ground.GetComponent<Renderer>();
        if (r != null) {
            Material m = GetTexturedMat(groundTex, new Color(0.7f, 0.6f, 0.5f));
            if (groundTex != null) m.mainTextureScale = new Vector2(6f, 6f); // tile 6x
            r.sharedMaterial = m;
        }
    }

    void BuildSkybox() {
        // Use camera background sky color since Unity skybox material would need ShaderLab;
        // simulate sky via large inverted-normal sphere behind camera.
        GameObject skySphere = GameObject.Find("SkyDome");
        if (skySphere == null) {
            skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skySphere.name = "SkyDome";
            skySphere.transform.localScale = new Vector3(-200f, -200f, -200f); // inverted normals
            Collider col = skySphere.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
        Texture2D skyTex = Resources.Load<Texture2D>("sky_olympus");
        Renderer r = skySphere.GetComponent<Renderer>();
        if (r != null) {
            Material m = GetTexturedMat(skyTex, new Color(0.4f, 0.3f, 0.5f));
            r.sharedMaterial = m;
        }
        if (mainCam != null) mainCam.clearFlags = CameraClearFlags.SolidColor;
    }

    void BuildPlayer() {
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "TitanWarrior";
        player.transform.position = new Vector3(0f, 1f, -8f);
        player.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        player.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.95f, 0.82f, 0.25f));
        Collider c = player.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }

    void SpawnPythia(Vector3 pos) {
        pythia = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pythia.name = "OraclePythia";
        pythia.transform.position = pos;
        pythia.transform.localScale = new Vector3(1.0f, 1.4f, 1.0f);
        pythia.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.85f, 0.7f, 1.0f));
        Collider c = pythia.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }

    void SpawnHermes(Vector3 pos) {
        hermes = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hermes.name = "Hermes";
        hermes.transform.position = pos;
        hermes.transform.localScale = new Vector3(1.0f, 1.5f, 1.0f);
        hermes.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(1f, 0.9f, 0.4f));
        Collider c = hermes.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }

    void SpawnScouts() {
        for (int i = 0; i < 2; i++) {
            float ang = i * Mathf.PI;
            Vector3 p = new Vector3(Mathf.Cos(ang)*8f, 1f, 4f + Mathf.Sin(ang)*4f);
            SpawnEnemy(EnemyType.Scout, p);
        }
    }

    void SpawnAgoraEnemies() {
        // 4 main enemies + 2 weak
        SpawnEnemy(EnemyType.Warrior,     new Vector3(-10f, 1f, 6f));
        SpawnEnemy(EnemyType.Warrior,     new Vector3( 10f, 1f, 6f));
        SpawnEnemy(EnemyType.Archer,      new Vector3(  0f, 1f, 14f));
        SpawnEnemy(EnemyType.Shieldbearer,new Vector3(-6f,  1f, 12f));
        SpawnEnemy(EnemyType.Priest,      new Vector3( 6f,  1f, 12f));
        SpawnEnemy(EnemyType.Scout,       new Vector3(  0f, 1f, -2f));
    }

    void SpawnAltarHammer(Vector3 pos) {
        altarHammer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        altarHammer.name = "Aeron_Hammer";
        altarHammer.transform.position = pos;
        altarHammer.transform.localScale = new Vector3(2.5f, 0.4f, 2.5f);
        altarHammer.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.85f, 0.7f, 0.25f));
        Collider c = altarHammer.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
    }

    void SpawnEnemy(EnemyType t, Vector3 pos) {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        EnemyAgent a = new EnemyAgent();
        a.go = g;
        a.type = t;
        g.transform.position = pos;
        switch (t) {
            case EnemyType.Scout:
                g.name = "HopliteScout";
                g.transform.localScale = new Vector3(0.85f,0.85f,0.85f);
                g.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.55f, 0.18f, 0.18f));
                a.hp = a.hpMax = 60f; a.dmg = 8f; a.speed = 2.4f; a.range = 1.8f;
                break;
            case EnemyType.Warrior:
                g.name = "HopliteWarrior";
                g.transform.localScale = new Vector3(1.0f,1.0f,1.0f);
                g.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.80f, 0.15f, 0.15f));
                a.hp = a.hpMax = 120f; a.dmg = 14f; a.speed = 2.0f; a.range = 1.8f;
                break;
            case EnemyType.Archer:
                g.name = "HopliteArcher";
                g.transform.localScale = new Vector3(0.85f,1.0f,0.85f);
                g.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.95f, 0.85f, 0.20f));
                a.hp = a.hpMax = 80f; a.dmg = 20f; a.speed = 1.6f; a.range = 8f; a.isRanged = true;
                a.rangedCD = 2f;
                break;
            case EnemyType.Shieldbearer:
                g.name = "Shieldbearer";
                g.transform.localScale = new Vector3(1.2f,1.2f,1.2f);
                g.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.70f, 0.50f, 0.20f));
                a.hp = a.hpMax = 220f; a.dmg = 10f; a.speed = 1.4f; a.range = 1.8f;
                break;
            case EnemyType.Priest:
                g.name = "PriestOfErebus";
                g.transform.localScale = new Vector3(0.95f,1.15f,0.95f);
                g.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.5f, 0.2f, 0.7f));
                a.hp = a.hpMax = 100f; a.dmg = 0f; a.speed = 1.8f; a.range = 4f; a.isHealer = true;
                break;
        }
        Collider col = g.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        enemies.Add(a);
    }

    void SpawnBoss(Vector3 pos) {
        boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        boss.name = "FallenHoplite";
        boss.transform.position = pos;
        boss.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        boss.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.30f, 0.10f, 0.55f));
        Collider c = boss.GetComponent<Collider>();
        if (c != null) Object.Destroy(c);
        bossHP = bossHPMax;
        bossPhase = 1;
        bossTelegraph = 0f;
        bossAddsSpawned = 0;
    }

    void SpawnPortalToNextZone(Vector3 pos) {
        zoneTrigger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zoneTrigger.name = "ZonePortal";
        zoneTrigger.transform.position = pos;
        zoneTrigger.transform.localScale = new Vector3(3f, 0.1f, 3f);
        zoneTrigger.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.3f, 0.7f, 1f, 0.6f));
        Collider col = zoneTrigger.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
    }
    #endregion

    // ============================================================
    #region InputController
    // ============================================================
    void ComputeButtonRects() {
        float sw = Screen.width, sh = Screen.height;
        float refW = 1080f, refH = 1920f;
        float scaleW = sw/refW, scaleH = sh/refH;
        float scale = Mathf.Lerp(scaleW, scaleH, 0.5f);
        joyRadiusPx = JOY_RADIUS_PX_REF * scale;

        float btnSize = 200f * scale;
        float half = btnSize * 0.5f;
        // Anchored bottom-right at (-110, 140) / (-310, 140) / (-110, 360)
        Vector2 cSlam   = new Vector2(sw + (-110f)*scale, (140f)*scale);
        Vector2 cInf    = new Vector2(sw + (-320f)*scale, (140f)*scale);
        Vector2 cAegis  = new Vector2(sw + (-110f)*scale, (360f)*scale);
        rectSlam    = new Rect(cSlam.x - half,   cSlam.y - half,   btnSize, btnSize);
        rectInferno = new Rect(cInf.x - half,    cInf.y - half,    btnSize, btnSize);
        rectAegis   = new Rect(cAegis.x - half,  cAegis.y - half,  btnSize, btnSize);
        joyCenterScreenPx = new Vector2(240f*scale, 240f*scale);
    }

    bool tappedSlam, tappedInferno, tappedAegis;
    bool tappedDialogContinue;
    Vector2 lastTapPos;

    void ReadInput() {
        tappedSlam = tappedInferno = tappedAegis = false;
        tappedDialogContinue = false;
        lastTapPos = Vector2.zero;

        // CONSTRAINT 39: joystick MUST be zero when no touch
        bool anyJoystickTouch = false;

        if (Input.touchCount > 0) {
            for (int i = 0; i < Input.touchCount; i++) {
                Touch t = Input.GetTouch(i);
                Vector2 pos = t.position;
                int phase = (int)t.phase;
                lastTapPos = pos;

                if (phase == 0) { // Began
                    // Skill buttons first
                    if (rectSlam.Contains(pos))    { tappedSlam = true; continue; }
                    if (rectInferno.Contains(pos)) { tappedInferno = true; continue; }
                    if (rectAegis.Contains(pos))   { tappedAegis = true; continue; }

                    // Joystick: left half of screen
                    if (joyTouchId < 0 && pos.x < Screen.width * 0.5f) {
                        joyTouchId = t.fingerId;
                        UpdateJoyHandle(pos);
                        anyJoystickTouch = true;
                    }
                    // Else: dialog continue
                    tappedDialogContinue = true;
                }
                else if (phase == 1 || phase == 2) { // Moved/Stationary
                    if (t.fingerId == joyTouchId) {
                        UpdateJoyHandle(pos);
                        anyJoystickTouch = true;
                    }
                }
                else if (phase == 3 || phase == 4) { // Ended/Cancelled
                    if (t.fingerId == joyTouchId) {
                        joyTouchId = -1;
                    }
                }
            }
        }
        else if (Input.GetMouseButtonDown(0)) {
            Vector3 mp = Input.mousePosition;
            Vector2 pos = new Vector2(mp.x, mp.y);
            lastTapPos = pos;
            if (rectSlam.Contains(pos))         tappedSlam = true;
            else if (rectInferno.Contains(pos)) tappedInferno = true;
            else if (rectAegis.Contains(pos))   tappedAegis = true;
            else tappedDialogContinue = true;
        }

        // CONSTRAINT 39: reset on no active joystick touch
        if (!anyJoystickTouch && joyTouchId < 0) {
            joystickInput = Vector2.zero;
            if (joyHandleRT != null) joyHandleRT.anchoredPosition = Vector2.zero;
        }
    }

    void UpdateJoyHandle(Vector2 touchScreenPx) {
        Vector2 delta = touchScreenPx - joyCenterScreenPx;
        if (delta.magnitude > joyRadiusPx) delta = delta.normalized * joyRadiusPx;
        // Deadzone
        float n = delta.magnitude / joyRadiusPx;
        if (n < 0.15f) joystickInput = Vector2.zero;
        else joystickInput = delta / joyRadiusPx;
        if (joyHandleRT != null) joyHandleRT.anchoredPosition = delta;
    }
    #endregion

    // ============================================================
    #region PlayerController
    // ============================================================
    void UpdatePlayer(float dt) {
        if (player == null) return;

        // Movement (constraint 39: stop when joystickInput == 0)
        if (joystickInput.sqrMagnitude > 0.02f) {
            Vector3 mv = new Vector3(joystickInput.x, 0f, joystickInput.y) * playerSpeed * dt;
            Vector3 p = player.transform.position + mv;
            // Clamp to arena
            p.x = Mathf.Clamp(p.x, -27f, 27f);
            p.z = Mathf.Clamp(p.z, -27f, 27f);
            player.transform.position = p;
        }

        // MP regen
        playerMP = Mathf.Min(playerMPMax, playerMP + 9f * dt);

        // Aegis
        if (aegisActive > 0f) aegisActive -= dt;

        // Damage flash decay
        if (damageFlashTimer > 0f) damageFlashTimer -= dt;

        // Skill CDs
        for (int i = 0; i < 3; i++) {
            skillCD[i] = Mathf.Max(0f, skillCD[i] - dt);
            if (skillFlashT[i] > 0f) skillFlashT[i] -= dt;
        }

        // HP regen (slow, 1.5/s when not in damage flash)
        if (damageFlashTimer <= 0f && playerHP < playerHPMax)
            playerHP = Mathf.Min(playerHPMax, playerHP + 1.5f * dt);

        // Death
        if (playerHP <= 0f && state != State.Lost) {
            state = State.Lost;
            ShowDefeat();
        }

        // Camera follow
        if (mainCam != null) {
            Vector3 target = player.transform.position + new Vector3(0f, 16f, -12f);
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, target, 7f * dt);
        }
    }
    #endregion

    // ============================================================
    #region SkillController
    // ============================================================
    void TryCastSlam() {
        if (skillCD[0] > 0f) { ShowAction("Slam: cooldown"); return; }
        if (playerMP < 8f)   { ShowAction("Slam: not enough MP"); return; }
        playerMP -= 8f;
        skillCD[0] = 2.5f;
        skillFlashT[0] = 0.35f;
        ShowAction("SLAM!");
        SpawnRingVFX(player.transform.position, new Color(1f, 0.85f, 0.20f, 0.7f), 0.45f, 5.5f);
        PlaySfx(clipSlam);

        Vector3 pp = player.transform.position;
        for (int i = enemies.Count-1; i >= 0; i--) {
            EnemyAgent a = enemies[i];
            if (a == null || a.go == null) { enemies.RemoveAt(i); continue; }
            if (Vector3.Distance(a.go.transform.position, pp) < 5.5f) {
                DamageEnemy(a, 100f);
            }
        }
        if (boss != null && Vector3.Distance(boss.transform.position, pp) < 5.5f) {
            DamageBoss(100f);
        }
    }

    void TryCastInferno() {
        if (skillCD[1] > 0f) { ShowAction("Inferno: cooldown"); return; }
        if (playerMP < 14f)  { ShowAction("Inferno: not enough MP"); return; }
        playerMP -= 14f;
        skillCD[1] = 4f;
        skillFlashT[1] = 0.35f;
        ShowAction("INFERNO!");
        SpawnRingVFX(player.transform.position, new Color(1f, 0.35f, 0.10f, 0.7f), 0.55f, 9f);
        PlaySfx(clipInferno);

        Vector3 pp = player.transform.position;
        for (int i = enemies.Count-1; i >= 0; i--) {
            EnemyAgent a = enemies[i];
            if (a == null || a.go == null) { enemies.RemoveAt(i); continue; }
            if (Vector3.Distance(a.go.transform.position, pp) < 9f) {
                DamageEnemy(a, 160f);
            }
        }
        if (boss != null && Vector3.Distance(boss.transform.position, pp) < 9f) {
            DamageBoss(160f);
        }
    }

    void TryCastAegis() {
        if (!Q.unlockedAegis) { ShowAction("Aegis: not unlocked yet"); return; }
        if (skillCD[2] > 0f) { ShowAction("Aegis: cooldown"); return; }
        if (playerMP < 12f)  { ShowAction("Aegis: not enough MP"); return; }
        playerMP -= 12f;
        skillCD[2] = 8f;
        skillFlashT[2] = 0.35f;
        aegisActive = 3f;
        ShowAction("AEGIS!");
        SpawnRingVFX(player.transform.position, new Color(0.30f, 0.65f, 1.0f, 0.6f), 0.6f, 4f);
    }
    #endregion

    // ============================================================
    #region EnemyAI
    // ============================================================
    enum EnemyType { Scout, Warrior, Archer, Shieldbearer, Priest }

    class EnemyAgent {
        public GameObject go;
        public EnemyType type;
        public float hp, hpMax;
        public float dmg;
        public float speed;
        public float range;
        public bool isRanged, isHealer;
        public float rangedCD;
        public float rangedTimer;
    }

    void UpdateEnemies(float dt) {
        if (player == null) return;
        Vector3 pp = player.transform.position;

        for (int i = enemies.Count-1; i >= 0; i--) {
            EnemyAgent a = enemies[i];
            if (a == null || a.go == null) { enemies.RemoveAt(i); continue; }
            Vector3 dir = pp - a.go.transform.position;
            dir.y = 0f;
            float d = dir.magnitude;

            if (a.isHealer) {
                // Priest heals nearest enemy within 4m
                EnemyAgent target = null; float minD = 4f;
                foreach (var b in enemies) {
                    if (b == a || b == null || b.go == null) continue;
                    if (b.hp >= b.hpMax) continue;
                    float dd = Vector3.Distance(b.go.transform.position, a.go.transform.position);
                    if (dd < minD) { minD = dd; target = b; }
                }
                if (target != null) {
                    target.hp = Mathf.Min(target.hpMax, target.hp + 30f * dt);
                }
                // Keep distance from player
                if (d < 4f) {
                    a.go.transform.position -= dir.normalized * a.speed * dt;
                } else if (d > 7f) {
                    a.go.transform.position += dir.normalized * a.speed * dt;
                }
            }
            else if (a.isRanged) {
                // Archer: keep range 6..8m, fire periodically
                if (d > a.range) {
                    a.go.transform.position += dir.normalized * a.speed * dt;
                } else if (d < 5f) {
                    a.go.transform.position -= dir.normalized * a.speed * dt;
                }
                a.rangedTimer -= dt;
                if (a.rangedTimer <= 0f && d < a.range + 1f) {
                    a.rangedTimer = a.rangedCD;
                    if (aegisActive <= 0f) {
                        playerHP -= a.dmg;
                        damageFlashTimer = 0.18f;
                        PlaySfx(clipHit);
                    }
                    SpawnRingVFX(a.go.transform.position, new Color(1f, 0.8f, 0.2f, 0.5f), 0.3f, 1f);
                }
            }
            else {
                // Melee: walk to player, hit when in range
                if (d > a.range) {
                    a.go.transform.position += dir.normalized * a.speed * dt;
                } else {
                    if (aegisActive <= 0f) {
                        playerHP -= a.dmg * dt;
                        damageFlashTimer = 0.15f;
                    }
                }
            }
        }
    }

    void DamageEnemy(EnemyAgent a, float dmg) {
        a.hp -= dmg;
        SpawnRingVFX(a.go.transform.position, new Color(1f, 0.3f, 0.3f, 0.7f), 0.2f, 0.8f);
        PlaySfx(clipHit);
        if (a.hp <= 0f) {
            Object.Destroy(a.go);
            enemies.Remove(a);
            OnEnemyKilled();
        }
    }

    void OnEnemyKilled() {
        if (currentZone == Zone.AwakeningGrove) {
            Q.killedScoutsZ1 = (int)Mathf.Min(2, Q.killedScoutsZ1 + 1);
            if (Q.killedScoutsZ1 >= 2 && Q.talkedPythia && !Q.unlockedZone2) {
                StartDialog(DialogId.PythiaAfterScouts, () => {
                    Q.unlockedZone2 = true;
                    SpawnPortalToNextZone(new Vector3(0f, 0.1f, 18f));
                    UpdateQuestLog();
                    SaveGame();
                });
            }
        }
        else if (currentZone == Zone.BrokenAgora) {
            Q.killedAgoraZ2 = (int)Mathf.Min(6, Q.killedAgoraZ2 + 1);
            if (Q.killedAgoraZ2 >= 4 && Q.unlockedAegis && !Q.unlockedZone3) {
                Q.unlockedZone3 = true;
                SpawnPortalToNextZone(new Vector3(0f, 0.1f, 22f));
            }
        }
        UpdateQuestLog();
        SaveGame();
    }
    #endregion

    // ============================================================
    #region BossAI
    // ============================================================
    void UpdateBoss(float dt) {
        if (boss == null || player == null) return;
        Vector3 dir = player.transform.position - boss.transform.position;
        dir.y = 0f;
        float d = dir.magnitude;

        // Phase progression
        float hpPct = bossHP / bossHPMax;
        int newPhase = hpPct > 0.66f ? 1 : (hpPct > 0.33f ? 2 : 3);
        if (newPhase != bossPhase) {
            bossPhase = newPhase;
            ShowAction("BOSS PHASE " + bossPhase + "!");
            if (bossPhase == 3 && bossAddsSpawned == 0) {
                // Phase 3: summon 3 scouts
                for (int i = 0; i < 3; i++) {
                    float ang = i * Mathf.PI * 2f / 3f;
                    Vector3 p = boss.transform.position + new Vector3(Mathf.Cos(ang)*4f, 0f, Mathf.Sin(ang)*4f);
                    p.y = 1f;
                    SpawnEnemy(EnemyType.Scout, p);
                }
                bossAddsSpawned = 3;
            }
        }

        // Telegraph slam (phase 2+)
        if (bossTelegraph > 0f) {
            bossTelegraph -= dt;
            if (bossTelegraphRing != null) {
                float t = 1f - bossTelegraph/1.5f;
                bossTelegraphRing.transform.localScale = new Vector3(0.5f + t*4f, 0.05f, 0.5f + t*4f);
            }
            if (bossTelegraph <= 0f) {
                // Slam impact
                if (Vector3.Distance(player.transform.position, bossTelegraphPos) < 4.5f && aegisActive <= 0f) {
                    playerHP -= 80f;
                    damageFlashTimer = 0.4f;
                }
                if (bossTelegraphRing != null) { Object.Destroy(bossTelegraphRing); bossTelegraphRing = null; }
                SpawnRingVFX(bossTelegraphPos, new Color(0.6f, 0f, 0.8f, 0.7f), 0.5f, 4.5f);
            }
        } else {
            if (d > 2.8f) {
                boss.transform.position += dir.normalized * 2.6f * dt;
            } else if (aegisActive <= 0f) {
                playerHP -= 25f * dt;
                damageFlashTimer = 0.15f;
            }
            // Phase 2+: telegraph slam every 5 seconds
            if (bossPhase >= 2 && Random.value < 0.005f) {
                bossTelegraph = 1.5f;
                bossTelegraphPos = player.transform.position;
                bossTelegraphRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bossTelegraphRing.transform.position = new Vector3(bossTelegraphPos.x, 0.06f, bossTelegraphPos.z);
                bossTelegraphRing.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
                bossTelegraphRing.GetComponent<Renderer>().sharedMaterial = GetStandardMat(new Color(0.6f, 0f, 0.8f, 0.5f));
                Collider col = bossTelegraphRing.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
            }
        }
    }

    void DamageBoss(float dmg) {
        bossHP -= dmg;
        SpawnRingVFX(boss.transform.position, new Color(1f, 0.4f, 0.4f, 0.7f), 0.25f, 1.5f);
        PlaySfx(clipHit);
        if (bossHP <= 0f) {
            Object.Destroy(boss); boss = null;
            Q.bossDefeated = true;
            SaveGame();
            ShowVictory();
        }
    }
    #endregion

    // ============================================================
    #region VFXController
    // ============================================================
    void SpawnRingVFX(Vector3 pos, Color c, float ttl, float maxRadius) {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.transform.position = new Vector3(pos.x, 0.08f, pos.z);
        g.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
        g.GetComponent<Renderer>().sharedMaterial = GetStandardMat(c);
        Collider col = g.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        vfxList.Add(g);
        vfxTTL.Add(ttl);
        // Store target radius via name encoding (cheap)
        g.name = "VFX_" + maxRadius.ToString("0.0");
    }

    void UpdateVFX(float dt) {
        for (int i = vfxList.Count-1; i >= 0; i--) {
            vfxTTL[i] -= dt;
            if (vfxTTL[i] <= 0f || vfxList[i] == null) {
                if (vfxList[i] != null) Object.Destroy(vfxList[i]);
                vfxList.RemoveAt(i);
                vfxTTL.RemoveAt(i);
            } else {
                // Grow ring linearly
                float maxR = 4f;
                string n = vfxList[i].name;
                if (n.Length > 4) float.TryParse(n.Substring(4), out maxR);
                Vector3 ls = vfxList[i].transform.localScale;
                ls.x += (maxR*2f) * dt;
                ls.z += (maxR*2f) * dt;
                vfxList[i].transform.localScale = ls;
            }
        }
    }
    #endregion

    // ============================================================
    #region HUDController
    // ============================================================
    void BuildHUD() {
        // Canvas
        GameObject canvasGO = new GameObject("HUDCanvas");
        hudCanvas = canvasGO.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform p = canvasGO.transform;

        // Zone name (top center)
        txtZoneName = MakeText(p, "ZoneName", new Vector2(0.5f,1f), new Vector2(0f,-30f),
            new Vector2(900f,50f), TextAnchor.MiddleCenter, 36, "Awakening Grove");
        txtZoneName.color = new Color(1f, 0.9f, 0.6f);

        // HP bar
        MakeImage(p, "HPbg", new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(40f,-90f), new Vector2(580f,42f), new Color(0.10f,0.04f,0.04f,0.85f), out _);
        hpFillImg = MakeImage(p, "HPfill", new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(44f,-94f), new Vector2(572f,34f), new Color(0.85f, 0.12f, 0.12f), out _);

        // MP bar
        MakeImage(p, "MPbg", new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(40f,-140f), new Vector2(580f,32f), new Color(0.05f,0.05f,0.18f,0.85f), out _);
        mpFillImg = MakeImage(p, "MPfill", new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(44f,-144f), new Vector2(572f,24f), new Color(0.2f, 0.45f, 0.95f), out _);

        // Diag (top-right)
        txtDiag = MakeText(p, "Diag", new Vector2(1f,1f), new Vector2(-30f,-30f),
            new Vector2(420f,60f), TextAnchor.UpperRight, 20, "v1.0.0");
        txtDiag.color = new Color(0.7f,0.7f,0.7f);

        // Quest log (left side)
        MakeImage(p, "QuestBG", new Vector2(0f,1f), new Vector2(0f,1f),
            new Vector2(20f,-200f), new Vector2(620f,180f), new Color(0f,0f,0f,0.45f), out _);
        txtQuestLog = MakeText(p, "QuestLog", new Vector2(0f,1f), new Vector2(30f,-210f),
            new Vector2(600f,170f), TextAnchor.UpperLeft, 22, "");
        txtQuestLog.color = new Color(1f, 0.95f, 0.7f);

        // Action text (center, middle-upper)
        txtAction = MakeText(p, "Action", new Vector2(0.5f,1f), new Vector2(0f,-440f),
            new Vector2(900f,60f), TextAnchor.MiddleCenter, 40, "");
        txtAction.color = new Color(1f, 0.95f, 0.4f);

        // Joystick visual (bottom-left)
        GameObject joyBg = new GameObject("JoyBG");
        joyBg.transform.SetParent(p, false);
        Image jbgImg = joyBg.AddComponent<Image>();
        jbgImg.color = new Color(1f, 1f, 1f, 0.25f);
        RectTransform jbgRT = joyBg.GetComponent<RectTransform>();
        jbgRT.anchorMin = new Vector2(0f, 0f);
        jbgRT.anchorMax = new Vector2(0f, 0f);
        jbgRT.pivot = new Vector2(0.5f, 0.5f);
        jbgRT.anchoredPosition = new Vector2(240f, 240f);
        jbgRT.sizeDelta = new Vector2(340f, 340f);

        GameObject joyHnd = new GameObject("JoyHandle");
        joyHnd.transform.SetParent(joyBg.transform, false);
        Image jhImg = joyHnd.AddComponent<Image>();
        jhImg.color = new Color(1f, 0.9f, 0.4f, 0.85f);
        joyHandleRT = joyHnd.GetComponent<RectTransform>();
        joyHandleRT.anchorMin = new Vector2(0.5f, 0.5f);
        joyHandleRT.anchorMax = new Vector2(0.5f, 0.5f);
        joyHandleRT.pivot = new Vector2(0.5f, 0.5f);
        joyHandleRT.anchoredPosition = Vector2.zero;
        joyHandleRT.sizeDelta = new Vector2(140f, 140f);

        // Skill buttons (with sprites!)
        slamBtnImg    = BuildSkillBtn(p, "BtnSlam",    new Vector2(-110f, 140f), new Color(0.7f,0.35f,0.10f), spriteSlam,    out slamIconImg);
        infernoBtnImg = BuildSkillBtn(p, "BtnInferno", new Vector2(-320f, 140f), new Color(0.75f,0.15f,0.10f), spriteInferno, out infernoIconImg);
        aegisBtnImg   = BuildSkillBtn(p, "BtnAegis",   new Vector2(-110f, 360f), new Color(0.15f,0.40f,0.75f), spriteAegis,   out aegisIconImg);

        // Damage flash overlay
        damageFlashImg = MakeImage(p, "DmgFlash", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f,0f,0f,0f), out _);
        RectTransform dfrt = damageFlashImg.rectTransform;
        dfrt.anchorMin = Vector2.zero; dfrt.anchorMax = Vector2.one;
        dfrt.offsetMin = Vector2.zero; dfrt.offsetMax = Vector2.zero;

        // Victory overlay
        victoryOverlayImg = MakeImage(p, "VicOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.1f,0.55f,0.2f,0f), out _);
        RectTransform vrt = victoryOverlayImg.rectTransform;
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
        txtVictory = MakeText(p, "VicText", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1000f,300f), TextAnchor.MiddleCenter, 60, "");
        txtVictory.color = new Color(1f, 0.95f, 0.4f);

        // Defeat overlay
        defeatOverlayImg = MakeImage(p, "DefOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.4f,0.05f,0.05f,0f), out _);
        RectTransform drt = defeatOverlayImg.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        txtDefeat = MakeText(p, "DefText", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1000f,300f), TextAnchor.MiddleCenter, 60, "");
        txtDefeat.color = new Color(1f, 0.4f, 0.4f);

        // Dialog panel (bottom 30%)
        dialogPanelGO = new GameObject("DialogPanel");
        dialogPanelGO.transform.SetParent(p, false);
        dialogPanelImg = dialogPanelGO.AddComponent<Image>();
        dialogPanelImg.color = new Color(0f, 0f, 0f, 0.80f);
        RectTransform dprt = dialogPanelGO.GetComponent<RectTransform>();
        dprt.anchorMin = new Vector2(0f, 0f);
        dprt.anchorMax = new Vector2(1f, 0f);
        dprt.pivot = new Vector2(0.5f, 0f);
        dprt.anchoredPosition = new Vector2(0f, 0f);
        dprt.sizeDelta = new Vector2(0f, 520f);
        dialogSpeakerText = MakeText(dialogPanelGO.transform, "Speaker", new Vector2(0.5f,1f), new Vector2(0f,-40f), new Vector2(1000f,50f), TextAnchor.MiddleCenter, 36, "");
        dialogSpeakerText.color = new Color(1f, 0.95f, 0.5f);
        dialogBodyText = MakeText(dialogPanelGO.transform, "Body", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1000f,300f), TextAnchor.MiddleCenter, 30, "");
        dialogBodyText.color = Color.white;
        dialogContinueHint = MakeImage(dialogPanelGO.transform, "ContHint", new Vector2(1f,0f), new Vector2(1f,0f), new Vector2(-100f,40f), new Vector2(140f,40f), new Color(1f,0.9f,0.4f,0.5f), out _);
        Text contText = MakeText(dialogPanelGO.transform, "ContText", new Vector2(1f,0f), new Vector2(-100f,40f), new Vector2(140f,40f), TextAnchor.MiddleCenter, 22, "TAP");
        contText.color = Color.black;
        dialogPanelGO.SetActive(false);
    }

    Image BuildSkillBtn(Transform parent, string n, Vector2 pos, Color tint, Sprite icon, out Image iconImg) {
        GameObject g = new GameObject(n);
        g.transform.SetParent(parent, false);
        Image bg = g.AddComponent<Image>();
        bg.color = tint;
        RectTransform rt = g.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200f, 200f);

        // Icon child (uses generated sprite)
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(g.transform, false);
        iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(1f, 1f, 1f, 0.95f);
        if (icon != null) iconImg.sprite = icon;
        RectTransform irt = iconGO.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.5f, 0.5f);
        irt.anchorMax = new Vector2(0.5f, 0.5f);
        irt.pivot = new Vector2(0.5f, 0.5f);
        irt.anchoredPosition = Vector2.zero;
        irt.sizeDelta = new Vector2(170f, 170f);

        return bg;
    }

    Image MakeImage(Transform parent, string n, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color c, out GameObject go) {
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

    Text MakeText(Transform parent, string n, Vector2 anchor, Vector2 pos, Vector2 size, TextAnchor align, int fontSize, string content) {
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

    void UpdateHUD(float dt) {
        // HP fill
        if (hpFillImg != null) {
            float r = Mathf.Max(0f, playerHP/playerHPMax);
            hpFillImg.rectTransform.sizeDelta = new Vector2(572f * r, 34f);
        }
        if (mpFillImg != null) {
            float r = Mathf.Max(0f, playerMP/playerMPMax);
            mpFillImg.rectTransform.sizeDelta = new Vector2(572f * r, 24f);
        }
        // Damage flash
        if (damageFlashImg != null) {
            float a = Mathf.Clamp(damageFlashTimer*1.8f, 0f, 0.5f);
            damageFlashImg.color = new Color(1f, 0f, 0f, a);
        }
        // Skill button flash
        if (slamBtnImg != null) {
            float t = Mathf.Clamp01(skillFlashT[0]/0.35f);
            slamBtnImg.color = Color.Lerp(new Color(0.7f,0.35f,0.10f,1f), Color.white, t);
            if (skillCD[0] > 0f && slamIconImg != null) slamIconImg.color = new Color(0.4f,0.4f,0.4f,1f);
            else if (slamIconImg != null) slamIconImg.color = Color.white;
        }
        if (infernoBtnImg != null) {
            float t = Mathf.Clamp01(skillFlashT[1]/0.35f);
            infernoBtnImg.color = Color.Lerp(new Color(0.75f,0.15f,0.10f,1f), Color.white, t);
            if (skillCD[1] > 0f && infernoIconImg != null) infernoIconImg.color = new Color(0.4f,0.4f,0.4f,1f);
            else if (infernoIconImg != null) infernoIconImg.color = Color.white;
        }
        if (aegisBtnImg != null) {
            float t = Mathf.Clamp01(skillFlashT[2]/0.35f);
            Color baseC = Q.unlockedAegis ? new Color(0.15f,0.40f,0.75f,1f) : new Color(0.2f,0.2f,0.2f,0.6f);
            aegisBtnImg.color = Color.Lerp(baseC, Color.white, t);
            if (aegisIconImg != null) {
                if (!Q.unlockedAegis) aegisIconImg.color = new Color(0.3f,0.3f,0.3f,0.5f);
                else if (skillCD[2] > 0f) aegisIconImg.color = new Color(0.4f,0.4f,0.4f,1f);
                else aegisIconImg.color = Color.white;
            }
        }
        // Action text
        if (actionTextT > 0f) {
            actionTextT -= dt;
            if (txtAction != null) {
                txtAction.text = actionText;
                txtAction.color = new Color(1f,0.95f,0.4f, Mathf.Clamp01(actionTextT));
            }
        } else if (txtAction != null) txtAction.text = "";
        // Diag
        if (txtDiag != null)
            txtDiag.text = "HP " + (int)playerHP + "  MP " + (int)playerMP +
                           "  Joy(" + joystickInput.x.ToString("0.0") + "," + joystickInput.y.ToString("0.0") + ")";
    }

    void ShowAction(string s) {
        actionText = s;
        actionTextT = 2.0f;
    }

    void SetZoneName(string s) {
        if (txtZoneName != null) txtZoneName.text = s;
    }

    void UpdateQuestLog() {
        if (txtQuestLog == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>QUEST: The Fallen Hoplite</b>");
        sb.AppendLine((Q.talkedPythia ? "✓ " : "○ ") + "Hear Pythia's prophecy");
        sb.AppendLine((Q.killedScoutsZ1>=2 ? "✓ " : "○ ") + "Slay 2 Scouts ("+Q.killedScoutsZ1+"/2)");
        sb.AppendLine((Q.unlockedZone2 ? "✓ " : "○ ") + "Enter Broken Agora");
        sb.AppendLine((Q.unlockedAegis ? "✓ " : "○ ") + "Receive Aegis from Hermes");
        sb.AppendLine((Q.killedAgoraZ2>=4 ? "✓ " : "○ ") + "Defeat Agora guards ("+Q.killedAgoraZ2+"/4)");
        sb.AppendLine((Q.bossDefeated ? "✓ " : "○ ") + "Defeat the Fallen Hoplite");
        txtQuestLog.text = sb.ToString();
    }

    void ShowVictory() {
        if (victoryOverlayImg != null) victoryOverlayImg.color = new Color(0.1f, 0.55f, 0.2f, 0.65f);
        if (txtVictory != null) {
            txtVictory.text = "AETHER ECHOES\n\nThe hammer awakens.\nThe Titan remembers.\n\nChapter 1 complete.\n\nTap to restart.";
        }
        state = State.Won;
    }

    void ShowDefeat() {
        if (defeatOverlayImg != null) defeatOverlayImg.color = new Color(0.4f, 0.05f, 0.05f, 0.75f);
        if (txtDefeat != null) {
            txtDefeat.text = "DEFEAT\n\nThe gods laugh.\nTap to rise again.";
        }
        state = State.Lost;
    }

    void HideOverlays() {
        if (victoryOverlayImg != null) victoryOverlayImg.color = new Color(0.1f, 0.55f, 0.2f, 0f);
        if (defeatOverlayImg != null)  defeatOverlayImg.color  = new Color(0.4f, 0.05f, 0.05f, 0f);
        if (txtVictory != null) txtVictory.text = "";
        if (txtDefeat != null) txtDefeat.text = "";
    }
    #endregion

    // ============================================================
    #region DialogSystem
    // ============================================================
    void InitDialogScripts() {
        dialogScript = new string[6][];
        dialogScript[(int)DialogId.PythiaIntro] = new string[]{
            "PYTHIA|Eon... the Titan awakens.\nI have waited a thousand years.",
            "PYTHIA|The Hammer calls. You must reach the Altar of Echo.\nFirst, kill the scouts that guard this grove.",
            "PYTHIA|Slam crushes those near you.\nInferno burns those before you.\nThe gods forgot fear. Remind them."
        };
        dialogScript[(int)DialogId.PythiaAfterScouts] = new string[]{
            "PYTHIA|Well struck, Titan.\nA path now opens to the Broken Agora.",
            "PYTHIA|Hermes waits there.\nHe alone among the gods did not betray your kind."
        };
        dialogScript[(int)DialogId.HermesIntro] = new string[]{
            "HERMES|Eon. The grove still echoes with your footsteps.",
            "HERMES|Zeus thinks he betrayed everyone.\nHe was wrong.",
            "HERMES|Take the Aegis. It will shield you from the light of the false gods.\nUse it well at the Altar."
        };
        dialogScript[(int)DialogId.HermesGiveAegis] = new string[]{
            "HERMES|Aegis is yours. Cast it when the Boss telegraphs his fury.",
            "HERMES|Defeat the Agora guards. The path to the Altar lies beyond."
        };
        dialogScript[(int)DialogId.HermesBeforeBoss] = new string[]{
            "HERMES|The Fallen Hoplite waits.\nHe does not remember who he was — Achilles the Elder, first of the demigods.",
            "HERMES|Give him peace, Titan.\nThen the Hammer of Echo will be yours."
        };
    }

    void StartDialog(DialogId id, System.Action onClose) {
        activeDialog = id;
        dialogLine = 0;
        dialogTypeChar = 0;
        dialogTypeT = 0f;
        onDialogClose = onClose;
        if (dialogPanelGO != null) dialogPanelGO.SetActive(true);
        state = State.Dialog;
        RefreshDialogLine();
    }

    void RefreshDialogLine() {
        if (dialogScript == null || (int)activeDialog >= dialogScript.Length) return;
        string[] lines = dialogScript[(int)activeDialog];
        if (lines == null || dialogLine >= lines.Length) {
            CloseDialog();
            return;
        }
        string raw = lines[dialogLine];
        int sep = raw.IndexOf('|');
        string speaker = (sep > 0) ? raw.Substring(0, sep) : "";
        string body = (sep > 0) ? raw.Substring(sep+1) : raw;
        if (dialogSpeakerText != null) dialogSpeakerText.text = speaker;
        if (dialogBodyText != null) dialogBodyText.text = "";
        dialogTypeChar = 0;
        dialogTypeT = 0f;
    }

    void UpdateDialog(float dt) {
        if (activeDialog == DialogId.None) return;
        string[] lines = dialogScript[(int)activeDialog];
        if (lines == null || dialogLine >= lines.Length) { CloseDialog(); return; }
        string raw = lines[dialogLine];
        int sep = raw.IndexOf('|');
        string body = (sep > 0) ? raw.Substring(sep+1) : raw;

        // Typewriter
        if (dialogTypeChar < body.Length) {
            dialogTypeT += dt;
            if (dialogTypeT > 0.025f) {
                dialogTypeT = 0f;
                dialogTypeChar++;
                if (dialogBodyText != null) dialogBodyText.text = body.Substring(0, dialogTypeChar);
            }
        }

        if (tappedDialogContinue && !dialogTouchPrev) {
            // Skip to full text or next line
            if (dialogTypeChar < body.Length) {
                dialogTypeChar = body.Length;
                if (dialogBodyText != null) dialogBodyText.text = body;
            } else {
                dialogLine++;
                if (dialogLine >= lines.Length) CloseDialog();
                else RefreshDialogLine();
            }
        }
        dialogTouchPrev = tappedDialogContinue;
    }

    void CloseDialog() {
        if (dialogPanelGO != null) dialogPanelGO.SetActive(false);
        DialogId prev = activeDialog;
        activeDialog = DialogId.None;
        state = State.InZone;

        // Apply dialog effects
        if (prev == DialogId.PythiaIntro) Q.talkedPythia = true;
        if (prev == DialogId.HermesIntro) Q.talkedHermes = true;
        if (prev == DialogId.HermesIntro && !Q.unlockedAegis) {
            // Auto-trigger HermesGiveAegis after intro
            StartDialog(DialogId.HermesGiveAegis, () => {
                Q.unlockedAegis = true;
                ShowAction("Aegis unlocked!");
                UpdateQuestLog();
                SaveGame();
            });
            return;
        }

        UpdateQuestLog();
        SaveGame();
        if (onDialogClose != null) {
            var cb = onDialogClose;
            onDialogClose = null;
            cb();
        }
    }
    #endregion

    // ============================================================
    #region QuestSystem
    // ============================================================
    void CheckZoneTransitions() {
        if (zoneTrigger == null || player == null) return;
        float d = Vector3.Distance(player.transform.position, zoneTrigger.transform.position);
        if (d < 2f) {
            if (currentZone == Zone.AwakeningGrove) EnterZone(Zone.BrokenAgora);
            else if (currentZone == Zone.BrokenAgora) EnterZone(Zone.AltarArena);
        }
    }

    void CheckNPCInteractions() {
        if (player == null) return;
        if (pythia != null && Vector3.Distance(player.transform.position, pythia.transform.position) < 2.5f) {
            if (Q.killedScoutsZ1 >= 2 && !Q.unlockedZone2 && activeDialog == DialogId.None) {
                StartDialog(DialogId.PythiaAfterScouts, () => {
                    Q.unlockedZone2 = true;
                    SpawnPortalToNextZone(new Vector3(0f, 0.1f, 18f));
                    UpdateQuestLog();
                    SaveGame();
                });
            }
        }
        if (hermes != null && Vector3.Distance(player.transform.position, hermes.transform.position) < 2.5f) {
            if (!Q.talkedHermes && activeDialog == DialogId.None) {
                StartDialog(DialogId.HermesIntro, null);
            }
        }
    }
    #endregion

    // ============================================================
    #region AudioController
    // ============================================================
    void SetupAudio() {
        GameObject bgmGO = new GameObject("BGMSource");
        bgmSource = bgmGO.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.5f;
        bgmSource.spatialBlend = 0f;
        if (clipBGM != null) {
            bgmSource.clip = clipBGM;
            bgmSource.Play();
        }
        GameObject sfxGO = new GameObject("SFXSource");
        sfxSource = sfxGO.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = 0.8f;
        sfxSource.spatialBlend = 0f;
    }

    void PlaySfx(AudioClip c) {
        if (sfxSource != null && c != null) sfxSource.PlayOneShot(c);
    }
    #endregion

    // ============================================================
    #region GameStateMachine - Update
    // ============================================================
    void Update() {
        float dt = Time.deltaTime;
        ReadInput();

        switch (state) {
            case State.Boot:
                state = State.InZone;
                break;

            case State.Dialog:
                UpdateDialog(dt);
                break;

            case State.InZone:
                if (tappedSlam)    TryCastSlam();
                if (tappedInferno) TryCastInferno();
                if (tappedAegis)   TryCastAegis();
                UpdatePlayer(dt);
                UpdateEnemies(dt);
                UpdateBoss(dt);
                UpdateVFX(dt);
                CheckZoneTransitions();
                CheckNPCInteractions();
                break;

            case State.Won:
            case State.Lost:
                UpdateVFX(dt);
                if (tappedDialogContinue || tappedSlam || tappedInferno || tappedAegis) {
                    if (state == State.Won) {
                        ResetSave();
                        HideOverlays();
                        EnterZone(Zone.AwakeningGrove);
                        state = State.InZone;
                    } else {
                        // Defeat: restart current zone, keep flags
                        playerHP = playerHPMax;
                        HideOverlays();
                        EnterZone(currentZone);
                        state = State.InZone;
                    }
                }
                break;
        }

        UpdateHUD(dt);
    }
    #endregion
}
