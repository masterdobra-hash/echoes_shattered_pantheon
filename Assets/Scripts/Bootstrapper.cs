// Echoes: Shattered Pantheon - Episode 1: Fall of Olympus (VN, Iteration R)
// Path G (Visual Novel). UX polish per constraints 58-63:
//   58) Title menu bar at bottom, never overlaps portrait
//   59) Settings panel: Music toggle, SFX toggle, Language toggle
//   60) Total language switch via L() dictionary — no mixed-language strings
//   61) Smooth crossfade transitions between backgrounds
//   62) SKIP button bottom-right while typing
//   63) NEXT button bottom-right when typing finished
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrapper : MonoBehaviour
{
    // ============ ENUMS / STATE ============
    enum State { Title, Settings, Playing, Choice, Ended }
    enum Lang { EN, RU }

    class Line {
        public string speaker; public string bg; public string bgm; public string sfx;
        public string en; public string ru; public Choice[] choices;
    }
    class Choice { public string en; public string ru; }

    // ============ FIELDS ============
    Lang lang = Lang.EN;
    State state = State.Title;
    int idx = 0;
    List<Line> script;
    bool musicOn = true, sfxOn = true;

    Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    // UI roots
    Canvas canvas;
    Image bgImage;           // current background (visible)
    Image bgImageNext;       // next background during crossfade
    Image portraitImage;
    Image dialogBox, fadeOverlay;
    Image titleArt;          // title screen art layer
    Text dialogText, speakerText;
    Text titleText, subtitleText;
    Text skipBtnText, nextBtnText;
    Text musicBtnText, sfxBtnText, langBtnText, backBtnText, settingsBtnText, startBtnText, contBtnText, restartBtnText, endTitleText;
    GameObject titlePanel, settingsPanel, endPanel, choicePanel;
    GameObject menuBar; // bottom strip in title
    GameObject skipBtnGO, nextBtnGO;
    List<Button> choiceButtons = new List<Button>();

    AudioSource bgmSrc, sfxSrc;
    string currentBgm = "";
    string currentBgKey = "";

    // Typewriter
    string fullText = "";
    int typeIdx = 0;
    float typeTimer = 0f;
    const float TYPE_SPEED = 0.025f;
    bool typing = false;

    // Fade overlay (boot/exit)
    float fadeAlpha = 1f;
    int fadeDir = -1; // -1 in, +1 out, 0 idle

    // Crossfade between backgrounds
    bool crossfading = false;
    float crossTime = 0f;
    const float CROSS_DUR = 1.2f;
    string pendingBg = "";

    // NEXT button pulse
    float pulseT = 0f;

    // ============ ENTRY ============
    void Start()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
        QualitySettings.vSyncCount = 0;
        LoadAllSprites();
        LoadAllAudio();
        LoadProgress();
        BuildUI();
        BuildScript();
        ApplyAudioMutes();
        ShowTitle();
    }

    // ============ LOC DICTIONARY ============
    string L(string key)
    {
        bool en = (lang == Lang.EN);
        switch (key)
        {
            case "NEW_STORY":    return en ? "NEW STORY"     : "НОВАЯ ИСТОРИЯ";
            case "CONTINUE":     return en ? "CONTINUE"      : "ПРОДОЛЖИТЬ";
            case "SETTINGS":     return en ? "SETTINGS"      : "НАСТРОЙКИ";
            case "BACK":         return en ? "◀ BACK"        : "◀ НАЗАД";
            case "RESTART":      return en ? "RESTART"       : "ЗАНОВО";
            case "MUSIC":        return en ? "MUSIC"         : "МУЗЫКА";
            case "SFX":          return en ? "SFX"           : "ЗВУКИ";
            case "LANG":         return en ? "LANGUAGE"      : "ЯЗЫК";
            case "ON":           return en ? "ON"            : "ВКЛ";
            case "OFF":          return en ? "OFF"           : "ВЫКЛ";
            case "LANG_VAL":     return en ? "ENGLISH"       : "РУССКИЙ";
            case "SKIP":         return en ? "▶▶ SKIP"       : "▶▶ ПРОПУСТИТЬ";
            case "NEXT":         return en ? "NEXT ▶"        : "ДАЛЕЕ ▶";
            case "TITLE1":       return en ? "ECHOES"        : "ЭХО";
            case "TITLE2":       return en ? "SHATTERED PANTHEON" : "РАЗБИТЫЙ ПАНТЕОН";
            case "SUBTITLE":     return en ? "Episode 1: Fall of Olympus" : "Эпизод 1: Падение Олимпа";
            case "END_TITLE":    return en ? "TO BE CONTINUED" : "ПРОДОЛЖЕНИЕ СЛЕДУЕТ";
            case "SPEAKER_EON":     return en ? "Eon" : "Эон";
            case "SPEAKER_PYTHIA":  return en ? "Pythia" : "Пифия";
            case "SPEAKER_HERMES":  return en ? "Hermes" : "Гермес";
            case "SPEAKER_FALLEN":  return en ? "Fallen Hoplite" : "Падший Гоплит";
            default: return key;
        }
    }

    // ============ ASSETS ============
    void LoadAllSprites()
    {
        string[] names = new string[]{
            "portrait_eon","portrait_pythia","portrait_hermes","portrait_fallen",
            "bg_sarcophagus","bg_grove","bg_agora","bg_storm","bg_altar","bg_title"
        };
        foreach (var n in names)
        {
            var tex = Resources.Load<Texture2D>(n);
            if (tex == null) { Debug.LogWarning("Missing sprite: " + n); continue; }
            sprites[n] = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), 100f);
        }
    }
    void LoadAllAudio()
    {
        string[] names = new string[]{ "bgm_olympus","bgm_tense","sfx_blip","sfx_choice","sfx_transition" };
        foreach (var n in names)
        {
            var c = Resources.Load<AudioClip>(n);
            if (c == null) { Debug.LogWarning("Missing audio: " + n); continue; }
            clips[n] = c;
        }
    }

    // ============ UI BUILD ============
    void BuildUI()
    {
        var canvasGO = new GameObject("Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080,1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Background (visible) + Background (next, for crossfade)
        bgImage = MakeImage(canvas.transform, "BG", Color.black);
        Stretch(bgImage.rectTransform);
        bgImageNext = MakeImage(canvas.transform, "BGNext", new Color(1,1,1,0));
        Stretch(bgImageNext.rectTransform);

        // Portrait (center-right, large)
        portraitImage = MakeImage(canvas.transform, "Portrait", new Color(1,1,1,0));
        var pr = portraitImage.rectTransform;
        pr.anchorMin = new Vector2(0.5f,0.5f); pr.anchorMax = new Vector2(0.5f,0.5f);
        pr.anchoredPosition = new Vector2(0, 80);
        pr.sizeDelta = new Vector2(720, 1100);
        portraitImage.preserveAspect = true;

        // Dialog box
        dialogBox = MakeImage(canvas.transform, "DialogBox", new Color(0.05f,0.04f,0.08f,0.88f));
        var db = dialogBox.rectTransform;
        db.anchorMin = new Vector2(0,0); db.anchorMax = new Vector2(1,0);
        db.pivot = new Vector2(0.5f,0); db.anchoredPosition = new Vector2(0,40);
        db.sizeDelta = new Vector2(-80, 520);
        AddOutline(dialogBox.gameObject, new Color(0.9f,0.75f,0.3f,1f), 3);

        speakerText = MakeText(dialogBox.transform, "Speaker", "", 42, new Color(1f,0.85f,0.4f,1f));
        var st = speakerText.rectTransform;
        st.anchorMin = new Vector2(0,1); st.anchorMax = new Vector2(1,1);
        st.pivot = new Vector2(0,1); st.anchoredPosition = new Vector2(40,-25);
        st.sizeDelta = new Vector2(-80, 60);
        speakerText.fontStyle = FontStyle.Bold;

        dialogText = MakeText(dialogBox.transform, "Dialog", "", 36, new Color(0.95f,0.95f,0.92f,1f));
        var dt = dialogText.rectTransform;
        dt.anchorMin = new Vector2(0,0); dt.anchorMax = new Vector2(1,1);
        dt.offsetMin = new Vector2(40, 110); dt.offsetMax = new Vector2(-40, -90);
        dialogText.alignment = TextAnchor.UpperLeft;

        // Tap area covers whole screen for advance
        var tapGO = new GameObject("TapArea");
        tapGO.transform.SetParent(canvas.transform, false);
        var tapImg = tapGO.AddComponent<Image>();
        tapImg.color = new Color(0,0,0,0); tapImg.raycastTarget = true;
        Stretch(tapImg.rectTransform);
        var tapBtn = tapGO.AddComponent<Button>();
        tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(OnTap);

        dialogBox.transform.SetAsLastSibling();

        // SKIP button (bottom-right, while typing)
        skipBtnGO = MakeBottomRightButton(canvas.transform, "SkipBtn", "▶▶ SKIP", new Vector2(-30,610), OnSkip, out skipBtnText, new Color(0.9f,0.6f,0.2f,1f));
        skipBtnGO.SetActive(false);

        // NEXT button (bottom-right, when typing finished)
        nextBtnGO = MakeBottomRightButton(canvas.transform, "NextBtn", "NEXT ▶", new Vector2(-30,610), OnNext, out nextBtnText, new Color(0.4f,0.85f,0.5f,1f));
        nextBtnGO.SetActive(false);

        // === Title Panel ===
        titlePanel = new GameObject("TitlePanel");
        titlePanel.transform.SetParent(canvas.transform, false);
        var tpRT = titlePanel.AddComponent<RectTransform>();
        Stretch(tpRT);

        // Title art (uses bg_title) - full screen
        titleArt = MakeImage(titlePanel.transform, "TitleArt", Color.white);
        Stretch(titleArt.rectTransform);

        // Title text - upper portion, doesn't cover central art
        titleText = MakeText(titlePanel.transform, "Title", "", 110, new Color(1f,0.85f,0.4f,1f));
        var tt = titleText.rectTransform;
        tt.anchorMin = new Vector2(0,1); tt.anchorMax = new Vector2(1,1);
        tt.pivot = new Vector2(0.5f,1); tt.anchoredPosition = new Vector2(0,-180);
        tt.sizeDelta = new Vector2(-60, 220);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        AddOutline(titleText.gameObject, new Color(0,0,0,0.95f), 4);

        subtitleText = MakeText(titlePanel.transform, "Subtitle", "", 48, new Color(0.9f,0.85f,0.7f,1f));
        var sub = subtitleText.rectTransform;
        sub.anchorMin = new Vector2(0,1); sub.anchorMax = new Vector2(1,1);
        sub.pivot = new Vector2(0.5f,1); sub.anchoredPosition = new Vector2(0,-440);
        sub.sizeDelta = new Vector2(-60, 80);
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.fontStyle = FontStyle.Bold;
        AddOutline(subtitleText.gameObject, new Color(0,0,0,0.9f), 3);

        // Bottom menu bar (constraint 58: doesn't overlap portrait)
        menuBar = new GameObject("MenuBar");
        menuBar.transform.SetParent(titlePanel.transform, false);
        var mbImg = menuBar.AddComponent<Image>();
        mbImg.color = new Color(0.05f,0.04f,0.08f,0.85f);
        mbImg.raycastTarget = true;
        var mbRT = mbImg.rectTransform;
        mbRT.anchorMin = new Vector2(0,0); mbRT.anchorMax = new Vector2(1,0);
        mbRT.pivot = new Vector2(0.5f,0); mbRT.anchoredPosition = new Vector2(0,0);
        mbRT.sizeDelta = new Vector2(0, 560);
        AddOutline(menuBar, new Color(0.9f,0.75f,0.3f,1f), 3);

        // Three buttons inside menuBar
        var startBtn = MakeFlatButton(menuBar.transform, "StartBtn", new Vector2(0, 180), new Vector2(820,130), OnStartNew, out startBtnText);
        var contBtn  = MakeFlatButton(menuBar.transform, "ContBtn",  new Vector2(0, 30),  new Vector2(820,130), OnContinue, out contBtnText);
        var setBtn   = MakeFlatButton(menuBar.transform, "SetBtn",   new Vector2(0,-120), new Vector2(820,130), OnOpenSettings, out settingsBtnText);

        // === Settings Panel ===
        settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(canvas.transform, false);
        var spRT = settingsPanel.AddComponent<RectTransform>();
        Stretch(spRT);
        var spBg = MakeImage(settingsPanel.transform, "SpBG", new Color(0.02f,0.02f,0.05f,0.96f));
        Stretch(spBg.rectTransform);
        var spTitle = MakeText(settingsPanel.transform, "SetTitle", "", 80, new Color(1f,0.85f,0.4f,1f));
        var spTRT = spTitle.rectTransform;
        spTRT.anchorMin = new Vector2(0,1); spTRT.anchorMax = new Vector2(1,1);
        spTRT.pivot = new Vector2(0.5f,1); spTRT.anchoredPosition = new Vector2(0,-200);
        spTRT.sizeDelta = new Vector2(-60, 130);
        spTitle.alignment = TextAnchor.MiddleCenter;
        spTitle.fontStyle = FontStyle.Bold;
        // music, sfx, lang toggles
        MakeFlatButton(settingsPanel.transform, "MusicBtn",  new Vector2(0,  340), new Vector2(840,140), OnToggleMusic, out musicBtnText);
        MakeFlatButton(settingsPanel.transform, "SfxBtn",    new Vector2(0,  170), new Vector2(840,140), OnToggleSfx,   out sfxBtnText);
        MakeFlatButton(settingsPanel.transform, "LangBtn",   new Vector2(0,    0), new Vector2(840,140), OnToggleLang,  out langBtnText);
        MakeFlatButton(settingsPanel.transform, "BackBtn",   new Vector2(0, -340), new Vector2(640,140), OnBackFromSettings, out backBtnText);
        // store the title ref via a captured local: replace SetTitle each refresh in RefreshLocalizedUI via tag search-not-needed; we re-render whole UI on lang switch.
        // Trick: store on a dedicated field for re-render. Use settingsTitleText:
        settingsTitleText = spTitle;
        settingsPanel.SetActive(false);

        // === Choice Panel ===
        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(canvas.transform, false);
        var cpRT = choicePanel.AddComponent<RectTransform>();
        Stretch(cpRT);
        var cpBg = MakeImage(choicePanel.transform, "CpBG", new Color(0,0,0,0.55f));
        Stretch(cpBg.rectTransform);
        for (int i=0;i<3;i++)
        {
            int captured = i;
            Text cbText;
            var bgo = MakeFlatButton(choicePanel.transform, "Choice"+i, new Vector2(0, 220 - i*200), new Vector2(900,170), ()=>OnChoicePicked(captured), out cbText);
            // store button refs for later (find by name on update)
            var btnComp = bgo.GetComponent<Button>();
            choiceButtons.Add(btnComp);
            choiceTexts.Add(cbText);
        }
        choicePanel.SetActive(false);

        // === End Panel ===
        endPanel = new GameObject("EndPanel");
        endPanel.transform.SetParent(canvas.transform, false);
        var epRT = endPanel.AddComponent<RectTransform>();
        Stretch(epRT);
        var epBg = MakeImage(endPanel.transform, "EpBG", new Color(0,0,0,0.92f));
        Stretch(epBg.rectTransform);
        endTitleText = MakeText(endPanel.transform, "EndTitle", "", 80, new Color(1f,0.85f,0.4f,1f));
        var etRT = endTitleText.rectTransform;
        etRT.anchorMin = new Vector2(0.5f,0.5f); etRT.anchorMax = new Vector2(0.5f,0.5f);
        etRT.anchoredPosition = new Vector2(0, 240); etRT.sizeDelta = new Vector2(1000, 320);
        endTitleText.alignment = TextAnchor.MiddleCenter;
        endTitleText.fontStyle = FontStyle.Bold;
        MakeFlatButton(endPanel.transform, "RestartBtn", new Vector2(0,-120), new Vector2(640, 140), OnRestart, out restartBtnText);
        endPanel.SetActive(false);

        // Fade overlay (topmost)
        fadeOverlay = MakeImage(canvas.transform, "Fade", Color.black);
        Stretch(fadeOverlay.rectTransform);
        fadeOverlay.transform.SetAsLastSibling();
        fadeOverlay.raycastTarget = false;

        // Skip/Next must be ABOVE dialog box and tap area but BELOW fade overlay
        skipBtnGO.transform.SetAsLastSibling();
        nextBtnGO.transform.SetAsLastSibling();
        fadeOverlay.transform.SetAsLastSibling();

        // Audio
        var aGO = new GameObject("Audio");
        aGO.transform.SetParent(transform, false);
        bgmSrc = aGO.AddComponent<AudioSource>();
        bgmSrc.loop = true; bgmSrc.volume = 0.55f;
        sfxSrc = aGO.AddComponent<AudioSource>();
        sfxSrc.loop = false; sfxSrc.volume = 0.85f;
    }

    Text settingsTitleText;
    List<Text> choiceTexts = new List<Text>();

    GameObject MakeBottomRightButton(Transform parent, string name, string label, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick, out Text outLabel, Color accent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.04f,0.03f,0.07f,0.92f);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(1,0); rt.anchorMax = new Vector2(1,0);
        rt.pivot = new Vector2(1,0); rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(360, 110);
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor      = new Color(0.15f,0.12f,0.20f,0.95f);
        cb.highlightedColor = new Color(0.30f,0.22f,0.40f,1f);
        cb.pressedColor     = new Color(0.50f,0.35f,0.60f,1f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
        AddOutline(go, accent, 2);
        outLabel = MakeText(go.transform, "Label", label, 36, new Color(1f,0.95f,0.85f,1f));
        Stretch(outLabel.rectTransform);
        outLabel.alignment = TextAnchor.MiddleCenter;
        outLabel.fontStyle = FontStyle.Bold;
        return go;
    }

    GameObject MakeFlatButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick, out Text outLabel)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f,0.10f,0.18f,0.95f);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor      = new Color(0.18f,0.15f,0.25f,0.95f);
        cb.highlightedColor = new Color(0.32f,0.25f,0.42f,1f);
        cb.pressedColor     = new Color(0.45f,0.32f,0.55f,1f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
        AddOutline(go, new Color(0.9f,0.75f,0.3f,1f), 2);
        outLabel = MakeText(go.transform, "Label", "", 40, new Color(1f,0.95f,0.85f,1f));
        Stretch(outLabel.rectTransform);
        outLabel.alignment = TextAnchor.MiddleCenter;
        outLabel.fontStyle = FontStyle.Bold;
        return go;
    }

    Image MakeImage(Transform parent, string name, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c; img.raycastTarget = false;
        return img;
    }
    Text MakeText(Transform parent, string name, string content, int size, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = c;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
    void AddOutline(GameObject go, Color c, int thickness)
    {
        var ol = go.AddComponent<Outline>();
        ol.effectColor = c;
        ol.effectDistance = new Vector2(thickness, -thickness);
    }
    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // ============ SCRIPT (canon Episode 1) ============
    void BuildScript()
    {
        script = new List<Line>();
        Add("", "bg_sarcophagus", "bgm_olympus", "sfx_transition",
            "A thousand years of silence. Stone cracks. Dust falls. Something stirs in the dark.",
            "Тысяча лет тишины. Камень трескается. Пыль осыпается. Что-то шевелится во тьме.");
        Add("portrait_eon", "bg_sarcophagus", "", "",
            "Eon: ...where... am I? My hammer... cold. My hands... mortal.",
            "Эон: ...где... я? Молот... холодный. Руки... смертные.");
        Add("portrait_eon", "bg_sarcophagus", "", "",
            "Eon: I am Eon, last Titan of the Pact. I should not have woken. Olympus stands... yet I feel only ash.",
            "Эон: Я Эон, последний Титан Пакта. Я не должен был проснуться. Олимп стоит... но я чую лишь пепел.");
        Add("portrait_pythia", "bg_grove", "", "sfx_transition",
            "Pythia: Titan. You hear me through the cracked stone. You woke because the gods are gone.",
            "Пифия: Титан. Ты слышишь меня сквозь треснувший камень. Ты проснулся, потому что боги исчезли.");
        Add("portrait_pythia", "bg_grove", "", "",
            "Pythia: A thousand years past, Zeus fell. Hera screamed and was silenced. Olympus burned from within.",
            "Пифия: Тысячу лет назад Зевс пал. Гера закричала и была заглушена. Олимп сгорел изнутри.");
        Add("portrait_pythia", "bg_grove", "", "",
            "Pythia: Now mortals are slaves to a Fallen one — Achilles the Elder, demigod turned tyrant. He wears black armor cracked with violet light.",
            "Пифия: Теперь смертные — рабы Падшего. Ахилл Старший, полубог, ставший тираном. Он носит чёрные латы с фиолетовыми трещинами.");
        AddChoice(
            "Eon: Why me? I am a Titan. Why should I save mortals?",
            "Эон: Почему я? Я Титан. Зачем мне спасать смертных?",
            new []{"\"Because I swore the Pact.\"", "\"Because someone must.\"", "\"Because no god remains.\""},
            new []{"«Потому что я дал клятву.»", "«Потому что кто-то должен.»", "«Потому что не осталось богов.»"});
        Add("portrait_pythia", "bg_grove", "", "",
            "Pythia: Your reason matters less than your steps. Walk to the Agora. Hermes waits — what remains of him.",
            "Пифия: Твоя причина не так важна, как твои шаги. Иди на Агору. Гермес ждёт — то, что от него осталось.");
        Add("", "bg_agora", "bgm_tense", "sfx_transition",
            "The marketplace of Athens is ash and broken columns. Crows pick at bones. A figure leans on a cracked herald's staff.",
            "Рынок Афин — пепел и сломанные колонны. Вороны клюют кости. Фигура опирается на треснувший жезл вестника.");
        Add("portrait_hermes", "bg_agora", "", "",
            "Hermes: Eon. You took your time. A thousand years late and still in last season's sandals.",
            "Гермес: Эон. Не торопился. Опоздал на тысячу лет — да ещё в прошлогодних сандалиях.");
        Add("portrait_hermes", "bg_agora", "", "",
            "Hermes: I am all that's left of the messengers. The pantheon is meat for the Fallen. He devoured the divine spark of each god he killed.",
            "Гермес: Я всё, что осталось от вестников. Пантеон — мясо для Падшего. Он пожрал божественную искру каждого убитого бога.");
        Add("portrait_hermes", "bg_agora", "", "",
            "Hermes: He sits on Olympus now. Not on a throne — on a pile of broken statues. Waiting. He knows you woke.",
            "Гермес: Он сидит на Олимпе. Не на троне — на куче разбитых статуй. Ждёт. Он знает, что ты проснулся.");
        AddChoice(
            "Hermes: So. What kind of Titan are you, sleeper?",
            "Гермес: Так. Что ты за Титан, спящий?",
            new []{"\"I am the Pact. I keep it.\"", "\"I am vengeance for the silent gods.\"", "\"I am the last weight on the scale.\""},
            new []{"«Я — Пакт. Я храню его.»", "«Я — месть за умолкших богов.»", "«Я — последний вес на чаше весов.»"});
        Add("portrait_hermes", "bg_agora", "", "",
            "Hermes: Pretty words. The Fallen has prettier ones. He will tell you Olympus deserved to burn. Part of you will believe him.",
            "Гермес: Красивые слова. У Падшего — красивее. Он скажет тебе, что Олимп заслужил гореть. И часть тебя поверит ему.");
        Add("", "bg_storm", "bgm_tense", "sfx_transition",
            "The climb to Olympus is broken stairs and lightning. The sky is bruised purple. The air tastes of iron.",
            "Подъём на Олимп — сломанные ступени и молния. Небо синяком фиолетово. Воздух на вкус — железо.");
        Add("portrait_eon", "bg_storm", "", "",
            "Eon: I remember this storm. It was a hymn once. Now it screams.",
            "Эон: Я помню эту бурю. Когда-то это был гимн. Теперь — крик.");
        Add("", "bg_altar", "bgm_tense", "sfx_transition",
            "The altar at the summit. Twelve broken statues in a circle. In the center — the Fallen, armor cracked with violet light. A god-hammer rests beside him. Yours.",
            "Алтарь на вершине. Двенадцать разбитых статуй кругом. В центре — Падший, латы в фиолетовых трещинах. Молот-богов лежит рядом с ним. Твой.");
        Add("portrait_fallen", "bg_altar", "", "",
            "Fallen Hoplite: Titan. You wear flesh badly. Tell me — did you also dream of fire while you slept?",
            "Падший Гоплит: Титан. Ты плохо носишь плоть. Скажи — ты тоже видел во сне огонь, пока спал?");
        Add("portrait_fallen", "bg_altar", "", "",
            "Fallen Hoplite: I was Achilles. Then I was a hero. Then I was a saint of a dead pantheon. Then I was nothing. Then I was everything.",
            "Падший Гоплит: Я был Ахиллом. Потом героем. Потом святым мёртвого пантеона. Потом — ничем. Потом — всем.");
        Add("portrait_fallen", "bg_altar", "", "",
            "Fallen Hoplite: Zeus lied. Hera lied. Hermes — he lies even now, leaning on a stick in the agora. I ate them. Their light is in me. It will be in you.",
            "Падший Гоплит: Зевс лгал. Гера лгала. Гермес — лжёт даже сейчас, опираясь на палку на агоре. Я съел их. Их свет — во мне. Будет и в тебе.");
        AddChoice(
            "Fallen Hoplite: Pick up the hammer. Strike me — or kneel. Either way the Pact ends tonight.",
            "Падший Гоплит: Возьми молот. Бей меня — или склони колено. В любом случае Пакт кончится сегодня.",
            new []{"\"I take the hammer. The Pact does not end.\"", "\"You ate gods. You will choke on a Titan.\"", "\"Olympus was hollow. But mortals are not.\""},
            new []{"«Я беру молот. Пакт не кончится.»", "«Ты ел богов. Подавишься Титаном.»", "«Олимп был пуст. Но смертные — нет.»"});
        Add("portrait_eon", "bg_altar", "", "sfx_transition",
            "Eon: I lift the hammer. It remembers me. It is heavier than I remembered. Or I am lighter.",
            "Эон: Я поднимаю молот. Он помнит меня. Он тяжелее, чем я помнил. Или я — легче.");
        Add("portrait_fallen", "bg_altar", "", "",
            "Fallen Hoplite: Good. Strike. Strike well, Titan. I have wanted this for a thousand years.",
            "Падший Гоплит: Хорошо. Бей. Бей чисто, Титан. Я ждал этого тысячу лет.");
        Add("", "bg_altar", "", "sfx_blip",
            "Lightning. Stone. A scream that is not pain but release. The violet light leaves him and rises — twelve sparks, free, scattering into the storm.",
            "Молния. Камень. Крик — не боль, но освобождение. Фиолетовый свет покидает его и поднимается — двенадцать искр, свободные, рассеиваются в буре.");
        Add("portrait_eon", "bg_storm", "bgm_olympus", "",
            "Eon: The gods are not coming back. Not as they were. But the Pact remembers. And so do I.",
            "Эон: Боги не вернутся. Не такими, как были. Но Пакт помнит. И я помню.");
        Add("portrait_hermes", "bg_agora", "", "sfx_transition",
            "Hermes: He's alive. He's actually alive. The mortals are loud about it. There is wine again. There is laughter.",
            "Гермес: Он жив. Он правда жив. Смертные шумят. Снова есть вино. Снова есть смех.");
        Add("portrait_pythia", "bg_grove", "", "",
            "Pythia: Titan. Eon. The pantheon is shattered, but a Titan walks among mortals. That is not nothing.",
            "Пифия: Титан. Эон. Пантеон разбит, но Титан ходит среди смертных. Это не «ничто».");
        Add("portrait_eon", "bg_grove", "", "",
            "Eon: Episode One ends. The Pact holds. The next ruin already waits.",
            "Эон: Эпизод первый окончен. Пакт стоит. Следующая руина уже ждёт.");
    }
    void Add(string speaker, string bg, string bgm, string sfx, string en, string ru)
    {
        script.Add(new Line{ speaker=speaker, bg=bg, bgm=bgm, sfx=sfx, en=en, ru=ru, choices=null });
    }
    void AddChoice(string en, string ru, string[] enCh, string[] ruCh)
    {
        var c = new Choice[enCh.Length];
        for (int i=0;i<enCh.Length;i++) c[i] = new Choice{ en=enCh[i], ru=ruCh[i] };
        script.Add(new Line{ speaker="portrait_eon", bg="", bgm="", sfx="", en=en, ru=ru, choices=c });
    }

    // ============ LOCALIZED REFRESH ============
    void RefreshLocalizedUI()
    {
        // Title screen
        titleText.text = L("TITLE1") + "\n" + L("TITLE2");
        subtitleText.text = L("SUBTITLE");
        startBtnText.text = L("NEW_STORY");
        contBtnText.text  = L("CONTINUE");
        settingsBtnText.text = L("SETTINGS");
        // Settings screen
        settingsTitleText.text = L("SETTINGS");
        musicBtnText.text = L("MUSIC") + " : " + (musicOn ? L("ON") : L("OFF"));
        sfxBtnText.text   = L("SFX")   + " : " + (sfxOn   ? L("ON") : L("OFF"));
        langBtnText.text  = L("LANG")  + " : " + L("LANG_VAL");
        backBtnText.text  = L("BACK");
        // End screen
        endTitleText.text = L("END_TITLE");
        restartBtnText.text = L("RESTART");
        // Skip/Next
        skipBtnText.text = L("SKIP");
        nextBtnText.text = L("NEXT");
        // Current playing line
        if (state == State.Playing || state == State.Choice)
        {
            if (idx < script.Count)
            {
                var line = script[idx];
                speakerText.text = SpeakerLabel(line.speaker);
                fullText = (lang == Lang.EN) ? line.en : line.ru;
                if (typing)
                {
                    dialogText.text = typeIdx >= fullText.Length ? fullText : fullText.Substring(0, Math.Min(typeIdx, fullText.Length));
                }
                else
                {
                    dialogText.text = fullText;
                }
            }
        }
        // Choice texts
        if (state == State.Choice && idx < script.Count)
        {
            var line = script[idx];
            if (line.choices != null)
            {
                for (int i=0;i<choiceTexts.Count;i++)
                {
                    if (i < line.choices.Length)
                        choiceTexts[i].text = (lang == Lang.EN) ? line.choices[i].en : line.choices[i].ru;
                }
            }
        }
    }

    string SpeakerLabel(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        switch (id)
        {
            case "portrait_eon": return L("SPEAKER_EON");
            case "portrait_pythia": return L("SPEAKER_PYTHIA");
            case "portrait_hermes": return L("SPEAKER_HERMES");
            case "portrait_fallen": return L("SPEAKER_FALLEN");
            default: return "";
        }
    }

    // ============ STATE HANDLERS ============
    void ShowTitle()
    {
        state = State.Title;
        titlePanel.SetActive(true);
        settingsPanel.SetActive(false);
        choicePanel.SetActive(false);
        endPanel.SetActive(false);
        dialogBox.gameObject.SetActive(false);
        skipBtnGO.SetActive(false);
        nextBtnGO.SetActive(false);
        if (sprites.ContainsKey("bg_title"))
        {
            titleArt.sprite = sprites["bg_title"];
            titleArt.color = Color.white;
        }
        portraitImage.color = new Color(1,1,1,0);
        PlayBgm("bgm_olympus");
        RefreshLocalizedUI();
        fadeDir = -1;
    }

    void OnOpenSettings()
    {
        PlaySfx("sfx_choice");
        state = State.Settings;
        titlePanel.SetActive(false);
        settingsPanel.SetActive(true);
        RefreshLocalizedUI();
    }
    void OnBackFromSettings()
    {
        PlaySfx("sfx_choice");
        settingsPanel.SetActive(false);
        if (state == State.Settings) ShowTitle();
    }
    void OnToggleMusic()
    {
        PlaySfx("sfx_choice");
        musicOn = !musicOn;
        PlayerPrefs.SetInt("music", musicOn ? 1 : 0);
        ApplyAudioMutes();
        RefreshLocalizedUI();
    }
    void OnToggleSfx()
    {
        sfxOn = !sfxOn;
        PlayerPrefs.SetInt("sfx", sfxOn ? 1 : 0);
        ApplyAudioMutes();
        // play after applying so user hears state after enabling
        if (sfxOn) PlaySfx("sfx_choice");
        RefreshLocalizedUI();
    }
    void OnToggleLang()
    {
        PlaySfx("sfx_choice");
        lang = (lang == Lang.EN) ? Lang.RU : Lang.EN;
        PlayerPrefs.SetInt("lang", (int)lang);
        RefreshLocalizedUI();
    }

    void ApplyAudioMutes()
    {
        if (bgmSrc != null) bgmSrc.mute = !musicOn;
        if (sfxSrc != null) sfxSrc.mute = !sfxOn;
    }

    void OnStartNew()
    {
        PlaySfx("sfx_choice");
        idx = 0; SaveProgress();
        BeginPlay();
    }
    void OnContinue()
    {
        PlaySfx("sfx_choice");
        if (idx >= script.Count) idx = 0;
        BeginPlay();
    }
    void BeginPlay()
    {
        titlePanel.SetActive(false);
        settingsPanel.SetActive(false);
        dialogBox.gameObject.SetActive(true);
        state = State.Playing;
        ShowCurrentLine(initial: true);
    }

    void ShowCurrentLine(bool initial)
    {
        if (idx >= script.Count) { EndStory(); return; }
        var line = script[idx];

        // Crossfade background if changed
        if (!string.IsNullOrEmpty(line.bg) && sprites.ContainsKey(line.bg) && line.bg != currentBgKey)
        {
            if (initial || string.IsNullOrEmpty(currentBgKey))
            {
                bgImage.sprite = sprites[line.bg];
                bgImage.color = Color.white;
                bgImageNext.color = new Color(1,1,1,0);
                currentBgKey = line.bg;
                crossfading = false;
            }
            else
            {
                bgImageNext.sprite = sprites[line.bg];
                bgImageNext.color = new Color(1,1,1,0);
                pendingBg = line.bg;
                crossfading = true;
                crossTime = 0f;
            }
        }
        // Portrait
        if (!string.IsNullOrEmpty(line.speaker) && sprites.ContainsKey(line.speaker))
        {
            portraitImage.sprite = sprites[line.speaker];
            portraitImage.color = Color.white;
        }
        else { portraitImage.color = new Color(1,1,1,0); }

        if (!string.IsNullOrEmpty(line.bgm)) PlayBgm(line.bgm);
        if (!string.IsNullOrEmpty(line.sfx)) PlaySfx(line.sfx);

        speakerText.text = SpeakerLabel(line.speaker);
        fullText = (lang == Lang.EN) ? line.en : line.ru;
        typeIdx = 0; typeTimer = 0f; typing = true;
        dialogText.text = "";

        // SKIP visible, NEXT hidden
        skipBtnGO.SetActive(true);
        nextBtnGO.SetActive(false);

        if (line.choices == null) choicePanel.SetActive(false);
    }

    void ShowChoiceUI()
    {
        var line = script[idx];
        if (line.choices == null) { choicePanel.SetActive(false); return; }
        choicePanel.SetActive(true);
        state = State.Choice;
        skipBtnGO.SetActive(false);
        nextBtnGO.SetActive(false);
        for (int i=0;i<choiceButtons.Count;i++)
        {
            var go = choiceButtons[i].gameObject;
            if (i < line.choices.Length)
            {
                go.SetActive(true);
                choiceTexts[i].text = (lang == Lang.EN) ? line.choices[i].en : line.choices[i].ru;
            }
            else go.SetActive(false);
        }
    }

    void OnSkip()
    {
        if (state != State.Playing) return;
        if (!typing) return;
        typeIdx = fullText.Length;
        dialogText.text = fullText;
        typing = false;
        skipBtnGO.SetActive(false);
        var line = script[idx];
        if (line.choices != null) ShowChoiceUI();
        else nextBtnGO.SetActive(true);
    }
    void OnNext()
    {
        if (state != State.Playing) return;
        if (typing) return;
        var line = script[idx];
        if (line.choices != null) { ShowChoiceUI(); return; }
        idx++; SaveProgress();
        if (idx >= script.Count) { EndStory(); return; }
        ShowCurrentLine(initial: false);
    }
    void OnTap()
    {
        // Same semantics: tap on background = fast-forward typewriter, then advance
        if (state != State.Playing) return;
        if (typing) { OnSkip(); return; }
        OnNext();
    }
    void OnChoicePicked(int which)
    {
        PlaySfx("sfx_choice");
        choicePanel.SetActive(false);
        state = State.Playing;
        idx++; SaveProgress();
        if (idx >= script.Count) { EndStory(); return; }
        ShowCurrentLine(initial: false);
    }
    void EndStory()
    {
        state = State.Ended;
        choicePanel.SetActive(false);
        skipBtnGO.SetActive(false);
        nextBtnGO.SetActive(false);
        endPanel.SetActive(true);
        PlayBgm("bgm_olympus");
        RefreshLocalizedUI();
    }
    void OnRestart()
    {
        PlaySfx("sfx_choice");
        idx = 0; SaveProgress();
        endPanel.SetActive(false);
        ShowTitle();
    }

    // ============ SAVE / LOAD ============
    void SaveProgress()
    {
        PlayerPrefs.SetInt("idx", idx);
        PlayerPrefs.SetInt("lang", (int)lang);
        PlayerPrefs.SetInt("music", musicOn ? 1 : 0);
        PlayerPrefs.SetInt("sfx", sfxOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    void LoadProgress()
    {
        idx = PlayerPrefs.GetInt("idx", 0);
        lang = (Lang)PlayerPrefs.GetInt("lang", 0);
        musicOn = PlayerPrefs.GetInt("music", 1) == 1;
        sfxOn = PlayerPrefs.GetInt("sfx", 1) == 1;
    }

    // ============ AUDIO ============
    void PlayBgm(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (id == currentBgm && bgmSrc.isPlaying) return;
        if (!clips.ContainsKey(id)) return;
        bgmSrc.clip = clips[id];
        bgmSrc.Play();
        currentBgm = id;
    }
    void PlaySfx(string id)
    {
        if (!sfxOn) return;
        if (string.IsNullOrEmpty(id) || !clips.ContainsKey(id)) return;
        sfxSrc.PlayOneShot(clips[id]);
    }

    // ============ UPDATE ============
    void Update()
    {
        // Fade overlay (boot in/out)
        if (fadeDir != 0)
        {
            fadeAlpha += fadeDir * Time.deltaTime * 0.7f; // slower fade per constraint 61
            fadeAlpha = Mathf.Clamp01(fadeAlpha);
            fadeOverlay.color = new Color(0,0,0,fadeAlpha);
            if (fadeAlpha <= 0f || fadeAlpha >= 1f) fadeDir = 0;
        }

        // Background crossfade
        if (crossfading)
        {
            crossTime += Time.deltaTime;
            float a = Mathf.Clamp01(crossTime / CROSS_DUR);
            bgImageNext.color = new Color(1,1,1,a);
            bgImage.color = new Color(1,1,1,1f - a);
            if (a >= 1f)
            {
                // promote next to current
                bgImage.sprite = bgImageNext.sprite;
                bgImage.color = Color.white;
                bgImageNext.color = new Color(1,1,1,0);
                currentBgKey = pendingBg;
                crossfading = false;
            }
        }

        // Typewriter
        if (typing && (state == State.Playing || state == State.Choice))
        {
            typeTimer += Time.deltaTime;
            while (typeTimer >= TYPE_SPEED && typeIdx < fullText.Length)
            {
                typeTimer -= TYPE_SPEED;
                typeIdx++;
                dialogText.text = fullText.Substring(0, typeIdx);
                if (typeIdx % 6 == 0) PlaySfx("sfx_blip");
            }
            if (typeIdx >= fullText.Length)
            {
                typing = false;
                skipBtnGO.SetActive(false);
                var line = script[idx];
                if (line.choices != null) ShowChoiceUI();
                else nextBtnGO.SetActive(true);
            }
        }

        // NEXT button pulse
        if (nextBtnGO.activeSelf)
        {
            pulseT += Time.deltaTime * 3f;
            float p = 0.85f + 0.15f * Mathf.Sin(pulseT);
            nextBtnText.color = new Color(1f, 0.95f, 0.85f, p);
        }
    }
}
