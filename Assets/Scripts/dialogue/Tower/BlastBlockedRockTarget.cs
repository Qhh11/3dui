using UnityEngine;

public class BlastBlockedRockTarget : MonoBehaviour
{
    [SerializeField] private bool destroyOnlyOnce = true;

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

        RocketProjectile projectile = other.GetComponentInParent<RocketProjectile>();

        if (projectile == null)
        {
            return;
        }

        hasDestroyed = true;
        TowerFlowController.Instance?.OnBlockedRocksDestroyed();
    }
}