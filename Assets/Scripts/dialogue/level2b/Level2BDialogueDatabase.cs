using System.Collections.Generic;
using UnityEngine;

public class Level2BDialogueDatabase : MonoBehaviour
{
    public static Level2BDialogueDatabase Instance { get; private set; }

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
        if (sequences.TryGetValue(sequenceId, out DialogueSequence sequence))
        {
            return sequence;
        }

        Debug.LogWarning("Level2B sequence not found: " + sequenceId);
        return null;
    }

    private void BuildCharacters()
    {
        characters.Clear();

        AddCharacter(
            Level2BDialogueIds.Characters.Player,
            "Player",
            "player",
            Color.white
        );

        AddCharacter(
            Level2BDialogueIds.Characters.UFOTerminal,
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

    private DialogueCharacter GetCharacter(string id)
    {
        if (characters.TryGetValue(id, out DialogueCharacter character))
        {
            return character;
        }

        Debug.LogWarning("Level2B character not found: " + id);
        return null;
    }

    private void BuildSequences()
    {
        sequences.Clear();

        DialogueSequence portalInactive = NewSequence(Level2BDialogueIds.Sequences.PortalInactive);
        DialogueSequence portalActivated = NewSequence(Level2BDialogueIds.Sequences.PortalActivated);

        BuildPortalInactive(portalInactive);
        BuildPortalActivated(portalActivated);
    }

    private DialogueSequence NewSequence(string sequenceId)
    {
        DialogueSequence sequence = ScriptableObject.CreateInstance<DialogueSequence>();
        sequence.sequenceId = sequenceId;
        sequence.entries = new List<DialogueEntry>();

        sequences[sequenceId] = sequence;
        return sequence;
    }

    private void BuildPortalInactive(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(Level2BDialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(Level2BDialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(player, "This portal is not working."));
        sequence.entries.Add(Line(player, "It looks completely gray."));

        sequence.entries.Add(Line(terminal, "The portal is inactive."));
        sequence.entries.Add(Line(terminal, "The timeline frequency is still unstable."));

        sequence.entries.Add(Line(player, "What do we do now?"));

        sequence.entries.Add(Line(terminal, "I can attempt to synchronize my perspective with yours."));
        sequence.entries.Add(Line(terminal, "In your view, these mountains are volcanic."));
        sequence.entries.Add(Line(terminal, "In my view, they are still frozen."));

        sequence.entries.Add(Line(player, "So if your side changes, the portal may react?"));

        sequence.entries.Add(Line(terminal, "Correct."));
        sequence.entries.Add(Line(terminal, "New function available: Heat Ray."));
        sequence.entries.Add(Line(
            terminal,
            "Aim at the frozen mountains and hold the Melt button.",
            eventOnEnd: Level2BDialogueIds.Events.QuestSwitchToUfoForHeat
        ));
    }

    private void BuildPortalActivated(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(Level2BDialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(Level2BDialogueIds.Characters.UFOTerminal);

        sequence.entries.Add(Line(terminal, "Timeline frequency synchronized."));
        sequence.entries.Add(Line(terminal, "Portal activation detected."));

        sequence.entries.Add(Line(player, "The portal changed color."));
        sequence.entries.Add(Line(player, "And there are... planets behind it?"));

        sequence.entries.Add(Line(terminal, "The portal is now open."));
        sequence.entries.Add(Line(
            terminal,
            "We should proceed carefully.",
            eventOnEnd: Level2BDialogueIds.Events.QuestReturnToPlayerForPortal
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