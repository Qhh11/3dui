using UnityEngine;

public class NPCAutoDialogueTriggerById : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private string sequenceId = "villager_a_first_meet";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("3D Dialogue Indicator")]
    [SerializeField] private GameObject dialogueIndicator;

    private bool hasTriggered;

    private void Start()
    {
        RefreshIndicator();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        TryStartDialogue();
    }

    private void TryStartDialogue()
    {
        if (!CanStartDialogue())
        {
            return;
        }

        DialogueSequence sequence = Level1DialogueDatabase.Instance.GetSequence(sequenceId);

        if (sequence == null)
        {
            Debug.LogWarning($"Dialogue sequence not found: {sequenceId}");
            return;
        }

        hasTriggered = true;
        RefreshIndicator();

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    private bool CanStartDialogue()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return false;
        }

        if (DialogueRunner.Instance != null && DialogueRunner.Instance.IsDialogueOpen)
        {
            return false;
        }

        return true;
    }

    private void RefreshIndicator()
    {
        if (dialogueIndicator == null)
        {
            return;
        }

        dialogueIndicator.SetActive(CanShowIndicator());
    }

    private bool CanShowIndicator()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return false;
        }

        return true;
    }
}