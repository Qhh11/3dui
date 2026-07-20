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
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private MarkController markController;

    [Header("Settings")]
    public float rocketSpeed = 5000f;
    public float maxAimDistance = 50000f;
    public LayerMask aimMask = ~0;

    [Header("Cooldown")]
    public float fireCooldown = 1f;

    private bool isCoolingDown;
    
    private int lastFireFrame = -1;

    public bool FiredThisFrame => lastFireFrame == Time.frameCount;

    private void Start()
    {
        RefreshButtonState();
    }

    private void Update()
    {
        RefreshButtonState();
    }
    private void OnEnable()
    {
        isCoolingDown = false;
        lastFireFrame = -1;
        RefreshButtonState();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        isCoolingDown = false;
        lastFireFrame = -1;

        if (rocketButton != null)
        {
            rocketButton.interactable = false;
        }
    }

    public void Fire()
    {
        if (!CanFire())
            return;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (rocketPrefab == null || firePoint == null || aimCamera == null)
        {
            Debug.LogWarning("RocketLauncher missing reference.");
            return;
        }
        
        lastFireFrame = Time.frameCount;

        StartCoroutine(CooldownRoutine());

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

    private bool CanFire()
    {
        if (isCoolingDown)
            return false;

        if (siphonController != null &&
            siphonController.IsBusy)
        {
            return false;
        }

        if (markController != null &&
            (markController.IsBusy ||
             markController.ActedThisFrame))
        {
            return false;
        }

        if (GameplayInputLockService.IsBlocked)
        {
            return false;
        }

        return true;
    }

    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        RefreshButtonState();

        yield return new WaitForSeconds(fireCooldown);

        isCoolingDown = false;
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (rocketButton != null)
        {
            rocketButton.interactable = CanFire();
        }
    }
    
    public bool CanFireNow()
    {
        return CanFire();
    }

    public bool TryGetAimTarget(out MonoBehaviour target)
    {
        target = null;

        if (!enabled || !gameObject.activeInHierarchy)
            return false;

        if (!CanFire())
            return false;

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera == null)
            return false;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimMask, QueryTriggerInteraction.Collide))
            return false;

        BlastBlockedRockTarget rockTarget = hit.collider.GetComponentInParent<BlastBlockedRockTarget>();
        if (rockTarget != null)
        {
            if (!rockTarget.CanBeRocketTargetNow())
                return false;
            target = rockTarget;
            return true;
        }

        BlastBlockedRootTarget rootTarget = hit.collider.GetComponentInParent<BlastBlockedRootTarget>();
        if (rootTarget != null)
        {
            if (!rootTarget.CanBeRocketTargetNow())
                return false;
            target = rootTarget;
            return true;
        }

        return false;
    }
}
