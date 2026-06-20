// Echoes: Shattered Pantheon - Episode 1: Fall of Olympus
// Visual Novel (Path G) - bilingual EN/RU, single linear ending
// Bootstrapper.cs - all-in-one runtime, no scene wiring required
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrapper : MonoBehaviour
{
    // ============ ENUMS ============
    enum State { Title, Playing, Choice, Ended }
    enum Lang { EN, RU }

    // ============ STORY MODEL ============
    class Line
    {
        public string speaker;   // ID portrait (or "" for narrator)
        public string bg;        // background sprite ID
        public string bgm;       // music ID ("" = keep)
        public string sfx;       // one-shot sfx
        public string en;
        public string ru;
        public Choice[] choices; // null if just a line
    }
    class Choice { public string en; public string ru; }

    // ============ FIELDS ============
    Lang lang = Lang.EN;
    State state = State.Title;
    int idx = 0;
    List<Line> script;

    // Sprites
    Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    // UI
    Canvas canvas;
    Image bgImage, portraitImage, dialogBox, titleBg;
    Text dialogText, speakerText, titleText, subtitleText, langBtnText;
    GameObject titlePanel, gamePanel, choicePanel, endPanel;
    Button startBtn, continueBtn, langBtn, restartBtn;
    List<Button> choiceButtons = new List<Button>();

    // Audio
    AudioSource bgmSrc, sfxSrc;
    string currentBgm = "";

    // Typewriter
    string fullText = "";
    int typeIdx = 0;
    float typeTimer = 0f;
    const float TYPE_SPEED = 0.025f;
    bool typing = false;

    // Fade
    Image fadeOverlay;
    float fadeAlpha = 1f;
    int fadeDir = -1; // -1 fade in, +1 fade out, 0 idle

    // ============ ENTRY ============
    void Start()
    {
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.Portrait;
        QualitySettings.vSyncCount = 0;

        LoadAllSprites();
        LoadAllAudio();
        BuildUI();
        BuildScript();
        LoadProgress();
        ShowTitle();
    }

    // ============ ASSET LOADING ============
    void LoadAllSprites()
    {
        string[] names = new string[]
        {
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
        canvasGO.AddComponent<CanvasScaler>().referenceResolution = new Vector2(1080,1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Background
        bgImage = MakeImage(canvas.transform, "BG", new Color(0,0,0,1));
        Stretch(bgImage.rectTransform);

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

        // Speaker name
        speakerText = MakeText(dialogBox.transform, "Speaker", "", 42, new Color(1f,0.85f,0.4f,1f));
        var st = speakerText.rectTransform;
        st.anchorMin = new Vector2(0,1); st.anchorMax = new Vector2(1,1);
        st.pivot = new Vector2(0,1); st.anchoredPosition = new Vector2(40,-25);
        st.sizeDelta = new Vector2(-80, 60);
        speakerText.fontStyle = FontStyle.Bold;

        // Dialog text
        dialogText = MakeText(dialogBox.transform, "Dialog", "", 36, new Color(0.95f,0.95f,0.92f,1f));
        var dt = dialogText.rectTransform;
        dt.anchorMin = new Vector2(0,0); dt.anchorMax = new Vector2(1,1);
        dt.offsetMin = new Vector2(40, 110); dt.offsetMax = new Vector2(-40, -90);
        dialogText.alignment = TextAnchor.UpperLeft;

        // Tap-to-advance overlay (covers screen except choice/end panels)
        var tapGO = new GameObject("TapArea");
        tapGO.transform.SetParent(canvas.transform, false);
        var tapImg = tapGO.AddComponent<Image>();
        tapImg.color = new Color(0,0,0,0); tapImg.raycastTarget = true;
        Stretch(tapImg.rectTransform);
        var tapBtn = tapGO.AddComponent<Button>();
        tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(OnTap);

        // Move dialog box on top of tap area
        dialogBox.transform.SetAsLastSibling();

        // === Title Panel ===
        titlePanel = new GameObject("TitlePanel");
        titlePanel.transform.SetParent(canvas.transform, false);
        var tpRT = titlePanel.AddComponent<RectTransform>();
        Stretch(tpRT);
        titleBg = MakeImage(titlePanel.transform, "TitleBG", Color.black);
        Stretch(titleBg.rectTransform);

        titleText = MakeText(titlePanel.transform, "Title",
            "ECHOES\nSHATTERED PANTHEON", 96, new Color(1f,0.85f,0.4f,1f));
        var tt = titleText.rectTransform;
        tt.anchorMin = new Vector2(0.5f,0.5f); tt.anchorMax = new Vector2(0.5f,0.5f);
        tt.anchoredPosition = new Vector2(0, 380); tt.sizeDelta = new Vector2(1000, 320);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;

        subtitleText = MakeText(titlePanel.transform, "Subtitle",
            "Episode 1: Fall of Olympus", 48, new Color(0.85f,0.85f,0.85f,1f));
        var sub = subtitleText.rectTransform;
        sub.anchorMin = new Vector2(0.5f,0.5f); sub.anchorMax = new Vector2(0.5f,0.5f);
        sub.anchoredPosition = new Vector2(0, 180); sub.sizeDelta = new Vector2(1000, 80);
        subtitleText.alignment = TextAnchor.MiddleCenter;

        startBtn = MakeButton(titlePanel.transform, "StartBtn", "NEW STORY / НАЧАТЬ",
            new Vector2(0,-100), new Vector2(640, 130));
        startBtn.onClick.AddListener(OnStartNew);

        continueBtn = MakeButton(titlePanel.transform, "ContBtn", "CONTINUE / ПРОДОЛЖИТЬ",
            new Vector2(0,-260), new Vector2(640, 130));
        continueBtn.onClick.AddListener(OnContinue);

        langBtn = MakeButton(titlePanel.transform, "LangBtn", "EN | RU",
            new Vector2(0,-420), new Vector2(640, 110));
        langBtn.onClick.AddListener(OnToggleLang);
        langBtnText = langBtn.GetComponentInChildren<Text>();

        // === Choice Panel ===
        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(canvas.transform, false);
        var cpRT = choicePanel.AddComponent<RectTransform>();
        Stretch(cpRT);
        var cpBg = MakeImage(choicePanel.transform, "CpBG", new Color(0,0,0,0.55f));
        Stretch(cpBg.rectTransform);
        // 3 buttons stacked center
        for (int i=0;i<3;i++)
        {
            var b = MakeButton(choicePanel.transform, "Choice"+i, "—",
                new Vector2(0, 220 - i*200), new Vector2(900, 170));
            int captured = i;
            b.onClick.AddListener(()=>OnChoicePicked(captured));
            choiceButtons.Add(b);
        }
        choicePanel.SetActive(false);

        // === End Panel ===
        endPanel = new GameObject("EndPanel");
        endPanel.transform.SetParent(canvas.transform, false);
        var epRT = endPanel.AddComponent<RectTransform>();
        Stretch(epRT);
        var epBg = MakeImage(endPanel.transform, "EpBG", new Color(0,0,0,0.92f));
        Stretch(epBg.rectTransform);
        var endTitle = MakeText(endPanel.transform, "EndTitle",
            "TO BE CONTINUED\nПРОДОЛЖЕНИЕ СЛЕДУЕТ", 72, new Color(1f,0.85f,0.4f,1f));
        var etRT = endTitle.rectTransform;
        etRT.anchorMin = new Vector2(0.5f,0.5f); etRT.anchorMax = new Vector2(0.5f,0.5f);
        etRT.anchoredPosition = new Vector2(0, 240); etRT.sizeDelta = new Vector2(1000, 320);
        endTitle.alignment = TextAnchor.MiddleCenter;
        endTitle.fontStyle = FontStyle.Bold;
        restartBtn = MakeButton(endPanel.transform, "RestartBtn",
            "RESTART / ЗАНОВО", new Vector2(0,-120), new Vector2(640, 130));
        restartBtn.onClick.AddListener(OnRestart);
        endPanel.SetActive(false);

        // === Fade overlay (on top of everything) ===
        fadeOverlay = MakeImage(canvas.transform, "Fade", Color.black);
        Stretch(fadeOverlay.rectTransform);
        fadeOverlay.transform.SetAsLastSibling();
        fadeOverlay.raycastTarget = false;

        // Audio sources
        var aGO = new GameObject("Audio");
        aGO.transform.SetParent(transform, false);
        bgmSrc = aGO.AddComponent<AudioSource>();
        bgmSrc.loop = true; bgmSrc.volume = 0.55f;
        sfxSrc = aGO.AddComponent<AudioSource>();
        sfxSrc.loop = false; sfxSrc.volume = 0.85f;
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

    Button MakeButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
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
        cb.normalColor = new Color(0.18f,0.15f,0.25f,0.95f);
        cb.highlightedColor = new Color(0.32f,0.25f,0.42f,1f);
        cb.pressedColor = new Color(0.45f,0.32f,0.55f,1f);
        btn.colors = cb;
        AddOutline(go, new Color(0.9f,0.75f,0.3f,1f), 2);
        var txt = MakeText(go.transform, "Label", label, 40, new Color(1f,0.95f,0.85f,1f));
        Stretch(txt.rectTransform);
        txt.alignment = TextAnchor.MiddleCenter;
        return btn;
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
        // === ACT 1: Awakening ===
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

        // === ACT 2: First choice (cosmetic, leads to same hub) ===
        AddChoice(
            "Eon: Why me? I am a Titan. Why should I save mortals?",
            "Эон: Почему я? Я Титан. Зачем мне спасать смертных?",
            new []{"\"Because I swore the Pact.\"", "\"Because someone must.\"", "\"Because no god remains.\""},
            new []{"«Потому что я дал клятву.»", "«Потому что кто-то должен.»", "«Потому что не осталось богов.»"});

        Add("portrait_pythia", "bg_grove", "", "",
            "Pythia: Your reason matters less than your steps. Walk to the Agora. Hermes waits — what remains of him.",
            "Пифия: Твоя причина не так важна, как твои шаги. Иди на Агору. Гермес ждёт — то, что от него осталось.");

        // === ACT 3: Agora ===
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

        // === ACT 4: Second choice ===
        AddChoice(
            "Hermes: So. What kind of Titan are you, sleeper?",
            "Гермес: Так. Что ты за Титан, спящий?",
            new []{"\"I am the Pact. I keep it.\"", "\"I am vengeance for the silent gods.\"", "\"I am the last weight on the scale.\""},
            new []{"«Я — Пакт. Я храню его.»", "«Я — месть за умолкших богов.»", "«Я — последний вес на чаше весов.»"});

        Add("portrait_hermes", "bg_agora", "", "",
            "Hermes: Pretty words. The Fallen has prettier ones. He will tell you Olympus deserved to burn. Part of you will believe him.",
            "Гермес: Красивые слова. У Падшего — красивее. Он скажет тебе, что Олимп заслужил гореть. И часть тебя поверит ему.");

        // === ACT 5: Climb ===
        Add("", "bg_storm", "bgm_tense", "sfx_transition",
            "The climb to Olympus is broken stairs and lightning. The sky is bruised purple. The air tastes of iron.",
            "Подъём на Олимп — сломанные ступени и молния. Небо синяком фиолетово. Воздух на вкус — железо.");
        Add("portrait_eon", "bg_storm", "", "",
            "Eon: I remember this storm. It was a hymn once. Now it screams.",
            "Эон: Я помню эту бурю. Когда-то это был гимн. Теперь — крик.");

        // === ACT 6: The Fallen ===
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
            "Fallen Hoplite: Zeus lied. Hera lied. Hermes —" + " he lies even now, leaning on a stick in the agora. I ate them. Their light is in me. It will be in you.",
            "Падший Гоплит: Зевс лгал. Гера лгала. Гермес — лжёт даже сейчас, опираясь на палку на агоре. Я съел их. Их свет — во мне. Будет и в тебе.");

        // === ACT 7: Final choice (single ending, choice is dialogue only) ===
        AddChoice(
            "Fallen Hoplite: Pick up the hammer. Strike me — or kneel. Either way the Pact ends tonight.",
            "Падший Гоплит: Возьми молот. Бей меня — или склони колено. В любом случае Пакт кончится сегодня.",
            new []{"\"I take the hammer. The Pact does not end.\"", "\"You ate gods. You will choke on a Titan.\"", "\"Olympus was hollow. But mortals are not.\""},
            new []{"«Я беру молот. Пакт не кончится.»", "«Ты ел богов. Подавишься Титаном.»", "«Олимп был пуст. Но смертные — нет.»"});

        // === Linear ending (all roads converge) ===
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

    // ============ TITLE / FLOW ============
    void ShowTitle()
    {
        state = State.Title;
        titlePanel.SetActive(true);
        choicePanel.SetActive(false);
        endPanel.SetActive(false);
        if (sprites.ContainsKey("bg_title")) titleBg.sprite = sprites["bg_title"];
        titleBg.color = Color.white;
        portraitImage.color = new Color(1,1,1,0);
        dialogBox.gameObject.SetActive(false);
        PlayBgm("bgm_olympus");
        UpdateLangButton();
        fadeDir = -1;
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

    void OnToggleLang()
    {
        PlaySfx("sfx_choice");
        lang = (lang == Lang.EN) ? Lang.RU : Lang.EN;
        PlayerPrefs.SetInt("lang", (int)lang);
        UpdateLangButton();
        if (state == State.Playing) ShowCurrentLine();
        if (state == State.Choice) ShowChoiceUI();
    }

    void UpdateLangButton()
    {
        if (langBtnText != null)
            langBtnText.text = (lang == Lang.EN) ? "▶ ENGLISH | русский" : "▶ РУССКИЙ | english";
    }

    void BeginPlay()
    {
        titlePanel.SetActive(false);
        dialogBox.gameObject.SetActive(true);
        state = State.Playing;
        ShowCurrentLine();
    }

    // ============ LINE PLAYBACK ============
    void ShowCurrentLine()
    {
        if (idx >= script.Count) { EndStory(); return; }
        var line = script[idx];

        // Background
        if (!string.IsNullOrEmpty(line.bg) && sprites.ContainsKey(line.bg))
        {
            bgImage.sprite = sprites[line.bg];
            bgImage.color = Color.white;
        }
        // Portrait
        if (!string.IsNullOrEmpty(line.speaker) && sprites.ContainsKey(line.speaker))
        {
            portraitImage.sprite = sprites[line.speaker];
            portraitImage.color = Color.white;
        }
        else
        {
            portraitImage.color = new Color(1,1,1,0);
        }
        // BGM
        if (!string.IsNullOrEmpty(line.bgm)) PlayBgm(line.bgm);
        // SFX
        if (!string.IsNullOrEmpty(line.sfx)) PlaySfx(line.sfx);

        // Speaker label
        speakerText.text = SpeakerLabel(line.speaker);

        // Typewriter
        fullText = (lang == Lang.EN) ? line.en : line.ru;
        typeIdx = 0; typeTimer = 0f; typing = true;
        dialogText.text = "";

        // If this is a choice line, show choice UI after typewriter completes
        if (line.choices != null) { /* handled in Update when typing finishes */ }
        else { choicePanel.SetActive(false); }
    }

    string SpeakerLabel(string id)
    {
        if (string.IsNullOrEmpty(id)) return (lang == Lang.EN) ? "" : "";
        switch (id)
        {
            case "portrait_eon": return (lang == Lang.EN) ? "Eon" : "Эон";
            case "portrait_pythia": return (lang == Lang.EN) ? "Pythia" : "Пифия";
            case "portrait_hermes": return (lang == Lang.EN) ? "Hermes" : "Гермес";
            case "portrait_fallen": return (lang == Lang.EN) ? "Fallen Hoplite" : "Падший Гоплит";
            default: return "";
        }
    }

    void ShowChoiceUI()
    {
        var line = script[idx];
        if (line.choices == null) { choicePanel.SetActive(false); return; }
        choicePanel.SetActive(true);
        state = State.Choice;
        for (int i=0;i<choiceButtons.Count;i++)
        {
            if (i < line.choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                var lbl = choiceButtons[i].GetComponentInChildren<Text>();
                lbl.text = (lang == Lang.EN) ? line.choices[i].en : line.choices[i].ru;
            }
            else choiceButtons[i].gameObject.SetActive(false);
        }
    }

    void OnTap()
    {
        if (state != State.Playing) return;
        if (typing) { /* fast-forward */ typeIdx = fullText.Length; dialogText.text = fullText; typing = false; return; }
        var line = script[idx];
        if (line.choices != null) { ShowChoiceUI(); return; }
        idx++;
        SaveProgress();
        if (idx >= script.Count) { EndStory(); return; }
        ShowCurrentLine();
    }

    void OnChoicePicked(int which)
    {
        PlaySfx("sfx_choice");
        // Single linear ending: choice is dialogue flavor only, story continues
        choicePanel.SetActive(false);
        state = State.Playing;
        idx++;
        SaveProgress();
        if (idx >= script.Count) { EndStory(); return; }
        ShowCurrentLine();
    }

    void EndStory()
    {
        state = State.Ended;
        choicePanel.SetActive(false);
        endPanel.SetActive(true);
        PlayBgm("bgm_olympus");
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
        PlayerPrefs.Save();
    }
    void LoadProgress()
    {
        idx = PlayerPrefs.GetInt("idx", 0);
        lang = (Lang)PlayerPrefs.GetInt("lang", 0);
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
        if (string.IsNullOrEmpty(id) || !clips.ContainsKey(id)) return;
        sfxSrc.PlayOneShot(clips[id]);
    }

    // ============ UPDATE ============
    void Update()
    {
        // Fade
        if (fadeDir != 0)
        {
            fadeAlpha += fadeDir * Time.deltaTime * 1.2f;
            fadeAlpha = Mathf.Clamp01(fadeAlpha);
            fadeOverlay.color = new Color(0,0,0,fadeAlpha);
            if (fadeAlpha <= 0f || fadeAlpha >= 1f) fadeDir = 0;
        }

        // Typewriter
        if (typing)
        {
            typeTimer += Time.deltaTime;
            while (typeTimer >= TYPE_SPEED && typeIdx < fullText.Length)
            {
                typeTimer -= TYPE_SPEED;
                typeIdx++;
                dialogText.text = fullText.Substring(0, typeIdx);
                // soft blip every 3rd char
                if (typeIdx % 6 == 0) PlaySfx("sfx_blip");
            }
            if (typeIdx >= fullText.Length)
            {
                typing = false;
                var line = script[idx];
                if (line.choices != null) ShowChoiceUI();
            }
        }
    }
}
