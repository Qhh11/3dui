using System.Collections.Generic;
using UnityEngine;

public class Level1DialogueDatabase : MonoBehaviour
{
    public static Level1DialogueDatabase Instance { get; private set; }
    public const string Level1FirstUfoView = "level1_first_ufo_view";
    public const string VillagerDFirstMeet = "villager_d_first_meet";

    [Header("Optional portraits loaded from Resources/Portraits")]
    [SerializeField] private bool loadPortraitsFromResources = true;

    private readonly Dictionary<string, DialogueCharacter> characters = new();
    private readonly Dictionary<string, DialogueSequence> sequences = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildCharacters();
        BuildSequences();
    }

    public DialogueSequence GetSequence(string sequenceId)
    {
        if (string.IsNullOrWhiteSpace(sequenceId))
        {
            return null;
        }

        if (sequences.TryGetValue(sequenceId, out DialogueSequence sequence))
            return sequence;
        return null;
    }

    public DialogueCharacter GetCharacter(string characterId)
    {
        if (characters.TryGetValue(characterId, out DialogueCharacter character))
            return character;
        return null;
    }

    private void BuildCharacters()
    {
        characters.Clear();

        AddCharacter(DialogueIds.Characters.Player, "Player", "player", Color.white);
        AddCharacter(DialogueIds.Characters.UFOTerminal, "UFO Terminal", "ufo_terminal", new Color(0.55f, 0.85f, 1f));
        AddCharacter(DialogueIds.Characters.VillagerA, "Villager A", "villager_a", new Color(1f, 0.85f, 0.45f));
        AddCharacter(DialogueIds.Characters.VillagerB, "Villager B", "villager_b", new Color(1f, 0.75f, 0.45f));
        AddCharacter(DialogueIds.Characters.VillagerC, "Villager C", "villager_c", new Color(1f, 0.75f, 0.45f));
        AddCharacter(DialogueIds.Characters.VillagerD, "Villager D", "villager_d", new Color(1f, 0.75f, 0.45f));
    }

    private void AddCharacter(string id, string displayName, string portraitResourceName, Color nameColor)
    {
        DialogueCharacter character = ScriptableObject.CreateInstance<DialogueCharacter>();
        character.displayName = displayName;
        character.nameColor = nameColor;

        if (loadPortraitsFromResources)
            character.portrait = Resources.Load<Sprite>($"Portraits/{portraitResourceName}");

        characters[id] = character;
    }

    private void BuildSequences()
    {
        sequences.Clear();

        DialogueSequence prelude = NewSequence(DialogueIds.Sequences.Level1Prelude);
        DialogueSequence level1FirstUfoView = NewSequence(DialogueIds.Sequences.Level1FirstUfoView);
        DialogueSequence level1TentAnalyzed = NewSequence(DialogueIds.Sequences.Level1TentAnalyzed);
        DialogueSequence level1AfterSwitchBackToPlayer = NewSequence(DialogueIds.Sequences.Level1AfterSwitchBackToPlayer);

        DialogueSequence villagerAFirstMeet = NewSequence(DialogueIds.Sequences.VillagerAFirstMeet);
        DialogueSequence villagerAWrongAnswer = NewSequence(DialogueIds.Sequences.VillagerAWrongAnswer);
        DialogueSequence villagerACorrectAnswer = NewSequence(DialogueIds.Sequences.VillagerACorrectAnswer);

        BuildPrelude(prelude);
        BuildLevel1FirstUfoView(level1FirstUfoView);
        BuildLevel1TentAnalyzed(level1TentAnalyzed);
        BuildLevel1AfterSwitchBackToPlayer(level1AfterSwitchBackToPlayer);

        BuildVillagerAFirstMeet(villagerAFirstMeet, villagerAWrongAnswer, villagerACorrectAnswer);
        BuildVillagerAWrongAnswer(villagerAWrongAnswer, villagerAWrongAnswer, villagerACorrectAnswer);
        BuildVillagerACorrectAnswer(villagerACorrectAnswer);

        DialogueSequence wellArrival = NewSequence(DialogueIds.Sequences.WellArrival);
        DialogueSequence wellUfoView = NewSequence(DialogueIds.Sequences.WellUfoView);
        DialogueSequence wellEnergyPickup = NewSequence(DialogueIds.Sequences.WellEnergyPickup);
        BuildWellArrival(wellArrival);
        BuildWellUfoView(wellUfoView);
        BuildWellEnergyPickup(wellEnergyPickup);

        // --- Villager B: Well ---
        DialogueSequence villagerBFirstMeet = NewSequence(DialogueIds.Sequences.VillagerBFirstMeet);
        DialogueSequence abandonedWellArrival = NewSequence(DialogueIds.Sequences.AbandonedWellArrival);
        DialogueSequence abandonedWellAfterSiphon = NewSequence(DialogueIds.Sequences.AbandonedWellAfterSiphon);
        BuildVillagerBFirstMeet(villagerBFirstMeet);
        BuildAbandonedWellArrival(abandonedWellArrival);
        BuildAbandonedWellAfterSiphon(abandonedWellAfterSiphon);

        // --- Villager C: First House (wood beams) ---
        DialogueSequence villagerCIntro      = NewSequence(DialogueIds.Sequences.VillagerCFirstHouseIntro);
        DialogueSequence villagerCAfterBeams = NewSequence(DialogueIds.Sequences.VillagerCFirstHouseAfterBeams);
        BuildVillagerCFirstHouseIntro(villagerCIntro);
        BuildVillagerCFirstHouseAfterBeams(villagerCAfterBeams);
        
        // --- Villager D: LakeHouse ---
        DialogueSequence villagerDFirstMeet =
            NewSequence(VillagerDFirstMeet);

        BuildVillagerDFirstMeet(villagerDFirstMeet);
    }

    private DialogueSequence NewSequence(string sequenceId)
    {
        DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
        sequence.sequenceId = sequenceId;
        sequence.entries = new List<DialogueEntry>();
        sequences[sequenceId] = sequence;
        return sequence;
    }

    private void BuildPrelude(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(DialogueIds.Characters.UFOTerminal);
        sequence.entries.Add(Line(player, "Ugh... my head hurts."));
        sequence.entries.Add(Line(player, "What happened? UFO Terminal, can you hear me?", eventOnEnd: DialogueIds.Events.StartWakeUpFade));

        sequence.entries.Add(Line(terminal, "Warning. Warning."));
        sequence.entries.Add(Line(terminal, "Temporal turbulence residue detected."));
        sequence.entries.Add(Line(terminal, "Mechanical failure confirmed. We have crashed in an unknown area."));
        sequence.entries.Add(Line(terminal, "Backup energy units were scattered during impact."));
        sequence.entries.Add(Line(terminal, "All energy units must be recovered before departure."));

        sequence.entries.Add(Line(player, "Thank goodness. At least I can still reach you."));
        sequence.entries.Add(Line(player, "The energy units could be anywhere..."));
        sequence.entries.Add(Line(player, "Maybe I can use your scouting system to search for them."));

        sequence.entries.Add(Line(
            player,
            "I can tap the Switch icon in the top-right corner to change perspective."
        ));

        sequence.entries.Add(Line(
            player,
            "Let me try if this still works.",
            eventOnEnd: DialogueIds.Events.ShowSwitchHint
        ));
    }
    private void BuildLevel1FirstUfoView(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);

        sequence.entries.Add(Line(player, "Strange..."));
        sequence.entries.Add(Line(player, "Why does the world look different through the UFO?"));
        sequence.entries.Add(Line(player, "I still see grassland here, but this view looks ruined."));
        sequence.entries.Add(Line(
            player,
            "That tent stands out. I should investigate it.",
            eventOnEnd: DialogueIds.Events.ShowAnalyzeTent
        ));
    }

    private void BuildLevel1TentAnalyzed(DialogueSequence sequence)
    {
        DialogueCharacter terminal = GetCharacter(DialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal, "Energy fluctuation residue detected."));
        sequence.entries.Add(Line(terminal, "An energy unit once appeared in this target area."));
        sequence.entries.Add(Line(
            terminal,
            "Recommendation: investigate the tent from Player perspective.",
            eventOnEnd: DialogueIds.Events.QuestSwitchBackToPlayer
        ));
    }

    private void BuildLevel1AfterSwitchBackToPlayer(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);

        sequence.entries.Add(Line(
            player,
            "I should go take a look at that tent.",
            eventOnEnd: DialogueIds.Events.QuestGoToTent
        ));
    }

    private void BuildVillagerAFirstMeet(
        DialogueSequence sequence,
        DialogueSequence wrongAnswer,
        DialogueSequence correctAnswer)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerA);

        sequence.entries.Add(Line(villager, "A stranger? We do not see many new faces on this grassland.", eventOnStart: DialogueIds.Flags.MetVillagerA));
        sequence.entries.Add(Line(player, "We got lost. We are looking for a glowing stone. Have you seen one?"));
        sequence.entries.Add(Line(villager, "A glowing stone?"));
        sequence.entries.Add(Line(villager, "We do not tell the truth to outsiders."));
        sequence.entries.Add(Line(player, "Why? We do not mean any harm."));
        sequence.entries.Add(Line(villager, "That is not for you to decide."));
        sequence.entries.Add(Line(villager, "Too many people who should not be here have already come to this grassland."));
        sequence.entries.Add(Line(villager, "Some look like us. Some speak like us. But they do not belong here."));
        sequence.entries.Add(Line(player, "What do you mean?"));
        sequence.entries.Add(Line(villager, "Numbers, symbols, scratches, the shape on the tower roof... These are our marks."));
        sequence.entries.Add(Line(villager, "If you truly belong here, you should know today's mark."));

        DialogueEntry question = Line(villager, "Tell me. What shape is on top of our tower?");
        question.choices.Add(Choice("Circle", wrongAnswer));
        question.choices.Add(Choice("Star", correctAnswer, setFlagOnSelect: DialogueIds.Flags.AnsweredTowerMark));
        question.choices.Add(Choice("Square", wrongAnswer));
        sequence.entries.Add(question);
    }

    private void BuildVillagerAWrongAnswer(
        DialogueSequence sequence,
        DialogueSequence wrongAnswer,
        DialogueSequence correctAnswer)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerA);

        sequence.entries.Add(Line(villager, "Wrong."));
        sequence.entries.Add(Line(villager, "You do not belong here."));
        sequence.entries.Add(Line(villager, "I will not tell you anything."));
        sequence.entries.Add(Line(player, "Wait. I may have seen it wrong."));
        sequence.entries.Add(Line(player, "Let me try again."));

        DialogueEntry question = Line(villager, "Then answer carefully. What shape is on top of our tower?");
        question.choices.Add(Choice("Circle", wrongAnswer));
        question.choices.Add(Choice("Star", correctAnswer, setFlagOnSelect: DialogueIds.Flags.AnsweredTowerMark));
        question.choices.Add(Choice("Square", wrongAnswer));
        sequence.entries.Add(question);
    }

    private void BuildVillagerACorrectAnswer(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(DialogueIds.Characters.UFOTerminal);
        DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerA);

        sequence.entries.Add(Line(villager, "...Star."));
        sequence.entries.Add(Line(villager, "So you have seen it."));
        sequence.entries.Add(Line(player, "Was that a test?"));
        sequence.entries.Add(Line(villager, "A warning."));
        sequence.entries.Add(Line(villager, "The last person who answered the mark wrong still insisted that he lived here."));
        sequence.entries.Add(Line(villager, "The next morning, his house was still there. But he was gone."));
        sequence.entries.Add(Line(villager, "Only drag marks were left near the lake."));
        sequence.entries.Add(Line(terminal, "Signal unstable..."));
        sequence.entries.Add(Line(terminal, "Temporal noise detected..."));
        sequence.entries.Add(Line(player, "What was that?"));
        sequence.entries.Add(Line(player, "I think I saw someone lying near the lake."));
        sequence.entries.Add(Line(terminal, "Signal lost."));
        sequence.entries.Add(Line(terminal, "Image cannot be confirmed."));
        sequence.entries.Add(Line(terminal, "Suggestion: continue investigating the energy fluctuation."));
        sequence.entries.Add(Line(villager, "You saw something too, did you not?"));
        sequence.entries.Add(Line(villager, "Then stop pretending nothing is wrong."));
        sequence.entries.Add(Line(player, "Do you know what is happening here?"));
        sequence.entries.Add(Line(villager, "I only know this grassland keeps repeating itself."));
        sequence.entries.Add(Line(villager, "Some things are thrown away, and the next day they return."));
        sequence.entries.Add(Line(villager, "Some people are dead, and the next morning they stand at their doors again."));
        sequence.entries.Add(Line(villager, "But their marks are different."));
        sequence.entries.Add(Line(player, "Different marks..."));
        sequence.entries.Add(Line(villager, "Last night, I went to draw water."));
        sequence.entries.Add(Line(villager, "I saw something glowing near the well."));
        sequence.entries.Add(Line(villager, "If you are looking for a glowing object, go there."));
        sequence.entries.Add(Line(villager, "But I warn you. Do not bring it back.", eventOnEnd: DialogueIds.Events.QuestGoToWell));
    }

    // 段1: 到达水井,小人发现能量痕迹但草太厚找不到 → 触发切UFO视角
    private void BuildWellArrival(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
    
        sequence.entries.Add(Line(player, "Wait... there is something glowing faintly near the well."));
        sequence.entries.Add(Line(player, "It looks like traces of an energy unit."));
        sequence.entries.Add(Line(player, "It must be here somewhere, but the grass is too thick. I cannot find it."));
        sequence.entries.Add(Line(player, "UFO, can you scan this spot from your side?",
            eventOnEnd: DialogueIds.Events.ActivateWellArea));
            // activate_well_area: 激活UFO视角下的水井触发区域
    }
    
    // 段2: UFO视角下,小人看到能源块 → 触发切回+标记位置
    private void BuildWellUfoView(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
    
        sequence.entries.Add(Line(player, "The grass here looks dead."));
        sequence.entries.Add(Line(player, "There it is. The stone was hiding right underneath.",
            eventOnEnd: DialogueIds.Events.WellMarkEnergy));
        // gameplay: 标记能源块位置,切回小人视角,玩家点击草丛拾取
    }

    // 段3: 拾取后,UFO终端报告解锁虹吸
    private void BuildWellEnergyPickup(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(DialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal, "Energy unit detected."));
        sequence.entries.Add(Line(terminal, "Power recharging..."));
        sequence.entries.Add(Line(terminal, "Basic functions restored."));
        sequence.entries.Add(Line(terminal, "Siphon function... online."));

        sequence.entries.Add(Line(player, "You fixed a function?"));

        sequence.entries.Add(Line(terminal, "Siphon function can extract lightweight objects."));
        sequence.entries.Add(Line(terminal, 
            "Aim at target and activate.",
            eventOnEnd: DialogueIds.Events.UnlockSiphon));
    }

    // =============================================
    // VILLAGER B — Water Well
    // =============================================
    
    private void BuildVillagerBFirstMeet(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerB);

        sequence.entries.Add(Line(villager, "Hey! What are you doing?",
            eventOnStart: DialogueIds.Flags.MetVillagerB));
        sequence.entries.Add(Line(villager, "You cannot pick that up! Drop it! Now!"));

        sequence.entries.Add(Line(player, "Why?"));
        sequence.entries.Add(Line(player, "It is just a glowing energy unit."));

        sequence.entries.Add(Line(villager, "Energy unit?"));
        sequence.entries.Add(Line(villager,
            "You outsiders always give pretty names to things you should not touch."));
        sequence.entries.Add(Line(villager, "It is cursed."));
        sequence.entries.Add(Line(villager,
            "Whoever touches it gets pulled into strange things."));

        sequence.entries.Add(Line(player, "Sounds like a legend."));

        sequence.entries.Add(Line(villager, "It is not a legend!"));
        sequence.entries.Add(Line(villager,
            "I see it near the well every night."));
        sequence.entries.Add(Line(villager,
            "I have buried it. Burned it. Thrown it in the river."));
        sequence.entries.Add(Line(villager,
            "The next day, it always comes back to the same spot."));
        sequence.entries.Add(Line(villager,
            "As if it does not belong here, yet it keeps trying to return."));

        sequence.entries.Add(Line(player,
            "Maybe someone just put it back."));

        sequence.entries.Add(Line(villager, "You do not believe me?"));
        sequence.entries.Add(Line(villager,
            "Then go look at the well."));
        sequence.entries.Add(Line(villager,
            "Last night, I threw one of these down there myself."));
        sequence.entries.Add(Line(villager,
            "The well is deep. No one can fish it out."));
        sequence.entries.Add(Line(villager,
            "If you can find it down there, then I am not crazy.",
            eventOnEnd: DialogueIds.Events.QuestRetrieveFromAbandonedWell));
    }


    // =============================================
    // 段2: 到达废井,UFO终端提示用虹吸
    // 触发时机: 玩家走到废井旁
    // 结束后 → gameplay: 玩家操作虹吸打捞
    // =============================================

    private void BuildAbandonedWellArrival(DialogueSequence sequence)
    {
        DialogueCharacter terminal = GetCharacter(DialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal, "Weak energy signature detected."));
        sequence.entries.Add(Line(terminal, "Target located at the bottom of the well."));
        sequence.entries.Add(Line(terminal, "Recommendation: use siphon function.",
            eventOnEnd: DialogueIds.Events.ActivateAbandonedWellArea));
        // gameplay: UI提示"切UFO视角,对准井底,启动虹吸"
        // 玩家操作完成后 → 触发段3
    }


    // =============================================
    // 段3: 虹吸捞出物体后,平行时空揭示
    // 触发时机: 虹吸把东西吸出来之后
    // =============================================

    private void BuildAbandonedWellAfterSiphon(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(DialogueIds.Characters.UFOTerminal);
        DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerB);

        sequence.entries.Add(Line(player, "...How is this possible?"));
        sequence.entries.Add(Line(player,
            "The one we just picked up is still here."));
        sequence.entries.Add(Line(player,
            "But there is another one in the well. Identical."));

        sequence.entries.Add(Line(villager, "I told you."));
        sequence.entries.Add(Line(villager, "It always comes back."));
        sequence.entries.Add(Line(villager, "Every single time."));

        sequence.entries.Add(Line(terminal, "Scan result: anomaly detected."));
        sequence.entries.Add(Line(terminal,
            "This energy unit's core is depleted."));
        sequence.entries.Add(Line(terminal,
            "Residual frequency highly matches the active unit."));
        sequence.entries.Add(Line(terminal,
            "Possible cause: a break in local time."));
        sequence.entries.Add(Line(terminal,
            "There may be traces from another timeline here."));

        sequence.entries.Add(Line(player, "Another timeline?"));
        sequence.entries.Add(Line(player,
            "So this could be the same energy unit, but from a different time?"));

        sequence.entries.Add(Line(terminal, "That theory is likely."));
        sequence.entries.Add(Line(terminal,
            "The active unit and the depleted unit may originate from different timelines."));
        sequence.entries.Add(Line(terminal,
            "The same object overlapping across multiple timelines."));

        sequence.entries.Add(Line(player, "So what I see is the present."));
        sequence.entries.Add(Line(player,
            "What you see might be the future."));
        sequence.entries.Add(Line(player,
            "And these energy units... may be falling out from different points in time."));

        sequence.entries.Add(Line(terminal, "Reasoning accepted."));
        sequence.entries.Add(Line(terminal,
            "Recommendation: continue recovering energy units."));
        sequence.entries.Add(Line(terminal,
            "The more recovered, the more stable the temporal signal."));

        sequence.entries.Add(Line(player,
            "Then we definitely cannot leave them lying around."));
        sequence.entries.Add(Line(player,
            "We will collect every single one."));
        sequence.entries.Add(Line(player,
            "To fix you, and to find out what is really happening here."));

        // 转向居民B问路
        sequence.entries.Add(Line(player,
            "Are there more glowing stones nearby?"));

        sequence.entries.Add(Line(villager, "I do not know."));
        sequence.entries.Add(Line(villager,
            "But if you really want to find out, walk deeper into the grassland."));
        sequence.entries.Add(Line(villager,
            "The first house there belongs to an old friend of mine."));
        sequence.entries.Add(Line(villager,
            "Who lost what, where something strange happened, which paths still work... he remembers everything."));
        sequence.entries.Add(Line(villager,
            "If you are looking for things that should not exist, ask him.",
            eventOnEnd: DialogueIds.Events.QuestGoToFirstHouse));
    }

