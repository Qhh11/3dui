using UnityEngine;

/// <summary>
/// AR 驱动的 UFO Virtual Camera。
///
/// rotationCalibration：完整旋转校准。
/// translationYawCalibration：纯 Yaw 位移校准。
///
/// User -> UFO Hint：立即预对准，交给 Cinemachine Blend。
/// UFO -> Hint：平滑改变旋转校准，不移动 UFO。
/// </summary>
public class UfoMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cam;
    public Transform devicePose;
    public Transform head;
    public PlayerController playerController;

    [Header("Placement")]
    public Vector3 headOffset =
        new Vector3(0f, 2000f, 0f);

    public float moveScale = 8000f;

    [Header("Debug")]
    public bool debugLog = false;

    private Vector3 baseDevicePos;
    private Vector3 anchorPos;

    private Quaternion rotationCalibration =
        Quaternion.identity;

    private Quaternion translationYawCalibration =
        Quaternion.identity;

    private bool active;
    private float logTimer;

    private bool playerControllerWasEnabled;
    private bool pendingPlayerControllerRestore;

    // UFO 内平滑转向只插值校准四元数。
    // 相机位置和设备姿态仍然实时生效。
    private bool rotationBlendActive;
    private Quaternion rotationBlendStart;
    private Quaternion rotationBlendTarget;
    private float rotationBlendStartTime;
    private float rotationBlendDuration;

    public bool IsActive => active;
    public bool IsRecenterBlending =>
        rotationBlendActive;

    private void Update()
    {
        TryRestorePlayerControllerAfterLock();
    }

    private void LateUpdate()
    {
        if (!active ||
            cam == null ||
            devicePose == null)
        {
            return;
        }

        UpdateRotationBlend();
        ApplyCurrentPoseImmediately();

        if (!debugLog)
            return;

        logTimer += Time.deltaTime;

        if (logTimer < 0.3f)
            return;

        logTimer = 0f;

        Debug.Log(
            $"[UFO] rotation=" +
            $"{cam.rotation.eulerAngles:F1} " +
            $"position={cam.position:F1} " +
            $"hintBlend={rotationBlendActive}"
        );
    }

    public void EnterUfo()
    {
        if (!PrepareEnter())
            return;

        CancelRotationBlend();

        Transform anchor =
            ResolveHeadTransform();

        anchorPos =
            anchor.position + headOffset;

        baseDevicePos =
            devicePose.position;

        float anchorYaw =
            anchor.eulerAngles.y;

        float deviceYaw =
            HeadingOf(devicePose.rotation);

        float yawOffset =
            Mathf.DeltaAngle(
                deviceYaw,
                anchorYaw
            );

        translationYawCalibration =
            Quaternion.Euler(
                0f,
                yawOffset,
                0f
            );

        rotationCalibration =
            translationYawCalibration;

        active = true;
        ApplyCurrentPoseImmediately();
    }

    /// <summary>
    /// 进入 UFO 时立即把目标 Virtual Camera 对准目标。
    /// Cinemachine Brain 之后负责从 User Camera 平滑混合过来。
    /// </summary>
    public void EnterUfoLookingAt(
        Vector3 targetPosition)
    {
        if (!PrepareEnter())
            return;

        CancelRotationBlend();

        Transform anchor =
            ResolveHeadTransform();

        anchorPos =
            anchor.position + headOffset;

        baseDevicePos =
            devicePose.position;

        active = true;

        Quaternion targetCalibration =
            CalculateLookCalibration(
                targetPosition
            );

        rotationCalibration =
            targetCalibration;

        UpdateTranslationYawForLook(
            targetCalibration *
            devicePose.rotation
        );

        ApplyCurrentPoseImmediately();
    }

    /// <summary>
    /// 保留旧接口：立即转向。
    /// </summary>
    public void RecenterLookingAt(
        Vector3 targetPosition)
    {
        RecenterLookingAt(
            targetPosition,
            0f
        );
    }

    /// <summary>
    /// 已在 UFO View 时重新对准目标。
    ///
    /// blendDuration <= 0：立即转向。
    /// blendDuration > 0：平滑转向。
    ///
    /// 不重置位置锚点，不改变已经发生的 AR 平移。
    /// </summary>
    public void RecenterLookingAt(
        Vector3 targetPosition,
        float blendDuration)
    {
        if (!active ||
            cam == null ||
            devicePose == null)
        {
            EnterUfoLookingAt(targetPosition);
            return;
        }

        Quaternion targetCalibration =
            CalculateLookCalibration(
                targetPosition
            );

        if (blendDuration <= 0f)
        {
            CancelRotationBlend();

            rotationCalibration =
                targetCalibration;

            ApplyCurrentPoseImmediately();
            return;
        }

        rotationBlendStart =
            rotationCalibration;

        rotationBlendTarget =
            targetCalibration;

        rotationBlendStartTime =
            Time.unscaledTime;

        rotationBlendDuration =
            Mathf.Max(
                0.0001f,
                blendDuration
            );

        rotationBlendActive = true;
    }

    public void ExitUfo()
    {
        active = false;
        CancelRotationBlend();

        if (playerController == null)
            return;

        if (!playerControllerWasEnabled)
        {
            pendingPlayerControllerRestore = false;
            return;
        }

        if (GameplayInputLockService.IsBlocked)
        {
            pendingPlayerControllerRestore = true;
            return;
        }

        playerController.enabled = true;
        pendingPlayerControllerRestore = false;
    }

    private bool PrepareEnter()
    {
        if (cam == null &&
            Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        if (cam == null ||
            devicePose == null)
        {
            Debug.LogWarning(
                "[UFO] cam 或 devicePose 未赋值，" +
                "无法启用 AR UFO。",
                this
            );

            return false;
        }

        if (playerController != null)
        {
            if (!active)
            {
                playerControllerWasEnabled =
                    playerController.enabled;
            }

            pendingPlayerControllerRestore = false;
            playerController.enabled = false;
        }

        return true;
    }

    private Transform ResolveHeadTransform()
    {
        if (head != null)
            return head;

        if (Camera.main != null)
            return Camera.main.transform;

        return cam;
    }

    private Quaternion CalculateLookCalibration(
        Vector3 targetPosition)
    {
        Vector3 cameraPosition =
            EvaluateCurrentWorldPosition();

        Vector3 direction =
            targetPosition - cameraPosition;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            direction =
                ResolveHeadTransform().forward;
        }

        Quaternion desiredWorldRotation =
            SafeLookRotation(direction);

        return
            desiredWorldRotation *
            Quaternion.Inverse(
                devicePose.rotation
            );
    }

    private void UpdateTranslationYawForLook(
        Quaternion desiredWorldRotation)
    {
        float desiredYaw =
            HeadingOf(desiredWorldRotation);

        float deviceYaw =
            HeadingOf(devicePose.rotation);

        float yawOffset =
            Mathf.DeltaAngle(
                deviceYaw,
                desiredYaw
            );

        translationYawCalibration =
            Quaternion.Euler(
                0f,
                yawOffset,
                0f
            );
    }

    private void UpdateRotationBlend()
    {
        if (!rotationBlendActive)
            return;

        float elapsed =
            Time.unscaledTime -
            rotationBlendStartTime;

        float t = Mathf.Clamp01(
            elapsed /
            rotationBlendDuration
        );

        // SmoothStep 避免开始和结束突然加速/停止。
        float eased =
            t * t * (3f - 2f * t);

        rotationCalibration =
            Quaternion.Slerp(
                rotationBlendStart,
                rotationBlendTarget,
                eased
            );

        if (t < 1f)
            return;

        rotationCalibration =
            rotationBlendTarget;

        rotationBlendActive = false;
    }

    private void CancelRotationBlend()
    {
        rotationBlendActive = false;
        rotationBlendDuration = 0f;
    }

    private Quaternion SafeLookRotation(
        Vector3 direction)
    {
        Vector3 forward =
            direction.normalized;

        Vector3 up = Vector3.up;

        // 目标接近正上/正下时避免 forward 和 up 共线。
        if (Mathf.Abs(
                Vector3.Dot(forward, up)) >
            0.98f)
        {
            Vector3 candidate =
                cam != null
                    ? cam.forward
                    : Vector3.forward;

            up = Vector3.ProjectOnPlane(
                candidate,
                forward
            );

            if (up.sqrMagnitude < 0.0001f)
            {
                up = Vector3.ProjectOnPlane(
                    Vector3.right,
                    forward
                );
            }

            up.Normalize();
        }

        return Quaternion.LookRotation(
            forward,
            up
        );
    }

    private Vector3 EvaluateCurrentWorldPosition()
    {
        if (devicePose == null)
            return anchorPos;

        Vector3 deltaPosition =
            devicePose.position -
            baseDevicePos;

        return
            anchorPos +
            (translationYawCalibration *
             deltaPosition) *
            moveScale;
    }

    private void ApplyCurrentPoseImmediately()
    {
        if (cam == null ||
            devicePose == null)
        {
            return;
        }

        Quaternion worldRotation =
            rotationCalibration *
            devicePose.rotation;

        Vector3 worldPosition =
            EvaluateCurrentWorldPosition();

        cam.SetPositionAndRotation(
            worldPosition,
            worldRotation
        );
    }

    private void TryRestorePlayerControllerAfterLock()
    {
        if (!pendingPlayerControllerRestore ||
            playerController == null ||
            GameplayInputLockService.IsBlocked)
        {
            return;
        }

        if (playerControllerWasEnabled)
            playerController.enabled = true;

        pendingPlayerControllerRestore = false;
    }

    private static float HeadingOf(
        Quaternion rotation)
    {
        Vector3 right =
            rotation * Vector3.right;

        right.y = 0f;

        if (right.sqrMagnitude >= 0.000001f)
        {
            return Mathf.Atan2(
                -right.z,
                right.x
            ) * Mathf.Rad2Deg;
        }

        Vector3 forward =
            rotation * Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude >= 0.000001f)
        {
            return Mathf.Atan2(
                forward.x,
                forward.z
            ) * Mathf.Rad2Deg;
        }

        return 0f;
    }
}