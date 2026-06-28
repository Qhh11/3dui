using System.Collections;
using UnityEngine;

public class DelayedDialogueTrigger : MonoBehaviour
{
    [SerializeField] private string sequenceId;
    [SerializeField] private float delay = 1f;
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;
    private int triggerVersion;

    public void TriggerWithDelay()
    {
        if (triggerOnlyOnce && hasTriggered)
            return;

        hasTriggered = true;
        triggerVersion++;

        StartCoroutine(
            DelayedTrigger(triggerVersion)
        );
    }

    private IEnumerator DelayedTrigger(int version)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (version != triggerVersion)
            yield break;

        if (DialogueRunner.Instance != null &&
            DialogueRunner.Instance.IsDialogueOpen)
        {
            yield break;
        }

        DialogueSequence seq =
            Level1DialogueDatabase.Instance
                .GetSequence(sequenceId);

        if (seq != null)
            DialogueRunner.Instance.StartDialogue(seq);
    }
}