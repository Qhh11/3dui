using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class BoundaryRecoverTrigger : MonoBehaviour
{
    [Header("Recover")]
    [SerializeField] private bool usePlayerRespawn = true;
    [SerializeField] private Transform recoverPoint;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsUfoView())
            return;

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || player.IsRespawning)
            return;

        if (usePlayerRespawn || recoverPoint == null)
        {
            player.Respawn();
            return;
        }

        player.TeleportTo(recoverPoint);
    }

    private bool IsUfoView()
    {
        return
            SwitchPerspectiveButtonController.Instance != null &&
            SwitchPerspectiveButtonController.Instance.IsUfoView;
    }
}