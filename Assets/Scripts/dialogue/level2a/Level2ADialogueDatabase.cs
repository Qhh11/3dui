using System.Collections.Generic;
using UnityEngine;

public class Level2ADialogueDatabase : MonoBehaviour, IDialogueDatabase
{
    public static Level2ADialogueDatabase Instance { get; private set; }

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
            return null;

        if (sequences.TryGetValue(sequenceId, out DialogueSequence sequence))
            return sequence;

        Debug.LogWarning("Level2A sequence not found: " + sequenceId);
        return null;
    }

    private DialogueCharacter GetCharacter(string id)
    {
        if (characters.TryGetValue(id, out DialogueCharacter character))
            return character;

        Debug.LogWarning("Level2A character not found: " + id);
        return null;
    }

    private void BuildCharacters()
    {
        characters.Clear();

        AddCharacter(
            Level2ADialogueIds.Characters.Player,
            "Player", "player", Color.white);

        AddCharacter(
            Level2ADialogueIds.Characters.UFOTerminal,
            "UFO Terminal", "ufo_terminal",
            new Color(0.55f, 0.85f, 1f));
    }

    private void AddCharacter(string id, string displayName,
        string portraitResourceName, Color nameColor)
    {
        DialogueCharacter character = ScriptableObject.CreateInstance<DialogueCharacter>();
        character.displayName = displayName;
        character.nameColor = nameColor;

        if (loadPortraitsFromResources)
            character.portrait = Resources.Load<Sprite>(
                "Portraits/" + portraitResourceName);

        characters[id] = character;
    }

    private void BuildSequences()
    {
        sequences.Clear();

        DialogueSequence prelude = NewSequence(Level2ADialogueIds.Sequences.Prelude);
        DialogueSequence firstUfo = NewSequence(Level2ADialogueIds.Sequences.FirstUfoView);
        DialogueSequence energyFound = NewSequence(Level2ADialogueIds.Sequences.EnergyFound);
        DialogueSequence siphonFailed = NewSequence(Level2ADialogueIds.Sequences.SiphonFailed);
        DialogueSequence rootsCleared = NewSequence(Level2ADialogueIds.Sequences.RootsCleared);
        DialogueSequence siphonSuccess = NewSequence(Level2ADialogueIds.Sequences.SiphonSuccess);
        DialogueSequence portalDiscovered = NewSequence(Level2ADialogueIds.Sequences.PortalDiscovered);
        
        BuildPrelude(prelude);
        BuildFirstUfoView(firstUfo);
        BuildEnergyFound(energyFound);
        BuildSiphonFailed(siphonFailed);
        BuildRootsCleared(rootsCleared);
        BuildSiphonSuccess(siphonSuccess);
        BuildPortalDiscovered(portalDiscovered);
    }

    private DialogueSequence NewSequence(string sequenceId)
    {
        DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
        sequence.sequenceId = sequenceId;
        sequence.entries = new List<DialogueEntry>();
        sequences[sequenceId] = sequence;
        return sequence;
    }

    // =============================================
    // Prelude: 从传送门白光中恢复
    // =============================================
    private void BuildPrelude(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(Level2ADialogueIds.Characters.Player);

        sequence.entries.Add(Line(player,
            "This place looks so strange..."));
        sequence.entries.Add(Line(player,
            "The ground is full of cracks, and there are dead tree roots everywhere."));
        sequence.entries.Add(Line(player,
            "There are even volcanoes over there."));
        sequence.entries.Add(Line(player,
            "UFO, is what you see still different from mine?",
            eventOnEnd: Level2ADialogueIds.Events.ShowSwitchHint));
    }

    // =============================================
    // 第一次UFO视角: 火山变冰山
    // =============================================
    private void BuildFirstUfoView(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(Level2ADialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(Level2ADialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal,
            "Environment scan complete."));
        sequence.entries.Add(Line(terminal,
            "Surface environments from both perspectives are highly consistent."));
        sequence.entries.Add(Line(terminal,
            "Only difference: volcanoes in Player view become ice mountains in UFO view."));
        sequence.entries.Add(Line(terminal,
            "It seems we are now in very close timelines."));

        sequence.entries.Add(Line(player,
            "The volcanoes on your side are ice mountains?"));
        sequence.entries.Add(Line(player,
            "Everything else is the same, just this one difference?"));

        sequence.entries.Add(Line(terminal, "Correct."));
        sequence.entries.Add(Line(terminal,
            "The temporal structure of this place is chaotic."));
        sequence.entries.Add(Line(terminal,
            "But if we can perform certain actions to interfere with the timeline..."));
        sequence.entries.Add(Line(terminal,
            "According to quantum observation theory, when two observers reach identical results..."));
        sequence.entries.Add(Line(terminal,
            "The timeline collapses into a single state."));
        sequence.entries.Add(Line(terminal,
            "In other words, if we can make everything in both views identical..."));
        sequence.entries.Add(Line(terminal,
            "We can return to the same timeline."));

        sequence.entries.Add(Line(player,
            "Then let us try."));
        sequence.entries.Add(Line(player,
            "Maybe your perspective can reveal more clues.",
            eventOnEnd: Level2ADialogueIds.Events.QuestExploreUfo));
    }

    // =============================================
    // UFO探索: 发现被树根缠住的能源块
    // =============================================
    private void BuildEnergyFound(DialogueSequence sequence)
    {
        DialogueCharacter terminal = GetCharacter(Level2ADialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal,
            "Energy signal detected."));
        sequence.entries.Add(Line(terminal,
            "Target is entangled by tree roots.",
            eventOnEnd: Level2ADialogueIds.Events.QuestSiphonEnergy));
    }

    // =============================================
    // 虹吸失败: 树根锁住了能源块
    // =============================================
    private void BuildSiphonFailed(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(Level2ADialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(Level2ADialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal,
            "Extraction failed."));
        sequence.entries.Add(Line(terminal,
            "Target is locked by root structure."));
        sequence.entries.Add(Line(terminal,
            "Recommendation: clear root obstacles first."));

        sequence.entries.Add(Line(player,
            "It is tangled in the roots. Cannot pull it out."));
        sequence.entries.Add(Line(player,
            "Maybe we can use Shooting to blast the roots away.",
            eventOnEnd: Level2ADialogueIds.Events.QuestShootRoots));
    }

    // =============================================
    // 树根被摧毁后
    // =============================================
    private void BuildRootsCleared(DialogueSequence sequence)
    {
        DialogueCharacter terminal = GetCharacter(Level2ADialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal,
            "Obstacle cleared."));
        sequence.entries.Add(Line(terminal,
            "Extraction should now be possible.",
            eventOnEnd: Level2ADialogueIds.Events.QuestSiphonAfterRoots));
    }

    // =============================================
    // 虹吸成功 + Signal Lost转场
    // =============================================
    private void BuildSiphonSuccess(DialogueSequence sequence)
    {
        DialogueCharacter terminal = GetCharacter(Level2ADialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal,
            "Energy unit recovered successfully."));
        sequence.entries.Add(Line(terminal,
            "Power recharging...",
            eventOnEnd: Level2ADialogueIds.Events.PlaySignalLost));
    }

    // =============================================
    // Signal Lost恢复后: 路尽头出现传送门
    // =============================================
    private void BuildPortalDiscovered(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(Level2ADialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(Level2ADialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal, "Wait."));
        sequence.entries.Add(Line(terminal,
            "A portal has appeared at the end of the road."));
        sequence.entries.Add(Line(terminal,
            "Energy recovery seems to have triggered a local temporal correction."));
        sequence.entries.Add(Line(terminal,
            "Perhaps we can leave this area through that portal."));

        sequence.entries.Add(Line(player, "A portal?"));
        sequence.entries.Add(Line(player,
            "Let us go check it out.",
            eventOnEnd: Level2ADialogueIds.Events.QuestGoToPortal));
        // 玩家走到传送门 → 触发Level2B的PortalInactive对话
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
}