// =============================================
// 居民C — 草原深处第一户 (intro，触发木梁玩法)
// 触发时机: 玩家点击居民C对话
// 结束后 → gameplay: 拖拽清理三根木梁
// =============================================
private void BuildVillagerCFirstHouseIntro(DialogueSequence sequence)
{
    DialogueCharacter player   = GetCharacter(DialogueIds.Characters.Player);
    DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerC);

    sequence.entries.Add(Line(villager, "You came at just the right time."));
    sequence.entries.Add(Line(villager, "Last night the ground shook, and half my house caved in."));
    sequence.entries.Add(Line(villager, "Fallen beams are blocking the door, and my family is still trapped inside."));

    sequence.entries.Add(Line(player, "The ground shook?"));
    sequence.entries.Add(Line(player, "Does that happen often here?"));

    sequence.entries.Add(Line(villager, "Only recently."));
    sequence.entries.Add(Line(villager, "Sometimes I hear a rumble at night, and by morning the paths have changed."));
    sequence.entries.Add(Line(villager, "The bridge moves. The cracks in the houses move too."));
    sequence.entries.Add(Line(villager, "As if someone stitched different grasslands together."));

    sequence.entries.Add(Line(player, "We'll help you move the beams."));

    sequence.entries.Add(Line(villager, "Really?"));
    sequence.entries.Add(Line(villager, "That would be a relief."));
    sequence.entries.Add(Line(villager,
        "Just clear those beams away from the door. Careful not to drop them on anyone.",
        eventOnEnd: DialogueIds.Events.QuestClearWoodBeams));
}

