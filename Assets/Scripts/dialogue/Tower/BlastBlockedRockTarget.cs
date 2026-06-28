using UnityEngine;

public class BlastBlockedRockTarget : MonoBehaviour
{
    [SerializeField] private bool destroyOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许被炸毁（空=无前置）。建议填阻路对话结束后的 tower_rocks_dialogue_done，防止提前炸石。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮则不再允许炸毁（可选）。")]
    [SerializeField] private string blockedByFlag = "";

    private bool hasDestroyed;

    private void OnCollisionEnter(Collision collision)
    {
        TryDestroyFromCollider(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDestroyFromCollider(other);
    }

    private void TryDestroyFromCollider(Collider other)
    {
        if (destroyOnlyOnce && hasDestroyed)
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

        RocketProjectile projectile = other.GetComponentInParent<RocketProjectile>();

        if (projectile == null)
        {
            return;
        }

        hasDestroyed = true;
        TowerFlowController.Instance?.OnBlockedRocksDestroyed();
    }
}
