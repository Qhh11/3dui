using UnityEngine;

public class TowerAreaTrigger : MonoBehaviour
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