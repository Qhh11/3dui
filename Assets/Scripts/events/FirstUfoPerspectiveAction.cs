using System.Collections;
using UnityEngine;

public class FirstUfoPerspectiveAction : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private string firstUfoSequenceId =
        "level1_first_ufo_view";

    [SerializeField] private float delayBeforeDialogue = 2f;

    [Header("Objective")]
    [SerializeField] private bool completeCurrentObjective = true;

    private bool hasTriggered;

    public void Trigger()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;
        StartCoroutine(RunRoutine());
    }

    private IEnumerator RunRoutine()
    {
        if (completeCurrentObjective &&
            ObjectivePanelController.Instance != null)
        {
            ObjectivePanelController.Instance
                .CompleteCurrentObjective();
        }

        yield return new WaitForSecondsRealtime(
            delayBeforeDialogue
        );

        if (DialogueRunner.Instance != null &&
            DialogueRunner.Instance.IsDialogueOpen)
        {
            yield break;
        }

        DialogueSequence sequence =
            Level1DialogueDatabase.Instance
                .GetSequence(firstUfoSequenceId);

        if (sequence == null)
        {
            Debug.LogWarning(
                $"Dialogue sequence not found: {firstUfoSequenceId}"
            );
            yield break;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
    }
}