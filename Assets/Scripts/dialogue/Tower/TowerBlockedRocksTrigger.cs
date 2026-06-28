using UnityEngine;

public class TowerBlockedRocksTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许触发（空=无前置）。建议填捡到核心后解锁的 tower_blast_ready。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮则不再触发（可选）。")]
    [SerializeField] private string blockedByFlag = "";

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTrigger(other);
    }

    private void TryTrigger(Collider other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (!IsPlayer(other))
        {
            return;
        }

        if (!QuestProgress.Has(requiredFlag))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(blockedByFlag) && QuestProgress.Has(blockedByFlag))
        {
            return;
        }

        bool accepted = TowerFlowController.Instance != null &&
                        TowerFlowController.Instance.TryStartBlockedRocksDialogue();

        if (accepted)
        {
            hasTriggered = true;
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        Transform root = other.transform.root;
        return root != null && root.CompareTag(playerTag);
    }
}