// =============================================
// 居民C — 清理木梁之后 (指向第二户)
// 触发时机: 三根木梁全部归位后，由 WoodBeamPuzzle 自动播放
// 结束后 → objective: 前往第二户人家
// =============================================
private void BuildVillagerCFirstHouseAfterBeams(DialogueSequence sequence)
{
    DialogueCharacter player   = GetCharacter(DialogueIds.Characters.Player);
    DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerC);

    sequence.entries.Add(Line(villager, "Thank you so much."));
    sequence.entries.Add(Line(villager, "If not for you, I wouldn't have known what to do."));

    sequence.entries.Add(Line(player, "Don't mention it."));
    sequence.entries.Add(Line(player, "By the way, we're looking for a kind of glowing stone. Have you seen one?"));

    sequence.entries.Add(Line(villager, "A glowing stone?"));
    sequence.entries.Add(Line(villager, "Not here, I'm afraid."));
    sequence.entries.Add(Line(villager, "But the next house deeper in — I remember something bright hanging by his window."));
    sequence.entries.Add(Line(villager, "He says it glows on its own at night, and throws shadows that aren't quite right."));
    sequence.entries.Add(Line(villager, "You could go and ask him."));

    sequence.entries.Add(Line(player, "The second house, further in?"));

    sequence.entries.Add(Line(villager,
        "That's right. His house stands beside the river."));

    sequence.entries.Add(Line(villager,
        "The bridge nearby has been wandering lately, so the path may look wrong."));

    sequence.entries.Add(Line(villager,
        "Find the house first and ask the resident there. He knows that crossing better than I do."));

    sequence.entries.Add(Line(villager,
        "Nothing on this grassland stays where it belongs anymore.",
        eventOnEnd: DialogueIds.Events.QuestGoToSecondHouse));
}

