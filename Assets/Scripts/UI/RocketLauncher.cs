using UnityEngine;

public class RocketLauncher : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;
    public GameObject rocketPrefab;
    public Transform firePoint;

    [Header("Settings")]
    public float rocketSpeed = 35f;
    public float maxAimDistance = 200f;
    public LayerMask aimMask = ~0;

    public void Fire()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        if (rocketPrefab == null || firePoint == null || aimCamera == null)
        {
            Debug.LogWarning("RocketLauncher missing reference.");
            return;
        }

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * maxAimDistance;
        }

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        GameObject rocket = Instantiate(
            rocketPrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDirection)
        );

        RocketProjectile projectile = rocket.GetComponent<RocketProjectile>();

        if (projectile != null)
        {
            projectile.Launch(shootDirection, rocketSpeed);
        }
    }
}