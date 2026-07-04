using UnityEngine;

public class Level2BPortalEntryTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnlyOnce = true;

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