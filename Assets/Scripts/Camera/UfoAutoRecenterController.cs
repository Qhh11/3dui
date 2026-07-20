using Cinemachine;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// 当玩家被锁在 UFO 视角且 AR 漂移已经让所有激活关卡模型
/// 持续超出安全距离时，平滑恢复 UFO 的位置锚点和朝向。
/// </summary>
[DefaultExecutionOrder(1000)]
public class UfoAutoRecenterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera outputCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineVirtualCamera
        ufoVirtualCamera;
    [SerializeField] private UfoMovement ufoMovement;
    [SerializeField] private SwitchPerspectiveButtonController
        switchController;
    [SerializeField] private Transform recoveryLookTarget;

    [Header("Active Level Models")]
    [SerializeField] private GameObject[] levelRoots;

    [Header("Recovery")]
    [Min(1f)]
    [SerializeField] private float recoveryDistance = 8000f;

    [Min(0.1f)]
    [SerializeField] private float outOfRangeDuration = 3f;

    [Min(0f)]
    [SerializeField] private float recenterDuration = 1.25f;

    [Min(0f)]
    [SerializeField] private float recoveryCooldown = 5f;

    [Range(0.05f, 1f)]
    [SerializeField] private float checkInterval = 0.25f;

    [Header("Tracking Safety")]
    [SerializeField] private bool requireStableTracking = true;

    [Min(0f)]
    [SerializeField] private float stableTrackingDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Renderer[][] levelRenderers;
    private float nextCheckTime;
    private float outOfRangeStartTime = -1f;
    private float trackingStableStartTime = -1f;
    private float cooldownUntil;

    private void Awake()
    {
        CacheLevelRenderers();

        if (outputCamera == null)
            outputCamera = Camera.main;

        if (cinemachineBrain == null &&
            outputCamera != null)
        {
            cinemachineBrain =
                outputCamera.GetComponent<CinemachineBrain>();
        }
    }

    private void OnDisable()
    {
        ResetDetection();
        trackingStableStartTime = -1f;
    }

    private void Update()
    {
        float now = Time.unscaledTime;

        if (now < nextCheckTime)
            return;

        nextCheckTime =
            now + Mathf.Max(0.05f, checkInterval);

        if (!CanMonitor(now))
        {
            ResetDetection();
            return;
        }

        if (!TryGetDistanceToActiveLevels(
                outputCamera.transform.position,
                out float nearestDistance))
        {
            // 没有有效的激活模型时不能可靠判断漂移。
            ResetDetection();
            return;
        }

        if (nearestDistance <= recoveryDistance)
        {
            ResetDetection();
            return;
        }

        if (outOfRangeStartTime < 0f)
        {
            outOfRangeStartTime = now;
            return;
        }

        if (now < cooldownUntil ||
            now - outOfRangeStartTime <
            outOfRangeDuration)
        {
            return;
        }

        Vector3 lookTarget =
            recoveryLookTarget != null
                ? recoveryLookTarget.position
                : ResolveFallbackLookTarget();

        if (debugLog)
        {
            Debug.Log(
                $"[UFO Recenter] distance={nearestDistance:F1}, " +
                $"target={lookTarget}",
                this
            );
        }

        ufoMovement.RecenterPoseLookingAt(
            lookTarget,
            recenterDuration
        );

        cooldownUntil =
            now + Mathf.Max(0f, recoveryCooldown);

        ResetDetection();
    }

    private bool CanMonitor(float now)
    {
        if (outputCamera == null ||
            ufoMovement == null ||
            switchController == null ||
            !switchController.IsUfoView ||
            !ufoMovement.IsActive ||
            ufoMovement.IsRecenterBlending ||
            switchController.CanPlayerSwitchNow)
        {
            trackingStableStartTime = -1f;
            return false;
        }

        if (cinemachineBrain != null &&
            (cinemachineBrain.IsBlending ||
             (ufoVirtualCamera != null &&
              cinemachineBrain.ActiveVirtualCamera !=
              (ICinemachineCamera)ufoVirtualCamera)))
        {
            trackingStableStartTime = -1f;
            return false;
        }

        return IsTrackingStable(now);
    }

    private bool IsTrackingStable(float now)
    {
        if (!requireStableTracking)
            return true;

#if UNITY_EDITOR
        return true;
#else
        bool tracking =
            ARSession.state ==
                ARSessionState.SessionTracking &&
            ARSession.notTrackingReason ==
                NotTrackingReason.None;

        if (!tracking)
        {
            trackingStableStartTime = -1f;
            return false;
        }

        if (trackingStableStartTime < 0f)
            trackingStableStartTime = now;

        return
            now - trackingStableStartTime >=
            stableTrackingDuration;
#endif
    }

    private bool TryGetDistanceToActiveLevels(
        Vector3 cameraPosition,
        out float nearestDistance)
    {
        float nearestSqrDistance = float.PositiveInfinity;
        bool foundRenderer = false;

        if (levelRoots == null ||
            levelRenderers == null)
        {
            nearestDistance = 0f;
            return false;
        }

        int count = Mathf.Min(
            levelRoots.Length,
            levelRenderers.Length
        );

        for (int levelIndex = 0;
             levelIndex < count;
             levelIndex++)
        {
            GameObject root = levelRoots[levelIndex];

            if (root == null ||
                !root.activeInHierarchy)
            {
                continue;
            }

            Renderer[] renderers =
                levelRenderers[levelIndex];

            if (renderers == null)
                continue;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (!IsModelRenderer(renderer))
                    continue;

                foundRenderer = true;

                float sqrDistance =
                    renderer.bounds.SqrDistance(
                        cameraPosition
                    );

                if (sqrDistance < nearestSqrDistance)
                    nearestSqrDistance = sqrDistance;
            }
        }

        nearestDistance = foundRenderer
            ? Mathf.Sqrt(nearestSqrDistance)
            : 0f;

        return foundRenderer;
    }

    private static bool IsModelRenderer(Renderer renderer)
    {
        return
            renderer != null &&
            renderer.enabled &&
            renderer.gameObject.activeInHierarchy &&
            !(renderer is ParticleSystemRenderer) &&
            !(renderer is TrailRenderer) &&
            !(renderer is LineRenderer);
    }

    private Vector3 ResolveFallbackLookTarget()
    {
        if (levelRoots != null)
        {
            for (int i = 0; i < levelRoots.Length; i++)
            {
                GameObject root = levelRoots[i];

                if (root != null &&
                    root.activeInHierarchy)
                {
                    return root.transform.position;
                }
            }
        }

        return outputCamera.transform.position +
               outputCamera.transform.forward;
    }

    private void CacheLevelRenderers()
    {
        if (levelRoots == null)
        {
            levelRenderers = new Renderer[0][];
            return;
        }

        levelRenderers =
            new Renderer[levelRoots.Length][];

        for (int i = 0; i < levelRoots.Length; i++)
        {
            GameObject root = levelRoots[i];

            levelRenderers[i] = root != null
                ? root.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
        }
    }

    private void ResetDetection()
    {
        outOfRangeStartTime = -1f;
    }
}
