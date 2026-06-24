using System.Collections;
using UnityEngine;

public class DelayedDialogueTrigger : MonoBehaviour
{
    [SerializeField] private string sequenceId;
    [SerializeField] private float delay = 1f;
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    public void TriggerWithDelay()
    {
        if (triggerOnlyOnce && hasTriggered) return;
        hasTriggered = true;

        StartCoroutine(DelayedTrigger());
    }

    private IEnumerator DelayedTrigger()
    {
        yield return new WaitForSeconds(delay);

        DialogueSequence seq = Level1DialogueDatabase.Instance
            .GetSequence(sequenceId);
        if (seq != null)
            DialogueRunner.Instance.StartDialogue(seq);
    }
}