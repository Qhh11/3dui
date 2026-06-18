using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RocketLauncher : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;
    public GameObject rocketPrefab;
    public Transform firePoint;
    [SerializeField] private Button rocketButton;

    [Header("Settings")]
    public float rocketSpeed = 5000f;
    public float maxAimDistance = 50000f;
    public LayerMask aimMask = ~0;

    [Header("Cooldown")]
    public float fireCooldown = 1f;

    private bool isCoolingDown;

    public void Fire()
    {
        if (isCoolingDown)
            return;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (rocketPrefab == null || firePoint == null || aimCamera == null)
        {
            Debug.LogWarning("RocketLauncher missing reference.");
            return;
        }

        StartCoroutine(CooldownRoutine());

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask))
            targetPoint = hit.point;
        else
            targetPoint = ray.origin + ray.direction * maxAimDistance;

        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        GameObject rocket = Instantiate(
            rocketPrefab,
            firePoint.position,
            Quaternion.LookRotation(shootDirection)
        );

        RocketProjectile projectile = rocket.GetComponent<RocketProjectile>();

        if (projectile != null)
            projectile.Launch(shootDirection, rocketSpeed);
    }

    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        SetButtonInteractable(false);

        yield return new WaitForSeconds(fireCooldown);

        isCoolingDown = false;
        SetButtonInteractable(true);
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (rocketButton != null)
            rocketButton.interactable = interactable;
    }
}