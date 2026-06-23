using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class WaterKillZone : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || player.IsRespawning)
        {
            return;
        }

        if (WaterRescueController.Instance != null)
        {
            WaterRescueController.Instance
                .RequestRescue(player);
        }
        else
        {
            Debug.LogWarning(
                "场景中没有 WaterRescueController，" +
                "执行直接重生。",
                this
            );

            player.Respawn();
        }
    }
}