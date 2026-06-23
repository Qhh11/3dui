using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class RespawnCheckpoint : MonoBehaviour
{
    [Header("检查点")]
    [Tooltip("玩家重生后实际出现的位置")]
    [SerializeField] private Transform respawnPoint;

    [Tooltip("出生岛为 0，后续岛屿依次使用 1、2、3……")]
    [SerializeField] private int checkpointOrder = 1;

    [SerializeField] private bool activateOnlyOnce = true;

    private bool hasActivated;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activateOnlyOnce && hasActivated)
        {
            return;
        }

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        bool activated =
            player.TrySetRespawnPoint(
                respawnPoint,
                checkpointOrder
            );

        if (activated)
        {
            hasActivated = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (respawnPoint == null)
        {
            return;
        }

        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            respawnPoint.position,
            1f
        );

        Gizmos.DrawLine(
            respawnPoint.position,
            respawnPoint.position +
            respawnPoint.forward * 2f
        );
    }
}
