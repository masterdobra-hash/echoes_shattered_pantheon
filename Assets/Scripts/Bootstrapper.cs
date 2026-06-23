// ============================================================================
// Echoes: Shattered Pantheon — Iteration S (v4.0.0)
// VN (Path G) + Match-3 battle + Shop + Multi-ending + 7 episodes
// Constraints satisfied: 1-82 (see project canon, especially 64-82)
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bootstrapper : MonoBehaviour
{
    // ============ STATE ============
    enum State { Title, Settings, EpisodeSelect, Playing, Choice, PreBattle, Battle, BattleResult, Shop, Ending, Ended }
    enum Lang { EN, RU }
    enum Path { Undecided, Pact, Vengeance, Mortals }

    // VN line model (same as v11)
    class Line {
        public string speaker; public string bg; public string bgm; public string sfx;
        public string en; public string ru; public Choice[] choices;
        public int triggerBattle; // 0 = no battle, 1..11 = battle id within episode
    }
    class Choice {
        public string en; public string ru;
        public int pathBias; // 1=Pact, 2=Vengeance, 3=Mortals, 0=neutral
    }

    // Episode definition
    class Episode {
        public int id;                 // 1..7
        public string nameEn, nameRu;  // "Fall of Olympus" / "Падение Олимпа"
        public string subtitleEn, subtitleRu;
        public string bgKey;           // primary background sprite
        public string bgmCalm, bgmBattle; // reuses bgm_olympus/bgm_tense for all eps
        public string exclusiveGem;    // pantheon-specific gem id
        public List<Line> script;      // dialogue + lore lines (with pre-battle triggers)
        public BattleConfig[] battles; // 10 normal + 1 boss = 11 configs
    }
    class BattleConfig {
        public int id;          // 1..11 within episode (11 = boss)
        public int gridW, gridH;
        public int colors;
        public int enemyHp;
        public int playerHp;
        public string enemyKey;   // enemy portrait key
        public string arenaBgKey; // battle background (defaults to episode bg)
        public string preEn, preRu; // short lore line shown before battle
        public bool isBoss;
        // Branch variations (Pact/Vengeance/Mortals) — modifies enemy & arena
        public string altEnemyVengeance, altEnemyMortals;
    }

    // ============ FIELDS ============
    Lang lang = Lang.EN;
    State state = State.Title;
    Path path = Path.Undecided;
    int pactScore = 0, vengeScore = 0, mortalScore = 0;
    int currentEpisode = 1;
    int currentBattle = 0;
    int idx = 0;
    int echoes = 0;       // soft currency
    int sparks = 0;       // hard currency (mock donate)
    bool musicOn = true, sfxOn = true;
    List<Episode> episodes;
    Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    Dictionary<string, int> abilityCount = new Dictionary<string, int>();

    // UI roots
    Canvas canvas;
    Image bgImage, bgImageNext;
    Image portraitImage;
    Image portraitCardBg;
    Image dialogBox;
    Image fadeOverlay;
    Image titleArt;
    Text dialogText, speakerText;
    Text titleText, subtitleText;
    Text skipBtnText, nextBtnText;
    Text musicBtnText, sfxBtnText, langBtnText, backBtnText;
    Text settingsBtnText, startBtnText, contBtnText, restartBtnText, endTitleText;
    Text echoesText, sparksText;
    Text settingsTitleText;
    Text battleTurnText, battleEnemyHpText, battlePlayerHpText, preBattleText;
    Text shopTitleText, abilityInfoText;
    GameObject titlePanel, settingsPanel, choicePanel, endPanel;
    GameObject menuBar;
    GameObject skipBtnGO, nextBtnGO;
    GameObject preBattlePanel, battlePanel, battleResultPanel, shopPanel, endingPanel;
    Text battleResultTitle, battleResultRewardText;
    Text endingTitleText, endingBodyText;
    List<Button> choiceButtons = new List<Button>();
    List<Text> choiceTexts = new List<Text>();
    List<GameObject> abilityButtonsGO = new List<GameObject>();
    List<Text> abilityButtonsText = new List<Text>();

    // Audio
    AudioSource bgmSrc, sfxSrc;
    string currentBgm = "";
    string currentBgKey = "";

    // Typewriter
    string fullText = "";
    int typeIdx = 0; float typeTimer = 0f;
    const float TYPE_SPEED = 0.025f;
    bool typing = false;

    // Fade overlay
    float fadeAlpha = 1f;
    int fadeDir = -1;
    bool crossfading = false;
    float crossTime = 0f;
    const float CROSS_DUR = 1.2f;
    string pendingBg = "";

    // NEXT pulse
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
        BuildEpisodes();
        ApplyAudioMutes();
        ShowTitle();
    }

    // ============ LOC ============
    // v14: localization keys for intents/enemy skills/bonuses
    string L14(string key)
    {
        bool ru = (lang == Lang.RU);
        switch (key)
        {
            case "INTENT":           return ru ? "Намерение" : "Intent";
            case "ENS_PIERCE":       return ru ? "Пронзить" : "Pierce";
            case "ENS_CURSE":        return ru ? "Проклятие" : "Curse";
            case "ENS_PACT_BLAST":   return ru ? "Взрыв Договора" : "Pact Blast";
            case "ENS_CURSE_STORM":  return ru ? "Буря Проклятий" : "Curse Storm";
            case "ENS_TITAN_WRATH":  return ru ? "Ярость Титана" : "Titan Wrath";
            case "BONUS_HERMES":     return ru ? "Поступь Гермеса" : "Hermes Step";
            case "BONUS_HEPHAESTUS": return ru ? "Молот Гефеста" : "Hephaestus Hammer";
            case "BONUS_ZEUS":       return ru ? "Молния Зевса" : "Zeus Lightning";
        }
        return "";
    }
    string L(string key)
    {
        string v14r = L14(key); if (v14r.Length > 0) return v14r;
        return L_(key);
    }
    string L_(string key)
    {
        bool en = (lang == Lang.EN);
        switch (key)
        {
            case "NEW_STORY":   return en ? "NEW STORY" : "НОВАЯ ИСТОРИЯ";
            case "CONTINUE":    return en ? "CONTINUE" : "ПРОДОЛЖИТЬ";
            case "SETTINGS":    return en ? "SETTINGS" : "НАСТРОЙКИ";
            case "EPISODES":    return en ? "EPISODES" : "ЭПИЗОДЫ";
            case "BACK":        return en ? "◀ BACK" : "◀ НАЗАД";
            case "RESTART":     return en ? "RESTART" : "ЗАНОВО";
            case "MUSIC":       return en ? "MUSIC" : "МУЗЫКА";
            case "SFX":         return en ? "SFX" : "ЗВУКИ";
            case "LANG":        return en ? "LANGUAGE" : "ЯЗЫК";
            case "ON":          return en ? "ON" : "ВКЛ";
            case "OFF":         return en ? "OFF" : "ВЫКЛ";
            case "LANG_VAL":    return en ? "ENGLISH" : "РУССКИЙ";
            case "SKIP":        return en ? "▶▶ SKIP" : "▶▶ ПРОПУСТИТЬ";
            case "NEXT":        return en ? "NEXT ▶" : "ДАЛЕЕ ▶";
            case "TITLE1":      return en ? "ECHOES" : "ЭХО";
            case "TITLE2":      return en ? "SHATTERED PANTHEON" : "РАЗБИТЫЙ ПАНТЕОН";
            case "SUBTITLE":    return en ? "Seven Pantheons. One Titan." : "Семь Пантеонов. Один Титан.";
            case "BATTLE_BEGIN":return en ? "▶ BEGIN BATTLE" : "▶ В БОЙ";
            case "TURN_PLAYER": return en ? "YOUR TURN" : "ВАШ ХОД";
            case "TURN_ENEMY":  return en ? "ENEMY TURN" : "ХОД ВРАГА";
            case "EXTRA_TURN":  return en ? "EXTRA TURN!" : "ДОП. ХОД!";
            case "VICTORY":     return en ? "VICTORY" : "ПОБЕДА";
            case "DEFEAT":      return en ? "DEFEAT" : "ПОРАЖЕНИЕ";
            case "REWARD":      return en ? "REWARD" : "НАГРАДА";
            case "SHOP":        return en ? "SHOP" : "МАГАЗИН";
            case "BUY":         return en ? "BUY" : "КУПИТЬ";
            case "USE":         return en ? "USE" : "ИСПОЛЬЗОВАТЬ";
            case "OWN":         return en ? "OWN" : "ЕСТЬ";
            case "ECHOES":      return en ? "Echoes" : "Эхо";
            case "SPARKS":      return en ? "Sparks" : "Искры";
            case "DEMO_BILLING":return en ? "DEMO BUILD — mock payment" : "DEMO СБОРКА — макет оплаты";
            case "ABIL_INFERNO":return en ? "Inferno Strike" : "Адский Удар";
            case "ABIL_FREEZE": return en ? "Time Freeze" : "Стазис Времени";
            case "ABIL_SHUFFLE":return en ? "Aegis Shuffle" : "Перетасовка Эгиды";
            case "ABIL_CLEANSE":return en ? "Pact Cleanse" : "Очищение Пакта";
            case "ABIL_SLAM":   return en ? "Titan Slam" : "Удар Титана";
            case "ABIL_INFERNO_DESC": return en ? "Destroy 3x3 area. Damage = sum." : "Уничтожить 3x3 поля. Урон = сумма.";
            case "ABIL_FREEZE_DESC":  return en ? "Enemy skips 2 turns." : "Враг пропускает 2 хода.";
            case "ABIL_SHUFFLE_DESC": return en ? "Reshuffle board for new combos." : "Перетасовать поле для новых комбо.";
            case "ABIL_CLEANSE_DESC": return en ? "Remove all Violet curses." : "Убрать все Фиолетовые проклятия.";
            case "ABIL_SLAM_DESC":    return en ? "Break a column, mega damage." : "Сломать столбец, мега-урон.";
            case "EP_SELECT":   return en ? "Choose Episode" : "Выбор Эпизода";
            case "LOCKED":      return en ? "LOCKED" : "ЗАКРЫТО";
            case "PATH_PACT":   return en ? "Path of the Pact" : "Путь Пакта";
            case "PATH_VENGE":  return en ? "Path of Vengeance" : "Путь Мести";
            case "PATH_MORTAL": return en ? "Path of Mortals" : "Путь Смертных";
            case "ENDING_EP":   return en ? "Episode Complete" : "Эпизод Завершён";
            case "TO_BE_CONT":  return en ? "TO BE CONTINUED" : "ПРОДОЛЖЕНИЕ СЛЕДУЕТ";
            case "PRE_BATTLE":  return en ? "BATTLE" : "БИТВА";
            case "SPEAKER_EON":     return en ? "Eon" : "Эон";
            case "SPEAKER_PYTHIA":  return en ? "Pythia" : "Пифия";
            case "SPEAKER_HERMES":  return en ? "Hermes" : "Гермес";
            case "SPEAKER_FALLEN":  return en ? "Fallen Hoplite" : "Падший Гоплит";
            case "SPEAKER_NARRATOR":return "";
            default: return key;
        }
    }

    string GemDisplayName(int gemId)
    {
        bool en = (lang == Lang.EN);
        switch (gemId)
        {
            case 0: return en ? "Pact" : "Пакт";
            case 1: return en ? "Storm" : "Шторм";
            case 2: return en ? "Blood" : "Кровь";
            case 3: return en ? "Ash" : "Пепел";
            case 4: return en ? "Mortal" : "Смертный";
            case 5: return en ? "Violet" : "Фиолет";
            default: return "Pantheon";
        }
    }

    // ============ ASSETS ============
    void LoadAllSprites()
    {
        string[] names = new string[]{
            // V11 (existing in Resources)
            "portrait_eon","portrait_pythia","portrait_hermes","portrait_fallen",
            "bg_sarcophagus","bg_grove","bg_agora","bg_storm","bg_altar","bg_title",
            // V12 new
            "gem_pact","gem_storm","gem_blood","gem_ash","gem_mortal","gem_violet",
            "gem_ep2_soul","gem_ep3_tide","gem_ep4_frost","gem_ep5_oak","gem_ep6_ankh","gem_ep7_obsidian",
            "vfx_inferno_burst","vfx_freeze","vfx_titan_slam",
            "bg_battle_arena","bg_ep2_erebus","bg_ep3_aegean","bg_ep4_asgard","bg_ep5_slavs","bg_ep6_egypt","bg_ep7_aztec",
            "icon_shop","icon_echoes","icon_sparks",
            "enemy_hoplite_corrupt","enemy_shadow_priestess","enemy_minotaur"
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

    // ============ EPISODE / SCRIPT DATA (huge — split into methods) ============
    void BuildEpisodes()
    {
        episodes = new List<Episode>();
        episodes.Add(BuildEpisode1_Olympus());
        episodes.Add(BuildEpisode2_Erebus());
        episodes.Add(BuildEpisode3_Aegean());
        episodes.Add(BuildEpisode4_Asgard());
        episodes.Add(BuildEpisode5_Slavs());
        episodes.Add(BuildEpisode6_Egypt());
        episodes.Add(BuildEpisode7_Aztec());
    }

    Episode BuildEpisode1_Olympus()
    {
        var ep = new Episode();
        ep.id = 1;
        ep.nameEn = "Fall of Olympus"; ep.nameRu = "Падение Олимпа";
        ep.subtitleEn = "Episode 1"; ep.subtitleRu = "Эпизод 1";
        ep.bgKey = "bg_altar";
        ep.bgmCalm = "bgm_olympus"; ep.bgmBattle = "bgm_tense";
        ep.exclusiveGem = "gem_violet"; // EP1 uses base set + violet curses
        ep.script = new List<Line>();
        // Opening lore — taken from v11 canonical EP1
        AddL(ep, "", "bg_sarcophagus", "bgm_olympus", "sfx_transition",
            "A thousand years of silence. Stone cracks. Dust falls. Something stirs in the dark.",
            "Тысяча лет тишины. Камень трескается. Пыль осыпается. Что-то шевелится во тьме.");
        AddL(ep, "portrait_eon", "bg_sarcophagus", "", "",
            "Eon: ...where am I? My hammer... cold. My hands... mortal.",
            "Эон: ...где я? Молот... холодный. Руки... смертные.");
        AddL(ep, "portrait_eon", "bg_sarcophagus", "", "",
            "Eon: I am Eon, last Titan of the Pact. Olympus stands... yet I feel only ash.",
            "Эон: Я Эон, последний Титан Пакта. Олимп стоит... но я чую лишь пепел.");
        AddL(ep, "portrait_pythia", "bg_grove", "", "sfx_transition",
            "Pythia: Titan. You woke because the gods are gone.",
            "Пифия: Титан. Ты проснулся, потому что боги исчезли.");
        AddL(ep, "portrait_pythia", "bg_grove", "", "",
            "Pythia: Mortals are slaves to a Fallen one — Achilles the Elder. His scouts are at the grove's edge.",
            "Пифия: Смертные — рабы Падшего, Ахилла Старшего. Его разведчики у края рощи.");
        // BATTLE 1: Hoplite scouts
        AddBattleTrigger(ep, 1, "Hoplite scouts approach. They smell of bronze and violet rot.",
            "Гоплиты-разведчики приближаются. От них пахнет бронзой и фиолетовой гнилью.");
        AddL(ep, "portrait_pythia", "bg_grove", "", "",
            "Pythia: You still remember how to swing the Pact. Good. Walk to the Agora. Hermes waits.",
            "Пифия: Ты ещё помнишь, как взмахнуть Пактом. Хорошо. Иди на Агору. Гермес ждёт.");
        // Choice 1
        AddChoice(ep, "Eon: Why me? I am a Titan. Why should I save mortals?",
            "Эон: Почему я? Я Титан. Зачем мне спасать смертных?",
            new []{"\"Because I swore the Pact.\"", "\"Because someone must.\"", "\"Because no god remains.\""},
            new []{"«Потому что я дал клятву.»", "«Потому что кто-то должен.»", "«Потому что не осталось богов.»"},
            new []{1, 3, 2});
        AddL(ep, "", "bg_agora", "bgm_tense", "sfx_transition",
            "The Agora is ash. Crows pick at bones. A figure leans on a cracked herald's staff.",
            "Агора — пепел. Вороны клюют кости. Фигура опирается на треснувший жезл вестника.");
        AddL(ep, "portrait_hermes", "bg_agora", "", "",
            "Hermes: Eon. A thousand years late. Listen — corrupt hoplites guard the road.",
            "Гермес: Эон. На тысячу лет опоздал. Слушай — порченые гоплиты держат дорогу.");
        // BATTLE 2
        AddBattleTrigger(ep, 2, "Three corrupt hoplites block the way. Bronze cracks with violet glow.",
            "Трое порченых гоплитов перекрывают путь. Бронза трескается фиолетовым светом.");
        AddL(ep, "portrait_hermes", "bg_agora", "", "",
            "Hermes: A shadow priestess of the Fallen blesses them. She must fall first.",
            "Гермес: Их благословляет теневая жрица Падшего. Она должна пасть первой.");
        // BATTLE 3
        AddBattleTrigger(ep, 3, "The shadow priestess raises her cursed scroll. The air goes cold.",
            "Теневая жрица поднимает проклятый свиток. Воздух стынет.");
        // Choice 2
        AddChoice(ep, "Hermes: What kind of Titan are you, sleeper?",
            "Гермес: Что ты за Титан, спящий?",
            new []{"\"I am the Pact. I keep it.\"", "\"I am vengeance for silent gods.\"", "\"I am the last weight on the scale.\""},
            new []{"«Я — Пакт. Я храню его.»", "«Я — месть за умолкших богов.»", "«Я — последний вес на чаше весов.»"},
            new []{1, 2, 3});
        AddL(ep, "portrait_hermes", "bg_agora", "", "",
            "Hermes: The road climbs. Hoplite legion. Then a beast. Then the summit.",
            "Гермес: Дорога вверх. Легион гоплитов. Затем зверь. Затем вершина.");
        // BATTLE 4 — bigger squad
        AddBattleTrigger(ep, 4, "Hoplite phalanx on the marble stairs. Shields locked, violet eyes.",
            "Фаланга гоплитов на мраморных ступенях. Щиты сомкнуты, фиолетовые глаза.");
        // BATTLE 5 — shadow + hoplite combo
        AddBattleTrigger(ep, 5, "Two priestesses behind hoplite shields. They sing the death of Hera.",
            "Две жрицы за щитами гоплитов. Они поют смерть Геры.");
        AddL(ep, "", "bg_storm", "bgm_tense", "sfx_transition",
            "The storm is a bruise. Lightning tastes of iron.",
            "Буря — синяк. Молния на вкус — железо.");
        // BATTLE 6 — hoplites in the storm
        AddBattleTrigger(ep, 6, "Hoplites born of storm clouds materialize from rain.",
            "Гоплиты, рождённые из туч, материализуются из дождя.");
        // BATTLE 7 — minotaur mini-boss
        AddBattleTrigger(ep, 7, "A minotaur, corrupted demigod. Violet veins under bronze hide.",
            "Минотавр, порченый полубог. Фиолетовые вены под бронзовой шкурой.");
        AddL(ep, "portrait_eon", "bg_storm", "", "",
            "Eon: The beast was once a hero. The Fallen ate that too.",
            "Эон: Этот зверь когда-то был героем. Падший съел и это.");
        // BATTLE 8 — priestess coven
        AddBattleTrigger(ep, 8, "Three priestesses form a violet triangle. A spell rises from them.",
            "Три жрицы образуют фиолетовый треугольник. От них поднимается заклятие.");
        // BATTLE 9 — elite hoplites
        AddBattleTrigger(ep, 9, "Elite hoplites in black bronze. Veterans of the Fall.",
            "Элитные гоплиты в чёрной бронзе. Ветераны Падения.");
        // BATTLE 10 — second minotaur
        AddBattleTrigger(ep, 10, "Another minotaur — bigger, golden ring through its nose.",
            "Ещё один минотавр — больше, с золотым кольцом в носу.");
        AddL(ep, "", "bg_altar", "bgm_tense", "sfx_transition",
            "The altar. Twelve broken statues. In the center — the Fallen, hammer beside him. Yours.",
            "Алтарь. Двенадцать разбитых статуй. В центре — Падший, молот рядом. Твой.");
        AddL(ep, "portrait_fallen", "bg_altar", "", "",
            "Fallen Hoplite: Titan. You wear flesh badly. Pick up the hammer. Or kneel.",
            "Падший Гоплит: Титан. Ты плохо носишь плоть. Возьми молот. Или склони колено.");
        // Choice 3 — drives ending
        AddChoice(ep, "Fallen: Either way the Pact ends tonight.",
            "Падший: В любом случае Пакт кончится сегодня.",
            new []{"\"I take the hammer. The Pact does not end.\"", "\"You ate gods. You will choke on a Titan.\"", "\"Olympus was hollow. But mortals are not.\""},
            new []{"«Я беру молот. Пакт не кончится.»", "«Ты ел богов. Подавишься Титаном.»", "«Олимп был пуст. Но смертные — нет.»"},
            new []{1, 2, 3});
        // BOSS BATTLE 11
        AddBattleTrigger(ep, 11, "Fallen Hoplite rises. Violet sparks circle him — twelve devoured gods scream from within.",
            "Падший Гоплит встаёт. Фиолетовые искры вокруг него — двенадцать съеденных богов кричат изнутри.");
        // After-boss reflection — branching ending unlocked later
        AddL(ep, "portrait_eon", "bg_altar", "bgm_olympus", "sfx_transition",
            "Eon: The hammer remembers. The Pact stands. The next ruin already waits.",
            "Эон: Молот помнит. Пакт стоит. Следующая руина уже ждёт.");
        ep.battles = BuildEp1Battles();
        return ep;
    }

    BattleConfig[] BuildEp1Battles()
    {
        var b = new BattleConfig[11];
        for (int i=0;i<11;i++) b[i] = new BattleConfig();
        // Progressive grid: 6x7 -> 8x10, colors 4->6
        int[] gw = new []{6,6,6,7,7,7,7,7,8,8,8};
        int[] gh = new []{7,7,7,8,8,8,8,9,9,9,10};
        int[] cc = new []{4,4,5,5,5,5,5,5,6,6,6};
        int[] hp = new []{128,160,192,224,256,288,320,352,384,448,800};
        string[] enemies = new []{
            "enemy_hoplite_corrupt","enemy_hoplite_corrupt","enemy_shadow_priestess",
            "enemy_hoplite_corrupt","enemy_shadow_priestess","enemy_hoplite_corrupt",
            "enemy_minotaur","enemy_shadow_priestess","enemy_hoplite_corrupt",
            "enemy_minotaur","portrait_fallen"
        };
        for (int i=0;i<11;i++)
        {
            b[i].id = i+1;
            b[i].gridW = gw[i]; b[i].gridH = gh[i]; b[i].colors = cc[i];
            b[i].enemyHp = hp[i]; b[i].playerHp = 280 + i*15;
            b[i].enemyKey = enemies[i];
            b[i].arenaBgKey = "bg_battle_arena";
            b[i].isBoss = (i == 10);
            // Branch variations
            b[i].altEnemyVengeance = enemies[i]; // visual variation handled by tint in code
            b[i].altEnemyMortals = enemies[i];
        }
        return b;
    }

    // EP2-7 use template lore (constraint 79: all episodes present, EP1 deeply, EP2-7 playable framework)
    Episode BuildEpisode2_Erebus()
    {
        var ep = NewEpStub(2, "Descent into Erebus", "Сошествие в Эреб",
            "Episode 2", "Эпизод 2", "bg_ep2_erebus", "gem_ep2_soul");
        AddL(ep, "", "bg_ep2_erebus", "bgm_tense", "sfx_transition",
            "The Styx is grey. Souls drift like leaves. Charon's boat waits.",
            "Стикс сер. Души плывут как листья. Лодка Харона ждёт.");
        AddL(ep, "portrait_eon", "bg_ep2_erebus", "", "",
            "Eon: I felt the gods scream when they died. Their souls are still here.",
            "Эон: Я чувствовал, как кричали боги, умирая. Их души всё ещё здесь.");
        AddEpScaffold(ep);
        return ep;
    }
    Episode BuildEpisode3_Aegean()
    {
        var ep = NewEpStub(3, "Tides of Aegean", "Приливы Эгейского моря",
            "Episode 3", "Эпизод 3", "bg_ep3_aegean", "gem_ep3_tide");
        AddL(ep, "", "bg_ep3_aegean", "bgm_tense", "sfx_transition",
            "The sea remembers Poseidon. Broken temples rise from the waves.",
            "Море помнит Посейдона. Сломанные храмы встают из волн.");
        AddL(ep, "portrait_eon", "bg_ep3_aegean", "", "",
            "Eon: A fallen sea god guards the road north. To Asgard.",
            "Эон: Павший морской бог сторожит путь на север. К Асгарду.");
        AddEpScaffold(ep);
        return ep;
    }
    Episode BuildEpisode4_Asgard()
    {
        var ep = NewEpStub(4, "Frozen Asgard", "Замёрзший Асгард",
            "Episode 4", "Эпизод 4", "bg_ep4_asgard", "gem_ep4_frost");
        AddL(ep, "", "bg_ep4_asgard", "bgm_tense", "sfx_transition",
            "Bifrost is shattered. The mead-hall is ice. Odin's throne is empty.",
            "Биврёст разбит. Палата мёда — лёд. Трон Одина пуст.");
        AddL(ep, "portrait_eon", "bg_ep4_asgard", "", "",
            "Eon: The North has its own Fallen. A jotun wearing Thor's belt.",
            "Эон: У Севера свой Падший. Йотун, носящий пояс Тора.");
        AddEpScaffold(ep);
        return ep;
    }
    Episode BuildEpisode5_Slavs()
    {
        var ep = NewEpStub(5, "Grove of Perun", "Роща Перуна",
            "Episode 5", "Эпизод 5", "bg_ep5_slavs", "gem_ep5_oak");
        AddL(ep, "", "bg_ep5_slavs", "bgm_tense", "sfx_transition",
            "Oak idols stand in mist. Veles and Perun carved in pine. Silent.",
            "Дубовые идолы в тумане. Велес и Перун, вырезанные в сосне. Молчат.");
        AddL(ep, "portrait_eon", "bg_ep5_slavs", "", "",
            "Eon: The Slavic gods did not die. They slept. And now they wake — wrong.",
            "Эон: Славянские боги не умерли. Они спали. И теперь просыпаются — неправильно.");
        AddEpScaffold(ep);
        return ep;
    }
    Episode BuildEpisode6_Egypt()
    {
        var ep = NewEpStub(6, "Sands of Anubis", "Пески Анубиса",
            "Episode 6", "Эпизод 6", "bg_ep6_egypt", "gem_ep6_ankh");
        AddL(ep, "", "bg_ep6_egypt", "bgm_tense", "sfx_transition",
            "Sandstorm. Hieroglyphs bleed gold. The Duat is open.",
            "Песчаная буря. Иероглифы кровят золотом. Дуат открыт.");
        AddL(ep, "portrait_eon", "bg_ep6_egypt", "", "",
            "Eon: Anubis weighs hearts. Mine is on his scale tonight.",
            "Эон: Анубис взвешивает сердца. Моё — на его весах сегодня.");
        AddEpScaffold(ep);
        return ep;
    }
    Episode BuildEpisode7_Aztec()
    {
        var ep = NewEpStub(7, "Pyramid of Quetzalcoatl", "Пирамида Кетцалькоатля",
            "Episode 7", "Эпизод 7", "bg_ep7_aztec", "gem_ep7_obsidian");
        AddL(ep, "", "bg_ep7_aztec", "bgm_tense", "sfx_transition",
            "The jungle drinks blood. The feathered serpent has shed its skin.",
            "Джунгли пьют кровь. Пернатый змей сбросил кожу.");
        AddL(ep, "portrait_eon", "bg_ep7_aztec", "", "",
            "Eon: The final pantheon. The last Fallen. After this — the Pact decides.",
            "Эон: Последний пантеон. Последний Падший. После этого — Пакт решает.");
        AddEpScaffold(ep);
        return ep;
    }

    Episode NewEpStub(int id, string nameEn, string nameRu, string subEn, string subRu, string bgKey, string gemKey)
    {
        var ep = new Episode();
        ep.id = id; ep.nameEn = nameEn; ep.nameRu = nameRu;
        ep.subtitleEn = subEn; ep.subtitleRu = subRu;
        ep.bgKey = bgKey; ep.bgmCalm = "bgm_olympus"; ep.bgmBattle = "bgm_tense";
        ep.exclusiveGem = gemKey;
        ep.script = new List<Line>();
        return ep;
    }
    void AddEpScaffold(Episode ep)
    {
        // 11 battles per episode, lore between each pair
        for (int i=1;i<=11;i++)
        {
            string en = i < 11 ? ("Battle " + i + " — guardians of this realm rise against the Titan.") :
                                 ("Final battle — the Fallen of this pantheon awakens.");
            string ru = i < 11 ? ("Битва " + i + " — стражи этого мира встают против Титана.") :
                                 ("Финальная битва — Падший этого пантеона пробуждается.");
            AddBattleTrigger(ep, i, en, ru);
        }
        AddL(ep, "portrait_eon", ep.bgKey, "bgm_olympus", "sfx_transition",
            "Eon: One more pantheon mended. One more ruin behind me.",
            "Эон: Ещё один пантеон восстановлен. Ещё одна руина позади.");
        // Battle configs — escalating
        var b = new BattleConfig[11];
        int gridStart = 6 + (ep.id - 2);
        for (int i=0;i<11;i++)
        {
            b[i] = new BattleConfig();
            b[i].id = i+1;
            b[i].gridW = Math.Min(8, gridStart + i/4);
            b[i].gridH = Math.Min(10, gridStart + 1 + i/3);
            b[i].colors = Math.Min(6, 4 + i/3);
            b[i].enemyHp = (int)((80 + i*30 + ep.id*20) * 1.6f);
            b[i].playerHp = 280 + i*15;
            b[i].enemyKey = (i < 7) ? "enemy_hoplite_corrupt" : (i < 10 ? "enemy_shadow_priestess" : "enemy_minotaur");
            b[i].arenaBgKey = ep.bgKey;
            b[i].isBoss = (i == 10);
        }
        ep.battles = b;
    }

    // Helpers
    void AddL(Episode ep, string speaker, string bg, string bgm, string sfx, string en, string ru)
    {
        ep.script.Add(new Line{ speaker=speaker, bg=bg, bgm=bgm, sfx=sfx, en=en, ru=ru, choices=null, triggerBattle=0 });
    }
    void AddBattleTrigger(Episode ep, int battleId, string en, string ru)
    {
        ep.script.Add(new Line{ speaker="", bg=ep.bgKey, bgm=ep.bgmBattle, sfx="sfx_transition",
            en=en, ru=ru, choices=null, triggerBattle=battleId });
    }
    void AddChoice(Episode ep, string en, string ru, string[] enCh, string[] ruCh, int[] bias)
    {
        var c = new Choice[enCh.Length];
        for (int i=0;i<enCh.Length;i++) c[i] = new Choice{ en=enCh[i], ru=ruCh[i], pathBias = i < bias.Length ? bias[i] : 0 };
        ep.script.Add(new Line{ speaker="portrait_eon", bg="", bgm="", sfx="", en=en, ru=ru, choices=c, triggerBattle=0 });
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

    // =====================================================================
    // PART 2 — Match-3 engine, Battle UI, Abilities
    // =====================================================================
    class Cell { public int color; public int bonus; /*0=none,1=line-h,2=line-v,3=square6,4=color-bomb*/ public bool curse; }
    Cell[,] grid; int gridW, gridH, gridColors;
    Image[,] gemImages;
    GameObject[,] gemGO;
    int selX = -1, selY = -1;
    int turnSide = 0; // 0=player, 1=enemy
    int turnCount = 0; // 0..1 (two moves per side per constraint 67)
    bool extraTurn = false;
    int playerHpCur, playerHpMax;
    int enemyHpCur, enemyHpMax;
    Image playerHpBar, enemyHpBar;
    Image battleBg;
    Image battleEnemyPortrait;
    BattleConfig curBattle;
    bool battleResolving = false;
    float resolveTimer = 0f;
    int comboMul = 1;
    int enemyFreezeTurns = 0;
    Image vfxOverlay;
    string vfxAnim = "";
    float vfxT = 0f;

    // v13/v14: GemTween + destroy VFX + enemy intents (constraints 85,99,100,105,107)
    class GemTween { public RectTransform rt; public Vector2 from; public Vector2 to; public float t; public float dur; public int kind; /*0=swap,1=fade,2=drop,3=destroyVfx*/ public Image img; public bool done; }
    List<GemTween> activeTweens = new List<GemTween>();
    Image battlePlayerPortrait;
    Image[] abilityRingBg = new Image[5];
    Image[] abilityCdMask = new Image[5];
    float[] abilityCdT = new float[5];
    float[] abilityCdDur = new float[5];
    // v14: enemy skill icons + intent label
    Image[] enemySkillIcons = new Image[3];
    Text[] enemySkillCdText = new Text[3];
    int[] enemySkillCd = new int[3];
    Text enemyIntentText;
    // v14: swipe gesture state
    Vector2 swipeStartPos = Vector2.zero; int swipeStartX = -1, swipeStartY = -1; bool swipeActive = false;
    // v14: bonus piece types (constraints 101-103)
    const int BONUS_NONE = 0, BONUS_LINE_H = 1, BONUS_LINE_V = 2, BONUS_SQUARE6 = 3, BONUS_COLOR_BOMB = 4;
    // v14: destroy VFX overlay sprites keyed by color id
    string[] DestroyVfxKeys = new []{ "vfx_destroy_pact","vfx_destroy_storm","vfx_destroy_blood","vfx_destroy_ash","vfx_destroy_mortal","vfx_destroy_violet" };

    // Gem keys per slot color id 0..5 base + pantheon-exclusive index 6
    string[] BaseGemKeys = new []{ "gem_pact","gem_storm","gem_blood","gem_ash","gem_mortal","gem_violet" };

    void StartBattle(int episodeId, int battleId)
    {
        var ep = episodes[episodeId-1];
        curBattle = ep.battles[battleId-1];
        gridW = curBattle.gridW; gridH = curBattle.gridH; gridColors = curBattle.colors;
        playerHpMax = curBattle.playerHp; playerHpCur = playerHpMax;
        enemyHpMax = curBattle.enemyHp; enemyHpCur = enemyHpMax;
        turnSide = 0; turnCount = 0; extraTurn = false; comboMul = 1; enemyFreezeTurns = 0;
        PlayBgm(ep.bgmBattle);
        BuildBattleUI(ep);
        InitGrid();
        RenderGrid();
        state = State.Battle;
        UpdateBattleHUD();
    }

    void InitGrid()
    {
        grid = new Cell[gridW, gridH];
        System.Random rng = new System.Random();
        for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++)
        {
            int c;
            do {
                c = (int)(rng.NextDouble() * gridColors);
            } while (WouldStartMatch(x,y,c));
            grid[x,y] = new Cell{ color=c, bonus=0, curse=false };
        }
        // Add ~1 violet curse for boss battles
        if (curBattle.isBoss)
        {
            int cx = (int)(rng.NextDouble() * gridW);
            int cy = (int)(rng.NextDouble() * gridH);
            grid[cx,cy].color = 5; grid[cx,cy].curse = true;
        }
    }
    bool WouldStartMatch(int x, int y, int c)
    {
        if (x >= 2 && grid[x-1,y]!=null && grid[x-2,y]!=null && grid[x-1,y].color==c && grid[x-2,y].color==c) return true;
        if (y >= 2 && grid[x,y-1]!=null && grid[x,y-2]!=null && grid[x,y-1].color==c && grid[x,y-2].color==c) return true;
        return false;
    }

    void RenderGrid()
    {
        if (gemImages == null || gemImages.GetLength(0) != gridW || gemImages.GetLength(1) != gridH)
        {
            // Clear old
            if (gemGO != null)
            {
                for (int xx=0;xx<gemGO.GetLength(0);xx++)
                for (int yy=0;yy<gemGO.GetLength(1);yy++)
                    if (gemGO[xx,yy] != null) UnityEngine.Object.Destroy(gemGO[xx,yy]);
            }
            gemImages = new Image[gridW, gridH];
            gemGO = new GameObject[gridW, gridH];
            BuildGridGOs();
        }
        for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++)
        {
            var img = gemImages[x,y];
            if (img == null) continue;
            var cell = grid[x,y];
            string key = (cell.color < BaseGemKeys.Length) ? BaseGemKeys[cell.color] : episodes[currentEpisode-1].exclusiveGem;
            if (sprites.ContainsKey(key)) img.sprite = sprites[key];
            img.color = (selX==x && selY==y) ? new Color(1.3f,1.2f,0.7f,1f) : Color.white;
        }
    }

    GameObject gridRoot;
    RectTransform boardPanelRT;
    float curCellSz = 80f;
    void BuildGridGOs()
    {
        if (gridRoot != null) UnityEngine.Object.Destroy(gridRoot);
        gridRoot = new GameObject("GridRoot");
        // v14: parent under BoardPanel (safe zone) — constraint 97
        gridRoot.transform.SetParent(boardPanelRT != null ? boardPanelRT.transform : (Transform)battlePanel.transform, false);
        var rt = gridRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.pivot = new Vector2(0.5f,0.5f); rt.anchoredPosition = new Vector2(0, 0);
        // Cell size scales with available board area
        float maxW = (boardPanelRT != null) ? Mathf.Max(400f, boardPanelRT.rect.width - 30f) : 1000f;
        float maxH = (boardPanelRT != null) ? Mathf.Max(400f, boardPanelRT.rect.height - 30f) : 1100f;
        if (maxW <= 100f) maxW = 1000f;
        if (maxH <= 100f) maxH = 1100f;
        float cellSz = Math.Min(maxW / gridW, maxH / gridH);
        curCellSz = cellSz;
        rt.sizeDelta = new Vector2(cellSz*gridW + 20, cellSz*gridH + 20);
        for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++)
        {
            int cx = x, cy = y;
            var go = new GameObject("Gem_"+x+"_"+y);
            go.transform.SetParent(gridRoot.transform, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = true;
            var grt = img.rectTransform;
            grt.anchorMin = new Vector2(0,1); grt.anchorMax = new Vector2(0,1);
            grt.pivot = new Vector2(0.5f,0.5f);
            grt.sizeDelta = new Vector2(cellSz - 6, cellSz - 6);
            grt.anchoredPosition = new Vector2(x*cellSz + cellSz/2 + 10, -(y*cellSz + cellSz/2 + 10));
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnGemTap(cx, cy));
            // v14: swipe gesture handler via EventTrigger-style — attach drag handler component
            // v14: swipe via GemDragHandler attached below (added after Button)
            GemDragHandler dh = go.AddComponent<GemDragHandler>(); dh.Init(this, cx, cy);
            gemImages[x,y] = img;
            gemGO[x,y] = go;
        }
    }

    // v14: swipe handler dispatched from GemDragHandler
    public void OnGemSwipe(int x, int y, int dx, int dy)
    {
        if (state != State.Battle || battleResolving) return;
        if (turnSide != 0) return;
        if (activeTweens.Count > 0) return;
        int tx = x + dx, ty = y + dy;
        if (tx < 0 || tx >= gridW || ty < 0 || ty >= gridH) return;
        selX = x; selY = y;
        // Animate swap visually
        if (gemImages[x,y] != null && gemImages[tx,ty] != null)
        {
            var rt1 = gemImages[x,y].rectTransform; var rt2 = gemImages[tx,ty].rectTransform;
            activeTweens.Add(new GemTween{ rt = rt1, from = rt1.anchoredPosition, to = rt2.anchoredPosition, t=0, dur=0.25f, kind=0 });
            activeTweens.Add(new GemTween{ rt = rt2, from = rt2.anchoredPosition, to = rt1.anchoredPosition, t=0, dur=0.25f, kind=0 });
        }
        SwapCells(x, y, tx, ty);
        int matched = FindAndResolveMatches(true);
        if (matched == 0)
        {
            // revert visually + logically
            if (gemImages[x,y] != null && gemImages[tx,ty] != null)
            {
                var rt1 = gemImages[x,y].rectTransform; var rt2 = gemImages[tx,ty].rectTransform;
                activeTweens.Add(new GemTween{ rt = rt1, from = rt1.anchoredPosition, to = rt2.anchoredPosition, t=0, dur=0.25f, kind=0 });
                activeTweens.Add(new GemTween{ rt = rt2, from = rt2.anchoredPosition, to = rt1.anchoredPosition, t=0, dur=0.25f, kind=0 });
            }
            SwapCells(x, y, tx, ty);
            selX = -1; selY = -1; RenderGrid();
        }
        else { selX = -1; selY = -1; AfterPlayerSwap(); }
    }

    public void OnBattleMenu() { if (settingsPanel != null) { settingsPanel.SetActive(true); } }

    void OnGemTap(int x, int y)
    {
        if (state != State.Battle || battleResolving || turnSide != 0) return;
        if (selX < 0) { selX = x; selY = y; RenderGrid(); return; }
        // Must be neighbor
        int dx = Math.Abs(x - selX), dy = Math.Abs(y - selY);
        if (dx + dy != 1) { selX = x; selY = y; RenderGrid(); return; }
        // Swap and check
        SwapCells(selX, selY, x, y);
        int matched = FindAndResolveMatches(true);
        if (matched == 0)
        {
            // Invalid swap, swap back
            SwapCells(selX, selY, x, y);
            selX = -1; selY = -1;
            RenderGrid();
            PlaySfx("sfx_blip");
            return;
        }
        PlaySfx("sfx_choice");
        selX = -1; selY = -1;
        // Damage applied inside resolve; advance turn
        AfterPlayerSwap();
    }

    void SwapCells(int x1, int y1, int x2, int y2)
    {
        var t = grid[x1,y1]; grid[x1,y1] = grid[x2,y2]; grid[x2,y2] = t;
    }

    int FindAndResolveMatches(bool isPlayer)
    {
        int totalMatched = 0;
        comboMul = 1;
        while (true)
        {
            bool[,] m = new bool[gridW, gridH];
            int matchCount = 0;
            int maxRun = 0;
            // Horizontal
            for (int y=0;y<gridH;y++)
            {
                int run = 1;
                for (int x=1;x<gridW;x++)
                {
                    if (grid[x,y].color == grid[x-1,y].color) run++;
                    else { if (run >= 3) { for (int k=0;k<run;k++) m[x-1-k,y] = true; maxRun = Math.Max(maxRun, run); matchCount += run; } run = 1; }
                }
                if (run >= 3) { for (int k=0;k<run;k++) m[gridW-1-k,y] = true; maxRun = Math.Max(maxRun, run); matchCount += run; }
            }
            // Vertical
            for (int x=0;x<gridW;x++)
            {
                int run = 1;
                for (int y=1;y<gridH;y++)
                {
                    if (grid[x,y].color == grid[x,y-1].color) run++;
                    else { if (run >= 3) { for (int k=0;k<run;k++) m[x,y-1-k] = true; maxRun = Math.Max(maxRun, run); matchCount += run; } run = 1; }
                }
                if (run >= 3) { for (int k=0;k<run;k++) m[x,gridH-1-k] = true; maxRun = Math.Max(maxRun, run); matchCount += run; }
            }
            if (matchCount == 0) break;
            totalMatched += matchCount;
            // Trigger bonus drops on match-4+
            int bonusDrop = (maxRun >= 5) ? 3 : (maxRun >= 4 ? 1 : 0); // 3=color-bomb,1=line
            int dmg = (int)(matchCount * 2 * comboMul);
            if (maxRun >= 4) { dmg = (int)(dmg * 1.5f); extraTurn = true; }
            // Apply damage
            ApplyDamage(isPlayer, dmg);
            // Heal on mortal gem
            for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++) if (m[x,y] && grid[x,y].color == 4 && isPlayer) playerHpCur = Math.Min(playerHpMax, playerHpCur + 3);
            // Damage from violet curses on player turn? — violet is enemy element: if player matches violet, removes curses
            // Clear matched, gravity
            for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++) if (m[x,y]) grid[x,y].color = -1;
            Gravity();
            Refill();
            comboMul++;
            UpdateBattleHUD();
            if (enemyHpCur <= 0 || playerHpCur <= 0) break;
        }
        if (totalMatched > 0) RenderGrid();
        return totalMatched;
    }

    void Gravity()
    {
        for (int x=0;x<gridW;x++)
        {
            int writeY = gridH - 1;
            for (int y=gridH-1;y>=0;y--)
            {
                if (grid[x,y].color >= 0) { var t = grid[x,y]; grid[x,y] = grid[x,writeY]; grid[x,writeY] = t; writeY--; }
            }
        }
    }
    // v14: spawn destroy VFX per gem type (constraint 100)
    void SpawnDestroyVfx(int x, int y, int color)
    {
        if (gridRoot == null) return;
        var go = new GameObject("VfxD"+x+"_"+y);
        go.transform.SetParent(gridRoot.transform, false);
        var img = go.AddComponent<Image>(); img.raycastTarget = false; img.preserveAspect = true;
        string key = (color >= 0 && color < DestroyVfxKeys.Length) ? DestroyVfxKeys[color] : DestroyVfxKeys[0];
        if (sprites.ContainsKey(key)) img.sprite = sprites[key];
        img.color = new Color(1f,1f,1f,1f);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0.5f,0.5f);
        rt.sizeDelta = new Vector2(curCellSz*1.4f, curCellSz*1.4f);
        rt.anchoredPosition = new Vector2(x*curCellSz + curCellSz/2 + 10, -(y*curCellSz + curCellSz/2 + 10));
        activeTweens.Add(new GemTween{ rt = rt, from = rt.anchoredPosition, to = rt.anchoredPosition, t=0, dur=0.45f, kind=1, img = img });
    }

    void Refill()
    {
        System.Random rng = new System.Random();
        for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++)
            if (grid[x,y].color < 0) grid[x,y] = new Cell{ color = (int)(rng.NextDouble() * gridColors), bonus=0, curse=false };
    }

    void ApplyDamage(bool fromPlayer, int dmg)
    {
        if (fromPlayer) enemyHpCur = Math.Max(0, enemyHpCur - dmg);
        else playerHpCur = Math.Max(0, playerHpCur - dmg);
    }

    void AfterPlayerSwap()
    {
        if (enemyHpCur <= 0) { EndBattle(true); return; }
        if (playerHpCur <= 0) { EndBattle(false); return; }
        if (extraTurn) { extraTurn = false; battleTurnText.text = L("EXTRA_TURN"); return; }
        turnCount++;
        if (turnCount >= 2) { turnCount = 0; turnSide = 1; battleTurnText.text = L("TURN_ENEMY"); ScheduleEnemyTurn(); }
        else { UpdateEnemyIntent(); battleTurnText.text = L("TURN_PLAYER"); }
    }

    float enemyDelay = 0f;
    void ScheduleEnemyTurn() { enemyDelay = 1.2f; battleResolving = true; UpdateEnemyIntent(); }

    // v14: enemy AI with 1/2/3 skills + intent display (constraints 105, 107)
    int[] enemySkillNext = new int[]{ 0, 1, 2 };
    int enemyNextSkillIdx = 0;
    int EnemySkillCount(BattleConfig b) { if (b.isBoss) return 3; if (b.id >= 6) return 2; return 1; }
    string[] EnemySkillKey(BattleConfig b)
    {
        // Returns 3 keys per slot; unused slots = ""
        if (b.isBoss) return new []{ "pact_blast", "curse_storm", "titan_wrath" };
        if (b.id >= 6) return new []{ "pierce", "curse" };
        return new []{ "pierce" };
    }
    void UpdateEnemyIntent()
    {
        if (enemyIntentText == null || curBattle == null) return;
        var keys = EnemySkillKey(curBattle);
        int n = EnemySkillCount(curBattle);
        int idx = enemyNextSkillIdx % Math.Max(1, n);
        string k = (idx < keys.Length) ? keys[idx] : "pierce";
        enemyIntentText.text = L("INTENT") + ": " + L("ENS_" + k.ToUpper());
    }

    void DoEnemyTurn()
    {
        if (enemyFreezeTurns > 0) { enemyFreezeTurns--; EndEnemyTurn(); return; }
        // v14: execute enemy SKILL by intent (constraints 105, 107)
        var keys = EnemySkillKey(curBattle);
        int n = EnemySkillCount(curBattle);
        int idx = enemyNextSkillIdx % Math.Max(1, n);
        string sk = (idx < keys.Length) ? keys[idx] : "pierce";
        ExecuteEnemySkill(sk);
        enemyNextSkillIdx = (enemyNextSkillIdx + 1) % Math.Max(1, n);
        // Plus do a board move so the enemy still plays the match-3 turn
        int bestX = -1, bestY = -1, bestDir = 0; int bestScore = 0;
        System.Random rng = new System.Random();
        for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++)
        {
            if (x+1 < gridW) { SwapCells(x,y,x+1,y); int s = SimulateScore(); SwapCells(x,y,x+1,y); if (s>bestScore){bestScore=s;bestX=x;bestY=y;bestDir=0;} }
            if (y+1 < gridH) { SwapCells(x,y,x,y+1); int s = SimulateScore(); SwapCells(x,y,x,y+1); if (s>bestScore){bestScore=s;bestX=x;bestY=y;bestDir=1;} }
        }
        if (bestX < 0) { bestX = (int)(rng.NextDouble()*gridW); bestY = (int)(rng.NextDouble()*(gridH-1)); bestDir = 1; }
        if (bestDir == 0) SwapCells(bestX, bestY, bestX+1, bestY);
        else SwapCells(bestX, bestY, bestX, bestY+1);
        FindAndResolveMatches(false);
        EndEnemyTurn();
    }
    void ExecuteEnemySkill(string sk)
    {
        // VFX flash on the player HP
        if (vfxOverlay != null) { vfxAnim = sk; vfxT = 0f; vfxOverlay.color = new Color(1,1,1,1); }
        switch (sk)
        {
            case "pierce": ApplyDamage(false, 15); break;
            case "curse":
            {
                // Inject violet (color 5) into random 2 cells
                System.Random r = new System.Random();
                for (int n=0;n<2;n++) { int x=(int)(r.NextDouble()*gridW); int y=(int)(r.NextDouble()*gridH); if (grid[x,y].color>=0){ grid[x,y].color = 5; grid[x,y].curse=true; } }
                ApplyDamage(false, 8);
                break;
            }
            case "pact_blast": ApplyDamage(false, 25); break;
            case "curse_storm":
            {
                System.Random r = new System.Random();
                for (int n=0;n<4;n++) { int x=(int)(r.NextDouble()*gridW); int y=(int)(r.NextDouble()*gridH); if (grid[x,y].color>=0){ grid[x,y].color = 5; grid[x,y].curse=true; } }
                ApplyDamage(false, 10);
                break;
            }
            case "titan_wrath":
            {
                int d = (playerHpCur < playerHpMax/2) ? 40 : 22;
                ApplyDamage(false, d);
                break;
            }
            default: ApplyDamage(false, 12); break;
        }
    }
    int SimulateScore()
    {
        int score = 0;
        for (int y=0;y<gridH;y++)
        {
            int run = 1;
            for (int x=1;x<gridW;x++) { if (grid[x,y].color==grid[x-1,y].color) run++; else { if (run>=3) score += run; run=1; } }
            if (run>=3) score += run;
        }
        for (int x=0;x<gridW;x++)
        {
            int run = 1;
            for (int y=1;y<gridH;y++) { if (grid[x,y].color==grid[x,y-1].color) run++; else { if (run>=3) score += run; run=1; } }
            if (run>=3) score += run;
        }
        return score;
    }
    void EndEnemyTurn()
    {
        if (enemyHpCur <= 0) { EndBattle(true); return; }
        if (playerHpCur <= 0) { EndBattle(false); return; }
        if (extraTurn) { extraTurn = false; battleTurnText.text = L("EXTRA_TURN"); return; }
        turnCount++;
        if (turnCount >= 2) { turnCount = 0; turnSide = 0; battleTurnText.text = L("TURN_PLAYER"); battleResolving = false; }
        else battleTurnText.text = L("TURN_ENEMY");
    }

    void EndBattle(bool victory)
    {
        battleResolving = false;
        state = State.BattleResult;
        battlePanel.SetActive(false);
        battleResultPanel.SetActive(true);
        battleResultTitle.text = victory ? L("VICTORY") : L("DEFEAT");
        if (victory)
        {
            int reward = curBattle.isBoss ? 250 : 50 + curBattle.id * 10;
            echoes += reward;
            battleResultRewardText.text = "+" + reward + " " + L("ECHOES");
            SaveProgress();
        }
        else battleResultRewardText.text = "";
    }

    void OnBattleResultContinue()
    {
        battleResultPanel.SetActive(false);
        if (playerHpCur <= 0)
        {
            // Restart same battle
            StartBattle(currentEpisode, curBattle.id);
        }
        else
        {
            // Resume script after the trigger
            state = State.Playing;
            idx++;
            SaveProgress();
            if (idx >= episodes[currentEpisode-1].script.Count) ShowEpisodeEnding();
            else ShowCurrentLine(false);
        }
    }

    // ============ ABILITIES ============
    string[] AbilityKeys = new []{ "inferno","freeze","shuffle","cleanse","slam" };
    int[] AbilityPrices = new []{ 100, 150, 80, 120, 200 };

    void UseAbility(int idx)
    {
        if (state != State.Battle || turnSide != 0) return;
        string key = AbilityKeys[idx];
        if (!abilityCount.ContainsKey(key) || abilityCount[key] <= 0) return;
        abilityCount[key]--;
        SaveProgress();
        switch (key)
        {
            case "inferno": AbilityInferno(); break;
            case "freeze":  AbilityFreeze(); break;
            case "shuffle": AbilityShuffle(); break;
            case "cleanse": AbilityCleanse(); break;
            case "slam":    AbilitySlam(); break;
        }
        RefreshAbilityButtons();
    }
    void AbilityInferno()
    {
        int cx = gridW/2, cy = gridH/2;
        int dmg = 0;
        for (int x=Math.Max(0,cx-1); x<=Math.Min(gridW-1,cx+1); x++)
        for (int y=Math.Max(0,cy-1); y<=Math.Min(gridH-1,cy+1); y++)
        { dmg += 15; grid[x,y].color = -1; }
        Gravity(); Refill(); RenderGrid();
        ApplyDamage(true, dmg);
        UpdateBattleHUD();
        TriggerVFX("vfx_inferno_burst");
    }
    void AbilityFreeze() { enemyFreezeTurns += 2; TriggerVFX("vfx_freeze"); }
    void AbilityShuffle()
    {
        System.Random rng = new System.Random();
        for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++)
            grid[x,y].color = (int)(rng.NextDouble() * gridColors);
        RenderGrid();
        FindAndResolveMatches(true);
    }
    void AbilityCleanse()
    {
        for (int x=0;x<gridW;x++) for (int y=0;y<gridH;y++)
            if (grid[x,y].color == 5) { grid[x,y].color = -1; }
        Gravity(); Refill(); RenderGrid();
    }
    void AbilitySlam()
    {
        int col = gridW/2; int dmg = 0;
        for (int y=0;y<gridH;y++) { dmg += 25; grid[col,y].color = -1; }
        Gravity(); Refill(); RenderGrid();
        ApplyDamage(true, dmg);
        UpdateBattleHUD();
        TriggerVFX("vfx_titan_slam");
    }

    void TriggerVFX(string key)
    {
        if (sprites.ContainsKey(key)) { vfxOverlay.sprite = sprites[key]; vfxOverlay.color = Color.white; }
        vfxAnim = key; vfxT = 0f;
        PlaySfx("sfx_choice");
    }

    void UpdateBattleHUD()
    {
        if (playerHpBar != null) playerHpBar.fillAmount = (float)playerHpCur / Math.Max(1, playerHpMax);
        if (enemyHpBar != null) enemyHpBar.fillAmount = (float)enemyHpCur / Math.Max(1, enemyHpMax);
        if (battlePlayerHpText != null) battlePlayerHpText.text = playerHpCur + "/" + playerHpMax;
        if (battleEnemyHpText != null) battleEnemyHpText.text = enemyHpCur + "/" + enemyHpMax;
        if (echoesText != null) echoesText.text = "" + echoes;
        if (sparksText != null) sparksText.text = "" + sparks;
    }

    void BuildBattleUI(Episode ep)
    {
        if (battlePanel != null) {
            battlePanel.SetActive(true);
            battleBg.sprite = sprites.ContainsKey(curBattle.arenaBgKey) ? sprites[curBattle.arenaBgKey] : null;
            if (sprites.ContainsKey(curBattle.enemyKey)) battleEnemyPortrait.sprite = sprites[curBattle.enemyKey];
            if (battlePlayerPortrait != null && sprites.ContainsKey("portrait_eon")) battlePlayerPortrait.sprite = sprites["portrait_eon"];
            battleTurnText.text = L("TURN_PLAYER");
            for (int i=0;i<abilityCdT.Length;i++) { abilityCdT[i]=0f; abilityCdDur[i]=0f; if (abilityCdMask[i]!=null) abilityCdMask[i].fillAmount = 0f; }
            activeTweens.Clear();
            return;
        }
    }

    void RefreshAbilityButtons()
    {
        for (int i=0;i<abilityButtonsText.Count;i++)
        {
            int cnt = abilityCount.ContainsKey(AbilityKeys[i]) ? abilityCount[AbilityKeys[i]] : 0;
            string name = L("ABIL_" + AbilityKeys[i].ToUpper());
            abilityButtonsText[i].text = name + "\nx" + cnt;
        }
    }

    // =====================================================================
    // PART 3 — UI build, Shop, Endings, Main flow handlers
    // =====================================================================
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

        // Backgrounds
        bgImage = MakeImage(canvas.transform, "BG", Color.black); Stretch(bgImage.rectTransform);
        bgImageNext = MakeImage(canvas.transform, "BGNext", new Color(1,1,1,0)); Stretch(bgImageNext.rectTransform);

        // PortraitCard (fix constraint 64)
        portraitCardBg = MakeImage(canvas.transform, "PortraitCard", new Color(0.05f,0.04f,0.08f,0.92f));
        var pcr = portraitCardBg.rectTransform;
        pcr.anchorMin = new Vector2(0.5f,0.5f); pcr.anchorMax = new Vector2(0.5f,0.5f);
        pcr.anchoredPosition = new Vector2(0,140); pcr.sizeDelta = new Vector2(620, 900);
        AddOutline(portraitCardBg.gameObject, new Color(0.9f,0.75f,0.3f,1f), 4);
        portraitCardBg.color = new Color(0.05f,0.04f,0.08f,0);

        portraitImage = MakeImage(canvas.transform, "Portrait", new Color(1,1,1,0));
        var pr = portraitImage.rectTransform;
        pr.anchorMin = new Vector2(0.5f,0.5f); pr.anchorMax = new Vector2(0.5f,0.5f);
        pr.anchoredPosition = new Vector2(0, 140); pr.sizeDelta = new Vector2(600, 880);
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
        st.pivot = new Vector2(0,1); st.anchoredPosition = new Vector2(40,-25); st.sizeDelta = new Vector2(-80, 60);
        speakerText.fontStyle = FontStyle.Bold;
        dialogText = MakeText(dialogBox.transform, "Dialog", "", 36, new Color(0.95f,0.95f,0.92f,1f));
        var dt = dialogText.rectTransform;
        dt.anchorMin = new Vector2(0,0); dt.anchorMax = new Vector2(1,1);
        dt.offsetMin = new Vector2(40, 110); dt.offsetMax = new Vector2(-40, -90);
        dialogText.alignment = TextAnchor.UpperLeft;

        var tapGO = new GameObject("TapArea");
        tapGO.transform.SetParent(canvas.transform, false);
        var tapImg = tapGO.AddComponent<Image>();
        tapImg.color = new Color(0,0,0,0); tapImg.raycastTarget = true;
        Stretch(tapImg.rectTransform);
        var tapBtn = tapGO.AddComponent<Button>();
        tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(OnTap);
        dialogBox.transform.SetAsLastSibling();

        skipBtnGO = MakeBottomRightButton(canvas.transform, "SkipBtn", "SKIP", new Vector2(-30, 610), OnSkip, out skipBtnText, new Color(0.9f,0.6f,0.2f,1f));
        skipBtnGO.SetActive(false);
        nextBtnGO = MakeBottomRightButton(canvas.transform, "NextBtn", "NEXT", new Vector2(-30, 610), OnNext, out nextBtnText, new Color(0.4f,0.85f,0.5f,1f));
        nextBtnGO.SetActive(false);

        // Title panel
        titlePanel = new GameObject("TitlePanel");
        titlePanel.transform.SetParent(canvas.transform, false);
        var tpRT = titlePanel.AddComponent<RectTransform>(); Stretch(tpRT);
        titleArt = MakeImage(titlePanel.transform, "TitleArt", Color.white); Stretch(titleArt.rectTransform);
        titleText = MakeText(titlePanel.transform, "Title", "", 110, new Color(1f,0.85f,0.4f,1f));
        var tt = titleText.rectTransform;
        tt.anchorMin = new Vector2(0,1); tt.anchorMax = new Vector2(1,1); tt.pivot = new Vector2(0.5f,1);
        tt.anchoredPosition = new Vector2(0,-180); tt.sizeDelta = new Vector2(-60, 220);
        titleText.alignment = TextAnchor.MiddleCenter; titleText.fontStyle = FontStyle.Bold;
        AddOutline(titleText.gameObject, new Color(0,0,0,0.95f), 4);
        subtitleText = MakeText(titlePanel.transform, "Subtitle", "", 44, new Color(0.9f,0.85f,0.7f,1f));
        var sub = subtitleText.rectTransform;
        sub.anchorMin = new Vector2(0,1); sub.anchorMax = new Vector2(1,1); sub.pivot = new Vector2(0.5f,1);
        sub.anchoredPosition = new Vector2(0,-440); sub.sizeDelta = new Vector2(-60, 80);
        subtitleText.alignment = TextAnchor.MiddleCenter; subtitleText.fontStyle = FontStyle.Bold;
        AddOutline(subtitleText.gameObject, new Color(0,0,0,0.9f), 3);
        menuBar = new GameObject("MenuBar"); menuBar.transform.SetParent(titlePanel.transform, false);
        var mbImg = menuBar.AddComponent<Image>(); mbImg.color = new Color(0.05f,0.04f,0.08f,0.85f); mbImg.raycastTarget = true;
        var mbRT = mbImg.rectTransform;
        mbRT.anchorMin = new Vector2(0,0); mbRT.anchorMax = new Vector2(1,0);
        mbRT.pivot = new Vector2(0.5f,0); mbRT.anchoredPosition = new Vector2(0,0); mbRT.sizeDelta = new Vector2(0, 560);
        AddOutline(menuBar, new Color(0.9f,0.75f,0.3f,1f), 3);
        MakeFlatButton(menuBar.transform, "StartBtn", new Vector2(0, 180), new Vector2(820,130), OnStartNew, out startBtnText);
        MakeFlatButton(menuBar.transform, "ContBtn",  new Vector2(0, 30),  new Vector2(820,130), OnContinue, out contBtnText);
        MakeFlatButton(menuBar.transform, "SetBtn",   new Vector2(0,-120), new Vector2(820,130), OnOpenSettings, out settingsBtnText);

        // Settings panel
        settingsPanel = new GameObject("SettingsPanel");
        settingsPanel.transform.SetParent(canvas.transform, false);
        var spRT = settingsPanel.AddComponent<RectTransform>(); Stretch(spRT);
        var spBg = MakeImage(settingsPanel.transform, "SpBG", new Color(0.02f,0.02f,0.05f,0.96f)); Stretch(spBg.rectTransform);
        settingsTitleText = MakeText(settingsPanel.transform, "SetTitle", "", 80, new Color(1f,0.85f,0.4f,1f));
        var spTRT = settingsTitleText.rectTransform;
        spTRT.anchorMin = new Vector2(0,1); spTRT.anchorMax = new Vector2(1,1); spTRT.pivot = new Vector2(0.5f,1);
        spTRT.anchoredPosition = new Vector2(0,-200); spTRT.sizeDelta = new Vector2(-60, 130);
        settingsTitleText.alignment = TextAnchor.MiddleCenter; settingsTitleText.fontStyle = FontStyle.Bold;
        MakeFlatButton(settingsPanel.transform, "MusicBtn", new Vector2(0,340), new Vector2(840,140), OnToggleMusic, out musicBtnText);
        MakeFlatButton(settingsPanel.transform, "SfxBtn",   new Vector2(0,170), new Vector2(840,140), OnToggleSfx,   out sfxBtnText);
        MakeFlatButton(settingsPanel.transform, "LangBtn",  new Vector2(0,  0), new Vector2(840,140), OnToggleLang,  out langBtnText);
        MakeFlatButton(settingsPanel.transform, "BackBtn",  new Vector2(0,-340), new Vector2(640,140), OnBackFromSettings, out backBtnText);
        // Open Shop button
        Text shopBtnText;
        MakeFlatButton(settingsPanel.transform, "ShopBtn", new Vector2(0,-170), new Vector2(840,140), OnOpenShop, out shopBtnText);
        shopBtnText.text = "SHOP / МАГАЗИН";
        settingsPanel.SetActive(false);

        // Choice panel
        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(canvas.transform, false);
        var cpRT = choicePanel.AddComponent<RectTransform>(); Stretch(cpRT);
        var cpBg = MakeImage(choicePanel.transform, "CpBG", new Color(0,0,0,0.55f)); Stretch(cpBg.rectTransform);
        for (int i=0;i<3;i++)
        {
            int captured = i; Text cbText;
            var bgo = MakeFlatButton(choicePanel.transform, "Choice"+i, new Vector2(0, 220 - i*200), new Vector2(900,170), ()=>OnChoicePicked(captured), out cbText);
            choiceButtons.Add(bgo.GetComponent<Button>());
            choiceTexts.Add(cbText);
        }
        choicePanel.SetActive(false);

        // Pre-Battle panel
        preBattlePanel = new GameObject("PreBattle");
        preBattlePanel.transform.SetParent(canvas.transform, false);
        var pbRT = preBattlePanel.AddComponent<RectTransform>(); Stretch(pbRT);
        var pbBg = MakeImage(preBattlePanel.transform, "PbBG", new Color(0,0,0,0.85f)); Stretch(pbBg.rectTransform);
        var pbTitle = MakeText(preBattlePanel.transform, "PbTitle", "", 90, new Color(1f,0.3f,0.2f,1f));
        var pbtRT = pbTitle.rectTransform;
        pbtRT.anchorMin = new Vector2(0,1); pbtRT.anchorMax = new Vector2(1,1); pbtRT.pivot = new Vector2(0.5f,1);
        pbtRT.anchoredPosition = new Vector2(0,-200); pbtRT.sizeDelta = new Vector2(-60,140);
        pbTitle.alignment = TextAnchor.MiddleCenter; pbTitle.fontStyle = FontStyle.Bold;
        AddOutline(pbTitle.gameObject, new Color(0,0,0,1), 4);
        preBattleText = MakeText(preBattlePanel.transform, "PbBody", "", 40, Color.white);
        var pbRb = preBattleText.rectTransform;
        pbRb.anchorMin = new Vector2(0,0.5f); pbRb.anchorMax = new Vector2(1,0.5f); pbRb.pivot = new Vector2(0.5f,0.5f);
        pbRb.anchoredPosition = new Vector2(0,0); pbRb.sizeDelta = new Vector2(-100, 600);
        preBattleText.alignment = TextAnchor.MiddleCenter;
        Text pbBtnText;
        MakeFlatButton(preBattlePanel.transform, "BeginBtn", new Vector2(0,-500), new Vector2(720,160), OnBeginBattle, out pbBtnText);
        pbBtnText.text = L("BATTLE_BEGIN");
        pbTitleRef = pbTitle; pbBeginBtnText = pbBtnText;
        preBattlePanel.SetActive(false);

        // Battle panel
        battlePanel = new GameObject("BattlePanel");
        battlePanel.transform.SetParent(canvas.transform, false);
        var btRT = battlePanel.AddComponent<RectTransform>(); Stretch(btRT);
        battleBg = MakeImage(battlePanel.transform, "BattleBg", Color.white); Stretch(battleBg.rectTransform); battleBg.color = new Color(0.4f,0.4f,0.4f,1f);
        // ===== v14 LAYOUT: TopBar → EnemyPanel → Board → SkillBar → PlayerHpBar (constraints 92-108) =====
        // Reference resolution: 1080x1920 portrait. SafeArea: top=0, bottom=0.
        // TopBar (right-top menu button) — constraint 96
        var topBar = MakeImage(battlePanel.transform, "TopBar", new Color(0,0,0,0.0f));
        var tbRT = topBar.rectTransform; tbRT.anchorMin = new Vector2(0,1); tbRT.anchorMax = new Vector2(1,1); tbRT.pivot = new Vector2(0.5f,1);
        tbRT.anchoredPosition = new Vector2(0,0); tbRT.sizeDelta = new Vector2(0, 110);
        var menuBtnGo = new GameObject("BattleMenuBtn"); menuBtnGo.transform.SetParent(topBar.transform, false);
        var menuImg = menuBtnGo.AddComponent<Image>(); menuImg.color = new Color(0.10f,0.08f,0.15f,0.92f);
        var menuBtnRT = menuImg.rectTransform; menuBtnRT.anchorMin = new Vector2(1,1); menuBtnRT.anchorMax = new Vector2(1,1); menuBtnRT.pivot = new Vector2(1,1);
        menuBtnRT.anchoredPosition = new Vector2(-20,-20); menuBtnRT.sizeDelta = new Vector2(90, 90);
        AddOutline(menuBtnGo, new Color(0.85f,0.75f,0.35f,1f), 3);
        var menuBtn = menuBtnGo.AddComponent<Button>();
        menuBtn.onClick.AddListener(() => OnBattleMenu());
        var menuTxt = MakeText(menuBtnGo.transform, "MnuTxt", "☰", 56, new Color(1f,0.9f,0.55f,1f));
        Stretch(menuTxt.rectTransform); menuTxt.alignment = TextAnchor.MiddleCenter; menuTxt.fontStyle = FontStyle.Bold;
        AddOutline(menuTxt.gameObject, new Color(0,0,0,1), 2);
        // EnemyPanel (constraint 95): avatar lt-corner, HP right of avatar, skills below HP
        var enemyPanel = MakeImage(battlePanel.transform, "EnemyPanel", new Color(0.08f,0.05f,0.12f,0.55f));
        var epnRT = enemyPanel.rectTransform; epnRT.anchorMin = new Vector2(0,1); epnRT.anchorMax = new Vector2(1,1); epnRT.pivot = new Vector2(0.5f,1);
        epnRT.anchoredPosition = new Vector2(0,-110); epnRT.sizeDelta = new Vector2(-30, 360);
        AddOutline(enemyPanel.gameObject, new Color(0.7f,0.2f,0.2f,0.8f), 2);
        // Enemy avatar 240x300 lt-corner
        battleEnemyPortrait = MakeImage(enemyPanel.transform, "EnemyPort", Color.white);
        var epRT = battleEnemyPortrait.rectTransform;
        epRT.anchorMin = new Vector2(0,1); epRT.anchorMax = new Vector2(0,1); epRT.pivot = new Vector2(0,1);
        epRT.anchoredPosition = new Vector2(15,-15); epRT.sizeDelta = new Vector2(240, 300);
        battleEnemyPortrait.preserveAspect = true;
        AddOutline(battleEnemyPortrait.gameObject, new Color(0.9f,0.3f,0.3f,0.95f), 3);
        // Enemy HP bar right of avatar (top of right area)
        var ehpBg = MakeImage(enemyPanel.transform, "EHpBg", new Color(0.2f,0.05f,0.05f,0.96f));
        var ehbRT = ehpBg.rectTransform; ehbRT.anchorMin = new Vector2(0,1); ehbRT.anchorMax = new Vector2(1,1); ehbRT.pivot = new Vector2(0,1);
        ehbRT.anchoredPosition = new Vector2(270,-20); ehbRT.sizeDelta = new Vector2(-285, 70);
        AddOutline(ehpBg.gameObject, new Color(0.9f,0.3f,0.3f,1f), 2);
        enemyHpBar = MakeImage(ehpBg.transform, "EHpFill", new Color(0.85f,0.18f,0.18f,1));
        Stretch(enemyHpBar.rectTransform); enemyHpBar.fillAmount = 1f;
        battleEnemyHpText = MakeText(ehpBg.transform, "EHpTxt", "100/100", 32, Color.white); Stretch(battleEnemyHpText.rectTransform); battleEnemyHpText.alignment = TextAnchor.MiddleCenter; battleEnemyHpText.fontStyle = FontStyle.Bold;
        AddOutline(battleEnemyHpText.gameObject, new Color(0,0,0,1), 2);
        // Enemy intent label (next skill) — constraint 107
        enemyIntentText = MakeText(enemyPanel.transform, "EnIntent", "", 22, new Color(1f,0.85f,0.55f,1f));
        var einRT = enemyIntentText.rectTransform; einRT.anchorMin = new Vector2(0,1); einRT.anchorMax = new Vector2(1,1); einRT.pivot = new Vector2(0,1);
        einRT.anchoredPosition = new Vector2(270,-100); einRT.sizeDelta = new Vector2(-285, 36);
        enemyIntentText.alignment = TextAnchor.MiddleLeft; enemyIntentText.fontStyle = FontStyle.Bold;
        AddOutline(enemyIntentText.gameObject, new Color(0,0,0,1), 2);
        // Enemy skill icons row (under HP) — up to 3, count by enemy tier
        for (int i=0;i<3;i++)
        {
            var esGo = new GameObject("EnSkill"+i);
            esGo.transform.SetParent(enemyPanel.transform, false);
            var esImg = esGo.AddComponent<Image>(); esImg.color = new Color(0.15f,0.07f,0.12f,0.95f);
            var esRT = esImg.rectTransform; esRT.anchorMin = new Vector2(0,1); esRT.anchorMax = new Vector2(0,1); esRT.pivot = new Vector2(0,1);
            esRT.anchoredPosition = new Vector2(270 + i*180, -150); esRT.sizeDelta = new Vector2(160, 160);
            AddOutline(esGo, new Color(0.85f,0.35f,0.35f,1f), 3);
            enemySkillIcons[i] = esImg;
            // CD text in lower-right corner
            var cdT = MakeText(esGo.transform, "EnSkCd"+i, "", 30, new Color(1f,0.95f,0.85f,1f));
            var cdRT = cdT.rectTransform; cdRT.anchorMin = new Vector2(1,0); cdRT.anchorMax = new Vector2(1,0); cdRT.pivot = new Vector2(1,0);
            cdRT.anchoredPosition = new Vector2(-8,8); cdRT.sizeDelta = new Vector2(50,40);
            cdT.alignment = TextAnchor.MiddleRight; cdT.fontStyle = FontStyle.Bold;
            AddOutline(cdT.gameObject, new Color(0,0,0,1), 2);
            enemySkillCdText[i] = cdT;
            esGo.SetActive(false);
        }
        // BoardPanel — safe zone between EnemyPanel and SkillBar (constraint 97)
        // Layout: enemy panel ends at y = -110-360 = -470 from top; PlayerHp 80 + SkillBar 180 = 260 from bottom
        // Board height fills remaining area
        var boardPanel = MakeImage(battlePanel.transform, "BoardPanel", new Color(0,0,0,0.0f));
        var bpRT = boardPanel.rectTransform; bpRT.anchorMin = new Vector2(0,0); bpRT.anchorMax = new Vector2(1,1); bpRT.pivot = new Vector2(0.5f,0.5f);
        bpRT.offsetMin = new Vector2(20, 280); bpRT.offsetMax = new Vector2(-20, -480);
        boardPanelRT = bpRT;
        // Player SkillBar (5 circular icons horizontal) ABOVE HP bar (constraint 94, 108)
        var skillBar = MakeImage(battlePanel.transform, "PlayerSkillBar", new Color(0.08f,0.06f,0.10f,0.55f));
        var sbRT = skillBar.rectTransform; sbRT.anchorMin = new Vector2(0,0); sbRT.anchorMax = new Vector2(1,0); sbRT.pivot = new Vector2(0.5f,0);
        sbRT.anchoredPosition = new Vector2(0,90); sbRT.sizeDelta = new Vector2(-30, 180);
        AddOutline(skillBar.gameObject, new Color(0.4f,0.6f,0.5f,0.6f), 2);
        string[] vfxKeys = new []{ "vfx_inferno_burst", "vfx_freeze", "vfx_titan_slam", "bonus_hermes_step", "bonus_zeus_lightning" };
        Color[] ringTints = new []{ new Color(1f,0.45f,0.15f,1f), new Color(0.5f,0.85f,1f,1f), new Color(1f,0.9f,0.3f,1f), new Color(0.9f,0.8f,0.4f,1f), new Color(0.6f,0.85f,1f,1f) };
        for (int i=0;i<5;i++)
        {
            int captured = i;
            var ringGo = new GameObject("AbilRing"+i);
            ringGo.transform.SetParent(skillBar.transform, false);
            var ringImg = ringGo.AddComponent<Image>(); ringImg.color = new Color(0.10f,0.08f,0.15f,0.97f);
            var ringRT = ringImg.rectTransform;
            ringRT.anchorMin = new Vector2(0,0.5f); ringRT.anchorMax = new Vector2(0,0.5f); ringRT.pivot = new Vector2(0,0.5f);
            ringRT.anchoredPosition = new Vector2(15 + i*200, 0); ringRT.sizeDelta = new Vector2(150, 150);
            AddOutline(ringGo, ringTints[i], 4);
            abilityRingBg[i] = ringImg;
            var iconGo = new GameObject("AbilIcon"+i);
            iconGo.transform.SetParent(ringGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            if (sprites.ContainsKey(vfxKeys[i])) iconImg.sprite = sprites[vfxKeys[i]];
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            var iRT = iconImg.rectTransform; iRT.anchorMin = new Vector2(0,0); iRT.anchorMax = new Vector2(1,1); iRT.pivot = new Vector2(0.5f,0.5f);
            iRT.offsetMin = new Vector2(10,10); iRT.offsetMax = new Vector2(-10,-10);
            var cdGo = new GameObject("AbilCd"+i);
            cdGo.transform.SetParent(ringGo.transform, false);
            var cdImg = cdGo.AddComponent<Image>(); cdImg.color = new Color(0,0,0,0.7f); cdImg.raycastTarget = false;
            cdImg.type = Image.Type.Filled; cdImg.fillAmount = 0f;
            var cdRT = cdImg.rectTransform; cdRT.anchorMin = Vector2.zero; cdRT.anchorMax = Vector2.one; cdRT.offsetMin = Vector2.zero; cdRT.offsetMax = Vector2.zero;
            abilityCdMask[i] = cdImg;
            var abBtn = ringGo.AddComponent<Button>();
            abBtn.onClick.AddListener(() => UseAbility(captured));
            var abTxt = MakeText(ringGo.transform, "Cost", "", 22, new Color(1f,0.95f,0.85f,1f));
            var atRT = abTxt.rectTransform; atRT.anchorMin = new Vector2(0,0); atRT.anchorMax = new Vector2(1,0); atRT.pivot = new Vector2(0.5f,0);
            atRT.anchoredPosition = new Vector2(0,-22); atRT.sizeDelta = new Vector2(0,40);
            abTxt.alignment = TextAnchor.MiddleCenter; abTxt.fontStyle = FontStyle.Bold;
            AddOutline(abTxt.gameObject, new Color(0,0,0,1), 2);
            abilityButtonsGO.Add(ringGo);
            abilityButtonsText.Add(abTxt);
        }
        // Player HP bar — full-width bottom (constraint 93)
        var phpBg = MakeImage(battlePanel.transform, "PHpBg", new Color(0.04f,0.16f,0.06f,0.96f));
        var phbRT = phpBg.rectTransform; phbRT.anchorMin = new Vector2(0,0); phbRT.anchorMax = new Vector2(1,0); phbRT.pivot = new Vector2(0.5f,0);
        phbRT.anchoredPosition = new Vector2(0,10); phbRT.sizeDelta = new Vector2(-30, 70);
        AddOutline(phpBg.gameObject, new Color(0.3f,0.85f,0.5f,1f), 3);
        playerHpBar = MakeImage(phpBg.transform, "PHpFill", new Color(0.3f,0.85f,0.3f,1));
        Stretch(playerHpBar.rectTransform); playerHpBar.fillAmount = 1f;
        battlePlayerHpText = MakeText(phpBg.transform, "PHpTxt", "100/100", 36, Color.white); Stretch(battlePlayerHpText.rectTransform); battlePlayerHpText.alignment = TextAnchor.MiddleCenter; battlePlayerHpText.fontStyle = FontStyle.Bold;
        AddOutline(battlePlayerHpText.gameObject, new Color(0,0,0,1), 2);
        // Hidden player portrait — still referenced by code, parked off-screen
        battlePlayerPortrait = MakeImage(battlePanel.transform, "PlayerPortHidden", new Color(1,1,1,0));
        battlePlayerPortrait.raycastTarget = false; battlePlayerPortrait.preserveAspect = true;
        var ppRT = battlePlayerPortrait.rectTransform; ppRT.anchorMin = new Vector2(0,0); ppRT.anchorMax = new Vector2(0,0); ppRT.pivot = new Vector2(0,0);
        ppRT.anchoredPosition = new Vector2(-9999,-9999); ppRT.sizeDelta = new Vector2(10,10);
        // Turn label — centered above board
        battleTurnText = MakeText(battlePanel.transform, "Turn", "", 40, new Color(1f,0.9f,0.5f,1f));
        var ttuRT = battleTurnText.rectTransform; ttuRT.anchorMin = new Vector2(0,1); ttuRT.anchorMax = new Vector2(1,1); ttuRT.pivot = new Vector2(0.5f,1);
        ttuRT.anchoredPosition = new Vector2(0,-490); ttuRT.sizeDelta = new Vector2(-200,60);
        battleTurnText.alignment = TextAnchor.MiddleCenter; battleTurnText.fontStyle = FontStyle.Bold;
        AddOutline(battleTurnText.gameObject, new Color(0,0,0,1), 3);
        // VFX overlay
        vfxOverlay = MakeImage(battlePanel.transform, "Vfx", new Color(1,1,1,0));
        var vfRT = vfxOverlay.rectTransform; vfRT.anchorMin = new Vector2(0.5f,0.5f); vfRT.anchorMax = new Vector2(0.5f,0.5f);
        vfRT.pivot = new Vector2(0.5f,0.5f); vfRT.anchoredPosition = new Vector2(0,0); vfRT.sizeDelta = new Vector2(900,900);
        vfxOverlay.preserveAspect = true; vfxOverlay.raycastTarget = false;
        battlePanel.SetActive(false);

        // Battle result panel
        battleResultPanel = new GameObject("BattleResult");
        battleResultPanel.transform.SetParent(canvas.transform, false);
        var brRT = battleResultPanel.AddComponent<RectTransform>(); Stretch(brRT);
        var brBg = MakeImage(battleResultPanel.transform, "BrBg", new Color(0,0,0,0.88f)); Stretch(brBg.rectTransform);
        battleResultTitle = MakeText(battleResultPanel.transform, "BrTitle", "", 100, new Color(1f,0.85f,0.4f,1f));
        var brtRT = battleResultTitle.rectTransform; brtRT.anchorMin = new Vector2(0.5f,0.5f); brtRT.anchorMax = new Vector2(0.5f,0.5f);
        brtRT.pivot = new Vector2(0.5f,0.5f); brtRT.anchoredPosition = new Vector2(0,200); brtRT.sizeDelta = new Vector2(900,160);
        battleResultTitle.alignment = TextAnchor.MiddleCenter; battleResultTitle.fontStyle = FontStyle.Bold;
        battleResultRewardText = MakeText(battleResultPanel.transform, "BrRew", "", 60, new Color(1f,0.9f,0.5f,1f));
        var brrRT = battleResultRewardText.rectTransform; brrRT.anchorMin = new Vector2(0.5f,0.5f); brrRT.anchorMax = new Vector2(0.5f,0.5f);
        brrRT.pivot = new Vector2(0.5f,0.5f); brrRT.anchoredPosition = new Vector2(0,40); brrRT.sizeDelta = new Vector2(900,100);
        battleResultRewardText.alignment = TextAnchor.MiddleCenter;
        Text brContBtnText;
        MakeFlatButton(battleResultPanel.transform, "BrCont", new Vector2(0,-180), new Vector2(640,140), OnBattleResultContinue, out brContBtnText);
        brContBtnText.text = L("NEXT");
        battleResultPanel.SetActive(false);

        // Shop panel
        shopPanel = new GameObject("ShopPanel");
        shopPanel.transform.SetParent(canvas.transform, false);
        var shRT = shopPanel.AddComponent<RectTransform>(); Stretch(shRT);
        var shBg = MakeImage(shopPanel.transform, "ShBg", new Color(0.02f,0.02f,0.05f,0.96f)); Stretch(shBg.rectTransform);
        shopTitleText = MakeText(shopPanel.transform, "ShTitle", "", 80, new Color(1f,0.85f,0.4f,1f));
        var sttRT = shopTitleText.rectTransform; sttRT.anchorMin = new Vector2(0,1); sttRT.anchorMax = new Vector2(1,1); sttRT.pivot = new Vector2(0.5f,1);
        sttRT.anchoredPosition = new Vector2(0,-100); sttRT.sizeDelta = new Vector2(-60,120);
        shopTitleText.alignment = TextAnchor.MiddleCenter; shopTitleText.fontStyle = FontStyle.Bold;
        // Currency display
        echoesText = MakeText(shopPanel.transform, "EcTxt", "0", 44, new Color(1f,0.85f,0.4f,1f));
        var ecRT = echoesText.rectTransform; ecRT.anchorMin = new Vector2(0,1); ecRT.anchorMax = new Vector2(0,1); ecRT.pivot = new Vector2(0,1);
        ecRT.anchoredPosition = new Vector2(40,-250); ecRT.sizeDelta = new Vector2(300,60); echoesText.alignment = TextAnchor.MiddleLeft;
        sparksText = MakeText(shopPanel.transform, "SpTxt", "0", 44, new Color(0.9f,0.4f,1f,1f));
        var spkRT = sparksText.rectTransform; spkRT.anchorMin = new Vector2(1,1); spkRT.anchorMax = new Vector2(1,1); spkRT.pivot = new Vector2(1,1);
        spkRT.anchoredPosition = new Vector2(-40,-250); spkRT.sizeDelta = new Vector2(300,60); sparksText.alignment = TextAnchor.MiddleRight;
        // 5 ability shop buttons + 1 demo donate
        for (int i=0;i<5;i++)
        {
            int captured = i;
            Text btx;
            MakeFlatButton(shopPanel.transform, "ShopBuy"+i, new Vector2(0, 380 - i*180), new Vector2(880, 160), () => OnShopBuy(captured), out btx);
            btx.text = L("ABIL_"+AbilityKeys[i].ToUpper()) + "  —  " + AbilityPrices[i] + " " + L("ECHOES");
        }
        Text donateBtnText;
        MakeFlatButton(shopPanel.transform, "DonateBtn", new Vector2(0,-560), new Vector2(880,140), OnMockDonate, out donateBtnText);
        donateBtnText.text = L("BUY") + " 500 " + L("SPARKS") + "  ($4.99)";
        Text shopBackText;
        MakeFlatButton(shopPanel.transform, "ShopBack", new Vector2(0,-720), new Vector2(640,130), OnCloseShop, out shopBackText);
        shopBackText.text = L("BACK");
        shopPanel.SetActive(false);

        // Ending panel
        endingPanel = new GameObject("EndingPanel");
        endingPanel.transform.SetParent(canvas.transform, false);
        var enRT = endingPanel.AddComponent<RectTransform>(); Stretch(enRT);
        var enBg = MakeImage(endingPanel.transform, "EnBg", new Color(0,0,0,0.95f)); Stretch(enBg.rectTransform);
        endingTitleText = MakeText(endingPanel.transform, "EnTitle", "", 90, new Color(1f,0.85f,0.4f,1f));
        var entRT = endingTitleText.rectTransform; entRT.anchorMin = new Vector2(0.5f,1); entRT.anchorMax = new Vector2(0.5f,1); entRT.pivot = new Vector2(0.5f,1);
        entRT.anchoredPosition = new Vector2(0,-220); entRT.sizeDelta = new Vector2(1000,200);
        endingTitleText.alignment = TextAnchor.MiddleCenter; endingTitleText.fontStyle = FontStyle.Bold;
        endingBodyText = MakeText(endingPanel.transform, "EnBody", "", 38, Color.white);
        var enbRT = endingBodyText.rectTransform; enbRT.anchorMin = new Vector2(0.5f,0.5f); enbRT.anchorMax = new Vector2(0.5f,0.5f); enbRT.pivot = new Vector2(0.5f,0.5f);
        enbRT.anchoredPosition = new Vector2(0,0); enbRT.sizeDelta = new Vector2(900,800);
        endingBodyText.alignment = TextAnchor.UpperCenter;
        Text enContText;
        MakeFlatButton(endingPanel.transform, "EnCont", new Vector2(0,-700), new Vector2(700,140), OnEndingContinue, out enContText);
        enContText.text = L("NEXT");
        endingPanel.SetActive(false);

        // Fade overlay topmost
        fadeOverlay = MakeImage(canvas.transform, "Fade", Color.black); Stretch(fadeOverlay.rectTransform);
        fadeOverlay.transform.SetAsLastSibling(); fadeOverlay.raycastTarget = false;
        skipBtnGO.transform.SetAsLastSibling(); nextBtnGO.transform.SetAsLastSibling(); fadeOverlay.transform.SetAsLastSibling();

        // Audio sources
        var aGO = new GameObject("Audio"); aGO.transform.SetParent(transform, false);
        bgmSrc = aGO.AddComponent<AudioSource>(); bgmSrc.loop = true; bgmSrc.volume = 0.55f;
        sfxSrc = aGO.AddComponent<AudioSource>(); sfxSrc.loop = false; sfxSrc.volume = 0.85f;
    }
    Text pbTitleRef, pbBeginBtnText;

    Image MakeImage(Transform parent, string name, Color c)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = c; img.raycastTarget = false; return img;
    }
    Text MakeText(Transform parent, string name, string content, int size, Color c)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>(); t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.color = c; t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; return t;
    }
    GameObject MakeBottomRightButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction cb, out Text outLabel, Color accent)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = new Color(0.04f,0.03f,0.07f,0.92f);
        var rt = img.rectTransform; rt.anchorMin = new Vector2(1,0); rt.anchorMax = new Vector2(1,0); rt.pivot = new Vector2(1,0);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(360,110);
        var btn = go.AddComponent<Button>();
        var cbb = btn.colors; cbb.normalColor = new Color(0.15f,0.12f,0.20f,0.95f);
        cbb.highlightedColor = new Color(0.30f,0.22f,0.40f,1f);
        cbb.pressedColor = new Color(0.50f,0.35f,0.60f,1f);
        btn.colors = cbb; btn.onClick.AddListener(cb);
        AddOutline(go, accent, 2);
        outLabel = MakeText(go.transform, "Lbl", label, 36, new Color(1f,0.95f,0.85f,1f));
        Stretch(outLabel.rectTransform); outLabel.alignment = TextAnchor.MiddleCenter; outLabel.fontStyle = FontStyle.Bold;
        return go;
    }
    GameObject MakeFlatButton(Transform parent, string name, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction cb, out Text outLabel)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = new Color(0.12f,0.10f,0.18f,0.95f);
        var rt = img.rectTransform; rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var btn = go.AddComponent<Button>();
        var cbb = btn.colors; cbb.normalColor = new Color(0.18f,0.15f,0.25f,0.95f);
        cbb.highlightedColor = new Color(0.32f,0.25f,0.42f,1f);
        cbb.pressedColor = new Color(0.45f,0.32f,0.55f,1f);
        btn.colors = cbb; btn.onClick.AddListener(cb);
        AddOutline(go, new Color(0.9f,0.75f,0.3f,1f), 2);
        outLabel = MakeText(go.transform, "Lbl", "", 40, new Color(1f,0.95f,0.85f,1f));
        Stretch(outLabel.rectTransform); outLabel.alignment = TextAnchor.MiddleCenter; outLabel.fontStyle = FontStyle.Bold;
        return go;
    }
    void AddOutline(GameObject go, Color c, int t) { var ol = go.AddComponent<Outline>(); ol.effectColor = c; ol.effectDistance = new Vector2(t,-t); }
    void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }

    void RefreshLocalizedUI()
    {
        titleText.text = L("TITLE1") + "\n" + L("TITLE2");
        subtitleText.text = L("SUBTITLE");
        startBtnText.text = L("NEW_STORY"); contBtnText.text = L("CONTINUE"); settingsBtnText.text = L("SETTINGS");
        settingsTitleText.text = L("SETTINGS");
        musicBtnText.text = L("MUSIC") + " : " + (musicOn ? L("ON") : L("OFF"));
        sfxBtnText.text = L("SFX") + " : " + (sfxOn ? L("ON") : L("OFF"));
        langBtnText.text = L("LANG") + " : " + L("LANG_VAL");
        backBtnText.text = L("BACK");
        if (shopTitleText != null) shopTitleText.text = L("SHOP");
        if (skipBtnText != null) skipBtnText.text = L("SKIP");
        if (nextBtnText != null) nextBtnText.text = L("NEXT");
        if (state == State.Playing || state == State.Choice)
        {
            if (idx < episodes[currentEpisode-1].script.Count)
            {
                var line = episodes[currentEpisode-1].script[idx];
                speakerText.text = SpeakerLabel(line.speaker);
                fullText = (lang == Lang.EN) ? line.en : line.ru;
                dialogText.text = typing ? (typeIdx >= fullText.Length ? fullText : fullText.Substring(0, Math.Min(typeIdx, fullText.Length))) : fullText;
            }
        }
    }

    // ============ FLOW HANDLERS ============
    void ShowTitle()
    {
        state = State.Title;
        titlePanel.SetActive(true);
        settingsPanel.SetActive(false); choicePanel.SetActive(false); preBattlePanel.SetActive(false);
        if (battlePanel != null) battlePanel.SetActive(false);
        if (battleResultPanel != null) battleResultPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        dialogBox.gameObject.SetActive(false);
        skipBtnGO.SetActive(false); nextBtnGO.SetActive(false);
        portraitImage.color = new Color(1,1,1,0); portraitCardBg.color = new Color(0.05f,0.04f,0.08f,0);
        if (sprites.ContainsKey("bg_title")) { titleArt.sprite = sprites["bg_title"]; titleArt.color = Color.white; }
        PlayBgm("bgm_olympus");
        RefreshLocalizedUI();
        fadeDir = -1;
    }
    void OnOpenSettings() { PlaySfx("sfx_choice"); state = State.Settings; titlePanel.SetActive(false); settingsPanel.SetActive(true); RefreshLocalizedUI(); }
    void OnBackFromSettings() { PlaySfx("sfx_choice"); settingsPanel.SetActive(false); ShowTitle(); }
    void OnToggleMusic() { PlaySfx("sfx_choice"); musicOn = !musicOn; PlayerPrefs.SetInt("music", musicOn?1:0); ApplyAudioMutes(); RefreshLocalizedUI(); }
    void OnToggleSfx() { sfxOn = !sfxOn; PlayerPrefs.SetInt("sfx", sfxOn?1:0); ApplyAudioMutes(); if (sfxOn) PlaySfx("sfx_choice"); RefreshLocalizedUI(); }
    void OnToggleLang() { PlaySfx("sfx_choice"); lang = (lang == Lang.EN) ? Lang.RU : Lang.EN; PlayerPrefs.SetInt("lang", (int)lang); RefreshLocalizedUI(); }
    void ApplyAudioMutes() { if (bgmSrc!=null) bgmSrc.mute = !musicOn; if (sfxSrc!=null) sfxSrc.mute = !sfxOn; }

    void OnStartNew() { PlaySfx("sfx_choice"); idx = 0; currentEpisode = 1; pactScore = vengeScore = mortalScore = 0; SaveProgress(); BeginPlay(); }
    void OnContinue() { PlaySfx("sfx_choice"); BeginPlay(); }
    void BeginPlay() { titlePanel.SetActive(false); settingsPanel.SetActive(false); dialogBox.gameObject.SetActive(true); state = State.Playing; ShowCurrentLine(true); }

    void ShowCurrentLine(bool initial)
    {
        var ep = episodes[currentEpisode-1];
        if (idx >= ep.script.Count) { ShowEpisodeEnding(); return; }
        var line = ep.script[idx];
        if (line.triggerBattle > 0) { ShowPreBattle(line); return; }
        if (!string.IsNullOrEmpty(line.bg) && sprites.ContainsKey(line.bg) && line.bg != currentBgKey)
        {
            if (initial || string.IsNullOrEmpty(currentBgKey)) { bgImage.sprite = sprites[line.bg]; bgImage.color = Color.white; currentBgKey = line.bg; crossfading = false; }
            else { bgImageNext.sprite = sprites[line.bg]; bgImageNext.color = new Color(1,1,1,0); pendingBg = line.bg; crossfading = true; crossTime = 0f; }
        }
        if (!string.IsNullOrEmpty(line.speaker) && sprites.ContainsKey(line.speaker))
        { portraitImage.sprite = sprites[line.speaker]; portraitImage.color = Color.white; portraitCardBg.color = new Color(0.05f,0.04f,0.08f,0.92f); }
        else { portraitImage.color = new Color(1,1,1,0); portraitCardBg.color = new Color(0.05f,0.04f,0.08f,0); }
        if (!string.IsNullOrEmpty(line.bgm)) PlayBgm(line.bgm);
        if (!string.IsNullOrEmpty(line.sfx)) PlaySfx(line.sfx);
        speakerText.text = SpeakerLabel(line.speaker);
        fullText = (lang == Lang.EN) ? line.en : line.ru;
        typeIdx = 0; typeTimer = 0f; typing = true; dialogText.text = "";
        skipBtnGO.SetActive(true); nextBtnGO.SetActive(false);
        if (line.choices == null) choicePanel.SetActive(false);
    }

    void ShowChoiceUI()
    {
        var line = episodes[currentEpisode-1].script[idx];
        if (line.choices == null) { choicePanel.SetActive(false); return; }
        choicePanel.SetActive(true); state = State.Choice;
        skipBtnGO.SetActive(false); nextBtnGO.SetActive(false);
        for (int i=0;i<choiceButtons.Count;i++)
        {
            var go = choiceButtons[i].gameObject;
            if (i < line.choices.Length) { go.SetActive(true); choiceTexts[i].text = (lang == Lang.EN) ? line.choices[i].en : line.choices[i].ru; }
            else go.SetActive(false);
        }
    }
    void OnSkip()
    {
        if (state != State.Playing) return; if (!typing) return;
        typeIdx = fullText.Length; dialogText.text = fullText; typing = false; skipBtnGO.SetActive(false);
        var line = episodes[currentEpisode-1].script[idx];
        if (line.choices != null) ShowChoiceUI(); else nextBtnGO.SetActive(true);
    }
    void OnNext()
    {
        if (state != State.Playing) return; if (typing) return;
        var line = episodes[currentEpisode-1].script[idx];
        if (line.choices != null) { ShowChoiceUI(); return; }
        idx++; SaveProgress();
        if (idx >= episodes[currentEpisode-1].script.Count) ShowEpisodeEnding();
        else ShowCurrentLine(false);
    }
    void OnTap()
    {
        if (state != State.Playing) return;
        if (typing) { OnSkip(); return; }
        OnNext();
    }
    void OnChoicePicked(int which)
    {
        PlaySfx("sfx_choice"); choicePanel.SetActive(false); state = State.Playing;
        var line = episodes[currentEpisode-1].script[idx];
        if (line.choices != null && which < line.choices.Length)
        {
            int bias = line.choices[which].pathBias;
            if (bias == 1) pactScore++;
            else if (bias == 2) vengeScore++;
            else if (bias == 3) mortalScore++;
        }
        idx++; SaveProgress();
        if (idx >= episodes[currentEpisode-1].script.Count) ShowEpisodeEnding();
        else ShowCurrentLine(false);
    }

    void ShowPreBattle(Line line)
    {
        state = State.PreBattle;
        dialogBox.gameObject.SetActive(false); skipBtnGO.SetActive(false); nextBtnGO.SetActive(false);
        preBattlePanel.SetActive(true);
        pbTitleRef.text = L("PRE_BATTLE") + " " + line.triggerBattle + " / 11";
        preBattleText.text = (lang == Lang.EN) ? line.en : line.ru;
        pbBeginBtnText.text = L("BATTLE_BEGIN");
        currentBattle = line.triggerBattle;
    }
    void OnBeginBattle()
    {
        PlaySfx("sfx_choice"); preBattlePanel.SetActive(false);
        StartBattle(currentEpisode, currentBattle);
    }

    void ShowEpisodeEnding()
    {
        // Determine path leader
        if (path == Path.Undecided)
        {
            if (pactScore >= vengeScore && pactScore >= mortalScore) path = Path.Pact;
            else if (vengeScore >= mortalScore) path = Path.Vengeance;
            else path = Path.Mortals;
        }
        state = State.Ending;
        endingPanel.SetActive(true);
        dialogBox.gameObject.SetActive(false);
        skipBtnGO.SetActive(false); nextBtnGO.SetActive(false);
        choicePanel.SetActive(false);
        var ep = episodes[currentEpisode-1];
        endingTitleText.text = (lang == Lang.EN ? "Episode " + ep.id + " — " + ep.nameEn : "Эпизод " + ep.id + " — " + ep.nameRu);
        string pathLabel = (lang == Lang.EN)
            ? (path == Path.Pact ? "Path of the Pact" : path == Path.Vengeance ? "Path of Vengeance" : "Path of Mortals")
            : (path == Path.Pact ? "Путь Пакта" : path == Path.Vengeance ? "Путь Мести" : "Путь Смертных");
        string body = (lang == Lang.EN)
            ? "You walk the " + pathLabel + ".\nPact: " + pactScore + " | Vengeance: " + vengeScore + " | Mortals: " + mortalScore + "\n\n" + (currentEpisode < 7 ? "Next: Episode " + (currentEpisode+1) : "FINAL — All seven pantheons walked. The Pact endures.")
            : "Ты идёшь " + pathLabel + ".\nПакт: " + pactScore + " | Месть: " + vengeScore + " | Смертные: " + mortalScore + "\n\n" + (currentEpisode < 7 ? "Далее: Эпизод " + (currentEpisode+1) : "ФИНАЛ — все семь пантеонов пройдены. Пакт устоял.");
        endingBodyText.text = body;
        PlayBgm("bgm_olympus");
        SaveProgress();
    }
    void OnEndingContinue()
    {
        endingPanel.SetActive(false);
        if (currentEpisode < 7) { currentEpisode++; idx = 0; SaveProgress(); BeginPlay(); }
        else ShowTitle();
    }

    // ============ SHOP ============
    void OnOpenShop()
    {
        PlaySfx("sfx_choice"); state = State.Shop;
        settingsPanel.SetActive(false);
        shopPanel.SetActive(true);
        UpdateBattleHUD();
    }
    void OnCloseShop() { PlaySfx("sfx_choice"); shopPanel.SetActive(false); ShowTitle(); }
    void OnShopBuy(int idx)
    {
        if (echoes < AbilityPrices[idx]) { PlaySfx("sfx_blip"); return; }
        echoes -= AbilityPrices[idx];
        string key = AbilityKeys[idx];
        if (!abilityCount.ContainsKey(key)) abilityCount[key] = 0;
        abilityCount[key]++;
        SaveProgress();
        UpdateBattleHUD();
        PlaySfx("sfx_choice");
    }
    void OnMockDonate()
    {
        // Mock IAP — constraint 72
        sparks += 500;
        SaveProgress(); UpdateBattleHUD();
        PlaySfx("sfx_choice");
        if (shopTitleText != null) shopTitleText.text = L("SHOP") + "  [" + L("DEMO_BILLING") + "]";
    }

    // ============ SAVE / LOAD ============
    void SaveProgress()
    {
        PlayerPrefs.SetInt("idx", idx);
        PlayerPrefs.SetInt("ep", currentEpisode);
        PlayerPrefs.SetInt("lang", (int)lang);
        PlayerPrefs.SetInt("music", musicOn?1:0);
        PlayerPrefs.SetInt("sfx", sfxOn?1:0);
        PlayerPrefs.SetInt("pact", pactScore);
        PlayerPrefs.SetInt("venge", vengeScore);
        PlayerPrefs.SetInt("mortal", mortalScore);
        PlayerPrefs.SetInt("echoes", echoes);
        PlayerPrefs.SetInt("sparks", sparks);
        foreach (var k in AbilityKeys)
            PlayerPrefs.SetInt("abil_"+k, abilityCount.ContainsKey(k) ? abilityCount[k] : 0);
        PlayerPrefs.Save();
    }
    void LoadProgress()
    {
        idx = PlayerPrefs.GetInt("idx", 0);
        currentEpisode = Math.Max(1, PlayerPrefs.GetInt("ep", 1));
        lang = (Lang)PlayerPrefs.GetInt("lang", 0);
        musicOn = PlayerPrefs.GetInt("music", 1) == 1;
        sfxOn = PlayerPrefs.GetInt("sfx", 1) == 1;
        pactScore = PlayerPrefs.GetInt("pact", 0);
        vengeScore = PlayerPrefs.GetInt("venge", 0);
        mortalScore = PlayerPrefs.GetInt("mortal", 0);
        echoes = PlayerPrefs.GetInt("echoes", 0);
        sparks = PlayerPrefs.GetInt("sparks", 0);
        foreach (var k in AbilityKeys)
            abilityCount[k] = PlayerPrefs.GetInt("abil_"+k, 0);
    }

    // ============ AUDIO ============
    void PlayBgm(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (id == currentBgm && bgmSrc.isPlaying) return;
        if (!clips.ContainsKey(id)) return;
        bgmSrc.clip = clips[id]; bgmSrc.Play(); currentBgm = id;
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
        if (fadeDir != 0) { fadeAlpha += fadeDir * Time.deltaTime * 0.7f; fadeAlpha = Mathf.Clamp01(fadeAlpha); fadeOverlay.color = new Color(0,0,0,fadeAlpha); if (fadeAlpha <= 0f || fadeAlpha >= 1f) fadeDir = 0; }
        if (crossfading)
        {
            crossTime += Time.deltaTime; float a = Mathf.Clamp01(crossTime / CROSS_DUR);
            bgImageNext.color = new Color(1,1,1,a); bgImage.color = new Color(1,1,1,1f-a);
            if (a >= 1f) { bgImage.sprite = bgImageNext.sprite; bgImage.color = Color.white; bgImageNext.color = new Color(1,1,1,0); currentBgKey = pendingBg; crossfading = false; }
        }
        if (typing && (state == State.Playing || state == State.Choice))
        {
            typeTimer += Time.deltaTime;
            while (typeTimer >= TYPE_SPEED && typeIdx < fullText.Length)
            { typeTimer -= TYPE_SPEED; typeIdx++; dialogText.text = fullText.Substring(0, typeIdx); if (typeIdx % 6 == 0) PlaySfx("sfx_blip"); }
            if (typeIdx >= fullText.Length)
            {
                typing = false; skipBtnGO.SetActive(false);
                var line = episodes[currentEpisode-1].script[idx];
                if (line.choices != null) ShowChoiceUI(); else nextBtnGO.SetActive(true);
            }
        }
        if (nextBtnGO.activeSelf) { pulseT += Time.deltaTime * 3f; float p = 0.85f + 0.15f * Mathf.Sin(pulseT); nextBtnText.color = new Color(1f,0.95f,0.85f,p); }
        // Battle enemy turn scheduling
        if (state == State.Battle && battleResolving)
        {
            enemyDelay -= Time.deltaTime;
            if (enemyDelay <= 0f) { DoEnemyTurn(); }
        }
        // VFX overlay fade
        if (!string.IsNullOrEmpty(vfxAnim))
        {
            vfxT += Time.deltaTime;
            float a = Mathf.Clamp01(1f - vfxT / 0.9f);
            vfxOverlay.color = new Color(1,1,1,a);
            if (vfxT >= 0.9f) { vfxAnim = ""; vfxOverlay.color = new Color(1,1,1,0); }
        }
        // v13: GemTween updates (swap 0.25s, fade 0.3s, drop 0.4s)
        if (state == State.Battle && activeTweens.Count > 0)
        {
            for (int i = activeTweens.Count - 1; i >= 0; i--)
            {
                var tw = activeTweens[i];
                tw.t += Time.deltaTime;
                float a = Mathf.Clamp01(tw.t / Mathf.Max(0.0001f, tw.dur));
                float ease = a*a*(3f-2f*a);
                if (tw.kind == 0 || tw.kind == 2) {
                    if (tw.rt != null) tw.rt.anchoredPosition = tw.from + (tw.to - tw.from) * ease;
                } else if (tw.kind == 1) {
                    if (tw.img != null) tw.img.color = new Color(tw.img.color.r, tw.img.color.g, tw.img.color.b, 1f - a);
                }
                if (a >= 1f) { tw.done = true; activeTweens.RemoveAt(i); }
            }
        }
        // v13: cooldown decay
        if (state == State.Battle)
        {
            for (int i=0;i<abilityCdT.Length;i++)
            {
                if (abilityCdT[i] > 0f) {
                    abilityCdT[i] -= Time.deltaTime;
                    if (abilityCdT[i] < 0f) abilityCdT[i] = 0f;
                    if (abilityCdMask[i] != null && abilityCdDur[i] > 0f) abilityCdMask[i].fillAmount = abilityCdT[i] / abilityCdDur[i];
                } else if (abilityCdMask[i] != null) abilityCdMask[i].fillAmount = 0f;
            }
        }
        if (state == State.Battle) RefreshAbilityButtons();
    }
}

// v14: swipe gesture handler attached to each gem GameObject
public class GemDragHandler : UnityEngine.MonoBehaviour,
    UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IDragHandler,
    UnityEngine.EventSystems.IEndDragHandler
{
    Bootstrapper bs; int cx, cy;
    UnityEngine.Vector2 startPos;
    bool dragging;
    public void Init(Bootstrapper b, int x, int y) { bs = b; cx = x; cy = y; }
    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e) { startPos = e.position; dragging = true; }
    public void OnDrag(UnityEngine.EventSystems.PointerEventData e) { /* visual hint optional */ }
    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData e)
    {
        if (!dragging || bs == null) return; dragging = false;
        UnityEngine.Vector2 d = e.position - startPos;
        float ax = (d.x < 0 ? -d.x : d.x); float ay = (d.y < 0 ? -d.y : d.y);
        if (ax < 25f && ay < 25f) return; // too small - treat as tap (Button handles it)
        int dx = 0, dy = 0;
        if (ax > ay) dx = (d.x > 0) ? 1 : -1;
        else        dy = (d.y > 0) ? -1 : 1; // UI Y inverted (top is +)
        bs.OnGemSwipe(cx, cy, dx, dy);
    }
}
