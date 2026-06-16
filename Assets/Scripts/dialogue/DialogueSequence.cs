using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public string sequenceId;
    public List<DialogueEntry> entries = new();
}

[System.Serializable]
public class DialogueEntry
{
    public DialogueCharacter speaker;

    [TextArea(2, 6)]
    public string text;

    [Header("Optional events")]
    public string eventOnStart;
    public string eventOnEnd;

    [Header("Optional choices")]
    public List<DialogueChoice> choices = new();
}

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public DialogueSequence nextSequence;
    public string setFlagOnSelect;
    public string eventOnSelect;
}
