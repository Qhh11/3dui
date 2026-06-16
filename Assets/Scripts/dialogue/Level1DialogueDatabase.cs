using System.Collections.Generic;
using UnityEngine;

public class Level1DialogueDatabase : MonoBehaviour
{
    public static Level1DialogueDatabase Instance { get; private set; }

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
            Debug.LogWarning("Dialogue sequence id is empty.");
            return null;
        }

        if (sequences.TryGetValue(sequenceId, out DialogueSequence sequence))
            return sequence;

        Debug.LogWarning($"Dialogue sequence not found: {sequenceId}");
        return null;
    }

    public DialogueCharacter GetCharacter(string characterId)
    {
        if (characters.TryGetValue(characterId, out DialogueCharacter character))
            return character;

        Debug.LogWarning($"Dialogue character not found: {characterId}");
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
        DialogueSequence villagerAFirstMeet = NewSequence(DialogueIds.Sequences.VillagerAFirstMeet);
        DialogueSequence villagerAWrongAnswer = NewSequence(DialogueIds.Sequences.VillagerAWrongAnswer);
        DialogueSequence villagerACorrectAnswer = NewSequence(DialogueIds.Sequences.VillagerACorrectAnswer);

        BuildPrelude(prelude);
        BuildVillagerAFirstMeet(villagerAFirstMeet, villagerAWrongAnswer, villagerACorrectAnswer);
        BuildVillagerAWrongAnswer(villagerAWrongAnswer, villagerAWrongAnswer, villagerACorrectAnswer);
        BuildVillagerACorrectAnswer(villagerACorrectAnswer);
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
        sequence.entries.Add(Line(player, "What happened? UFO Terminal, can you hear me?"));
        sequence.entries.Add(Line(terminal, "Warning. Warning."));
        sequence.entries.Add(Line(terminal, "Temporal turbulence detected."));
        sequence.entries.Add(Line(terminal, "Main system damaged. Emergency landing completed in an unknown area."));
        sequence.entries.Add(Line(terminal, "Backup energy units were scattered during impact."));
        sequence.entries.Add(Line(terminal, "All energy units must be recovered before departure."));
        sequence.entries.Add(Line(player, "Thank goodness. At least I can still reach you."));
        sequence.entries.Add(Line(player, "The energy units could be anywhere."));
        sequence.entries.Add(Line(player, "Maybe I can use your scouting system to locate them."));
        sequence.entries.Add(Line(player, "Let me try if this still works.", eventOnEnd: DialogueIds.Events.QuestInvestigateTent));
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
        question.choices.Add(Choice("Triangle", correctAnswer, setFlagOnSelect: DialogueIds.Flags.AnsweredTowerMark));
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
        question.choices.Add(Choice("Triangle", correctAnswer, setFlagOnSelect: DialogueIds.Flags.AnsweredTowerMark));
        question.choices.Add(Choice("Square", wrongAnswer));
        sequence.entries.Add(question);
    }

    private void BuildVillagerACorrectAnswer(DialogueSequence sequence)
    {
        DialogueCharacter player = GetCharacter(DialogueIds.Characters.Player);
        DialogueCharacter terminal = GetCharacter(DialogueIds.Characters.UFOTerminal);
        DialogueCharacter villager = GetCharacter(DialogueIds.Characters.VillagerA);

        sequence.entries.Add(Line(villager, "...Triangle."));
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
