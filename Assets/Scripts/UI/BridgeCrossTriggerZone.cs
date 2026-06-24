using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class BridgeCrossTriggerZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool onlyOnce = true;
    [SerializeField] private string completeEventKey = DialogueIds.Events.QuestGoToTower;

    private bool triggered;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onlyOnce && triggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        triggered = true;
        DialogueEventRouter.Instance?.Trigger(completeEventKey);
    }
}
