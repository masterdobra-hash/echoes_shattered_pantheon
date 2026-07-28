// ============================================================================
// Echoes: Shattered Pantheon — Iteration S (v5.0.0 — CLEAN REBUILD)
// Match-3 engine: clean model/view separation, coroutine state-machine.
// All VN, UI, lore, episodes, shop, endings preserved from v4.0.0.
// GUID: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb (Bootstrapper.cs.meta preserved)
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Bootstrapper : MonoBehaviour
{
    // ============ STATE ============
    enum State { Title, Settings, EpisodeSelect, Playing, Choice, PreBattle, Battle, BattleResult, Shop, Ending, Ended }
    enum Lang { EN, RU }
    enum Path { Undecided, Pact, Vengeance, Mortals }

    class Line {
        public string speaker; public string bg; public string bgm; public string sfx;
        public string en; public string ru; public Choice[] choices;
        public int triggerBattle;
    }
    class Choice {
        public string en; public string ru;
        public int pathBias;
    }
    class Episode {
        public int id;
        public string nameEn, nameRu;
        public string subtitleEn, subtitleRu;
        public string bgKey;
        public string bgmCalm, bgmBattle;
        public string exclusiveGem;
        public List<Line> script;
        public BattleConfig[] battles;
    }
    class BattleConfig {
        public int id;
        public int gridW, gridH;
        public int colors;
        public int enemyHp;
        public int playerHp;
        public string enemyKey;
        public string arenaBgKey;
        public string preEn, preRu;
        public bool isBoss;
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
    int echoes = 0;
    int sparks = 0;
    bool musicOn = true, sfxOn = true;
    List<Episode> episodes;
    Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    Dictionary<string, int> abilityCount = new Dictionary<string, int>();

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

    AudioSource bgmSrc, sfxSrc;
    string currentBgm = "";
    string currentBgKey = "";

    string fullText = "";
    int typeIdx = 0; float typeTimer = 0f;
    const float TYPE_SPEED = 0.025f;
    bool typing = false;

    float fadeAlpha = 1f;
    int fadeDir = -1;
    bool crossfading = false;
    float crossTime = 0f;
    const float CROSS_DUR = 1.2f;
    string pendingBg = "";

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
            "portrait_eon","portrait_pythia","portrait_hermes","portrait_fallen",
            "bg_sarcophagus","bg_grove","bg_agora","bg_storm","bg_altar","bg_title",
            "gem_pact","gem_storm","gem_blood","gem_ash","gem_mortal","gem_violet",
            "gem_ep2_soul","gem_ep3_tide","gem_ep4_frost","gem_ep5_oak","gem_ep6_ankh","gem_ep7_obsidian",
            "vfx_inferno_burst","vfx_freeze","vfx_titan_slam",
            "circle_mask","bonus_hermes_step","bonus_hephaestus_hammer","bonus_zeus_lightning",
            "vfx_destroy_pact","vfx_destroy_storm","vfx_destroy_blood","vfx_destroy_ash","vfx_destroy_mortal","vfx_destroy_violet",
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

    // ============ EPISODES (preserved from v4.0.0) ============
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
        ep.exclusiveGem = "gem_violet";
        ep.script = new List<Line>();
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
        AddBattleTrigger(ep, 1, "Hoplite scouts approach. They smell of bronze and violet rot.",
            "Гоплиты-разведчики приближаются. От них пахнет бронзой и фиолетовой гнилью.");
        AddL(ep, "portrait_pythia", "bg_grove", "", "",
            "Pythia: You still remember how to swing the Pact. Good. Walk to the Agora. Hermes waits.",
            "Пифия: Ты ещё помнишь, как взмахнуть Пактом. Хорошо. Иди на Агору. Гермес ждёт.");
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
        AddBattleTrigger(ep, 2, "Three corrupt hoplites block the way. Bronze cracks with violet glow.",
            "Трое порченых гоплитов перекрывают путь. Бронза трескается фиолетовым светом.");
        AddL(ep, "portrait_hermes", "bg_agora", "", "",
            "Hermes: A shadow priestess of the Fallen blesses them. She must fall first.",
            "Гермес: Их благословляет теневая жрица Падшего. Она должна пасть первой.");
        AddBattleTrigger(ep, 3, "The shadow priestess raises her cursed scroll. The air goes cold.",
            "Теневая жрица поднимает проклятый свиток. Воздух стынет.");
        AddChoice(ep, "Hermes: What kind of Titan are you, sleeper?",
            "Гермес: Что ты за Титан, спящий?",
            new []{"\"I am the Pact. I keep it.\"", "\"I am vengeance for silent gods.\"", "\"I am the last weight on the scale.\""},
            new []{"«Я — Пакт. Я храню его.»", "«Я — месть за умолкших богов.»", "«Я — последний вес на чаше весов.»"},
            new []{1, 2, 3});
        AddL(ep, "portrait_hermes", "bg_agora", "", "",
            "Hermes: The road climbs. Hoplite legion. Then a beast. Then the summit.",
            "Гермес: Дорога вверх. Легион гоплитов. Затем зверь. Затем вершина.");
        AddBattleTrigger(ep, 4, "Hoplite phalanx on the marble stairs. Shields locked, violet eyes.",
            "Фаланга гоплитов на мраморных ступенях. Щиты сомкнуты, фиолетовые глаза.");
        AddBattleTrigger(ep, 5, "Two priestesses behind hoplite shields. They sing the death of Hera.",
            "Две жрицы за щитами гоплитов. Они поют смерть Геры.");
        AddL(ep, "", "bg_storm", "bgm_tense", "sfx_transition",
            "The storm is a bruise. Lightning tastes of iron.",
            "Буря — синяк. Молния на вкус — железо.");
        AddBattleTrigger(ep, 6, "Hoplites born of storm clouds materialize from rain.",
            "Гоплиты, рождённые из туч, материализуются из дождя.");
        AddBattleTrigger(ep, 7, "A minotaur, corrupted demigod. Violet veins under bronze hide.",
            "Минотавр, порченый полубог. Фиолетовые вены под бронзовой шкурой.");
        AddL(ep, "portrait_eon", "bg_storm", "", "",
            "Eon: The beast was once a hero. The Fallen ate that too.",
            "Эон: Этот зверь когда-то был героем. Падший съел и это.");
        AddBattleTrigger(ep, 8, "Three priestesses form a violet triangle. A spell rises from them.",
            "Три жрицы образуют фиолетовый треугольник. От них поднимается заклятие.");
        AddBattleTrigger(ep, 9, "Elite hoplites in black bronze. Veterans of the Fall.",
            "Элитные гоплиты в чёрной бронзе. Ветераны Падения.");
        AddBattleTrigger(ep, 10, "Another minotaur — bigger, golden ring through its nose.",
            "Ещё один минотавр — больше, с золотым кольцом в носу.");
        AddL(ep, "", "bg_altar", "bgm_tense", "sfx_transition",
            "The altar. Twelve broken statues. In the center — the Fallen, hammer beside him. Yours.",
            "Алтарь. Двенадцать разбитых статуй. В центре — Падший, молот рядом. Твой.");
        AddL(ep, "portrait_fallen", "bg_altar", "", "",
            "Fallen Hoplite: Titan. You wear flesh badly. Pick up the hammer. Or kneel.",
            "Падший Гоплит: Титан. Ты плохо носишь плоть. Возьми молот. Или склони колено.");
        AddChoice(ep, "Fallen: Either way the Pact ends tonight.",
            "Падший: В любом случае Пакт кончится сегодня.",
            new []{"\"I take the hammer. The Pact does not end.\"", "\"You ate gods. You will choke on a Titan.\"", "\"Olympus was hollow. But mortals are not.\""},
            new []{"«Я беру молот. Пакт не кончится.»", "«Ты ел богов. Подавишься Титаном.»", "«Олимп был пуст. Но смертные — нет.»"},
            new []{1, 2, 3});
        AddBattleTrigger(ep, 11, "Fallen Hoplite rises. Violet sparks circle him — twelve devoured gods scream from within.",
            "Падший Гоплит встаёт. Фиолетовые искры вокруг него — двенадцать съеденных богов кричат изнутри.");
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
            b[i].altEnemyVengeance = enemies[i];
            b[i].altEnemyMortals = enemies[i];
        }
        return b;
    }

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
    // PART 2 — Match-3 engine (CLEAN REBUILD v5.0)
    // Key architectural changes:
    //   1. BoardModel is ALWAYS authoritative — view never drives state
    //   2. Input blocked during ANY resolve phase via BoardPhase enum
    //   3. Coroutine state machine: Input → Swap → FindMatches → Animate →
    //      CreateBonus → AnimateClear → Collapse → Refill → Cascade → FinishTurn
    //   4. RenderBoard() NEVER touches tweens — only sync-snaps all views to model
    //   5. GemViews are indexed by (x,y) slot, position calculated from model coords
    // =====================================================================

    // --- Board Model ---
    struct CellData
    {
        public int color;   // -1 = empty (falling)
        public int bonus;   // 0=none,1=lineH,2=lineV,3=hammer6x6,4=colorBomb
        public bool curse;
        public static CellData Empty => new CellData { color = -1, bonus = 0, curse = false };
        public bool IsEmpty => color < 0;
    }

    const int BONUS_NONE = 0, BONUS_LINE_H = 1, BONUS_LINE_V = 2, BONUS_HAMMER = 3, BONUS_COLOR_BOMB = 4;
    string[] BaseGemKeys  = { "gem_pact","gem_storm","gem_blood","gem_ash","gem_mortal","gem_violet" };
    string[] DestroyVfxKeys = { "vfx_destroy_pact","vfx_destroy_storm","vfx_destroy_blood",
                                  "vfx_destroy_ash","vfx_destroy_mortal","vfx_destroy_violet" };

    CellData[,] boardModel;   // single source of truth
    int boardW, boardH, boardColors;

    // --- Board View (GOs indexed by slot) ---
    GameObject[,]  gemGO;
    Image[,]       gemImg;
    RectTransform[,] gemRT;

    GameObject gridRoot;
    RectTransform boardPanelRT;
    float cellSz;

    // --- Battle state ---
    enum BoardPhase { Input, Resolving, EnemyTurn, Done }
    BoardPhase boardPhase = BoardPhase.Done;

    int  selX = -1, selY = -1;
    int  turnSide  = 0;   // 0=player,1=enemy
    int  turnCount = 0;   // 0..1 two moves per side
    bool extraTurn = false;
    int  playerHpCur, playerHpMax;
    int  enemyHpCur, enemyHpMax;
    int  comboMul  = 1;
    int  enemyFreezeTurns = 0;
    int  enemyNextSkillIdx = 0;
    bool pendingEnemyTurn = false;
    float enemyDelay = 0f;

    Image playerHpBar, enemyHpBar;
    Image battleBg, battleEnemyPortrait, battlePlayerPortrait;
    Image vfxOverlay;
    string vfxAnim = "";
    float  vfxT    = 0f;
    Text   enemyIntentText;
    Image[] enemySkillIcons  = new Image[3];
    Text[]  enemySkillCdText = new Text[3];
    int[]   enemySkillCd     = new int[3];
    Image[] abilityRingBg  = new Image[5];
    Image[] abilityCdMask  = new Image[5];
    float[] abilityCdT     = new float[5];
    float[] abilityCdDur   = new float[5];
    BattleConfig curBattle;

    // --- Ability zone highlight ---
    // Highlighted cells shown when player holds ability button (preview mode)
    List<Image> abilityHighlightImgs = new List<Image>();
    int abilityPreviewIdx = -1;   // which ability is being previewed (-1 = none)

    // --- Enemy turn overlay (dim + claw) ---
    Image enemyTurnDimImg;   // 5% black overlay during enemy turn
    Image enemyClawImg;      // enemy claw / hand / tentacle sprite
    float enemyClawT = 0f;   // animation progress
    bool  enemyClawActive = false;
    Vector2 enemyClawFrom, enemyClawTo;

    // --- Tutorial state ---
    bool tutorialBonusDone = false;      // shown "bonus gem" hint once
    bool tutorialAbilityDone = false;    // shown "ability" hint once
    GameObject tutorialPanelGO;
    Text tutorialText;
    float tutorialTimer = 0f;
    const float TUTORIAL_DUR = 4.0f;

    // --- Tween system (view-only, never drives model) ---
    class GemTween
    {
        public RectTransform rt;
        public Image img;
        public Vector2 from, to;
        public float t, dur;
        // kind: 0=move/swap, 1=fadeOut, 2=drop, 3=destroyVfx, 4=selectionPulse
        public int kind;
    }
    List<GemTween> activeTweens = new List<GemTween>();
    bool TweensActive => activeTweens.Count > 0;

    // Safely clear tweens — destroys VFX GameObjects before clearing
    void ClearTweensSafe()
    {
        foreach (var tw in activeTweens)
            if (tw.kind == 3 && tw.img != null && tw.img.gameObject != null)
                UnityEngine.Object.Destroy(tw.img.gameObject);
        activeTweens.Clear();
    }

    // Clear only non-VFX tweens (keeps VFX running to completion)
    void ClearMoveTweens()
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
            if (activeTweens[i].kind != 3)  // keep destroyVfx (kind=3)
                activeTweens.RemoveAt(i);
    }

    // ---- START BATTLE ----
    void StartBattle(int episodeId, int battleId)
    {
        var ep = episodes[episodeId-1];
        curBattle = ep.battles[battleId-1];
        boardW = curBattle.gridW; boardH = curBattle.gridH; boardColors = curBattle.colors;
        playerHpMax = curBattle.playerHp; playerHpCur = playerHpMax;
        enemyHpMax  = curBattle.enemyHp;  enemyHpCur  = enemyHpMax;
        turnSide = 0; turnCount = 0; extraTurn = false; comboMul = 1;
        enemyFreezeTurns = 0; enemyNextSkillIdx = 0;
        pendingEnemyTurn = false;
        selX = selY = -1;
        ClearTweensSafe();   // fix: destroy orphan VFX GOs, not just clear list
        ClearAbilityZone();  // clear any leftover zone highlights
        StopEnemyTurnVFX();  // ensure dim/claw are hidden on battle start
        // Reset tutorial flags per episode-1 battles 1-4 only
        if (currentEpisode == 1 && curBattle != null && curBattle.id == 1)
        { tutorialBonusDone = false; tutorialAbilityDone = false; }
        // fix: hide VN navigation buttons when entering battle
        if (nextBtnGO != null) nextBtnGO.SetActive(false);
        if (skipBtnGO != null) skipBtnGO.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (dialogBox != null) dialogBox.gameObject.SetActive(false);
        PlayBgm(ep.bgmBattle);
        RefreshBattlePanel();
        InitBoard();
        FullSyncView();
        state = State.Battle;
        boardPhase = BoardPhase.Input;
        UpdateBattleHUD();
        UpdateEnemyIntent();
        UpdateEnemySkillIcons();
    }

    void RefreshBattlePanel()
    {
        battlePanel.SetActive(true);
        battleBg.sprite = sprites.ContainsKey(curBattle.arenaBgKey) ? sprites[curBattle.arenaBgKey] : null;
        if (sprites.ContainsKey(curBattle.enemyKey)) battleEnemyPortrait.sprite = sprites[curBattle.enemyKey];
        if (sprites.ContainsKey("portrait_eon"))      battlePlayerPortrait.sprite = sprites["portrait_eon"];
        battleTurnText.text = L("TURN_PLAYER");
        for (int i=0;i<abilityCdT.Length;i++)
        {
            abilityCdT[i] = 0f; abilityCdDur[i] = 0f;
            if (abilityCdMask[i] != null) abilityCdMask[i].fillAmount = 0f;
        }
    }

    // ---- BOARD INIT ----
    void InitBoard()
    {
        boardModel = new CellData[boardW, boardH];
        var rng = new System.Random();
        for (int x = 0; x < boardW; x++)
        for (int y = 0; y < boardH; y++)
        {
            int c;
            do { c = rng.Next(0, boardColors); }
            while (WouldMatch(x, y, c));
            boardModel[x, y] = new CellData { color = c, bonus = BONUS_NONE, curse = false };
        }
        if (curBattle.isBoss)
        {
            int cx = rng.Next(0, boardW), cy = rng.Next(0, boardH);
            boardModel[cx, cy] = new CellData { color = 5, bonus = BONUS_NONE, curse = true };
        }
    }

    bool WouldMatch(int x, int y, int c)
    {
        if (x >= 2 && boardModel[x-1,y].color == c && boardModel[x-2,y].color == c) return true;
        if (y >= 2 && boardModel[x,y-1].color == c && boardModel[x,y-2].color == c) return true;
        return false;
    }

    // ---- BOARD GRID GAMEOBJECTS ----
    // Called once per battle; re-creates view GOs matching current boardW/boardH
    void BuildBoardGOs()
    {
        if (gridRoot != null) UnityEngine.Object.Destroy(gridRoot);
        gridRoot = new GameObject("GridRoot");
        var parent = boardPanelRT != null ? boardPanelRT.transform : battlePanel.transform;
        gridRoot.transform.SetParent(parent, false);
        var rt = gridRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        // fix: force canvas layout pass so boardPanelRT.rect is non-zero before computing cellSz
        Canvas.ForceUpdateCanvases();

        float maxW = boardPanelRT != null ? Mathf.Max(400f, boardPanelRT.rect.width  - 30f) : 1000f;
        float maxH = boardPanelRT != null ? Mathf.Max(400f, boardPanelRT.rect.height - 30f) : 1100f;
        if (maxW < 100f) maxW = 1000f;
        if (maxH < 100f) maxH = 1100f;
        cellSz = Mathf.Min(maxW / boardW, maxH / boardH);
        rt.sizeDelta = new Vector2(cellSz * boardW + 20f, cellSz * boardH + 20f);

        gemGO  = new GameObject[boardW, boardH];
        gemImg = new Image[boardW, boardH];
        gemRT  = new RectTransform[boardW, boardH];

        for (int x = 0; x < boardW; x++)
        for (int y = 0; y < boardH; y++)
        {
            int cx = x, cy = y;
            var go = new GameObject("Gem_" + x + "_" + y);
            go.transform.SetParent(gridRoot.transform, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = true;
            var grt = img.rectTransform;
            grt.anchorMin = new Vector2(0f, 1f);
            grt.anchorMax = new Vector2(0f, 1f);
            grt.pivot     = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(cellSz - 6f, cellSz - 6f);
            grt.anchoredPosition = ModelToViewPos(x, y);

            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnGemTap(cx, cy));

            var dh = go.AddComponent<GemDragHandler>();
            dh.Init(this, cx, cy);

            gemGO[x, y]  = go;
            gemImg[x, y] = img;
            gemRT[x, y]  = grt;
        }
    }

    Vector2 ModelToViewPos(int x, int y) =>
        new Vector2(x * cellSz + cellSz * 0.5f + 10f, -(y * cellSz + cellSz * 0.5f + 10f));

    // ---- FULL SYNC VIEW ----
    // Authoritative: destroy old GOs if size changed, rebuild, then sync sprites/positions
    void FullSyncView()
    {
        // Rebuild GO grid if size changed
        bool needRebuild = gemGO == null
                        || gemGO.GetLength(0) != boardW
                        || gemGO.GetLength(1) != boardH;
        if (needRebuild)
        {
            // fix: destroy VFX GOs before gridRoot rebuild so they don't become orphans
            ClearTweensSafe();
            if (gemGO != null)
            {
                for (int x=0;x<gemGO.GetLength(0);x++)
                for (int y=0;y<gemGO.GetLength(1);y++)
                    if (gemGO[x,y] != null) UnityEngine.Object.Destroy(gemGO[x,y]);
                gemGO  = null;
                gemImg = null;
                gemRT  = null;
            }
            BuildBoardGOs();
        }

        // Stop all non-VFX tweens — model is now authoritative
        // VFX (kind=3) are allowed to finish so their GOs get properly Destroyed
        ClearMoveTweens();

        // Sync each slot
        for (int x=0;x<boardW;x++)
        for (int y=0;y<boardH;y++)
        {
            var img = gemImg[x,y];
            if (img == null) continue;
            var cell = boardModel[x,y];

            // Sprite
            bool isBonusGem = cell.bonus != BONUS_NONE;
            string key;
            if      (cell.bonus == BONUS_LINE_H || cell.bonus == BONUS_LINE_V) key = "bonus_hermes_step";
            else if (cell.bonus == BONUS_HAMMER)    key = "bonus_hephaestus_hammer";
            else if (cell.bonus == BONUS_COLOR_BOMB) key = "bonus_zeus_lightning";
            else if (cell.color >= 0 && cell.color < BaseGemKeys.Length)
                key = BaseGemKeys[cell.color];
            else
                key = episodes[currentEpisode-1].exclusiveGem;

            if (sprites.ContainsKey(key))        img.sprite = sprites[key];
            else if (cell.color >= 0 && cell.color < BaseGemKeys.Length && sprites.ContainsKey(BaseGemKeys[cell.color]))
                img.sprite = sprites[BaseGemKeys[cell.color]];
            else
                img.sprite = null;

            // Position — always snap to model coordinate
            gemRT[x,y].anchoredPosition = ModelToViewPos(x, y);
            gemRT[x,y].localScale = Vector3.one;

            // Selection highlight — tint + scale pulse
            if (selX == x && selY == y)
            {
                img.color = new Color(1.4f, 1.25f, 0.5f, 1f);  // gold tint
                gemRT[x,y].localScale = new Vector3(1.18f, 1.18f, 1f); // pop up
                // Add pulse animation if not already running
                bool alreadyPulsing = false;
                foreach (var tw in activeTweens)
                    if (tw.kind == 4 && tw.rt == gemRT[x,y]) { alreadyPulsing = true; break; }
                if (!alreadyPulsing)
                    activeTweens.Add(new GemTween{ rt=gemRT[x,y], img=img,
                        from=new Vector2(1.18f,1.18f), to=new Vector2(1.08f,1.08f),
                        t=0, dur=0.4f, kind=4 });
            }
            else if (isBonusGem)
            {
                // Fix: bonus gems get a bright glowing tint and slightly larger scale
                // so players can clearly distinguish them from normal gems.
                switch (cell.bonus)
                {
                    case BONUS_LINE_H:
                    case BONUS_LINE_V:
                        img.color = new Color(0.6f, 1.0f, 1.8f, 1f); // bright cyan
                        break;
                    case BONUS_HAMMER:
                        img.color = new Color(1.8f, 1.2f, 0.3f, 1f); // bright orange
                        break;
                    case BONUS_COLOR_BOMB:
                        img.color = new Color(1.6f, 0.4f, 2.0f, 1f); // bright purple
                        break;
                    default:
                        img.color = new Color(1.5f, 1.5f, 1.5f, 1f);
                        break;
                }
                gemRT[x,y].localScale = new Vector3(1.12f, 1.12f, 1f); // slightly bigger
            }
            else
            {
                img.color = Color.white;
            }
        }
    }

    // ---- INPUT ----
    void OnGemTap(int x, int y)
    {
        if (state != State.Battle || boardPhase != BoardPhase.Input || turnSide != 0) return;

        // BONUS GEM: tap directly activates it (no need to swap)
        if (boardModel[x, y].bonus != BONUS_NONE)
        {
            selX = selY = -1;
            boardPhase = BoardPhase.Resolving;
            StartCoroutine(DoActivateBonusTap(x, y));
            return;
        }

        if (selX < 0)
        {
            selX = x; selY = y;
            FullSyncView();
            return;
        }

        // If selected cell was a bonus gem and we tap elsewhere — treat as bonus activation
        if (boardModel[selX, selY].bonus != BONUS_NONE)
        {
            int bx2 = selX, by2 = selY;
            selX = selY = -1;
            boardPhase = BoardPhase.Resolving;
            StartCoroutine(DoActivateBonusTap(bx2, by2));
            return;
        }

        int dx = Math.Abs(x - selX), dy = Math.Abs(y - selY);
        if (dx + dy != 1)
        {
            // Reselect
            selX = x; selY = y;
            FullSyncView();
            return;
        }

        int sx = selX, sy = selY;
        selX = selY = -1;
        StartCoroutine(DoPlayerSwap(sx, sy, x, y));
    }

    // Player explicitly taps a bonus gem to fire it
    IEnumerator DoActivateBonusTap(int x, int y)
    {
        battleTurnText.text = "";
        PlaySfx("sfx_choice");
        yield return StartCoroutine(ActivateBonus(x, y));
        if (CheckBattleEnd()) yield break;
        // Chain: after bonus explodes, resolve any new matches
        yield return StartCoroutine(ResolveAllMatches(true));
        if (CheckBattleEnd()) yield break;
        // Advance turn same as normal swap
        if (extraTurn)
        {
            extraTurn = false;
            battleTurnText.text = L("EXTRA_TURN");
            boardPhase = BoardPhase.Input;
        }
        else
        {
            turnCount++;
            if (turnCount >= 2)
            {
                turnCount = 0; turnSide = 1;
                battleTurnText.text = L("TURN_ENEMY");
                boardPhase = BoardPhase.EnemyTurn;
                UpdateEnemyIntent();
                StartEnemyTurnVFX();
                yield return new WaitForSeconds(0.4f);
                yield return StartCoroutine(DoEnemyTurn());
                StopEnemyTurnVFX();
            }
            else
            {
                battleTurnText.text = L("TURN_PLAYER");
                UpdateEnemyIntent();
                boardPhase = BoardPhase.Input;
            }
        }
    }

    public void OnGemSwipe(int x, int y, int dx, int dy)
    {
        if (state != State.Battle || boardPhase != BoardPhase.Input || turnSide != 0) return;
        // Swipe on a bonus gem activates it (swipe = intent to use)
        if (boardModel[x, y].bonus != BONUS_NONE)
        {
            selX = selY = -1;
            boardPhase = BoardPhase.Resolving;
            StartCoroutine(DoActivateBonusTap(x, y));
            return;
        }
        int tx = x + dx, ty = y + dy;
        if (tx < 0 || tx >= boardW || ty < 0 || ty >= boardH) return;
        // Swipe onto a bonus gem — activate the bonus gem instead of swapping
        if (boardModel[tx, ty].bonus != BONUS_NONE)
        {
            selX = selY = -1;
            boardPhase = BoardPhase.Resolving;
            StartCoroutine(DoActivateBonusTap(tx, ty));
            return;
        }
        selX = selY = -1;
        StartCoroutine(DoPlayerSwap(x, y, tx, ty));
    }

    // ---- PLAYER SWAP COROUTINE ----
    IEnumerator DoPlayerSwap(int ax, int ay, int bx, int by)
    {
        boardPhase = BoardPhase.Resolving;
        battleTurnText.text = "";

        // Animate swap
        yield return StartCoroutine(AnimateSwap(ax, ay, bx, by));

        // Try match
        SwapModel(ax, ay, bx, by);
        var matched = CollectMatches();

        if (matched == null || matched.Count == 0)
        {
            // Invalid — animate back, revert model
            yield return StartCoroutine(AnimateSwap(ax, ay, bx, by));
            SwapModel(ax, ay, bx, by);
            FullSyncView();
            PlaySfx("sfx_blip");
            boardPhase = BoardPhase.Input;
            battleTurnText.text = L("TURN_PLAYER");
            yield break;
        }

        PlaySfx("sfx_choice");
        yield return StartCoroutine(ResolveAllMatches(true));

        if (CheckBattleEnd()) yield break;

        // Advance turn
        if (extraTurn)
        {
            extraTurn = false;
            battleTurnText.text = L("EXTRA_TURN");
            boardPhase = BoardPhase.Input;
        }
        else
        {
            turnCount++;
            if (turnCount >= 2)
            {
                turnCount = 0; turnSide = 1;
                battleTurnText.text = L("TURN_ENEMY");
                boardPhase = BoardPhase.EnemyTurn;
                UpdateEnemyIntent();
                StartEnemyTurnVFX();
                yield return new WaitForSeconds(0.4f);
                yield return StartCoroutine(DoEnemyTurn());
                StopEnemyTurnVFX();
            }
            else
            {
                battleTurnText.text = L("TURN_PLAYER");
                UpdateEnemyIntent();
                boardPhase = BoardPhase.Input;
            }
        }
    }

    // ---- RESOLVE ALL MATCHES (CASCADE LOOP) ----
    IEnumerator ResolveAllMatches(bool isPlayer)
    {
        comboMul = 1;
        while (true)
        {
            var matches = CollectMatches();
            if (matches == null || matches.Count == 0) break;

            // Determine bonus info for NEW bonus gem creation from this match
            int bonusAnchorX = -1, bonusAnchorY = -1, bonusType = BONUS_NONE, bonusColor = -1;
            DetectBonus(matches, out bonusAnchorX, out bonusAnchorY, out bonusType, out bonusColor);

            // Check if any existing bonus gems are ADJACENT to the matched cells (chain trigger)
            // A bonus gem adjacent to a match gets chain-activated after normal gems clear
            var adjacentBonuses = new List<Vector2Int>();
            foreach (var pos in matches)
            {
                int[] ddx = { 1,-1, 0, 0 };
                int[] ddy = { 0, 0, 1,-1 };
                for (int d=0;d<4;d++)
                {
                    int nx = pos.x+ddx[d], ny = pos.y+ddy[d];
                    if (nx<0||nx>=boardW||ny<0||ny>=boardH) continue;
                    if (boardModel[nx,ny].bonus != BONUS_NONE)
                    {
                        var bpos = new Vector2Int(nx,ny);
                        if (!adjacentBonuses.Contains(bpos) && !matches.Contains(bpos))
                            adjacentBonuses.Add(bpos);
                    }
                }
            }

            // Damage
            int dmg = matches.Count * 2 * comboMul;
            bool hasLongRun = false;
            foreach (var pos in matches)
                if (CountRunAt(pos.x, pos.y) >= 4) { hasLongRun = true; break; }
            if (hasLongRun) { dmg = (int)(dmg * 1.5f); if (isPlayer) extraTurn = true; }
            ApplyDamage(isPlayer, dmg);

            // Mortal heal
            if (isPlayer)
                foreach (var pos in matches)
                    if (boardModel[pos.x, pos.y].color == 4)
                        playerHpCur = Math.Min(playerHpMax, playerHpCur + 3);

            UpdateBattleHUD();

            // Animate destroy
            yield return StartCoroutine(AnimateDestroy(matches));

            // Clear model
            foreach (var pos in matches)
                boardModel[pos.x, pos.y] = CellData.Empty;

            // Place bonus
            if (bonusType != BONUS_NONE && bonusAnchorX >= 0)
            {
                boardModel[bonusAnchorX, bonusAnchorY] = new CellData
                {
                    color = bonusColor,
                    bonus = bonusType,
                    curse = false
                };
                // Tutorial: first 4 battles — show hint about bonus gems
                if (!tutorialBonusDone && isPlayer && curBattle != null && curBattle.id <= 4)
                {
                    tutorialBonusDone = true;
                    string hintEn, hintRu;
                    switch (bonusType)
                    {
                        case BONUS_LINE_H:
                            hintEn = "Match 4 in a row → Line Gem! Tap it to clear a whole row!";
                            hintRu = "Совпадение 4 подряд → Линейный гем! Нажми, чтобы уничтожить всю строку!"; break;
                        case BONUS_LINE_V:
                            hintEn = "Match 4 vertically → Column Gem! Clears an entire column!";
                            hintRu = "Совпадение 4 по вертикали → Гем колонны! Уничтожает весь столбец!"; break;
                        case BONUS_HAMMER:
                            hintEn = "Special match → Hammer! Destroys 6 nearby gems!";
                            hintRu = "Особое совпадение → Молот! Уничтожает 6 соседних гемов!"; break;
                        case BONUS_COLOR_BOMB:
                            hintEn = "Match 5+ → Color Bomb! Removes ALL gems of one color!";
                            hintRu = "Совпадение 5+ → Цветовая бомба! Убирает ВСЕ гемы одного цвета!"; break;
                        default:
                            hintEn = "Bonus gem created! Tap it to trigger a powerful effect!";
                            hintRu = "Создан бонусный гем! Нажми, чтобы вызвать мощный эффект!"; break;
                    }
                    ShowTutorialHint(hintEn, hintRu);
                }
            }

            // Gravity + refill
            CollapseBoard();
            RefillBoard();

            // Animate drop
            yield return StartCoroutine(AnimateDrop());

            FullSyncView();
            comboMul++;

            if (enemyHpCur <= 0 || playerHpCur <= 0) break;

            // CHAIN: if any bonus gems were adjacent to the destroyed match, activate them now.
            // We activate one at a time; each ActivateBonus call collapses+refills, then we loop.
            if (adjacentBonuses.Count > 0)
            {
                foreach (var bpos in adjacentBonuses)
                {
                    // Make sure it still exists (wasn't destroyed by a previous chain)
                    if (bpos.x < boardW && bpos.y < boardH && boardModel[bpos.x, bpos.y].bonus != BONUS_NONE)
                    {
                        yield return StartCoroutine(ActivateBonus(bpos.x, bpos.y));
                        if (enemyHpCur <= 0 || playerHpCur <= 0) break;
                    }
                }
                if (enemyHpCur <= 0 || playerHpCur <= 0) break;
                // Continue cascade loop — new matches may have been created by chain explosions
            }
        }
    }

    // ---- MATCH DETECTION ----
    // Returns set of (x,y) positions that are part of a match-3+.
    // CRITICAL: bonus gems (bonus != BONUS_NONE) are EXCLUDED from normal match runs —
    // they act as wild-blockers and can only be activated by explicit tap or chain explosion.
    // However if a bonus gem IS part of a match (surrounded by same-color), we collect it
    // for chain-activation after the normal gems are destroyed.
    List<Vector2Int> CollectMatches()
    {
        var inMatch = new bool[boardW, boardH];
        // Horizontal runs — skip cells with bonus gems (they break the run)
        for (int y=0;y<boardH;y++)
        {
            int runStart = 0;
            for (int x=1;x<=boardW;x++)
            {
                bool cont = x < boardW
                         && !boardModel[x,y].IsEmpty   && !boardModel[x-1,y].IsEmpty
                         && boardModel[x,y].bonus   == BONUS_NONE   // bonus gems break runs
                         && boardModel[x-1,y].bonus == BONUS_NONE
                         && boardModel[x,y].color   == boardModel[x-1,y].color;
                if (!cont)
                {
                    int run = x - runStart;
                    if (run >= 3) for (int k=runStart;k<x;k++) inMatch[k,y] = true;
                    runStart = x;
                }
            }
        }
        // Vertical runs — same bonus exclusion
        for (int x=0;x<boardW;x++)
        {
            int runStart = 0;
            for (int y=1;y<=boardH;y++)
            {
                bool cont = y < boardH
                         && !boardModel[x,y].IsEmpty   && !boardModel[x,y-1].IsEmpty
                         && boardModel[x,y].bonus   == BONUS_NONE
                         && boardModel[x,y-1].bonus == BONUS_NONE
                         && boardModel[x,y].color   == boardModel[x,y-1].color;
                if (!cont)
                {
                    int run = y - runStart;
                    if (run >= 3) for (int k=runStart;k<y;k++) inMatch[x,k] = true;
                    runStart = y;
                }
            }
        }
        var result = new List<Vector2Int>();
        for (int x=0;x<boardW;x++)
        for (int y=0;y<boardH;y++)
            if (inMatch[x,y]) result.Add(new Vector2Int(x,y));
        return result;
    }

    int CountRunAt(int px, int py)
    {
        int c = boardModel[px,py].color;
        if (c < 0) return 0;
        // Horizontal run length at this cell
        int hLen = 1;
        for (int x=px-1; x>=0 && boardModel[x,py].color==c; x--) hLen++;
        for (int x=px+1; x<boardW && boardModel[x,py].color==c; x++) hLen++;
        int vLen = 1;
        for (int y=py-1; y>=0 && boardModel[px,y].color==c; y--) vLen++;
        for (int y=py+1; y<boardH && boardModel[px,y].color==c; y++) vLen++;
        return Math.Max(hLen, vLen);
    }

    void DetectBonus(List<Vector2Int> matches, out int bx, out int by, out int btype, out int bcolor)
    {
        bx = by = -1; btype = BONUS_NONE; bcolor = -1;

        // Build run-length maps for bonus detection
        var runH = new int[boardW, boardH];
        var runV = new int[boardW, boardH];
        int maxRun = 0;

        for (int y=0;y<boardH;y++)
        {
            int runStart = 0;
            for (int x=1;x<=boardW;x++)
            {
                bool cont = x<boardW && !boardModel[x,y].IsEmpty && !boardModel[x-1,y].IsEmpty
                         && boardModel[x,y].color==boardModel[x-1,y].color;
                if (!cont)
                {
                    int run = x - runStart;
                    for (int k=runStart;k<x;k++) runH[k,y] = run;
                    if (run > maxRun) maxRun = run;
                    runStart = x;
                }
            }
        }
        for (int x=0;x<boardW;x++)
        {
            int runStart = 0;
            for (int y=1;y<=boardH;y++)
            {
                bool cont = y<boardH && !boardModel[x,y].IsEmpty && !boardModel[x,y-1].IsEmpty
                         && boardModel[x,y].color==boardModel[x,y-1].color;
                if (!cont)
                {
                    int run = y - runStart;
                    for (int k=runStart;k<y;k++) runV[x,k] = run;
                    if (run > maxRun) maxRun = run;
                    runStart = y;
                }
            }
        }

        // Priority: Color Bomb (5) > T/L Hammer > Line
        foreach (var pos in matches)
        {
            int rh = runH[pos.x, pos.y];
            int rv = runV[pos.x, pos.y];
            if (rh >= 5 || rv >= 5)
            {
                bx = pos.x; by = pos.y;
                bcolor = boardModel[pos.x, pos.y].color;
                btype = BONUS_COLOR_BOMB;
                return;
            }
        }
        foreach (var pos in matches)
        {
            if (runH[pos.x,pos.y] >= 3 && runV[pos.x,pos.y] >= 3)
            {
                bx = pos.x; by = pos.y;
                bcolor = boardModel[pos.x,pos.y].color;
                btype = BONUS_HAMMER;
                return;
            }
        }
        foreach (var pos in matches)
        {
            int rh = runH[pos.x, pos.y];
            int rv = runV[pos.x, pos.y];
            if (rh >= 4)
            {
                bx = pos.x; by = pos.y;
                bcolor = boardModel[pos.x,pos.y].color;
                btype = BONUS_LINE_H;
                return;
            }
            if (rv >= 4)
            {
                bx = pos.x; by = pos.y;
                bcolor = boardModel[pos.x,pos.y].color;
                btype = BONUS_LINE_V;
                return;
            }
        }
    }

    // ---- GRAVITY + REFILL ----
    void CollapseBoard()
    {
        for (int x=0;x<boardW;x++)
        {
            int writeY = boardH - 1;
            for (int y=boardH-1; y>=0; y--)
            {
                if (!boardModel[x,y].IsEmpty)
                {
                    if (writeY != y)
                    {
                        boardModel[x, writeY] = boardModel[x, y];
                        boardModel[x, y] = CellData.Empty;
                    }
                    writeY--;
                }
            }
        }
    }

    void RefillBoard()
    {
        var rng = new System.Random();
        for (int x=0;x<boardW;x++)
        for (int y=0;y<boardH;y++)
            if (boardModel[x,y].IsEmpty)
                boardModel[x,y] = new CellData { color = rng.Next(0, boardColors), bonus = BONUS_NONE, curse = false };
    }

    void SwapModel(int ax, int ay, int bx, int by)
    {
        var tmp = boardModel[ax, ay];
        boardModel[ax, ay] = boardModel[bx, by];
        boardModel[bx, by] = tmp;
    }

    // ---- BONUS ACTIVATION ----
    // Called when player taps a bonus gem OR when a bonus gem is caught in a chain explosion.
    // Collects affected cells, deals damage, animates, clears model, then collapses+refills.
    // Does NOT start a new cascade — caller (DoActivateBonusTap / ResolveAllMatches) handles that.
    IEnumerator ActivateBonus(int x, int y)
    {
        var cell = boardModel[x,y];
        if (cell.bonus == BONUS_NONE) yield break;

        var toDestroy = new List<Vector2Int>();

        switch (cell.bonus)
        {
            case BONUS_LINE_H:
                // Destroy entire row
                for (int cx=0;cx<boardW;cx++) toDestroy.Add(new Vector2Int(cx, y));
                TriggerVFX("bonus_hermes_step");
                break;
            case BONUS_LINE_V:
                // Destroy entire column
                for (int cy=0;cy<boardH;cy++) toDestroy.Add(new Vector2Int(x, cy));
                TriggerVFX("bonus_hermes_step");
                break;
            case BONUS_HAMMER:
                // Destroy 7x7 area (radius 3) centred on gem
                for (int cx=Math.Max(0,x-3);cx<=Math.Min(boardW-1,x+3);cx++)
                for (int cy=Math.Max(0,y-3);cy<=Math.Min(boardH-1,y+3);cy++)
                    toDestroy.Add(new Vector2Int(cx,cy));
                TriggerVFX("bonus_hephaestus_hammer");
                break;
            case BONUS_COLOR_BOMB:
                // Destroy ALL gems of the most-common colour on the board
                var counts = new int[boardColors];
                for (int cx=0;cx<boardW;cx++) for (int cy=0;cy<boardH;cy++)
                    if (!boardModel[cx,cy].IsEmpty && boardModel[cx,cy].color >= 0 && boardModel[cx,cy].color < boardColors)
                        counts[boardModel[cx,cy].color]++;
                int targetColor = 0;
                for (int c=1;c<boardColors;c++) if (counts[c] > counts[targetColor]) targetColor = c;
                for (int cx=0;cx<boardW;cx++) for (int cy=0;cy<boardH;cy++)
                    if (!boardModel[cx,cy].IsEmpty && boardModel[cx,cy].color == targetColor)
                        toDestroy.Add(new Vector2Int(cx,cy));
                // Also include the bomb itself if not already in list
                if (!toDestroy.Exists(p => p.x==x && p.y==y)) toDestroy.Add(new Vector2Int(x,y));
                TriggerVFX("bonus_zeus_lightning");
                break;
        }

        // The bonus gem itself is always consumed
        if (!toDestroy.Exists(p => p.x==x && p.y==y)) toDestroy.Add(new Vector2Int(x,y));

        if (toDestroy.Count > 0)
        {
            // Damage scales with cells destroyed, bonus multiplier x3
            int dmg = toDestroy.Count * 3;
            ApplyDamage(true, dmg);
            UpdateBattleHUD();
            yield return StartCoroutine(AnimateDestroy(toDestroy));
            // Collect any bonus gems that were inside the blast area for chaining
            var chainBonuses = new List<Vector2Int>();
            foreach (var pos in toDestroy)
                if (boardModel[pos.x,pos.y].bonus != BONUS_NONE && !(pos.x==x && pos.y==y))
                    chainBonuses.Add(pos);
            // Clear model
            foreach (var pos in toDestroy) boardModel[pos.x, pos.y] = CellData.Empty;
            CollapseBoard();
            RefillBoard();
            yield return StartCoroutine(AnimateDrop());
            FullSyncView();
            // Chain-activate any bonuses caught in the blast (after collapse so positions are stable)
            // Note: after collapse positions shift — skip chaining for simplicity (avoid infinite loops)
            // We already cleared their model cells above, so they won't re-activate.
        }
    }

    // ---- ANIMATIONS ----
    const float SWAP_DUR    = 0.18f;
    const float DESTROY_DUR = 0.22f;
    const float DROP_DUR    = 0.25f;

    IEnumerator AnimateSwap(int ax, int ay, int bx, int by)
    {
        if (gemRT == null) yield break;
        var posA = ModelToViewPos(ax, ay);
        var posB = ModelToViewPos(bx, by);
        // Only clear move/drop tweens — VFX (kind=3) must survive to auto-Destroy their GOs
        ClearMoveTweens();
        // Snap to current model positions first, so swap starts from correct place
        if (gemRT[ax,ay] != null) { gemRT[ax,ay].anchoredPosition = posA; }
        if (gemRT[bx,by] != null) { gemRT[bx,by].anchoredPosition = posB; }
        if (gemRT[ax,ay] != null) activeTweens.Add(new GemTween{ rt=gemRT[ax,ay], from=posA, to=posB, t=0, dur=SWAP_DUR, kind=0 });
        if (gemRT[bx,by] != null) activeTweens.Add(new GemTween{ rt=gemRT[bx,by], from=posB, to=posA, t=0, dur=SWAP_DUR, kind=0 });
        yield return StartCoroutine(WaitTweensMove());
    }

    IEnumerator AnimateDestroy(List<Vector2Int> cells)
    {
        if (gridRoot == null || cells.Count == 0) yield break;
        foreach (var pos in cells)
        {
            if (gemRT != null && pos.x < boardW && pos.y < boardH && gemRT[pos.x,pos.y] != null)
                activeTweens.Add(new GemTween{ rt=gemRT[pos.x,pos.y], img=gemImg[pos.x,pos.y],
                    from=ModelToViewPos(pos.x,pos.y), to=ModelToViewPos(pos.x,pos.y),
                    t=0, dur=DESTROY_DUR, kind=1 });

            int c = boardModel[pos.x,pos.y].color;
            SpawnDestroyVfx(pos.x, pos.y, c);
        }
        yield return StartCoroutine(WaitTweens());
        // Reset alpha on gem images (they still exist, will be synced by FullSyncView)
        foreach (var pos in cells)
            if (gemImg != null && pos.x < boardW && pos.y < boardH && gemImg[pos.x,pos.y] != null)
                gemImg[pos.x,pos.y].color = Color.white;
    }

    IEnumerator AnimateDrop()
    {
        if (gemRT == null) yield break;
        // Only clear move tweens — VFX (kind=3) must survive to properly Destroy their GOs
        ClearMoveTweens();
        // Spawn new gems above the board so they visibly fall down
        for (int x=0;x<boardW;x++)
        for (int y=0;y<boardH;y++)
        {
            if (gemRT[x,y] == null) continue;
            var target = ModelToViewPos(x, y);
            var cur    = gemRT[x,y].anchoredPosition;
            // If gem is already near target, skip (it was there before collapse)
            if (Vector2.Distance(cur, target) < 1f) continue;
            // If gem spawned at exactly y=0 local (top edge), push start above board
            float startY = (cur.y > target.y - 2f)
                ? -((-1) * cellSz + cellSz * 0.5f + 10f)  // one row above board top
                : cur.y;
            var spawnPos = new Vector2(target.x, startY);
            gemRT[x,y].anchoredPosition = spawnPos;
            activeTweens.Add(new GemTween{ rt=gemRT[x,y],
                from=spawnPos, to=target, t=0, dur=DROP_DUR, kind=2 });
        }
        yield return StartCoroutine(WaitTweensMove());
    }

    IEnumerator WaitTweens()
    {
        while (activeTweens.Count > 0)
            yield return null;
    }

    // Wait only for move/drop/fade tweens (kind 0,1,2,4) — don't block on VFX (kind=3)
    IEnumerator WaitTweensMove()
    {
        bool anyMove;
        do {
            anyMove = false;
            foreach (var tw in activeTweens)
                if (tw.kind != 3) { anyMove = true; break; }
            if (anyMove) yield return null;
        } while (anyMove);
    }

    void SpawnDestroyVfx(int x, int y, int color)
    {
        if (gridRoot == null) return;
        var go = new GameObject("VfxD" + x + "_" + y);
        go.transform.SetParent(gridRoot.transform, false);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        string key = (color >= 0 && color < DestroyVfxKeys.Length) ? DestroyVfxKeys[color] : DestroyVfxKeys[0];
        if (sprites.ContainsKey(key)) img.sprite = sprites[key];
        img.color = Color.white;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0.5f,0.5f);
        rt.sizeDelta = new Vector2(cellSz * 1.4f, cellSz * 1.4f);
        rt.anchoredPosition = ModelToViewPos(x, y);
        activeTweens.Add(new GemTween{ rt=rt, img=img, from=rt.anchoredPosition, to=rt.anchoredPosition,
            t=0, dur=0.4f, kind=3 });
    }

    // ---- ENEMY TURN COROUTINE ----
    IEnumerator DoEnemyTurn()
    {
        if (enemyFreezeTurns > 0)
        {
            enemyFreezeTurns--;
            EndEnemyTurn();
            yield break;
        }

        var keys = EnemySkillKey(curBattle);
        int n    = EnemySkillCount(curBattle);
        int sidx = enemyNextSkillIdx % Math.Max(1, n);
        string sk = (sidx < keys.Length) ? keys[sidx] : "pierce";
        ExecuteEnemySkill(sk);
        enemyNextSkillIdx = (enemyNextSkillIdx + 1) % Math.Max(1, n);

        // Find best swap
        int bestX = -1, bestY = -1, bestDir = 0, bestScore = 0;
        var rng = new System.Random();
        for (int x=0;x<boardW;x++)
        for (int y=0;y<boardH;y++)
        {
            if (x+1 < boardW)
            {
                SwapModel(x,y,x+1,y);
                int s = SimScore();
                SwapModel(x,y,x+1,y);
                if (s > bestScore) { bestScore=s; bestX=x; bestY=y; bestDir=0; }
            }
            if (y+1 < boardH)
            {
                SwapModel(x,y,x,y+1);
                int s = SimScore();
                SwapModel(x,y,x,y+1);
                if (s > bestScore) { bestScore=s; bestX=x; bestY=y; bestDir=1; }
            }
        }
        if (bestX < 0)
        {
            bestX = rng.Next(0, boardW);
            bestY = rng.Next(0, boardH - 1);
            bestDir = 1;
        }

        int tx = bestX + (bestDir == 0 ? 1 : 0);
        int ty = bestY + (bestDir == 1 ? 1 : 0);
        yield return StartCoroutine(AnimateSwap(bestX, bestY, tx, ty));
        SwapModel(bestX, bestY, tx, ty);

        var matched = CollectMatches();
        if (matched != null && matched.Count > 0)
            yield return StartCoroutine(ResolveAllMatches(false));
        else
        {
            // revert
            yield return StartCoroutine(AnimateSwap(bestX, bestY, tx, ty));
            SwapModel(bestX, bestY, tx, ty);
            FullSyncView();
        }

        if (CheckBattleEnd()) yield break;
        EndEnemyTurn();
    }

    int SimScore()
    {
        int score = 0;
        for (int y=0;y<boardH;y++) { int r=1; for (int x=1;x<boardW;x++) { if (!boardModel[x,y].IsEmpty && boardModel[x,y].color==boardModel[x-1,y].color) r++; else { if(r>=3) score+=r; r=1; } } if(r>=3) score+=r; }
        for (int x=0;x<boardW;x++) { int r=1; for (int y=1;y<boardH;y++) { if (!boardModel[x,y].IsEmpty && boardModel[x,y].color==boardModel[x,y-1].color) r++; else { if(r>=3) score+=r; r=1; } } if(r>=3) score+=r; }
        return score;
    }

    void EndEnemyTurn()
    {
        if (CheckBattleEnd()) return;
        if (extraTurn)
        {
            extraTurn = false;
            battleTurnText.text = L("EXTRA_TURN");
        }
        else
        {
            turnCount++;
            if (turnCount >= 2)
            {
                turnCount = 0; turnSide = 0;
                battleTurnText.text = L("TURN_PLAYER");
                UpdateEnemyIntent();
            }
            else
            {
                battleTurnText.text = L("TURN_ENEMY");
            }
        }
        boardPhase = (turnSide == 0) ? BoardPhase.Input : BoardPhase.EnemyTurn;
        if (boardPhase == BoardPhase.EnemyTurn)
            StartCoroutine(EnemyTurnDelayed());
    }

    IEnumerator EnemyTurnDelayed()
    {
        StartEnemyTurnVFX();
        yield return new WaitForSeconds(0.4f);  // brief pause for dim to appear
        yield return StartCoroutine(DoEnemyTurn());
        StopEnemyTurnVFX();
    }

    bool CheckBattleEnd()
    {
        if (enemyHpCur <= 0) { EndBattle(true); return true; }
        if (playerHpCur <= 0) { EndBattle(false); return true; }
        return false;
    }

    void EndBattle(bool victory)
    {
        boardPhase = BoardPhase.Done;
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
            StartBattle(currentEpisode, curBattle.id);
        else
        {
            state = State.Playing;
            idx++;
            SaveProgress();
            if (idx >= episodes[currentEpisode-1].script.Count) ShowEpisodeEnding();
            else ShowCurrentLine(false);
        }
    }

    void ApplyDamage(bool fromPlayer, int dmg)
    {
        if (fromPlayer) enemyHpCur = Math.Max(0, enemyHpCur - dmg);
        else            playerHpCur = Math.Max(0, playerHpCur - dmg);
    }

    void UpdateBattleHUD()
    {
        if (playerHpBar  != null) playerHpBar.fillAmount  = (float)playerHpCur / Math.Max(1, playerHpMax);
        if (enemyHpBar   != null) enemyHpBar.fillAmount   = (float)enemyHpCur  / Math.Max(1, enemyHpMax);
        if (battlePlayerHpText != null) battlePlayerHpText.text = playerHpCur + "/" + playerHpMax;
        if (battleEnemyHpText  != null) battleEnemyHpText.text  = enemyHpCur  + "/" + enemyHpMax;
        if (echoesText != null) echoesText.text = "" + echoes;
        if (sparksText != null) sparksText.text = "" + sparks;
    }

    // ---- ENEMY SKILLS ----
    int EnemySkillCount(BattleConfig b) { if (b.isBoss) return 3; if (b.id >= 6) return 2; return 1; }
    string[] EnemySkillKey(BattleConfig b)
    {
        if (b.isBoss) return new []{ "pact_blast","curse_storm","titan_wrath" };
        if (b.id >= 6) return new []{ "pierce","curse" };
        return new []{ "pierce" };
    }
    void UpdateEnemyIntent()
    {
        if (enemyIntentText == null || curBattle == null) return;
        var keys = EnemySkillKey(curBattle);
        int n = EnemySkillCount(curBattle);
        int i = enemyNextSkillIdx % Math.Max(1, n);
        string k = (i < keys.Length) ? keys[i] : "pierce";
        enemyIntentText.text = L("INTENT") + ": " + L("ENS_" + k.ToUpper());
    }
    void UpdateEnemySkillIcons()
    {
        if (curBattle == null) return;
        int n = EnemySkillCount(curBattle);
        for (int i=0;i<3;i++)
        {
            if (enemySkillIcons[i] == null) continue;
            enemySkillIcons[i].gameObject.SetActive(i < n);
        }
    }
    void ExecuteEnemySkill(string sk)
    {
        // Fix: do NOT flash vfxOverlay white for enemy skills — it caused a full-screen white flash.
        // VFX is reserved for player abilities (TriggerVFX). Enemy turn is visualised by claw/dim overlay.
        var rng = new System.Random();
        switch (sk)
        {
            case "pierce": ApplyDamage(false, 15); break;
            case "curse":
                for (int n=0;n<2;n++) { int cx=rng.Next(0,boardW), cy=rng.Next(0,boardH); if (!boardModel[cx,cy].IsEmpty) boardModel[cx,cy] = new CellData{color=5,bonus=0,curse=true}; }
                ApplyDamage(false, 8); FullSyncView(); break;
            case "pact_blast": ApplyDamage(false, 25); break;
            case "curse_storm":
                for (int n=0;n<4;n++) { int cx=rng.Next(0,boardW), cy=rng.Next(0,boardH); if (!boardModel[cx,cy].IsEmpty) boardModel[cx,cy] = new CellData{color=5,bonus=0,curse=true}; }
                ApplyDamage(false, 10); FullSyncView(); break;
            case "titan_wrath":
                ApplyDamage(false, playerHpCur < playerHpMax/2 ? 40 : 22); break;
            default: ApplyDamage(false, 12); break;
        }
        UpdateBattleHUD();
    }

    // ---- ABILITIES ----
    string[] AbilityKeys   = { "inferno","freeze","shuffle","cleanse","slam" };
    int[]    AbilityPrices = { 100, 150, 80, 120, 200 };

    void UseAbility(int idx)
    {
        if (state != State.Battle || boardPhase != BoardPhase.Input || turnSide != 0) return;
        string key = AbilityKeys[idx];
        if (!abilityCount.ContainsKey(key) || abilityCount[key] <= 0) return;
        ClearAbilityZone();  // hide zone preview when ability fires
        abilityCount[key]--;
        SaveProgress();
        boardPhase = BoardPhase.Resolving;
        // Tutorial: abilities in first 4 battles
        if (!tutorialAbilityDone && curBattle != null && curBattle.id <= 4)
        {
            tutorialAbilityDone = true;
            ShowTutorialHint(
                "Ability activated! Abilities are powerful special moves — use them wisely!",
                "Умение активировано! Способности — мощные особые приёмы. Используй их с умом!"
            );
        }
        StartCoroutine(UseAbilityCoroutine(key));
    }

    IEnumerator UseAbilityCoroutine(string key)
    {
        switch (key)
        {
            case "inferno":  yield return StartCoroutine(AbilityInferno()); break;
            case "freeze":   AbilityFreeze(); break;
            case "shuffle":  yield return StartCoroutine(AbilityShuffle()); break;
            case "cleanse":  yield return StartCoroutine(AbilityCleanse()); break;
            case "slam":     yield return StartCoroutine(AbilitySlam()); break;
        }
        RefreshAbilityButtons();
        boardPhase = BoardPhase.Input;
    }

    IEnumerator AbilityInferno()
    {
        int cx = boardW/2, cy = boardH/2;
        var cells = new List<Vector2Int>();
        for (int x=Math.Max(0,cx-1);x<=Math.Min(boardW-1,cx+1);x++)
        for (int y=Math.Max(0,cy-1);y<=Math.Min(boardH-1,cy+1);y++)
            cells.Add(new Vector2Int(x,y));
        ApplyDamage(true, cells.Count * 15);
        UpdateBattleHUD();
        yield return StartCoroutine(AnimateDestroy(cells));
        foreach (var p in cells) boardModel[p.x,p.y] = CellData.Empty;
        CollapseBoard(); RefillBoard();
        yield return StartCoroutine(AnimateDrop());
        FullSyncView();
        TriggerVFX("vfx_inferno_burst");
    }

    void AbilityFreeze() { enemyFreezeTurns += 2; TriggerVFX("vfx_freeze"); }

    IEnumerator AbilityShuffle()
    {
        var rng = new System.Random();
        for (int x=0;x<boardW;x++) for (int y=0;y<boardH;y++)
            boardModel[x,y] = new CellData{ color=rng.Next(0,boardColors), bonus=BONUS_NONE, curse=false };
        FullSyncView();
        yield return StartCoroutine(ResolveAllMatches(true));
    }

    IEnumerator AbilityCleanse()
    {
        var cells = new List<Vector2Int>();
        for (int x=0;x<boardW;x++) for (int y=0;y<boardH;y++)
            if (boardModel[x,y].color == 5) cells.Add(new Vector2Int(x,y));
        if (cells.Count > 0)
        {
            yield return StartCoroutine(AnimateDestroy(cells));
            foreach (var p in cells) boardModel[p.x,p.y] = CellData.Empty;
            CollapseBoard(); RefillBoard();
            yield return StartCoroutine(AnimateDrop());
        }
        FullSyncView();
    }

    IEnumerator AbilitySlam()
    {
        int col = boardW/2;
        var cells = new List<Vector2Int>();
        for (int y=0;y<boardH;y++) cells.Add(new Vector2Int(col,y));
        ApplyDamage(true, cells.Count * 25);
        UpdateBattleHUD();
        yield return StartCoroutine(AnimateDestroy(cells));
        foreach (var p in cells) boardModel[p.x,p.y] = CellData.Empty;
        CollapseBoard(); RefillBoard();
        yield return StartCoroutine(AnimateDrop());
        FullSyncView();
        TriggerVFX("vfx_titan_slam");
    }

    void TriggerVFX(string key)
    {
        if (sprites.ContainsKey(key)) { vfxOverlay.sprite = sprites[key]; vfxOverlay.color = Color.white; }
        vfxAnim = key; vfxT = 0f;
        PlaySfx("sfx_choice");
    }

    // ---- ABILITY ZONE HIGHLIGHT ----
    // Shows semi-transparent tinted overlays on the cells that the ability would affect.
    // Called on ability button PointerDown; cleared on PointerUp or when ability fires.
    void ShowAbilityZone(int idx)
    {
        ClearAbilityZone();
        abilityPreviewIdx = idx;
        if (gridRoot == null) return;
        string key = AbilityKeys[idx];
        var cells = GetAbilityCells(key);
        Color tint = new Color(1f, 0.85f, 0.1f, 0.38f); // gold highlight
        foreach (var pos in cells)
        {
            if (pos.x < 0 || pos.x >= boardW || pos.y < 0 || pos.y >= boardH) continue;
            var go = new GameObject("AbilZone_" + pos.x + "_" + pos.y);
            go.transform.SetParent(gridRoot.transform, false);
            var img = go.AddComponent<Image>(); img.color = tint; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0.5f,0.5f);
            rt.sizeDelta = new Vector2(cellSz - 4f, cellSz - 4f);
            rt.anchoredPosition = ModelToViewPos(pos.x, pos.y);
            abilityHighlightImgs.Add(img);
        }
    }

    void ClearAbilityZone()
    {
        foreach (var img in abilityHighlightImgs)
            if (img != null && img.gameObject != null) UnityEngine.Object.Destroy(img.gameObject);
        abilityHighlightImgs.Clear();
        abilityPreviewIdx = -1;
    }

    // Returns the set of board cells that an ability would affect (mirrors ability logic)
    List<Vector2Int> GetAbilityCells(string key)
    {
        var cells = new List<Vector2Int>();
        switch (key)
        {
            case "inferno":
                int cx = boardW/2, cy = boardH/2;
                for (int x=Math.Max(0,cx-1);x<=Math.Min(boardW-1,cx+1);x++)
                for (int y=Math.Max(0,cy-1);y<=Math.Min(boardH-1,cy+1);y++)
                    cells.Add(new Vector2Int(x,y));
                break;
            case "slam":
                int col = boardW/2;
                for (int y=0;y<boardH;y++) cells.Add(new Vector2Int(col,y));
                break;
            case "cleanse":
                for (int x=0;x<boardW;x++) for (int y=0;y<boardH;y++)
                    if (boardModel[x,y].color == 5) cells.Add(new Vector2Int(x,y));
                break;
            case "freeze":
                // freeze affects the enemy, highlight entire board with blue tint
                for (int x=0;x<boardW;x++) for (int y=0;y<boardH;y++)
                    cells.Add(new Vector2Int(x,y));
                break;
            case "shuffle":
                // shuffle affects everything
                for (int x=0;x<boardW;x++) for (int y=0;y<boardH;y++)
                    cells.Add(new Vector2Int(x,y));
                break;
        }
        return cells;
    }

    // Colour tint per ability for the zone highlight
    Color GetAbilityZoneColor(string key)
    {
        switch (key)
        {
            case "inferno":  return new Color(1f, 0.4f, 0.1f, 0.42f);
            case "slam":     return new Color(1f, 0.9f, 0.2f, 0.42f);
            case "cleanse":  return new Color(0.5f, 1f, 0.5f, 0.42f);
            case "freeze":   return new Color(0.4f, 0.8f, 1f, 0.30f);
            case "shuffle":  return new Color(0.9f, 0.6f, 1f, 0.28f);
        }
        return new Color(1f, 0.85f, 0.1f, 0.38f);
    }

    // Coloured zone highlight (colour per ability)
    void ShowAbilityZoneColored(int idx)
    {
        ClearAbilityZone();
        abilityPreviewIdx = idx;
        if (gridRoot == null) return;
        string key = AbilityKeys[idx];
        var cells = GetAbilityCells(key);
        Color tint = GetAbilityZoneColor(key);
        foreach (var pos in cells)
        {
            if (pos.x < 0 || pos.x >= boardW || pos.y < 0 || pos.y >= boardH) continue;
            var go = new GameObject("AbilZone_" + pos.x + "_" + pos.y);
            go.transform.SetParent(gridRoot.transform, false);
            var img = go.AddComponent<Image>(); img.color = tint; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(0,1); rt.pivot = new Vector2(0.5f,0.5f);
            rt.sizeDelta = new Vector2(cellSz - 4f, cellSz - 4f);
            rt.anchoredPosition = ModelToViewPos(pos.x, pos.y);
            abilityHighlightImgs.Add(img);
        }
    }

    // ---- ENEMY TURN VISUAL (DIM + CLAW) ----
    void StartEnemyTurnVFX()
    {
        if (enemyTurnDimImg == null) return;
        // 5% dim
        enemyTurnDimImg.color = new Color(0,0,0,0.05f);
        // Claw: pick sprite based on enemy type
        if (enemyClawImg != null)
        {
            string clawKey = "portrait_fallen"; // default
            if (curBattle != null)
            {
                if (curBattle.enemyKey.Contains("minotaur"))        clawKey = "enemy_minotaur";
                else if (curBattle.enemyKey.Contains("shadow"))     clawKey = "enemy_shadow_priestess";
                else if (curBattle.enemyKey.Contains("hoplite"))    clawKey = "enemy_hoplite_corrupt";
                else                                                 clawKey = curBattle.enemyKey;
            }
            if (sprites.ContainsKey(clawKey)) { enemyClawImg.sprite = sprites[clawKey]; }
            // Animate: slide from off-screen top-right into the board area
            var rt = enemyClawImg.rectTransform;
            enemyClawFrom = new Vector2(160f, -160f);   // off-screen (top-right corner)
            enemyClawTo   = new Vector2(-80f, -380f);   // overlapping board area
            rt.anchoredPosition = enemyClawFrom;
            enemyClawImg.color = new Color(1,1,1,0.9f);
            enemyClawT = 0f;
            enemyClawActive = true;
        }
    }

    void StopEnemyTurnVFX()
    {
        if (enemyTurnDimImg != null) enemyTurnDimImg.color = new Color(0,0,0,0);
        if (enemyClawImg    != null) enemyClawImg.color    = new Color(1,1,1,0);
        enemyClawActive = false;
    }

    // ---- TUTORIAL HINTS ----
    void ShowTutorialHint(string msgEn, string msgRu)
    {
        if (tutorialPanelGO == null) return;
        tutorialText.text = (lang == Lang.EN) ? msgEn : msgRu;
        tutorialPanelGO.SetActive(true);
        tutorialTimer = TUTORIAL_DUR;
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
    // PART 3 — Update loop (only VN/UI/Fade/Tweens — no match logic here!)
    // =====================================================================
    void Update()
    {
        // Fade overlay
        if (fadeDir != 0)
        {
            fadeAlpha += fadeDir * Time.deltaTime * 0.7f;
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
            bgImage.color     = new Color(1,1,1,1f-a);
            if (a >= 1f)
            {
                bgImage.sprite = bgImageNext.sprite;
                bgImage.color = Color.white;
                bgImageNext.color = new Color(1,1,1,0);
                currentBgKey = pendingBg; crossfading = false;
            }
        }

        // Typewriter
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

        // Next-button pulse
        if (nextBtnGO.activeSelf)
        { pulseT += Time.deltaTime * 3f; float p = 0.85f + 0.15f * Mathf.Sin(pulseT); nextBtnText.color = new Color(1f,0.95f,0.85f,p); }

        // VFX overlay fade
        if (!string.IsNullOrEmpty(vfxAnim))
        {
            vfxT += Time.deltaTime;
            float a = Mathf.Clamp01(1f - vfxT / 0.9f);
            vfxOverlay.color = new Color(1,1,1,a);
            if (vfxT >= 0.9f) { vfxAnim = ""; vfxOverlay.color = new Color(1,1,1,0); }
        }

        // Tween processing (swap-move, fade-out, drop, destroyVfx)
        if (activeTweens.Count > 0)
        {
            for (int i = activeTweens.Count - 1; i >= 0; i--)
            {
                var tw = activeTweens[i];
                tw.t += Time.deltaTime;
                float a = Mathf.Clamp01(tw.t / Mathf.Max(0.0001f, tw.dur));
                float ease = a * a * (3f - 2f * a);
                switch (tw.kind)
                {
                    case 0: // move (swap / revert)
                    case 2: // drop
                        if (tw.rt != null) tw.rt.anchoredPosition = Vector2.LerpUnclamped(tw.from, tw.to, ease);
                        break;
                    case 1: // fade gem out during destroy
                        if (tw.img != null) tw.img.color = new Color(1,1,1, 1f - a);
                        break;
                    case 3: // destroy VFX overlay — fade + scale, then Destroy GO
                        if (tw.img != null) { tw.img.color = new Color(1f,1f,1f, 1f - a); if (tw.rt != null) tw.rt.localScale = new Vector3(1f + a * 0.6f, 1f + a * 0.6f, 1f); }
                        break;
                    case 4: // selection pulse — ping-pong scale (loops until removed)
                    {
                        float ping = Mathf.Abs(Mathf.Sin(tw.t * Mathf.PI * 2.5f));
                        float sc = Mathf.Lerp(1.08f, 1.20f, ping);
                        if (tw.rt != null) tw.rt.localScale = new Vector3(sc, sc, 1f);
                        if (a >= 1f) tw.t = 0f;  // loop: reset time, don't remove
                        break;
                    }
                }
                if (a >= 1f && tw.kind != 4) // kind=4 (pulse) loops, never auto-removed
                {
                    if (tw.kind == 3 && tw.img != null && tw.img.gameObject != null)
                        UnityEngine.Object.Destroy(tw.img.gameObject);
                    activeTweens.RemoveAt(i);
                }
            }
        }

        // Ability cooldown UI
        if (state == State.Battle)
        {
            for (int i=0;i<abilityCdT.Length;i++)
            {
                if (abilityCdT[i] > 0f)
                {
                    abilityCdT[i] -= Time.deltaTime;
                    if (abilityCdT[i] < 0f) abilityCdT[i] = 0f;
                    if (abilityCdMask[i] != null && abilityCdDur[i] > 0f)
                        abilityCdMask[i].fillAmount = abilityCdT[i] / abilityCdDur[i];
                }
                else if (abilityCdMask[i] != null)
                    abilityCdMask[i].fillAmount = 0f;
            }
            RefreshAbilityButtons();
        }

        // Enemy claw slide animation
        if (enemyClawActive && enemyClawImg != null)
        {
            enemyClawT = Mathf.MoveTowards(enemyClawT, 1f, Time.deltaTime * 2.2f);
            float ease = enemyClawT * enemyClawT * (3f - 2f * enemyClawT);
            enemyClawImg.rectTransform.anchoredPosition = Vector2.Lerp(enemyClawFrom, enemyClawTo, ease);
        }

        // Tutorial hint auto-hide
        if (tutorialTimer > 0f)
        {
            tutorialTimer -= Time.deltaTime;
            if (tutorialTimer <= 0f && tutorialPanelGO != null)
                tutorialPanelGO.SetActive(false);
        }
    }

    // =====================================================================
    // PART 4 — UI build (preserved from v4.0.0)
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

        bgImage     = MakeImage(canvas.transform, "BG", Color.black);     Stretch(bgImage.rectTransform);
        bgImageNext = MakeImage(canvas.transform, "BGNext", new Color(1,1,1,0)); Stretch(bgImageNext.rectTransform);

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

        dialogBox = MakeImage(canvas.transform, "DialogBox", new Color(0.05f,0.04f,0.08f,0.88f));
        var db = dialogBox.rectTransform;
        db.anchorMin = new Vector2(0,0); db.anchorMax = new Vector2(1,0);
        db.pivot = new Vector2(0.5f,0); db.anchoredPosition = new Vector2(0,40); db.sizeDelta = new Vector2(-80, 520);
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

        var tapGO = new GameObject("TapArea"); tapGO.transform.SetParent(canvas.transform, false);
        var tapImg = tapGO.AddComponent<Image>(); tapImg.color = new Color(0,0,0,0); tapImg.raycastTarget = true;
        Stretch(tapImg.rectTransform);
        var tapBtn = tapGO.AddComponent<Button>(); tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(OnTap);
        dialogBox.transform.SetAsLastSibling();

        skipBtnGO = MakeBottomRightButton(canvas.transform, "SkipBtn", "SKIP", new Vector2(-30, 610), OnSkip, out skipBtnText, new Color(0.9f,0.6f,0.2f,1f));
        skipBtnGO.SetActive(false);
        nextBtnGO = MakeBottomRightButton(canvas.transform, "NextBtn", "NEXT", new Vector2(-30, 610), OnNext, out nextBtnText, new Color(0.4f,0.85f,0.5f,1f));
        nextBtnGO.SetActive(false);

        // Title panel
        titlePanel = new GameObject("TitlePanel"); titlePanel.transform.SetParent(canvas.transform, false);
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
        settingsPanel = new GameObject("SettingsPanel"); settingsPanel.transform.SetParent(canvas.transform, false);
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
        Text shopBtnText;
        MakeFlatButton(settingsPanel.transform, "ShopBtn", new Vector2(0,-170), new Vector2(840,140), OnOpenShop, out shopBtnText);
        shopBtnText.text = "SHOP / МАГАЗИН";
        settingsPanel.SetActive(false);

        // Choice panel
        choicePanel = new GameObject("ChoicePanel"); choicePanel.transform.SetParent(canvas.transform, false);
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
        preBattlePanel = new GameObject("PreBattle"); preBattlePanel.transform.SetParent(canvas.transform, false);
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
        battlePanel = new GameObject("BattlePanel"); battlePanel.transform.SetParent(canvas.transform, false);
        var btRT = battlePanel.AddComponent<RectTransform>(); Stretch(btRT);
        battleBg = MakeImage(battlePanel.transform, "BattleBg", Color.white); Stretch(battleBg.rectTransform); battleBg.color = new Color(0.4f,0.4f,0.4f,1f);

        // TopBar — menu button top-right
        var menuBtnGo = new GameObject("BattleMenuBtn"); menuBtnGo.transform.SetParent(battlePanel.transform, false);
        var menuImg = menuBtnGo.AddComponent<Image>(); menuImg.color = new Color(0.10f,0.08f,0.15f,0.95f);
        if (sprites.ContainsKey("circle_mask")) menuImg.sprite = sprites["circle_mask"];
        var menuBtnRT = menuImg.rectTransform;
        menuBtnRT.anchorMin = new Vector2(1,1); menuBtnRT.anchorMax = new Vector2(1,1); menuBtnRT.pivot = new Vector2(1,1);
        menuBtnRT.anchoredPosition = new Vector2(-25,-25); menuBtnRT.sizeDelta = new Vector2(110, 110);
        menuBtnGo.transform.SetAsLastSibling();
        AddOutline(menuBtnGo, new Color(0.85f,0.75f,0.35f,1f), 3);
        var menuBtn = menuBtnGo.AddComponent<Button>();
        menuBtn.onClick.AddListener(() => OnBattleMenu());
        var menuTxt = MakeText(menuBtnGo.transform, "MnuTxt", "☰", 56, new Color(1f,0.9f,0.55f,1f));
        Stretch(menuTxt.rectTransform); menuTxt.alignment = TextAnchor.MiddleCenter; menuTxt.fontStyle = FontStyle.Bold;
        AddOutline(menuTxt.gameObject, new Color(0,0,0,1), 2);

        // EnemyPanel
        var enemyPanel = MakeImage(battlePanel.transform, "EnemyPanel", new Color(0.08f,0.05f,0.12f,0.55f));
        var epnRT = enemyPanel.rectTransform;
        epnRT.anchorMin = new Vector2(0,1); epnRT.anchorMax = new Vector2(1,1); epnRT.pivot = new Vector2(0.5f,1);
        epnRT.anchoredPosition = new Vector2(0,-110); epnRT.sizeDelta = new Vector2(-30, 360);
        AddOutline(enemyPanel.gameObject, new Color(0.7f,0.2f,0.2f,0.8f), 2);
        battleEnemyPortrait = MakeImage(enemyPanel.transform, "EnemyPort", Color.white);
        var epRT = battleEnemyPortrait.rectTransform;
        epRT.anchorMin = new Vector2(0,1); epRT.anchorMax = new Vector2(0,1); epRT.pivot = new Vector2(0,1);
        epRT.anchoredPosition = new Vector2(15,-15); epRT.sizeDelta = new Vector2(240, 300);
        battleEnemyPortrait.preserveAspect = true;
        AddOutline(battleEnemyPortrait.gameObject, new Color(0.9f,0.3f,0.3f,0.95f), 3);
        var ehpBg = MakeImage(enemyPanel.transform, "EHpBg", new Color(0.2f,0.05f,0.05f,0.96f));
        var ehbRT = ehpBg.rectTransform;
        ehbRT.anchorMin = new Vector2(0,1); ehbRT.anchorMax = new Vector2(1,1); ehbRT.pivot = new Vector2(0,1);
        ehbRT.anchoredPosition = new Vector2(270,-20); ehbRT.sizeDelta = new Vector2(-285, 70);
        AddOutline(ehpBg.gameObject, new Color(0.9f,0.3f,0.3f,1f), 2);
        enemyHpBar = MakeImage(ehpBg.transform, "EHpFill", new Color(0.85f,0.18f,0.18f,1));
        Stretch(enemyHpBar.rectTransform); enemyHpBar.fillAmount = 1f;
        battleEnemyHpText = MakeText(ehpBg.transform, "EHpTxt", "100/100", 32, Color.white);
        Stretch(battleEnemyHpText.rectTransform); battleEnemyHpText.alignment = TextAnchor.MiddleCenter; battleEnemyHpText.fontStyle = FontStyle.Bold;
        AddOutline(battleEnemyHpText.gameObject, new Color(0,0,0,1), 2);
        enemyIntentText = MakeText(enemyPanel.transform, "EnIntent", "", 22, new Color(1f,0.85f,0.55f,1f));
        var einRT = enemyIntentText.rectTransform;
        einRT.anchorMin = new Vector2(0,1); einRT.anchorMax = new Vector2(1,1); einRT.pivot = new Vector2(0,1);
        einRT.anchoredPosition = new Vector2(270,-100); einRT.sizeDelta = new Vector2(-285, 36);
        enemyIntentText.alignment = TextAnchor.MiddleLeft; enemyIntentText.fontStyle = FontStyle.Bold;
        AddOutline(enemyIntentText.gameObject, new Color(0,0,0,1), 2);
        for (int i=0;i<3;i++)
        {
            var esGo = new GameObject("EnSkill"+i); esGo.transform.SetParent(enemyPanel.transform, false);
            // Fix D: enemy skill slots styled distinctly — dark red/purple bg with vivid red border
            var esImg = esGo.AddComponent<Image>(); esImg.color = new Color(0.22f,0.05f,0.05f,0.97f);
            var esRT = esImg.rectTransform;
            esRT.anchorMin = new Vector2(0,1); esRT.anchorMax = new Vector2(0,1); esRT.pivot = new Vector2(0,1);
            esRT.anchoredPosition = new Vector2(270 + i*180, -150); esRT.sizeDelta = new Vector2(160, 160);
            AddOutline(esGo, new Color(1f,0.15f,0.15f,1f), 5);
            // skull / enemy indicator badge in top-left corner
            var badgeGo = new GameObject("EnSkBadge"+i); badgeGo.transform.SetParent(esGo.transform, false);
            var badgeImg = badgeGo.AddComponent<Image>(); badgeImg.color = new Color(0.9f,0.15f,0.15f,1f); badgeImg.raycastTarget = false;
            var badgeRT = badgeImg.rectTransform;
            badgeRT.anchorMin = new Vector2(0,1); badgeRT.anchorMax = new Vector2(0,1); badgeRT.pivot = new Vector2(0,1);
            badgeRT.anchoredPosition = new Vector2(0,0); badgeRT.sizeDelta = new Vector2(28,28);
            var badgeTxt = MakeText(badgeGo.transform, "EnSkBadgeTxt"+i, "!", 22, Color.white);
            Stretch(badgeTxt.rectTransform); badgeTxt.alignment = TextAnchor.MiddleCenter; badgeTxt.fontStyle = FontStyle.Bold;
            enemySkillIcons[i] = esImg;
            var cdT = MakeText(esGo.transform, "EnSkCd"+i, "", 30, new Color(1f,0.95f,0.85f,1f));
            var cdRT = cdT.rectTransform;
            cdRT.anchorMin = new Vector2(1,0); cdRT.anchorMax = new Vector2(1,0); cdRT.pivot = new Vector2(1,0);
            cdRT.anchoredPosition = new Vector2(-8,8); cdRT.sizeDelta = new Vector2(50,40);
            cdT.alignment = TextAnchor.MiddleRight; cdT.fontStyle = FontStyle.Bold;
            AddOutline(cdT.gameObject, new Color(0,0,0,1), 2);
            enemySkillCdText[i] = cdT;
            esGo.SetActive(false);
        }

        // BoardPanel safe zone
        var boardPanel = MakeImage(battlePanel.transform, "BoardPanel", new Color(0,0,0,0.0f));
        var bpRT = boardPanel.rectTransform;
        bpRT.anchorMin = new Vector2(0,0); bpRT.anchorMax = new Vector2(1,1); bpRT.pivot = new Vector2(0.5f,0.5f);
        bpRT.offsetMin = new Vector2(20, 280); bpRT.offsetMax = new Vector2(-20, -480);
        boardPanelRT = bpRT;

        // Player Skill Bar
        var skillBar = MakeImage(battlePanel.transform, "PlayerSkillBar", new Color(0.08f,0.06f,0.10f,0.55f));
        var sbRT = skillBar.rectTransform;
        sbRT.anchorMin = new Vector2(0,0); sbRT.anchorMax = new Vector2(1,0); sbRT.pivot = new Vector2(0.5f,0);
        sbRT.anchoredPosition = new Vector2(0,90); sbRT.sizeDelta = new Vector2(-30, 180);
        AddOutline(skillBar.gameObject, new Color(0.4f,0.6f,0.5f,0.6f), 2);
        string[] vfxKeys = { "vfx_inferno_burst","vfx_freeze","vfx_titan_slam","bonus_hermes_step","bonus_zeus_lightning" };
        Color[] ringTints = { new Color(1f,0.45f,0.15f,1f), new Color(0.5f,0.85f,1f,1f), new Color(1f,0.9f,0.3f,1f), new Color(0.9f,0.8f,0.4f,1f), new Color(0.6f,0.85f,1f,1f) };
        for (int i=0;i<5;i++)
        {
            int captured = i;
            var ringGo = new GameObject("AbilRing"+i); ringGo.transform.SetParent(skillBar.transform, false);
            var ringImg = ringGo.AddComponent<Image>(); ringImg.color = new Color(0.10f,0.08f,0.15f,0.97f);
            if (sprites.ContainsKey("circle_mask")) { ringImg.sprite = sprites["circle_mask"]; ringImg.color = ringTints[i]; }
            var ringRT = ringImg.rectTransform;
            ringRT.anchorMin = new Vector2(0,0.5f); ringRT.anchorMax = new Vector2(0,0.5f); ringRT.pivot = new Vector2(0,0.5f);
            ringRT.anchoredPosition = new Vector2(15 + i*200, 0); ringRT.sizeDelta = new Vector2(150, 150);
            AddOutline(ringGo, ringTints[i], 4);
            abilityRingBg[i] = ringImg;
            var iconGo = new GameObject("AbilIcon"+i); iconGo.transform.SetParent(ringGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            if (sprites.ContainsKey(vfxKeys[i])) iconImg.sprite = sprites[vfxKeys[i]];
            iconImg.color = Color.white; iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            var iRT = iconImg.rectTransform; iRT.anchorMin = new Vector2(0,0); iRT.anchorMax = new Vector2(1,1); iRT.pivot = new Vector2(0.5f,0.5f);
            iRT.offsetMin = new Vector2(10,10); iRT.offsetMax = new Vector2(-10,-10);
            var cdGo = new GameObject("AbilCd"+i); cdGo.transform.SetParent(ringGo.transform, false);
            var cdImg = cdGo.AddComponent<Image>(); cdImg.color = new Color(0,0,0,0.7f); cdImg.raycastTarget = false;
            cdImg.type = Image.Type.Filled; cdImg.fillAmount = 0f;
            var cdRT = cdImg.rectTransform; cdRT.anchorMin = Vector2.zero; cdRT.anchorMax = Vector2.one; cdRT.offsetMin = Vector2.zero; cdRT.offsetMax = Vector2.zero;
            abilityCdMask[i] = cdImg;
            // Fix: Button + EventTrigger on the same GO breaks Button.onClick in Unity.
            // Solution: use ONLY EventTrigger with PointerDown/Up/Click.
            var abET = ringGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            entryDown.callback.AddListener(_ => ShowAbilityZoneColored(captured));
            abET.triggers.Add(entryDown);
            var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            entryUp.callback.AddListener(_ => ClearAbilityZone());
            abET.triggers.Add(entryUp);
            // PointerClick replaces Button.onClick (no EventTrigger conflict)
            var entryClick = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryClick.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entryClick.callback.AddListener(_ => UseAbility(captured));
            abET.triggers.Add(entryClick);
            var abTxt = MakeText(ringGo.transform, "Cost", "", 22, new Color(1f,0.95f,0.85f,1f));
            var atRT = abTxt.rectTransform; atRT.anchorMin = new Vector2(0,0); atRT.anchorMax = new Vector2(1,0); atRT.pivot = new Vector2(0.5f,0);
            atRT.anchoredPosition = new Vector2(0,-22); atRT.sizeDelta = new Vector2(0,40);
            abTxt.alignment = TextAnchor.MiddleCenter; abTxt.fontStyle = FontStyle.Bold;
            AddOutline(abTxt.gameObject, new Color(0,0,0,1), 2);
            abilityButtonsGO.Add(ringGo);
            abilityButtonsText.Add(abTxt);
        }

        // Player HP bar
        var phpBg = MakeImage(battlePanel.transform, "PHpBg", new Color(0.04f,0.16f,0.06f,0.96f));
        var phbRT = phpBg.rectTransform;
        phbRT.anchorMin = new Vector2(0,0); phbRT.anchorMax = new Vector2(1,0); phbRT.pivot = new Vector2(0.5f,0);
        phbRT.offsetMin = new Vector2(15,10); phbRT.offsetMax = new Vector2(-15, 90);
        AddOutline(phpBg.gameObject, new Color(0.3f,0.85f,0.5f,1f), 3);
        playerHpBar = MakeImage(phpBg.transform, "PHpFill", new Color(0.3f,0.85f,0.3f,1));
        Stretch(playerHpBar.rectTransform); playerHpBar.fillAmount = 1f;
        battlePlayerHpText = MakeText(phpBg.transform, "PHpTxt", "100/100", 36, Color.white);
        Stretch(battlePlayerHpText.rectTransform); battlePlayerHpText.alignment = TextAnchor.MiddleCenter; battlePlayerHpText.fontStyle = FontStyle.Bold;
        AddOutline(battlePlayerHpText.gameObject, new Color(0,0,0,1), 2);

        battlePlayerPortrait = MakeImage(battlePanel.transform, "PlayerPortHidden", new Color(1,1,1,0));
        battlePlayerPortrait.raycastTarget = false; battlePlayerPortrait.preserveAspect = true;
        var ppRT = battlePlayerPortrait.rectTransform;
        ppRT.anchorMin = new Vector2(0,0); ppRT.anchorMax = new Vector2(0,0); ppRT.pivot = new Vector2(0,0);
        ppRT.anchoredPosition = new Vector2(-9999,-9999); ppRT.sizeDelta = new Vector2(10,10);

        battleTurnText = MakeText(battlePanel.transform, "Turn", "", 40, new Color(1f,0.9f,0.5f,1f));
        var ttuRT = battleTurnText.rectTransform;
        ttuRT.anchorMin = new Vector2(0,1); ttuRT.anchorMax = new Vector2(1,1); ttuRT.pivot = new Vector2(0.5f,1);
        ttuRT.anchoredPosition = new Vector2(0,-490); ttuRT.sizeDelta = new Vector2(-200,60);
        battleTurnText.alignment = TextAnchor.MiddleCenter; battleTurnText.fontStyle = FontStyle.Bold;
        AddOutline(battleTurnText.gameObject, new Color(0,0,0,1), 3);

        vfxOverlay = MakeImage(battlePanel.transform, "Vfx", new Color(1,1,1,0));
        var vfRT = vfxOverlay.rectTransform;
        vfRT.anchorMin = new Vector2(0.5f,0.5f); vfRT.anchorMax = new Vector2(0.5f,0.5f);
        vfRT.pivot = new Vector2(0.5f,0.5f); vfRT.anchoredPosition = new Vector2(0,0); vfRT.sizeDelta = new Vector2(900,900);
        vfxOverlay.preserveAspect = true; vfxOverlay.raycastTarget = false;

        // Enemy turn dim overlay (5% black, covers whole battle panel)
        enemyTurnDimImg = MakeImage(battlePanel.transform, "EnemyDim", new Color(0,0,0,0));
        Stretch(enemyTurnDimImg.rectTransform);
        enemyTurnDimImg.raycastTarget = false;

        // Enemy claw / hand sprite (slides in from top-right during enemy turn)
        enemyClawImg = MakeImage(battlePanel.transform, "EnemyClaw", new Color(1,1,1,0));
        var clawRT = enemyClawImg.rectTransform;   // renamed: ecRT already used below for echoesText
        clawRT.anchorMin = new Vector2(1,1); clawRT.anchorMax = new Vector2(1,1); clawRT.pivot = new Vector2(1,1);
        clawRT.sizeDelta = new Vector2(320,320); clawRT.anchoredPosition = new Vector2(0,0);
        enemyClawImg.preserveAspect = true; enemyClawImg.raycastTarget = false;

        // Tutorial hint panel (center of screen, auto-hides)
        tutorialPanelGO = new GameObject("TutPanel"); tutorialPanelGO.transform.SetParent(canvas.transform, false);
        var tutRT = tutorialPanelGO.AddComponent<RectTransform>();  // renamed: tpRT already used elsewhere
        tutRT.anchorMin = new Vector2(0.5f,0.5f); tutRT.anchorMax = new Vector2(0.5f,0.5f);
        tutRT.pivot = new Vector2(0.5f,0.5f); tutRT.anchoredPosition = new Vector2(0,320); tutRT.sizeDelta = new Vector2(820,200);
        var tpBg = tutorialPanelGO.AddComponent<Image>(); tpBg.color = new Color(0.05f,0.04f,0.10f,0.93f);
        AddOutline(tutorialPanelGO, new Color(0.8f,0.7f,0.3f,0.9f), 3);
        tutorialText = MakeText(tutorialPanelGO.transform, "TutTxt", "", 34, new Color(1f,0.95f,0.8f,1f));
        var tutTxtRT = tutorialText.rectTransform;  // renamed: ttRT may conflict
        tutTxtRT.anchorMin = Vector2.zero; tutTxtRT.anchorMax = Vector2.one;
        tutTxtRT.offsetMin = new Vector2(18,10); tutTxtRT.offsetMax = new Vector2(-18,-10);
        tutorialText.alignment = TextAnchor.MiddleCenter;
        tutorialPanelGO.SetActive(false);

        battlePanel.SetActive(false);

        // Battle result panel
        battleResultPanel = new GameObject("BattleResult"); battleResultPanel.transform.SetParent(canvas.transform, false);
        var brRT = battleResultPanel.AddComponent<RectTransform>(); Stretch(brRT);
        var brBg = MakeImage(battleResultPanel.transform, "BrBg", new Color(0,0,0,0.88f)); Stretch(brBg.rectTransform);
        battleResultTitle = MakeText(battleResultPanel.transform, "BrTitle", "", 100, new Color(1f,0.85f,0.4f,1f));
        var brtRT = battleResultTitle.rectTransform;
        brtRT.anchorMin = new Vector2(0.5f,0.5f); brtRT.anchorMax = new Vector2(0.5f,0.5f);
        brtRT.pivot = new Vector2(0.5f,0.5f); brtRT.anchoredPosition = new Vector2(0,200); brtRT.sizeDelta = new Vector2(900,160);
        battleResultTitle.alignment = TextAnchor.MiddleCenter; battleResultTitle.fontStyle = FontStyle.Bold;
        battleResultRewardText = MakeText(battleResultPanel.transform, "BrRew", "", 60, new Color(1f,0.9f,0.5f,1f));
        var brrRT = battleResultRewardText.rectTransform;
        brrRT.anchorMin = new Vector2(0.5f,0.5f); brrRT.anchorMax = new Vector2(0.5f,0.5f);
        brrRT.pivot = new Vector2(0.5f,0.5f); brrRT.anchoredPosition = new Vector2(0,40); brrRT.sizeDelta = new Vector2(900,100);
        battleResultRewardText.alignment = TextAnchor.MiddleCenter;
        Text brContBtnText;
        MakeFlatButton(battleResultPanel.transform, "BrCont", new Vector2(0,-180), new Vector2(640,140), OnBattleResultContinue, out brContBtnText);
        brContBtnText.text = L("NEXT");
        battleResultPanel.SetActive(false);

        // Shop panel
        shopPanel = new GameObject("ShopPanel"); shopPanel.transform.SetParent(canvas.transform, false);
        var shRT = shopPanel.AddComponent<RectTransform>(); Stretch(shRT);
        var shBg = MakeImage(shopPanel.transform, "ShBg", new Color(0.02f,0.02f,0.05f,0.96f)); Stretch(shBg.rectTransform);
        shopTitleText = MakeText(shopPanel.transform, "ShTitle", "", 80, new Color(1f,0.85f,0.4f,1f));
        var sttRT = shopTitleText.rectTransform;
        sttRT.anchorMin = new Vector2(0,1); sttRT.anchorMax = new Vector2(1,1); sttRT.pivot = new Vector2(0.5f,1);
        sttRT.anchoredPosition = new Vector2(0,-100); sttRT.sizeDelta = new Vector2(-60,120);
        shopTitleText.alignment = TextAnchor.MiddleCenter; shopTitleText.fontStyle = FontStyle.Bold;
        echoesText = MakeText(shopPanel.transform, "EcTxt", "0", 44, new Color(1f,0.85f,0.4f,1f));
        var ecRT = echoesText.rectTransform;
        ecRT.anchorMin = new Vector2(0,1); ecRT.anchorMax = new Vector2(0,1); ecRT.pivot = new Vector2(0,1);
        ecRT.anchoredPosition = new Vector2(40,-250); ecRT.sizeDelta = new Vector2(300,60); echoesText.alignment = TextAnchor.MiddleLeft;
        sparksText = MakeText(shopPanel.transform, "SpTxt", "0", 44, new Color(0.9f,0.4f,1f,1f));
        var spkRT = sparksText.rectTransform;
        spkRT.anchorMin = new Vector2(1,1); spkRT.anchorMax = new Vector2(1,1); spkRT.pivot = new Vector2(1,1);
        spkRT.anchoredPosition = new Vector2(-40,-250); spkRT.sizeDelta = new Vector2(300,60); sparksText.alignment = TextAnchor.MiddleRight;
        for (int i=0;i<5;i++)
        {
            int captured = i; Text btx;
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
        endingPanel = new GameObject("EndingPanel"); endingPanel.transform.SetParent(canvas.transform, false);
        var enRT = endingPanel.AddComponent<RectTransform>(); Stretch(enRT);
        var enBg = MakeImage(endingPanel.transform, "EnBg", new Color(0,0,0,0.95f)); Stretch(enBg.rectTransform);
        endingTitleText = MakeText(endingPanel.transform, "EnTitle", "", 90, new Color(1f,0.85f,0.4f,1f));
        var entRT = endingTitleText.rectTransform;
        entRT.anchorMin = new Vector2(0.5f,1); entRT.anchorMax = new Vector2(0.5f,1); entRT.pivot = new Vector2(0.5f,1);
        entRT.anchoredPosition = new Vector2(0,-220); entRT.sizeDelta = new Vector2(1000,200);
        endingTitleText.alignment = TextAnchor.MiddleCenter; endingTitleText.fontStyle = FontStyle.Bold;
        endingBodyText = MakeText(endingPanel.transform, "EnBody", "", 38, Color.white);
        var enbRT = endingBodyText.rectTransform;
        enbRT.anchorMin = new Vector2(0.5f,0.5f); enbRT.anchorMax = new Vector2(0.5f,0.5f); enbRT.pivot = new Vector2(0.5f,0.5f);
        enbRT.anchoredPosition = new Vector2(0,0); enbRT.sizeDelta = new Vector2(900,800);
        endingBodyText.alignment = TextAnchor.UpperCenter;
        Text enContText;
        MakeFlatButton(endingPanel.transform, "EnCont", new Vector2(0,-700), new Vector2(700,140), OnEndingContinue, out enContText);
        enContText.text = L("NEXT");
        endingPanel.SetActive(false);

        fadeOverlay = MakeImage(canvas.transform, "Fade", Color.black); Stretch(fadeOverlay.rectTransform);
        fadeOverlay.transform.SetAsLastSibling(); fadeOverlay.raycastTarget = false;
        skipBtnGO.transform.SetAsLastSibling(); nextBtnGO.transform.SetAsLastSibling(); fadeOverlay.transform.SetAsLastSibling();

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
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(1,0); rt.anchorMax = new Vector2(1,0); rt.pivot = new Vector2(1,0);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(360,110);
        var btn = go.AddComponent<Button>();
        var cbb = btn.colors; cbb.normalColor = new Color(0.15f,0.12f,0.20f,0.95f);
        cbb.highlightedColor = new Color(0.30f,0.22f,0.40f,1f); cbb.pressedColor = new Color(0.50f,0.35f,0.60f,1f);
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
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var btn = go.AddComponent<Button>();
        var cbb = btn.colors; cbb.normalColor = new Color(0.18f,0.15f,0.25f,0.95f);
        cbb.highlightedColor = new Color(0.32f,0.25f,0.42f,1f); cbb.pressedColor = new Color(0.45f,0.32f,0.55f,1f);
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

    void OnTap()
    {
        if (state == State.Playing && !typing) { OnNext(); return; }
        if (state == State.Playing && typing) { SkipTypewriter(); }
    }
    void SkipTypewriter()
    {
        typeIdx = fullText.Length; dialogText.text = fullText; typing = false; typeTimer = 0f;
        skipBtnGO.SetActive(false);
        var line = episodes[currentEpisode-1].script[idx];
        if (line.choices != null) ShowChoiceUI(); else nextBtnGO.SetActive(true);
    }
    void OnSkip()
    {
        PlaySfx("sfx_choice");
        // Always hide immediately — whichever branch we take next will re-show if needed
        skipBtnGO.SetActive(false);
        nextBtnGO.SetActive(false);
        var ep = episodes[currentEpisode-1];
        while (idx < ep.script.Count)
        {
            var line = ep.script[idx];
            if (line.triggerBattle > 0) { ShowPreBattle(line); return; }
            if (line.choices != null) { SkipTypewriter(); ShowChoiceUI(); return; }
            idx++;
        }
        ShowEpisodeEnding();
    }
    void OnNext()
    {
        PlaySfx("sfx_blip"); typing = false;
        var line = episodes[currentEpisode-1].script[idx];
        if (typing) { SkipTypewriter(); return; }
        idx++;
        SaveProgress();
        if (idx >= episodes[currentEpisode-1].script.Count) ShowEpisodeEnding();
        else ShowCurrentLine(false);
        nextBtnGO.SetActive(false);
    }
    void ShowChoiceUI()
    {
        var line = episodes[currentEpisode-1].script[idx];
        choicePanel.SetActive(true);
        for (int i=0;i<choiceButtons.Count;i++)
        {
            if (line.choices == null || i >= line.choices.Length)
            { choiceButtons[i].gameObject.SetActive(false); continue; }
            choiceButtons[i].gameObject.SetActive(true);
            choiceTexts[i].text = (lang == Lang.EN) ? line.choices[i].en : line.choices[i].ru;
        }
        nextBtnGO.SetActive(false);
    }
    void OnChoicePicked(int ci)
    {
        PlaySfx("sfx_choice");
        var line = episodes[currentEpisode-1].script[idx];
        if (line.choices != null && ci < line.choices.Length)
        {
            int bias = line.choices[ci].pathBias;
            if (bias == 1) pactScore++;
            else if (bias == 2) vengeScore++;
            else if (bias == 3) mortalScore++;
        }
        choicePanel.SetActive(false);
        idx++;
        SaveProgress();
        if (idx >= episodes[currentEpisode-1].script.Count) ShowEpisodeEnding();
        else ShowCurrentLine(false);
    }

    void ShowPreBattle(Line line)
    {
        state = State.PreBattle;
        currentBattle = line.triggerBattle;
        preBattlePanel.SetActive(true);
        dialogBox.gameObject.SetActive(false);
        choicePanel.SetActive(false);
        if (pbTitleRef != null) pbTitleRef.text = L("PRE_BATTLE");
        preBattleText.text = (lang == Lang.EN) ? line.en : line.ru;
        if (pbBeginBtnText != null) pbBeginBtnText.text = L("BATTLE_BEGIN");
    }
    void OnBeginBattle()
    {
        PlaySfx("sfx_choice");
        preBattlePanel.SetActive(false);
        StartBattle(currentEpisode, currentBattle);
    }
    public void OnBattleMenu() { if (settingsPanel != null) settingsPanel.SetActive(true); }

    void ShowEpisodeEnding()
    {
        state = State.Ending;
        endingPanel.SetActive(true); dialogBox.gameObject.SetActive(false);
        choicePanel.SetActive(false); preBattlePanel.SetActive(false);
        // Determine ending based on path scores
        if (pactScore >= vengeScore && pactScore >= mortalScore) path = Path.Pact;
        else if (vengeScore >= mortalScore) path = Path.Vengeance;
        else path = Path.Mortals;
        string pathName = path == Path.Pact ? L("PATH_PACT") : (path == Path.Vengeance ? L("PATH_VENGE") : L("PATH_MORTAL"));
        endingTitleText.text = L("ENDING_EP") + " " + currentEpisode;
        string en = "Episode " + currentEpisode + " complete.\nYour path: " + pathName + ".\n\n" + L("TO_BE_CONT");
        string ru = "Эпизод " + currentEpisode + " завершён.\nВаш путь: " + pathName + ".\n\n" + L("TO_BE_CONT");
        endingBodyText.text = (lang == Lang.EN) ? en : ru;
    }
    void OnEndingContinue()
    {
        PlaySfx("sfx_choice");
        endingPanel.SetActive(false);
        if (currentEpisode < 7) { currentEpisode++; idx = 0; SaveProgress(); ShowCurrentLine(true); }
        else { state = State.Ended; ShowTitle(); }
    }

    void OnOpenShop() { PlaySfx("sfx_choice"); shopPanel.SetActive(true); UpdateBattleHUD(); }
    void OnCloseShop() { PlaySfx("sfx_choice"); shopPanel.SetActive(false); }
    void OnShopBuy(int abilIdx)
    {
        PlaySfx("sfx_choice");
        int price = AbilityPrices[abilIdx];
        if (echoes < price) return;
        echoes -= price;
        string key = AbilityKeys[abilIdx];
        if (!abilityCount.ContainsKey(key)) abilityCount[key] = 0;
        abilityCount[key]++;
        SaveProgress();
        UpdateBattleHUD();
        RefreshAbilityButtons();
    }
    void OnMockDonate()
    {
        PlaySfx("sfx_choice");
        sparks += 500; echoes += 200;
        SaveProgress(); UpdateBattleHUD();
    }

    void PlayBgm(string key)
    {
        if (key == currentBgm || !clips.ContainsKey(key)) return;
        currentBgm = key;
        if (bgmSrc != null) { bgmSrc.clip = clips[key]; bgmSrc.Play(); }
    }
    void PlaySfx(string key)
    {
        if (sfxSrc != null && clips.ContainsKey(key)) sfxSrc.PlayOneShot(clips[key]);
    }

    void SaveProgress()
    {
        PlayerPrefs.SetInt("ep", currentEpisode);
        PlayerPrefs.SetInt("idx", idx);
        PlayerPrefs.SetInt("echoes", echoes);
        PlayerPrefs.SetInt("sparks", sparks);
        PlayerPrefs.SetInt("pact", pactScore);
        PlayerPrefs.SetInt("venge", vengeScore);
        PlayerPrefs.SetInt("mortal", mortalScore);
        PlayerPrefs.SetInt("music", musicOn?1:0);
        PlayerPrefs.SetInt("sfx", sfxOn?1:0);
        PlayerPrefs.SetInt("lang", (int)lang);
        foreach (var kv in abilityCount) PlayerPrefs.SetInt("ab_"+kv.Key, kv.Value);
        PlayerPrefs.Save();
    }
    void LoadProgress()
    {
        currentEpisode = PlayerPrefs.GetInt("ep", 1);
        idx            = PlayerPrefs.GetInt("idx", 0);
        echoes         = PlayerPrefs.GetInt("echoes", 0);
        sparks         = PlayerPrefs.GetInt("sparks", 0);
        pactScore      = PlayerPrefs.GetInt("pact", 0);
        vengeScore     = PlayerPrefs.GetInt("venge", 0);
        mortalScore    = PlayerPrefs.GetInt("mortal", 0);
        musicOn        = PlayerPrefs.GetInt("music", 1) == 1;
        sfxOn          = PlayerPrefs.GetInt("sfx",   1) == 1;
        lang           = (Lang)PlayerPrefs.GetInt("lang", 0);
        string[] abKeys = { "inferno","freeze","shuffle","cleanse","slam" };
        foreach (var k in abKeys) abilityCount[k] = PlayerPrefs.GetInt("ab_"+k, 0);
        if (currentEpisode < 1 || currentEpisode > 7) currentEpisode = 1;
        if (idx < 0) idx = 0;
    }
}

// ============ GemDragHandler (attached per gem cell) ============
public class GemDragHandler : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    Bootstrapper board;
    int cellX, cellY;
    Vector2 dragStart;
    const float SWIPE_THRESHOLD = 30f;

    public void Init(Bootstrapper b, int x, int y) { board = b; cellX = x; cellY = y; }

    public void OnBeginDrag(PointerEventData ev) { dragStart = ev.position; }

    public void OnEndDrag(PointerEventData ev)
    {
        Vector2 delta = ev.position - dragStart;
        if (delta.magnitude < SWIPE_THRESHOLD) return;
        int dx = 0, dy = 0;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dx = delta.x > 0 ? 1 : -1;
        else
            dy = delta.y > 0 ? -1 : 1;   // UI Y is inverted (up=positive screen but -y in grid)
        board.OnGemSwipe(cellX, cellY, dx, dy);
    }
}