// 湖泊中心house对话
private void BuildVillagerDFirstMeet(DialogueSequence sequence)
{
    DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
    DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerD);

    sequence.entries.Add(Line(villager, "If you are asking about the glowing stone, you are too late."));
    sequence.entries.Add(Line(player, "You really found one?"));
    sequence.entries.Add(Line(villager, "A few days ago. I hung it by the window."));
    sequence.entries.Add(Line(villager, "But the tower keeper came and took it away."));
    sequence.entries.Add(Line(player, "So the energy unit is at the tower?"));
    sequence.entries.Add(Line(villager, "Probably. But you need to cross the river first."));
    sequence.entries.Add(Line(
        villager,
        "The only bridge is on the wrong side now.",
        eventOnStart: "focus_lake_bridge"
    ));
    sequence.entries.Add(Line(
        player,
        "Then we need to move the bridge.",
        eventOnEnd: DialogueIds.Events.QuestMoveBridgeSecondCrossing
    ));
}

    private DialogueEntry Line(
        DialogueCharacter speaker,
        string text,
        string eventOnStart = "",
        string eventOnEnd = "")
    {
        return new DialogueEntry
        {
            speaker = speaker,
            text = text,
            eventOnStart = eventOnStart,
            eventOnEnd = eventOnEnd,
            choices = new List<DialogueChoice>()
        };
    }

    private DialogueChoice Choice(
        string text,
        DialogueSequence nextSequence,
        string setFlagOnSelect = "",
        string eventOnSelect = "")
    {
        return new DialogueChoice
        {
            text = text,
            nextSequence = nextSequence,
            setFlagOnSelect = setFlagOnSelect,
            eventOnSelect = eventOnSelect
        };
    }
}
