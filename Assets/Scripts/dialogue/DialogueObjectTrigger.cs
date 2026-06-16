using UnityEngine;
using UnityEngine.Events;

public class DialogueObjectTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueSequence dialogueSequence;
    [SerializeField] private string eventKey;
    [SerializeField] private bool onlyOnce;
    [SerializeField] private UnityEvent onInteract;

    private bool hasInteracted;

    public void Interact()
    {
        if (onlyOnce && hasInteracted) return;
        hasInteracted = true;

        if (!string.IsNullOrWhiteSpace(eventKey))
            DialogueEventRouter.Instance?.Trigger(eventKey);

        onInteract?.Invoke();

        if (dialogueSequence != null)
            DialogueRunner.Instance?.StartDialogue(dialogueSequence);
    }
}
