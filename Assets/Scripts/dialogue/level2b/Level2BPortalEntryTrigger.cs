using UnityEngine;

public class Level2BPortalEntryTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;
    [Tooltip("必须已点亮这个 flag 才允许触发（空=无前置）。塔的入口建议填上一关的 tower_unlocked。")]
    [SerializeField] private string requiredFlag = "";
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
        bool accepted = Level2BFlowController.Instance != null &&
                        Level2BFlowController.Instance.TryEnterPortal();

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