using UnityEngine;

public class NPCAutoDialogueTriggerById : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private string sequenceId = "villager_a_first_meet";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许触发（空=无前置要求）。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮，则不再触发（可选）。")]
    [SerializeField] private string blockedByFlag = "";

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("3D Dialogue Indicator")]
    [SerializeField] private GameObject dialogueIndicator;

    private bool hasTriggered;

    private void Start()
    {
        RefreshIndicator();
    }

    private void Update()
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

        if (!QuestProgress.Has(requiredFlag))
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(blockedByFlag) && QuestProgress.Has(blockedByFlag))
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

        bool wanted = CanShowIndicator();
        if (dialogueIndicator.activeSelf != wanted)
            dialogueIndicator.SetActive(wanted);
    }

    private bool CanShowIndicator()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return false;
        }
        if (!QuestProgress.Has(requiredFlag))
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(blockedByFlag) && QuestProgress.Has(blockedByFlag))
        {
            return false;
        }

        return true;
    }
}
