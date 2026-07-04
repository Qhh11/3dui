using UnityEngine;

public class NPCDialogueTriggerById : MonoBehaviour
{
    [SerializeField] private MonoBehaviour dialogueDatabase;

    [Header("Dialogue")]
    [SerializeField] private string sequenceId = "villager_a_first_meet";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("Dialogue can only start after this flag is set. Leave empty for no requirement.")]
    [SerializeField] private string requiredFlag = "";

    [Tooltip("Dialogue will not start if this flag is already set. Optional.")]
    [SerializeField] private string blockedByFlag = "";

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("3D Dialogue Indicator")]
    [SerializeField] private GameObject dialogueIndicator;

    private IDialogueDatabase database;
    private bool hasTriggered;

    private void Start()
    {
        database = dialogueDatabase as IDialogueDatabase;

        if (database == null && dialogueDatabase != null)
            database = dialogueDatabase.GetComponent<IDialogueDatabase>();

        if (database == null)
        {
            Debug.LogError(
                $"{name}: Dialogue Database is not assigned or does not implement IDialogueDatabase."
            );
        }

        RefreshIndicator();
    }

    private void Update()
    {
        RefreshIndicator();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        TryStartDialogue();
    }

    private void TryStartDialogue()
    {
        if (!CanStartDialogue())
            return;

        DialogueSequence sequence = database?.GetSequence(sequenceId);

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
        if (GameplayInputLockService.IsBlocked)
            return false;

        if (triggerOnlyOnce && hasTriggered)
            return false;

        if (!string.IsNullOrWhiteSpace(requiredFlag) &&
            !QuestProgress.Has(requiredFlag))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(blockedByFlag) &&
            QuestProgress.Has(blockedByFlag))
        {
            return false;
        }

        if (DialogueRunner.Instance != null &&
            DialogueRunner.Instance.IsDialogueOpen)
        {
            return false;
        }

        return true;
    }

    private void RefreshIndicator()
    {
        if (dialogueIndicator == null)
            return;

        bool wanted = CanShowIndicator();

        if (dialogueIndicator.activeSelf != wanted)
            dialogueIndicator.SetActive(wanted);
    }

    private bool CanShowIndicator()
    {
        if (triggerOnlyOnce && hasTriggered)
            return false;

        if (!string.IsNullOrWhiteSpace(requiredFlag) &&
            !QuestProgress.Has(requiredFlag))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(blockedByFlag) &&
            QuestProgress.Has(blockedByFlag))
        {
            return false;
        }

        return true;
    }
}