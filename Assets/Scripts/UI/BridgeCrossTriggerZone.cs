using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class BridgeCrossTriggerZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool onlyOnce = true;
    [SerializeField] private string completeEventKey = DialogueIds.Events.QuestGoToTower;
    [Tooltip("必须已点亮这个 flag 才允许触发（空=无前置要求）。用于让本步只能在上一步完成后触发。")]
    [SerializeField] private string requiredFlag = "";
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
        
        if (!QuestProgress.Has(requiredFlag))
            return;
        
        triggered = true;
        DialogueEventRouter.Instance?.Trigger(completeEventKey);
    }
}
