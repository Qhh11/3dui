using System.Collections.Generic;
using UnityEngine;

public class TowerDialogueDatabase : MonoBehaviour
{
    public static TowerDialogueDatabase Instance { get; private set; }

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
            Debug.LogWarning("Tower dialogue sequence id is empty.");
            return null;
        }

        if (sequences.TryGetValue(sequenceId, out DialogueSequence sequence))
        {
            return sequence;
        }

        Debug.LogWarning("Tower dialogue sequence not found: " + sequenceId);
        return null;
    }

    private void BuildCharacters()
    {
        characters.Clear();

        AddCharacter(
            TowerDialogueIds.Characters.Player,
            "Player",
            "player",
            Color.white
        );

        AddCharacter(
            TowerDialogueIds.Characters.UFOTerminal,
            "UFO Terminal",
            "ufo_terminal",
            new Color(0.55f, 0.85f, 1f)
        );
    }

    private void AddCharacter(string id, string displayName, string portraitResourceName, Color nameColor)
    {
        DialogueCharacter character = ScriptableObject.CreateInstance<DialogueCharacter>();
        character.displayName = displayName;
        character.nameColor = nameColor;

        if (loadPortraitsFromResources)
        {
            character.portrait = Resources.Load<Sprite>("Portraits/" + portraitResourceName);
        }

        characters[id] = character;
    }

    private DialogueCharacter GetCharacter(string characterId)
    {
        if (characters.TryGetValue(characterId, out DialogueCharacter character))
        {
            return character;
        }

        Debug.LogWarning("Tower dialogue character not found: " + characterId);
        return null;
    }

    private void BuildSequences()
    {
        sequences.Clear();

        DialogueSequence towerIntro = NewSequence(TowerDialogueIds.Sequences.TowerIntro);
        DialogueSequence blastUnlocked = NewSequence(TowerDialogueIds.Sequences.BlastUnlocked);
        DialogueSequence blockedRocksFound = NewSequence(TowerDialogueIds.Sequences.BlockedRocksFound);
        DialogueSequence afterRocksDestroyed = NewSequence(TowerDialogueIds.Sequences.AfterRocksDestroyed);

        BuildTowerIntro(towerIntro);
        BuildBlastUnlocked(blastUnlocked);
        BuildBlockedRocksFound(blockedRocksFound);
        BuildAfterRocksDestroyed(afterRocksDestroyed);
    }

    private DialogueSequence NewSequence(string sequenceId)
    {
        DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
        sequence.sequenceId = sequenceId;
        sequence.entries = new List<DialogueEntry>();

        sequences[sequenceId] = sequence;
        return sequence;
    }

    private void BuildTowerIntro(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(TowerDialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(TowerDialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(player, "This is the tower."));
        sequence.entries.Add(Line(player, "The entrance is completely sealed."));

        sequence.entries.Add(Line(terminal, "Energy residue detected nearby."));
        sequence.entries.Add(Line(terminal, "Signal source is not inside the tower."));

        sequence.entries.Add(Line(player, "Nearby?"));
        sequence.entries.Add(Line(
            player,
            "Something is glowing beside the tower.",
            eventOnEnd: TowerDialogueIds.Events.QuestCollectTowerEnergyCore
        ));
    }

    private void BuildBlastUnlocked(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(TowerDialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(TowerDialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal, "Energy core recovered."));
        sequence.entries.Add(Line(terminal, "Power restored."));
        sequence.entries.Add(Line(terminal, "Blast function unlocked."));

        sequence.entries.Add(Line(player, "Blast function? So now you can shoot?"));

        sequence.entries.Add(Line(terminal, "Correct."));
        sequence.entries.Add(Line(terminal, "Blast can destroy unstable obstacles."));
        sequence.entries.Add(Line(terminal, "However, the target must be confirmed first."));

        sequence.entries.Add(Line(
            player,
            "Then I should go back and check the blocked passage.",
            eventOnEnd: TowerDialogueIds.Events.QuestReturnToPlayerAfterCore
        ));
    }

    private void BuildBlockedRocksFound(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(TowerDialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(TowerDialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(player, "This must be the path."));
        sequence.entries.Add(Line(player, "But the rocks are blocking everything."));

        sequence.entries.Add(Line(terminal, "Unstable obstacle detected."));
        sequence.entries.Add(Line(terminal, "Blast function can destroy this barrier."));

        sequence.entries.Add(Line(
            player,
            "Then we need to use the UFO view.",
            eventOnEnd: TowerDialogueIds.Events.QuestSwitchToUfoForBlast
        ));
    }

    private void BuildAfterRocksDestroyed(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(TowerDialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(TowerDialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal, "Warning."));
        sequence.entries.Add(Line(terminal, "Signal interference increasing."));

        sequence.entries.Add(Line(player, "What happened?"));
        sequence.entries.Add(Line(player, "That road... was it always there?"));

        sequence.entries.Add(Line(terminal, "Signal unstable."));
        sequence.entries.Add(Line(terminal, "Newly exposed path detected."));
        sequence.entries.Add(Line(terminal, "Destination unknown."));

        sequence.entries.Add(Line(player, "If this path just appeared, then it must mean something changed."));
        sequence.entries.Add(Line(
            player,
            "We should follow it.",
            eventOnEnd: TowerDialogueIds.Events.QuestReturnToPlayerAfterBlast
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
}