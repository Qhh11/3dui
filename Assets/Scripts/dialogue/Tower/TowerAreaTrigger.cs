using UnityEngine;

public class TowerAreaTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许触发（空=无前置）。塔的入口建议填上一关的 tower_unlocked。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮则不再触发（可选）。")]
    [SerializeField] private string blockedByFlag = "";

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
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

        hasTriggered = true;
        TowerFlowController.Instance?.StartTowerIntro();
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        Transform root = other.transform.root;

        if (root != null && root.CompareTag(playerTag))
        {
            return true;
        }

        return false;
    }
}
