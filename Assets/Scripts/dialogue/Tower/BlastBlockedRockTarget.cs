using UnityEngine;

public class BlastBlockedRockTarget : MonoBehaviour
{
    [SerializeField] private bool destroyOnlyOnce = true;

    [Header("Progression Gate")]
    [SerializeField] private string requiredFlag = "";
    [SerializeField] private string blockedByFlag = "";

    [Header("Outline")]
    [SerializeField] private UfoSelectableOutline outline;

    private bool hasDestroyed;

    private void Awake()
    {
        if (outline == null)
            outline = GetComponent<UfoSelectableOutline>();

        if (outline == null)
            outline = GetComponentInChildren<UfoSelectableOutline>(true);
    }

    public bool CanBeRocketTargetNow()
    {
        if (destroyOnlyOnce && hasDestroyed)
            return false;

        if (!QuestProgress.Has(requiredFlag))
            return false;

        if (!string.IsNullOrWhiteSpace(blockedByFlag) &&
            QuestProgress.Has(blockedByFlag))
        {
            return false;
        }

        return true;
    }

    public void SetOutlineState(UfoSelectableOutline.OutlineState state)
    {
        if (outline != null)
            outline.SetOutlineState(state);
    }

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
        if (!CanBeRocketTargetNow())
            return;

        RocketProjectile projectile =
            other.GetComponentInParent<RocketProjectile>();

        if (projectile == null)
            return;

        hasDestroyed = true;
        SetOutlineState(UfoSelectableOutline.OutlineState.Default);
        TowerFlowController.Instance?.OnBlockedRocksDestroyed();
    }
}