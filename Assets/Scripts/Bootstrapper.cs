// =============================================================================
// Echoes: Shattered Pantheon — Iteration P (Path C: 2D Hades-style)
// =============================================================================
// FULL 2D REWRITE: orthographic camera + SpriteRenderer + tilemap.
// NO MORE CAPSULES. NO MORE CreatePrimitive(). All visuals = real sprites.
// =============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrapper : MonoBehaviour
{
    // ============================================================
    #region Constants/State
    // ============================================================
    enum Zone { None, AwakeningGrove, BrokenAgora, AltarArena, Victory, Defeat }
    enum State { Boot, InZone, Dialog, Won, Lost }
    enum EnemyType { Scout, Warrior, Archer, Shieldbearer, Priest }

    State state = State.Boot;
    Zone currentZone = Zone.None;

    [System.Serializable]
    public class QuestFlags {
        public bool talkedPythia;
        public int  killedScoutsZ1;
        public bool unlockedZone2;
        public bool talkedHermes;
        public bool unlockedAegis;
        public int  killedAgoraZ2;
        public bool unlockedZone3;
        public bool bossDefeated;
    }
    QuestFlags Q = new QuestFlags();
    #endregion

    // ============================================================
    #region World / Refs
    // ============================================================
    Camera mainCam;
    GameObject worldRoot;
    GameObject player;
    GameObject pythia;
    GameObject hermes;
    GameObject boss;
    GameObject altarHammer;
    GameObject zoneTrigger;
    SpriteRenderer playerSR;

    class EnemyAgent {
        public GameObject go;
        public SpriteRenderer sr;
        public EnemyType type;
        public float hp, hpMax;
        public float dmg;
        public float speed;
        public float range;
        public bool isRanged, isHealer;
        public float rangedCD, rangedTimer;
        public float hitFlashT;
    }
    List<EnemyAgent> enemies = new List<EnemyAgent>();
    List<GameObject> vfxList = new List<GameObject>();
    List<float> vfxTTL = new List<float>();
    List<float> vfxStartScale = new List<float>();
    List<float> vfxEndScale = new List<float>();
    List<float> vfxBornTime = new List<float>();
    List<SpriteRenderer> vfxSR = new List<SpriteRenderer>();

    // Audio
    AudioSource bgmSource, sfxSource;
    AudioClip clipBGM, clipSlam, clipInferno, clipHit;

    // Sprites loaded once
    Sprite spPlayer, spScout, spWarrior, spBoss, spPythia, spHermes;
    Sprite spTileGround, spBgArena;
    Sprite spVfxSlam, spVfxInferno, spVfxAegis, spVfxHit;
    Sprite spIconSlam, spIconInferno, spIconAegis;

    // Player stats
    float playerHP = 500f, playerHPMax = 500f;
    float playerMP = 200f, playerMPMax = 200f;
    float playerSpeed = 7f;
    float aegisActive = 0f;
    float damageFlashTimer = 0f;
    float playerHitFlashT = 0f;
    float playerFacing = 1f; // -1 left, +1 right

    // Boss
    float bossHP = 1500f, bossHPMax = 1500f;
    int bossPhase = 1;
    float bossTelegraph = 0f;
    Vector3 bossTelegraphPos;
    GameObject bossTelegraphGO;
    SpriteRenderer bossTelegraphSR;
    int bossAddsSpawned = 0;
    float bossHitFlashT = 0f;

    // Skills
    float[] skillCD = new float[3];
    float[] skillFlashT = new float[3];
    const float SLAM_RANGE = 4.5f;
    const float INFERNO_RANGE = 7.5f;

    // Input
    int joyTouchId = -1;
    Vector2 joyCenterScreenPx;
    float joyRadiusPx;
    Vector2 joystickInput = Vector2.zero;
    const float JOY_RADIUS_PX_REF = 160f;
    Rect rectSlam, rectInferno, rectAegis;

    // HUD
    Canvas hudCanvas;
    RectTransform joyBgRT, joyHandleRT;
    Image hpFillImg, mpFillImg, damageFlashImg;
    Image victoryOverlayImg, defeatOverlayImg;
    Image slamBtnImg, infernoBtnImg, aegisBtnImg;
    Image slamIconImg, infernoIconImg, aegisIconImg;
    Text txtQuestLog, txtAction, txtDiag, txtZoneName;
    Text txtVictory, txtDefeat;
    GameObject dialogPanelGO;
    Image dialogPanelImg;
    Text dialogSpeakerText, dialogBodyText;

    // Dialog
    enum DialogId { None, PythiaIntro, PythiaAfterScouts, HermesIntro, HermesGiveAegis, HermesBeforeBoss }
    DialogId activeDialog = DialogId.None;
    int dialogLine = 0;
    string[][] dialogScript;
    float dialogTypeT = 0f;
    int dialogTypeChar = 0;
    bool dialogTouchPrev = false;
    System.Action onDialogClose;

    string actionText = "";
    float actionTextT = 0f;

    float waveStartDelay = 1.0f;
    float spawnedWave = 0f;

    bool tappedSlam, tappedInferno, tappedAegis, tappedDialogContinue;

    // Sorting orders
    const int SORT_BG       = -100;
    const int SORT_GROUND   = -90;
    const int SORT_VFX_LOW  = 5;
    const int SORT_NPC      = 10;
    const int SORT_ENEMY    = 20;
    const int SORT_PLAYER   = 30;
    const int SORT_BOSS     = 25;
    const int SORT_VFX_HI   = 50;
    #endregion

    // ============================================================
    #region Lifecycle
    // ============================================================
    void Awake() {
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        LoadAllResources();
        LoadGame();
        BuildCamera2D();
        BuildHUD();
        ComputeButtonRects();
        SetupAudio();
        InitDialogScripts();

        if (currentZone == Zone.None) currentZone = Zone.AwakeningGrove;
        EnterZone(currentZone);
        state = State.InZone;
    }

    void LoadAllResources() {
        spPlayer      = LoadSprite("sprite_player");
        spScout       = LoadSprite("sprite_scout");
        spWarrior     = LoadSprite("sprite_warrior");
        spBoss        = LoadSprite("sprite_boss");
        spPythia      = LoadSprite("sprite_pythia");
        spHermes      = LoadSprite("sprite_hermes");
        spTileGround  = LoadSprite("tile_ground");
        spBgArena     = LoadSprite("bg_arena");
        spVfxSlam     = LoadSprite("vfx_slam");
        spVfxInferno  = LoadSprite("vfx_inferno");
        spVfxAegis    = LoadSprite("vfx_aegis");
        spVfxHit      = LoadSprite("vfx_hit");
        spIconSlam    = LoadSprite("icon_slam");
        spIconInferno = LoadSprite("icon_inferno");
        spIconAegis   = LoadSprite("icon_aegis");

        clipBGM     = Resources.Load<AudioClip>("bgm_olympus");
        clipSlam    = Resources.Load<AudioClip>("sfx_slam");
        clipInferno = Resources.Load<AudioClip>("sfx_inferno");
        clipHit     = Resources.Load<AudioClip>("sfx_hit");
    }

    Sprite LoadSprite(string name) {
        Texture2D t = Resources.Load<Texture2D>(name);
        if (t == null) return null;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }
    #endregion

    // ============================================================
    #region Save/Load
    // ============================================================
    void SaveGame() {
        PlayerPrefs.SetInt("Q_talkedPythia", Q.talkedPythia ? 1 : 0);
        PlayerPrefs.SetInt("Q_killedScoutsZ1", Q.killedScoutsZ1);
        PlayerPrefs.SetInt("Q_unlockedZone2", Q.unlockedZone2 ? 1 : 0);
        PlayerPrefs.SetInt("Q_talkedHermes", Q.talkedHermes ? 1 : 0);
        PlayerPrefs.SetInt("Q_unlockedAegis", Q.unlockedAegis ? 1 : 0);
        PlayerPrefs.SetInt("Q_killedAgoraZ2", Q.killedAgoraZ2);
        PlayerPrefs.SetInt("Q_unlockedZone3", Q.unlockedZone3 ? 1 : 0);
        PlayerPrefs.SetInt("Q_bossDefeated", Q.bossDefeated ? 1 : 0);
        PlayerPrefs.SetInt("Q_currentZone", (int)currentZone);
        PlayerPrefs.Save();
    }

    void LoadGame() {
        Q.talkedPythia = PlayerPrefs.GetInt("Q_talkedPythia", 0) == 1;
        Q.killedScoutsZ1 = PlayerPrefs.GetInt("Q_killedScoutsZ1", 0);
        Q.unlockedZone2 = PlayerPrefs.GetInt("Q_unlockedZone2", 0) == 1;
        Q.talkedHermes = PlayerPrefs.GetInt("Q_talkedHermes", 0) == 1;
        Q.unlockedAegis = PlayerPrefs.GetInt("Q_unlockedAegis", 0) == 1;
        Q.killedAgoraZ2 = PlayerPrefs.GetInt("Q_killedAgoraZ2", 0);
        Q.unlockedZone3 = PlayerPrefs.GetInt("Q_unlockedZone3", 0) == 1;
        Q.bossDefeated = PlayerPrefs.GetInt("Q_bossDefeated", 0) == 1;
        currentZone = (Zone)PlayerPrefs.GetInt("Q_currentZone", (int)Zone.AwakeningGrove);
    }

    void ResetSave() {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Q = new QuestFlags();
        currentZone = Zone.AwakeningGrove;
    }
    #endregion

    // ============================================================
    #region Camera (2D Orthographic)
    // ============================================================
    void BuildCamera2D() {
        GameObject g = new GameObject("MainCamera");
        g.tag = "MainCamera";
        mainCam = g.AddComponent<Camera>();
        mainCam.orthographic = true;
        mainCam.orthographicSize = 8f; // Field of view: ~16 units vertical
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.05f, 0.03f, 0.08f, 1f);
        mainCam.transform.position = new Vector3(0f, 0f, -10f);
        mainCam.transform.rotation = Quaternion.identity;
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane = 100f;
        g.AddComponent<AudioListener>();
    }
    #endregion

    // ============================================================
    #region World Builder
    // ============================================================
    void ClearWorld() {
        if (worldRoot != null) Object.Destroy(worldRoot);
        worldRoot = new GameObject("WorldRoot");
        foreach (var e in enemies) if (e != null && e.go != null) Object.Destroy(e.go);
        enemies.Clear();
        if (boss != null) { Object.Destroy(boss); boss = null; }
        if (pythia != null) { Object.Destroy(pythia); pythia = null; }
        if (hermes != null) { Object.Destroy(hermes); hermes = null; }
        if (altarHammer != null) { Object.Destroy(altarHammer); altarHammer = null; }
        if (zoneTrigger != null) { Object.Destroy(zoneTrigger); zoneTrigger = null; }
        if (bossTelegraphGO != null) { Object.Destroy(bossTelegraphGO); bossTelegraphGO = null; }
        foreach (var v in vfxList) if (v != null) Object.Destroy(v);
        vfxList.Clear();
        vfxTTL.Clear();
        vfxStartScale.Clear();
        vfxEndScale.Clear();
        vfxBornTime.Clear();
        vfxSR.Clear();
        if (player != null) { Object.Destroy(player); player = null; playerSR = null; }
    }

    GameObject MakeSpriteGO(string name, Sprite s, Vector3 pos, float worldSize, int sortOrder, Transform parent = null) {
        GameObject g = new GameObject(name);
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.position = pos;
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = sortOrder;
        // Calculate scale so sprite is `worldSize` units tall
        if (s != null) {
            float pixelHeight = s.rect.height;
            float ppu = s.pixelsPerUnit;
            float spriteWorldH = pixelHeight / ppu;
            float scale = worldSize / Mathf.Max(0.001f, spriteWorldH);
            g.transform.localScale = new Vector3(scale, scale, 1f);
        }
        return g;
    }

    void EnterZone(Zone z) {
        ClearWorld();
        currentZone = z;
        SaveGame();

        // Build ground tilemap (large grid)
        BuildGroundTiles(z);

        // Build player
        player = MakeSpriteGO("Player", spPlayer, new Vector3(0f, -3f, 0f), 1.6f, SORT_PLAYER, worldRoot.transform);
        playerSR = player.GetComponent<SpriteRenderer>();
        playerHP = playerHPMax;
        playerMP = playerMPMax;

        switch (z) {
            case Zone.AwakeningGrove:
                SetZoneName("Awakening Grove");
                pythia = MakeSpriteGO("Pythia", spPythia, new Vector3(0f, 4f, 0f), 1.8f, SORT_NPC, worldRoot.transform);
                SpawnScouts();
                if (!Q.talkedPythia) StartDialog(DialogId.PythiaIntro, null);
                break;

            case Zone.BrokenAgora:
                SetZoneName("Broken Agora");
                hermes = MakeSpriteGO("Hermes", spHermes, new Vector3(5f, 4f, 0f), 1.8f, SORT_NPC, worldRoot.transform);
                SpawnAgoraEnemies();
                if (!Q.talkedHermes) StartDialog(DialogId.HermesIntro, null);
                break;

            case Zone.AltarArena:
                SetZoneName("Altar of Echo");
                // Big arena background centered
                MakeSpriteGO("ArenaBG", spBgArena, new Vector3(0f, 0f, 1f), 20f, SORT_BG, worldRoot.transform);
                altarHammer = MakeSpriteGO("AltarHammer", spVfxSlam, new Vector3(0f, 5f, 0f), 1.5f, SORT_NPC, worldRoot.transform);
                SpawnBoss(new Vector3(0f, 6f, 0f));
                StartDialog(DialogId.HermesBeforeBoss, null);
                break;
        }

        UpdateQuestLog();
    }

    void BuildGroundTiles(Zone z) {
        // Tile a large 40x40 area (16x16 grid of 2.5x2.5 tiles)
        // For AltarArena, ground tiles still visible but covered partially by bg_arena sprite
        int gridSize = 16;
        float tileSize = 3f;
        float half = gridSize * tileSize * 0.5f;
        Color tint = new Color(0.85f, 0.80f, 0.75f, 1f);
        if (z == Zone.BrokenAgora) tint = new Color(0.9f, 0.85f, 0.80f, 1f);
        if (z == Zone.AltarArena) tint = new Color(0.7f, 0.6f, 0.55f, 1f);

        for (int x = 0; x < gridSize; x++) {
            for (int y = 0; y < gridSize; y++) {
                Vector3 p = new Vector3(-half + x * tileSize + tileSize * 0.5f, -half + y * tileSize + tileSize * 0.5f, 0f);
                GameObject g = MakeSpriteGO("Tile_" + x + "_" + y, spTileGround, p, tileSize, SORT_GROUND, worldRoot.transform);
                SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = tint;
            }
        }
    }

    void SpawnScouts() {
        for (int i = 0; i < 2; i++) {
            float ang = i * Mathf.PI;
            Vector3 p = new Vector3(Mathf.Cos(ang) * 5f, 1f + Mathf.Sin(ang) * 2f, 0f);
            SpawnEnemy(EnemyType.Scout, p);
        }
    }

    void SpawnAgoraEnemies() {
        SpawnEnemy(EnemyType.Warrior,      new Vector3(-6f, 2f, 0f));
        SpawnEnemy(EnemyType.Warrior,      new Vector3( 6f, 2f, 0f));
        SpawnEnemy(EnemyType.Archer,       new Vector3( 0f, 7f, 0f));
        SpawnEnemy(EnemyType.Shieldbearer, new Vector3(-4f, 5f, 0f));
        SpawnEnemy(EnemyType.Priest,       new Vector3( 4f, 5f, 0f));
        SpawnEnemy(EnemyType.Scout,        new Vector3( 0f,-2f, 0f));
    }

    void SpawnEnemy(EnemyType t, Vector3 pos) {
        EnemyAgent a = new EnemyAgent();
        a.type = t;
        Sprite spr = spScout;
        float worldSize = 1.4f;
        switch (t) {
            case EnemyType.Scout:        spr = spScout;    worldSize = 1.3f; a.hp=a.hpMax=60f;  a.dmg=8f;  a.speed=2.6f; a.range=1.6f; break;
            case EnemyType.Warrior:      spr = spWarrior;  worldSize = 1.5f; a.hp=a.hpMax=120f; a.dmg=14f; a.speed=2.2f; a.range=1.7f; break;
            case EnemyType.Archer:       spr = spScout;    worldSize = 1.4f; a.hp=a.hpMax=80f;  a.dmg=20f; a.speed=1.8f; a.range=6f; a.isRanged=true; a.rangedCD=2f; break;
            case EnemyType.Shieldbearer: spr = spWarrior;  worldSize = 1.8f; a.hp=a.hpMax=220f; a.dmg=10f; a.speed=1.5f; a.range=1.8f; break;
            case EnemyType.Priest:       spr = spPythia;   worldSize = 1.6f; a.hp=a.hpMax=100f; a.dmg=0f;  a.speed=2.0f; a.range=4f; a.isHealer=true; break;
        }
        a.go = MakeSpriteGO("Enemy_" + t, spr, pos, worldSize, SORT_ENEMY, worldRoot.transform);
        a.sr = a.go.GetComponent<SpriteRenderer>();
        // Tint enemies red except priest
        if (a.sr != null) {
            if (t == EnemyType.Priest) a.sr.color = new Color(0.85f, 0.7f, 1f);
            else if (t == EnemyType.Archer) a.sr.color = new Color(1f, 0.85f, 0.4f);
            else if (t == EnemyType.Shieldbearer) a.sr.color = new Color(0.8f, 0.65f, 0.4f);
            else a.sr.color = new Color(1f, 0.65f, 0.65f);
        }
        enemies.Add(a);
    }

    void SpawnBoss(Vector3 pos) {
        boss = MakeSpriteGO("FallenHoplite", spBoss, pos, 3.2f, SORT_BOSS, worldRoot.transform);
        SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.85f, 0.7f, 1f);
        bossHP = bossHPMax;
        bossPhase = 1;
        bossTelegraph = 0f;
        bossAddsSpawned = 0;
        bossHitFlashT = 0f;
    }

    void SpawnPortalToNextZone(Vector3 pos) {
        zoneTrigger = MakeSpriteGO("ZonePortal", spVfxAegis, pos, 2.5f, SORT_VFX_LOW, worldRoot.transform);
        SpriteRenderer sr = zoneTrigger.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.4f, 0.8f, 1f, 0.85f);
    }
    #endregion

    // ============================================================
    #region Input
    // ============================================================
    void ComputeButtonRects() {
        float sw = Screen.width, sh = Screen.height;
        float refW = 1080f, refH = 1920f;
        float scaleW = sw/refW, scaleH = sh/refH;
        float scale = Mathf.Lerp(scaleW, scaleH, 0.5f);
        joyRadiusPx = JOY_RADIUS_PX_REF * scale;

        float btnSize = 200f * scale;
        float half = btnSize * 0.5f;
        Vector2 cSlam   = new Vector2(sw + (-110f)*scale, (140f)*scale);
        Vector2 cInf    = new Vector2(sw + (-320f)*scale, (140f)*scale);
        Vector2 cAegis  = new Vector2(sw + (-110f)*scale, (360f)*scale);
        rectSlam    = new Rect(cSlam.x - half,  cSlam.y - half,  btnSize, btnSize);
        rectInferno = new Rect(cInf.x - half,   cInf.y - half,   btnSize, btnSize);
        rectAegis   = new Rect(cAegis.x - half, cAegis.y - half, btnSize, btnSize);
        joyCenterScreenPx = new Vector2(240f*scale, 240f*scale);
    }

    void ReadInput() {
        tappedSlam = tappedInferno = tappedAegis = false;
        tappedDialogContinue = false;
        bool anyJoyTouch = false;

        if (Input.touchCount > 0) {
            for (int i = 0; i < Input.touchCount; i++) {
                Touch t = Input.GetTouch(i);
                Vector2 pos = t.position;
                int phase = (int)t.phase;

                if (phase == 0) {
                    if (rectSlam.Contains(pos))    { tappedSlam = true; continue; }
                    if (rectInferno.Contains(pos)) { tappedInferno = true; continue; }
                    if (rectAegis.Contains(pos))   { tappedAegis = true; continue; }
                    if (joyTouchId < 0 && pos.x < Screen.width * 0.5f) {
                        joyTouchId = t.fingerId;
                        UpdateJoyHandle(pos);
                        anyJoyTouch = true;
                    } else {
                        tappedDialogContinue = true;
                    }
                } else if (phase == 1 || phase == 2) {
                    if (t.fingerId == joyTouchId) {
                        UpdateJoyHandle(pos);
                        anyJoyTouch = true;
                    }
                } else if (phase == 3 || phase == 4) {
                    if (t.fingerId == joyTouchId) joyTouchId = -1;
                }
            }
        }
        else if (Input.GetMouseButtonDown(0)) {
            Vector3 mp = Input.mousePosition;
            Vector2 pos = new Vector2(mp.x, mp.y);
            if (rectSlam.Contains(pos))         tappedSlam = true;
            else if (rectInferno.Contains(pos)) tappedInferno = true;
            else if (rectAegis.Contains(pos))   tappedAegis = true;
            else tappedDialogContinue = true;
        }

        // Constraint 39: zero joystick when no touch
        if (!anyJoyTouch && joyTouchId < 0) {
            joystickInput = Vector2.zero;
            if (joyHandleRT != null) joyHandleRT.anchoredPosition = Vector2.zero;
        }
    }

    void UpdateJoyHandle(Vector2 touchScreenPx) {
        Vector2 delta = touchScreenPx - joyCenterScreenPx;
        if (delta.magnitude > joyRadiusPx) delta = delta.normalized * joyRadiusPx;
        float n = delta.magnitude / joyRadiusPx;
        if (n < 0.15f) joystickInput = Vector2.zero;
        else joystickInput = delta / joyRadiusPx;
        if (joyHandleRT != null) joyHandleRT.anchoredPosition = delta;
    }
    #endregion

    // ============================================================
    #region Player
    // ============================================================
    void UpdatePlayer(float dt) {
        if (player == null) return;

        if (joystickInput.sqrMagnitude > 0.02f) {
            Vector3 mv = new Vector3(joystickInput.x, joystickInput.y, 0f) * playerSpeed * dt;
            Vector3 p = player.transform.position + mv;
            p.x = Mathf.Clamp(p.x, -22f, 22f);
            p.y = Mathf.Clamp(p.y, -22f, 22f);
            player.transform.position = p;
            // Flip player based on horizontal velocity
            if (joystickInput.x > 0.1f) playerFacing = 1f;
            else if (joystickInput.x < -0.1f) playerFacing = -1f;
            if (playerSR != null) {
                Vector3 ls = player.transform.localScale;
                ls.x = Mathf.Abs(ls.x) * playerFacing;
                player.transform.localScale = ls;
            }
        }

        playerMP = Mathf.Min(playerMPMax, playerMP + 10f * dt);
        if (aegisActive > 0f) aegisActive -= dt;
        if (damageFlashTimer > 0f) damageFlashTimer -= dt;
        if (playerHitFlashT > 0f) {
            playerHitFlashT -= dt;
            if (playerSR != null) {
                float k = Mathf.Clamp01(playerHitFlashT / 0.18f);
                playerSR.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), k);
            }
        } else if (playerSR != null) playerSR.color = Color.white;

        for (int i = 0; i < 3; i++) {
            skillCD[i] = Mathf.Max(0f, skillCD[i] - dt);
            if (skillFlashT[i] > 0f) skillFlashT[i] -= dt;
        }

        if (damageFlashTimer <= 0f && playerHP < playerHPMax)
            playerHP = Mathf.Min(playerHPMax, playerHP + 2f * dt);

        if (playerHP <= 0f && state != State.Lost) {
            state = State.Lost;
            ShowDefeat();
        }

        // Camera follow player (smooth)
        if (mainCam != null) {
            Vector3 target = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, target, 7f * dt);
        }
    }
    #endregion

    // ============================================================
    #region Skills
    // ============================================================
    void TryCastSlam() {
        if (skillCD[0] > 0f) { ShowAction("Slam: cooldown"); return; }
        if (playerMP < 8f) { ShowAction("Slam: not enough MP"); return; }
        playerMP -= 8f;
        skillCD[0] = 2.5f;
        skillFlashT[0] = 0.35f;
        ShowAction("SLAM!");
        SpawnVFXSprite(spVfxSlam, player.transform.position, 1f, SLAM_RANGE * 2.2f, 0.45f, new Color(1f,0.85f,0.4f,0.95f));
        PlaySfx(clipSlam);

        Vector3 pp = player.transform.position;
        for (int i = enemies.Count - 1; i >= 0; i--) {
            var a = enemies[i];
            if (a == null || a.go == null) { enemies.RemoveAt(i); continue; }
            if (Vector3.Distance(a.go.transform.position, pp) < SLAM_RANGE) {
                DamageEnemy(a, 100f);
            }
        }
        if (boss != null && Vector3.Distance(boss.transform.position, pp) < SLAM_RANGE) {
            DamageBoss(100f);
        }
    }

    void TryCastInferno() {
        if (skillCD[1] > 0f) { ShowAction("Inferno: cooldown"); return; }
        if (playerMP < 14f) { ShowAction("Inferno: not enough MP"); return; }
        playerMP -= 14f;
        skillCD[1] = 4f;
        skillFlashT[1] = 0.35f;
        ShowAction("INFERNO!");
        // Spawn inferno forward of player based on facing
        Vector3 forward = player.transform.position + new Vector3(playerFacing * 2.5f, 0f, 0f);
        SpawnVFXSprite(spVfxInferno, forward, 1f, INFERNO_RANGE * 1.8f, 0.55f, new Color(1f,0.5f,0.25f,0.95f));
        PlaySfx(clipInferno);

        Vector3 pp = player.transform.position;
        for (int i = enemies.Count - 1; i >= 0; i--) {
            var a = enemies[i];
            if (a == null || a.go == null) { enemies.RemoveAt(i); continue; }
            // Cone test: enemy must be in forward direction
            Vector3 dir = a.go.transform.position - pp;
            if (dir.magnitude < INFERNO_RANGE && (dir.x * playerFacing) > -1f) {
                DamageEnemy(a, 160f);
            }
        }
        if (boss != null) {
            Vector3 dir = boss.transform.position - pp;
            if (dir.magnitude < INFERNO_RANGE && (dir.x * playerFacing) > -1f) DamageBoss(160f);
        }
    }

    void TryCastAegis() {
        if (!Q.unlockedAegis) { ShowAction("Aegis: locked (find Hermes)"); return; }
        if (skillCD[2] > 0f) { ShowAction("Aegis: cooldown"); return; }
        if (playerMP < 12f) { ShowAction("Aegis: not enough MP"); return; }
        playerMP -= 12f;
        skillCD[2] = 8f;
        skillFlashT[2] = 0.35f;
        aegisActive = 3f;
        ShowAction("AEGIS!");
        SpawnVFXSprite(spVfxAegis, player.transform.position, 0.8f, 2.5f, 3f, new Color(0.6f,0.85f,1f,0.7f));
    }
    #endregion

    // ============================================================
    #region Enemy AI
    // ============================================================
    void UpdateEnemies(float dt) {
        if (player == null) return;
        Vector3 pp = player.transform.position;

        for (int i = enemies.Count - 1; i >= 0; i--) {
            var a = enemies[i];
            if (a == null || a.go == null) { enemies.RemoveAt(i); continue; }
            Vector3 dir = pp - a.go.transform.position;
            float d = dir.magnitude;

            // Hit flash
            if (a.hitFlashT > 0f) {
                a.hitFlashT -= dt;
                if (a.sr != null) {
                    float k = Mathf.Clamp01(a.hitFlashT / 0.15f);
                    a.sr.color = Color.Lerp(GetEnemyTint(a.type), new Color(1f, 0.4f, 0.4f), k);
                }
            } else if (a.sr != null) a.sr.color = GetEnemyTint(a.type);

            // Face player
            if (a.go.transform.localScale.x != 0f) {
                Vector3 ls = a.go.transform.localScale;
                float sign = dir.x > 0 ? 1f : (dir.x < 0 ? -1f : Mathf.Sign(ls.x));
                ls.x = Mathf.Abs(ls.x) * sign;
                a.go.transform.localScale = ls;
            }

            if (a.isHealer) {
                EnemyAgent heal = null; float minD = 4f;
                foreach (var b in enemies) {
                    if (b == a || b == null || b.go == null) continue;
                    if (b.hp >= b.hpMax) continue;
                    float dd = Vector3.Distance(b.go.transform.position, a.go.transform.position);
                    if (dd < minD) { minD = dd; heal = b; }
                }
                if (heal != null) heal.hp = Mathf.Min(heal.hpMax, heal.hp + 30f * dt);
                if (d < 4f) a.go.transform.position -= dir.normalized * a.speed * dt;
                else if (d > 7f) a.go.transform.position += dir.normalized * a.speed * dt;
            }
            else if (a.isRanged) {
                if (d > a.range) a.go.transform.position += dir.normalized * a.speed * dt;
                else if (d < 5f) a.go.transform.position -= dir.normalized * a.speed * dt;
                a.rangedTimer -= dt;
                if (a.rangedTimer <= 0f && d < a.range + 1f) {
                    a.rangedTimer = a.rangedCD;
                    if (aegisActive <= 0f) {
                        playerHP -= a.dmg;
                        damageFlashTimer = 0.18f;
                        playerHitFlashT = 0.18f;
                        SpawnVFXSprite(spVfxHit, player.transform.position, 0.5f, 1.5f, 0.3f, new Color(1f,0.9f,0.5f,1f));
                        PlaySfx(clipHit);
                    }
                }
            }
            else {
                if (d > a.range) a.go.transform.position += dir.normalized * a.speed * dt;
                else if (aegisActive <= 0f) {
                    playerHP -= a.dmg * dt;
                    damageFlashTimer = 0.15f;
                    if (Random.value < 0.05f) playerHitFlashT = 0.15f;
                }
            }
        }
    }

    Color GetEnemyTint(EnemyType t) {
        switch (t) {
            case EnemyType.Priest: return new Color(0.85f, 0.7f, 1f);
            case EnemyType.Archer: return new Color(1f, 0.85f, 0.4f);
            case EnemyType.Shieldbearer: return new Color(0.8f, 0.65f, 0.4f);
            default: return new Color(1f, 0.65f, 0.65f);
        }
    }

    void DamageEnemy(EnemyAgent a, float dmg) {
        a.hp -= dmg;
        a.hitFlashT = 0.15f;
        SpawnVFXSprite(spVfxHit, a.go.transform.position, 0.5f, 1.6f, 0.3f, new Color(1f,0.9f,0.5f,1f));
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
                    SpawnPortalToNextZone(new Vector3(0f, 8f, 0f));
                    UpdateQuestLog();
                    SaveGame();
                });
            }
        }
        else if (currentZone == Zone.BrokenAgora) {
            Q.killedAgoraZ2 = (int)Mathf.Min(6, Q.killedAgoraZ2 + 1);
            if (Q.killedAgoraZ2 >= 4 && Q.unlockedAegis && !Q.unlockedZone3) {
                Q.unlockedZone3 = true;
                SpawnPortalToNextZone(new Vector3(0f, 9f, 0f));
            }
        }
        UpdateQuestLog();
        SaveGame();
    }
    #endregion

    // ============================================================
    #region Boss
    // ============================================================
    void UpdateBoss(float dt) {
        if (boss == null || player == null) return;
        Vector3 dir = player.transform.position - boss.transform.position;
        float d = dir.magnitude;

        if (bossHitFlashT > 0f) {
            bossHitFlashT -= dt;
            SpriteRenderer sr = boss.GetComponent<SpriteRenderer>();
            if (sr != null) {
                float k = Mathf.Clamp01(bossHitFlashT / 0.2f);
                sr.color = Color.Lerp(new Color(0.85f, 0.7f, 1f), new Color(1f, 0.4f, 0.4f), k);
            }
        }

        float hpPct = bossHP / bossHPMax;
        int newPhase = hpPct > 0.66f ? 1 : (hpPct > 0.33f ? 2 : 3);
        if (newPhase != bossPhase) {
            bossPhase = newPhase;
            ShowAction("BOSS PHASE " + bossPhase + "!");
            if (bossPhase == 3 && bossAddsSpawned == 0) {
                for (int i = 0; i < 3; i++) {
                    float ang = i * Mathf.PI * 2f / 3f;
                    Vector3 p = boss.transform.position + new Vector3(Mathf.Cos(ang)*2.5f, Mathf.Sin(ang)*2.5f, 0f);
                    SpawnEnemy(EnemyType.Scout, p);
                }
                bossAddsSpawned = 3;
            }
        }

        if (bossTelegraph > 0f) {
            bossTelegraph -= dt;
            if (bossTelegraphGO != null) {
                float t = 1f - bossTelegraph/1.5f;
                bossTelegraphGO.transform.localScale = new Vector3(0.5f + t*3f, 0.5f + t*3f, 1f);
            }
            if (bossTelegraph <= 0f) {
                if (Vector3.Distance(player.transform.position, bossTelegraphPos) < 3.5f && aegisActive <= 0f) {
                    playerHP -= 80f;
                    damageFlashTimer = 0.4f;
                    playerHitFlashT = 0.3f;
                }
                if (bossTelegraphGO != null) { Object.Destroy(bossTelegraphGO); bossTelegraphGO = null; }
                SpawnVFXSprite(spVfxSlam, bossTelegraphPos, 1f, 4f, 0.5f, new Color(0.7f, 0.3f, 1f, 0.85f));
            }
        } else {
            if (d > 2.5f) boss.transform.position += dir.normalized * 2.5f * dt;
            else if (aegisActive <= 0f) {
                playerHP -= 25f * dt;
                damageFlashTimer = 0.15f;
            }
            if (bossPhase >= 2 && Random.value < 0.006f) {
                bossTelegraph = 1.5f;
                bossTelegraphPos = player.transform.position;
                bossTelegraphGO = MakeSpriteGO("BossTel", spVfxSlam, bossTelegraphPos, 1.5f, SORT_VFX_LOW, worldRoot.transform);
                SpriteRenderer sr = bossTelegraphGO.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.7f, 0.2f, 0.9f, 0.5f);
            }
        }
    }

    void DamageBoss(float dmg) {
        bossHP -= dmg;
        bossHitFlashT = 0.2f;
        SpawnVFXSprite(spVfxHit, boss.transform.position, 0.7f, 2f, 0.3f, new Color(1f,0.9f,0.5f,1f));
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
    #region VFX
    // ============================================================
    void SpawnVFXSprite(Sprite s, Vector3 pos, float startSize, float endSize, float ttl, Color tint) {
        if (s == null) return;
        GameObject g = MakeSpriteGO("VFX", s, pos, startSize, SORT_VFX_HI, worldRoot != null ? worldRoot.transform : null);
        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = tint;
        vfxList.Add(g);
        vfxTTL.Add(ttl);
        vfxStartScale.Add(startSize);
        vfxEndScale.Add(endSize);
        vfxBornTime.Add(Time.time);
        vfxSR.Add(sr);
    }

    void UpdateVFX(float dt) {
        for (int i = vfxList.Count - 1; i >= 0; i--) {
            vfxTTL[i] -= dt;
            if (vfxTTL[i] <= 0f || vfxList[i] == null) {
                if (vfxList[i] != null) Object.Destroy(vfxList[i]);
                vfxList.RemoveAt(i);
                vfxTTL.RemoveAt(i);
                vfxStartScale.RemoveAt(i);
                vfxEndScale.RemoveAt(i);
                vfxBornTime.RemoveAt(i);
                vfxSR.RemoveAt(i);
            } else {
                float age = Time.time - vfxBornTime[i];
                float total = age + vfxTTL[i];
                float k = total > 0 ? Mathf.Clamp01(age / total) : 0f;
                float sz = Mathf.Lerp(vfxStartScale[i], vfxEndScale[i], k);
                if (vfxList[i] != null) {
                    // Rescale (sprites have unit size 1f by default after MakeSpriteGO)
                    GameObject g = vfxList[i];
                    Sprite s = vfxSR[i] != null ? vfxSR[i].sprite : null;
                    if (s != null) {
                        float spriteWorldH = s.rect.height / s.pixelsPerUnit;
                        float scale = sz / Mathf.Max(0.001f, spriteWorldH);
                        g.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                    if (vfxSR[i] != null) {
                        Color c = vfxSR[i].color;
                        c.a = Mathf.Lerp(1f, 0f, k);
                        vfxSR[i].color = c;
                    }
                }
            }
        }
    }
    #endregion

    // ============================================================
    #region HUD
    // ============================================================
    void BuildHUD() {
        GameObject canvasGO = new GameObject("HUDCanvas");
        hudCanvas = canvasGO.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform p = canvasGO.transform;

        txtZoneName = MakeText(p, "ZoneName", new Vector2(0.5f,1f), new Vector2(0f,-30f), new Vector2(900f,50f), TextAnchor.MiddleCenter, 36, "Awakening Grove");
        txtZoneName.color = new Color(1f, 0.9f, 0.6f);

        MakeImage(p, "HPbg", new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(40f,-90f), new Vector2(580f,42f), new Color(0.10f,0.04f,0.04f,0.85f), out _);
        hpFillImg = MakeImage(p, "HPfill", new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(44f,-94f), new Vector2(572f,34f), new Color(0.85f, 0.12f, 0.12f), out _);

        MakeImage(p, "MPbg", new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(40f,-140f), new Vector2(580f,32f), new Color(0.05f,0.05f,0.18f,0.85f), out _);
        mpFillImg = MakeImage(p, "MPfill", new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(44f,-144f), new Vector2(572f,24f), new Color(0.2f, 0.45f, 0.95f), out _);

        txtDiag = MakeText(p, "Diag", new Vector2(1f,1f), new Vector2(-30f,-30f), new Vector2(420f,60f), TextAnchor.UpperRight, 20, "v2.0.0");
        txtDiag.color = new Color(0.7f,0.7f,0.7f);

        MakeImage(p, "QuestBG", new Vector2(0f,1f), new Vector2(0f,1f), new Vector2(20f,-200f), new Vector2(620f,200f), new Color(0f,0f,0f,0.55f), out _);
        txtQuestLog = MakeText(p, "QuestLog", new Vector2(0f,1f), new Vector2(30f,-210f), new Vector2(600f,190f), TextAnchor.UpperLeft, 22, "");
        txtQuestLog.color = new Color(1f, 0.95f, 0.7f);

        txtAction = MakeText(p, "Action", new Vector2(0.5f,1f), new Vector2(0f,-440f), new Vector2(900f,60f), TextAnchor.MiddleCenter, 40, "");
        txtAction.color = new Color(1f, 0.95f, 0.4f);

        // Joystick
        GameObject joyBg = new GameObject("JoyBG");
        joyBg.transform.SetParent(p, false);
        Image jbgImg = joyBg.AddComponent<Image>();
        jbgImg.color = new Color(1f, 1f, 1f, 0.25f);
        joyBgRT = joyBg.GetComponent<RectTransform>();
        joyBgRT.anchorMin = new Vector2(0f, 0f);
        joyBgRT.anchorMax = new Vector2(0f, 0f);
        joyBgRT.pivot = new Vector2(0.5f, 0.5f);
        joyBgRT.anchoredPosition = new Vector2(240f, 240f);
        joyBgRT.sizeDelta = new Vector2(340f, 340f);

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

        slamBtnImg    = BuildSkillBtn(p, "BtnSlam",    new Vector2(-110f, 140f), new Color(0.7f,0.35f,0.10f), spIconSlam,    out slamIconImg);
        infernoBtnImg = BuildSkillBtn(p, "BtnInferno", new Vector2(-320f, 140f), new Color(0.75f,0.15f,0.10f), spIconInferno, out infernoIconImg);
        aegisBtnImg   = BuildSkillBtn(p, "BtnAegis",   new Vector2(-110f, 360f), new Color(0.15f,0.40f,0.75f), spIconAegis,   out aegisIconImg);

        damageFlashImg = MakeImage(p, "DmgFlash", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f,0f,0f,0f), out _);
        RectTransform dfrt = damageFlashImg.rectTransform;
        dfrt.anchorMin = Vector2.zero; dfrt.anchorMax = Vector2.one;
        dfrt.offsetMin = Vector2.zero; dfrt.offsetMax = Vector2.zero;

        victoryOverlayImg = MakeImage(p, "VicOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.1f,0.55f,0.2f,0f), out _);
        RectTransform vrt = victoryOverlayImg.rectTransform;
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
        txtVictory = MakeText(p, "VicText", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1000f,300f), TextAnchor.MiddleCenter, 60, "");
        txtVictory.color = new Color(1f, 0.95f, 0.4f);

        defeatOverlayImg = MakeImage(p, "DefOverlay", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.4f,0.05f,0.05f,0f), out _);
        RectTransform drt = defeatOverlayImg.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        txtDefeat = MakeText(p, "DefText", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(1000f,300f), TextAnchor.MiddleCenter, 60, "");
        txtDefeat.color = new Color(1f, 0.4f, 0.4f);

        dialogPanelGO = new GameObject("DialogPanel");
        dialogPanelGO.transform.SetParent(p, false);
        dialogPanelImg = dialogPanelGO.AddComponent<Image>();
        dialogPanelImg.color = new Color(0f, 0f, 0f, 0.85f);
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
        if (hpFillImg != null) {
            float r = Mathf.Max(0f, playerHP/playerHPMax);
            hpFillImg.rectTransform.sizeDelta = new Vector2(572f * r, 34f);
        }
        if (mpFillImg != null) {
            float r = Mathf.Max(0f, playerMP/playerMPMax);
            mpFillImg.rectTransform.sizeDelta = new Vector2(572f * r, 24f);
        }
        if (damageFlashImg != null) {
            float a = Mathf.Clamp(damageFlashTimer*1.8f, 0f, 0.5f);
            damageFlashImg.color = new Color(1f, 0f, 0f, a);
        }
        if (slamBtnImg != null) {
            float t = Mathf.Clamp01(skillFlashT[0]/0.35f);
            slamBtnImg.color = Color.Lerp(new Color(0.7f,0.35f,0.10f,1f), Color.white, t);
            if (slamIconImg != null) slamIconImg.color = skillCD[0]>0f ? new Color(0.4f,0.4f,0.4f,1f) : Color.white;
        }
        if (infernoBtnImg != null) {
            float t = Mathf.Clamp01(skillFlashT[1]/0.35f);
            infernoBtnImg.color = Color.Lerp(new Color(0.75f,0.15f,0.10f,1f), Color.white, t);
            if (infernoIconImg != null) infernoIconImg.color = skillCD[1]>0f ? new Color(0.4f,0.4f,0.4f,1f) : Color.white;
        }
        if (aegisBtnImg != null) {
            float t = Mathf.Clamp01(skillFlashT[2]/0.35f);
            Color baseC = Q.unlockedAegis ? new Color(0.15f,0.40f,0.75f,1f) : new Color(0.2f,0.2f,0.2f,0.6f);
            aegisBtnImg.color = Color.Lerp(baseC, Color.white, t);
            if (aegisIconImg != null) {
                if (!Q.unlockedAegis) aegisIconImg.color = new Color(0.3f,0.3f,0.3f,0.5f);
                else aegisIconImg.color = skillCD[2]>0f ? new Color(0.4f,0.4f,0.4f,1f) : Color.white;
            }
        }
        if (actionTextT > 0f) {
            actionTextT -= dt;
            if (txtAction != null) {
                txtAction.text = actionText;
                txtAction.color = new Color(1f,0.95f,0.4f, Mathf.Clamp01(actionTextT));
            }
        } else if (txtAction != null) txtAction.text = "";

        if (txtDiag != null)
            txtDiag.text = "HP " + (int)playerHP + "  MP " + (int)playerMP;
    }

    void ShowAction(string s) { actionText = s; actionTextT = 2.0f; }
    void SetZoneName(string s) { if (txtZoneName != null) txtZoneName.text = s; }

    void UpdateQuestLog() {
        if (txtQuestLog == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>QUEST: The Fallen Hoplite</b>");
        sb.AppendLine((Q.talkedPythia ? "[x] " : "[ ] ") + "Hear Pythia's prophecy");
        sb.AppendLine((Q.killedScoutsZ1>=2 ? "[x] " : "[ ] ") + "Slay 2 Scouts ("+Q.killedScoutsZ1+"/2)");
        sb.AppendLine((Q.unlockedZone2 ? "[x] " : "[ ] ") + "Enter Broken Agora");
        sb.AppendLine((Q.unlockedAegis ? "[x] " : "[ ] ") + "Receive Aegis from Hermes");
        sb.AppendLine((Q.killedAgoraZ2>=4 ? "[x] " : "[ ] ") + "Defeat Agora guards ("+Q.killedAgoraZ2+"/4)");
        sb.AppendLine((Q.bossDefeated ? "[x] " : "[ ] ") + "Defeat the Fallen Hoplite");
        txtQuestLog.text = sb.ToString();
    }

    void ShowVictory() {
        if (victoryOverlayImg != null) victoryOverlayImg.color = new Color(0.1f, 0.55f, 0.2f, 0.65f);
        if (txtVictory != null) txtVictory.text = "AETHER ECHOES\n\nThe hammer awakens.\nThe Titan remembers.\n\nChapter 1 complete.\n\nTap to restart.";
        state = State.Won;
    }

    void ShowDefeat() {
        if (defeatOverlayImg != null) defeatOverlayImg.color = new Color(0.4f, 0.05f, 0.05f, 0.75f);
        if (txtDefeat != null) txtDefeat.text = "DEFEAT\n\nThe gods laugh.\nTap to rise again.";
        state = State.Lost;
    }

    void HideOverlays() {
        if (victoryOverlayImg != null) victoryOverlayImg.color = new Color(0.1f, 0.55f, 0.2f, 0f);
        if (defeatOverlayImg != null) defeatOverlayImg.color = new Color(0.4f, 0.05f, 0.05f, 0f);
        if (txtVictory != null) txtVictory.text = "";
        if (txtDefeat != null) txtDefeat.text = "";
    }
    #endregion

    // ============================================================
    #region Dialog
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
            "HERMES|Take the Aegis. It will shield you from the light of the false gods."
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
        if (lines == null || dialogLine >= lines.Length) { CloseDialog(); return; }
        string raw = lines[dialogLine];
        int sep = raw.IndexOf('|');
        string speaker = (sep > 0) ? raw.Substring(0, sep) : "";
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

        if (dialogTypeChar < body.Length) {
            dialogTypeT += dt;
            if (dialogTypeT > 0.025f) {
                dialogTypeT = 0f;
                dialogTypeChar++;
                if (dialogBodyText != null) dialogBodyText.text = body.Substring(0, dialogTypeChar);
            }
        }

        if (tappedDialogContinue && !dialogTouchPrev) {
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

        if (prev == DialogId.PythiaIntro) Q.talkedPythia = true;
        if (prev == DialogId.HermesIntro) Q.talkedHermes = true;
        if (prev == DialogId.HermesIntro && !Q.unlockedAegis) {
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
    #region Quest / Transitions
    // ============================================================
    void CheckZoneTransitions() {
        if (zoneTrigger == null || player == null) return;
        if (Vector3.Distance(player.transform.position, zoneTrigger.transform.position) < 1.5f) {
            if (currentZone == Zone.AwakeningGrove) EnterZone(Zone.BrokenAgora);
            else if (currentZone == Zone.BrokenAgora) EnterZone(Zone.AltarArena);
        }
    }

    void CheckNPCInteractions() {
        if (player == null) return;
        if (pythia != null && Vector3.Distance(player.transform.position, pythia.transform.position) < 2f) {
            if (Q.killedScoutsZ1 >= 2 && !Q.unlockedZone2 && activeDialog == DialogId.None) {
                StartDialog(DialogId.PythiaAfterScouts, () => {
                    Q.unlockedZone2 = true;
                    SpawnPortalToNextZone(new Vector3(0f, 8f, 0f));
                    UpdateQuestLog();
                    SaveGame();
                });
            }
        }
        if (hermes != null && Vector3.Distance(player.transform.position, hermes.transform.position) < 2f) {
            if (!Q.talkedHermes && activeDialog == DialogId.None)
                StartDialog(DialogId.HermesIntro, null);
        }
    }
    #endregion

    // ============================================================
    #region Audio
    // ============================================================
    void SetupAudio() {
        GameObject bgmGO = new GameObject("BGMSource");
        bgmSource = bgmGO.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.5f;
        bgmSource.spatialBlend = 0f;
        if (clipBGM != null) { bgmSource.clip = clipBGM; bgmSource.Play(); }
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
    #region Game Loop
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
