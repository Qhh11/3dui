using System.Collections;
using UnityEngine;

public class DialogueStartOnAwakeById : MonoBehaviour
{
    [SerializeField] private string sequenceId = DialogueIds.Sequences.Level1Prelude;
    [SerializeField] private float delaySeconds = 0.2f;

    private IEnumerator Start()
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);
        else
            yield return null;

        DialogueSequence sequence = Level1DialogueDatabase.Instance?.GetSequence(sequenceId);

        if (sequence == null)
        {
            Debug.LogWarning($"Start dialogue sequence not found: {sequenceId}");
            yield break;
        }

        DialogueRunner.Instance?.StartDialogue(sequence);
    }
}
