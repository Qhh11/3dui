using UnityEngine;
using UnityEngine.Events;

public class DialogueObjectTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueSequence dialogueSequence;
    [SerializeField] private string eventKey;
    [SerializeField] private bool onlyOnce;
    [SerializeField] private UnityEvent onInteract;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许交互（空=无前置要求）。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮，则不再允许交互（可选）。")]
    [SerializeField] private string blockedByFlag = "";

    private bool hasInteracted;

    public void Interact()
    {
        if (GameplayInputLockService.IsBlocked)
            return;

        if (DialogueRunner.Instance != null &&
            DialogueRunner.Instance.IsDialogueOpen)
        {
            return;
        }

        if (onlyOnce && hasInteracted)
            return;
        
        // —— 进度门控 ——
        if (!QuestProgress.Has(requiredFlag))
            return;

        if (!string.IsNullOrWhiteSpace(blockedByFlag) &&
            QuestProgress.Has(blockedByFlag))
        {
            return;
        }

        hasInteracted = true;

        if (!string.IsNullOrWhiteSpace(eventKey))
            DialogueEventRouter.Instance?.Trigger(eventKey);

        onInteract?.Invoke();

        if (dialogueSequence != null)
            DialogueRunner.Instance?.StartDialogue(dialogueSequence);
    }
}