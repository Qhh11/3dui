using UnityEngine;

public class CinemachineGyroMouseLookController : MonoBehaviour
{
    [Header("Gyroscope")]
    [SerializeField] private bool useGyroOnDevice = true;
    [SerializeField] private bool useGyroInEditor = false;

    [Header("Editor Mouse Fallback")]
    [SerializeField] private bool useMouseInEditor = true;
    [SerializeField] private bool requireRightMouseButton = true;
    [SerializeField] private float mouseSensitivity = 2.0f;

    [Header("Rotation Limits")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Initial Rotation")]
    [SerializeField] private Vector3 baseEulerOffset = Vector3.zero;

    private bool gyroAvailable;
    private float yaw;
    private float pitch;

    private void Start()
    {
        gyroAvailable = SystemInfo.supportsGyroscope;

        if (gyroAvailable && useGyroOnDevice)
        {
            Input.gyro.enabled = true;
        }

        Vector3 startEuler = transform.localEulerAngles;
        yaw = startEuler.y;
        pitch = NormalizeAngle(startEuler.x);
    }

    private void Update()
    {
        if (ShouldUseMouseFallback())
        {
            UpdateMouseRotation();
            return;
        }

        if (ShouldUseGyro())
        {
            UpdateGyroRotation();
        }
    }

    private bool ShouldUseMouseFallback()
    {
#if UNITY_EDITOR
        return useMouseInEditor;
#else
        return false;
#endif
    }

    private bool ShouldUseGyro()
    {
        if (!useGyroOnDevice)
        {
            return false;
        }

        if (!gyroAvailable)
        {
            return false;
        }

#if UNITY_EDITOR
        return useGyroInEditor;
#else
        return true;
#endif
    }

    private void UpdateMouseRotation()
    {
        if (requireRightMouseButton && !Input.GetMouseButton(1))
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void UpdateGyroRotation()
    {
        Quaternion deviceRotation = Input.gyro.attitude;

        Quaternion unityRotation = new Quaternion(
            deviceRotation.x,
            deviceRotation.y,
            -deviceRotation.z,
            -deviceRotation.w
        );

        Quaternion correction = Quaternion.Euler(90f, 0f, 0f);
        Quaternion offset = Quaternion.Euler(baseEulerOffset);

        transform.localRotation = offset * correction * unityRotation;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}