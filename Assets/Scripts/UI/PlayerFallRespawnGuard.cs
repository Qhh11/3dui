using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerFallRespawnGuard : MonoBehaviour
{
    [SerializeField] private float minimumY = -500f;

    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (player == null || player.IsRespawning)
            return;

        if (transform.position.y > minimumY)
            return;

        player.Respawn();
    }
}