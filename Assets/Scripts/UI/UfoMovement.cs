using UnityEngine;

/// <summary>
/// AR 驱动的 UFO 视角（整体姿态跟随版）：
///   - 朝向：直接跟随手机整体姿态（四元数），只把朝向【水平偏移】到人物方向；
///     => 全方向平滑连续，无万向节、无对折、无跳变；俯仰是绝对的（手机朝下=UFO朝下）。
///   - 位置：前后左右 + 高低，按同一水平偏移对齐，Y 保持竖直。
///
/// 注意：这种"真窗户"式跟随会把手机的【滚转】也带进来（手机左右歪，画面也歪）。
/// 正常握持时滚转很小。如需强制地平线水平，会牺牲正上/正下处的平滑（有奇点），见说明。
/// </summary>
public class UfoMovement : MonoBehaviour
{
    [Header("引用")]
    public Transform cam;
    public Transform devicePose;
    public Transform head;
    public PlayerController playerController;

    [Header("锚点（头顶固定位置）")]
    public Vector3 headOffset = new Vector3(0f, 3f, 0f);

    [Header("位移缩放")]
    public float moveScale = 10f;

    [Header("调试")]
    public bool debugLog = false;

    private Vector3 baseDevicePos;
    private Vector3 anchorPos;
    private float yawOffset;     // 把设备朝向水平偏到锚点朝向的角度
    private bool active;
    private float logTimer;

    void LateUpdate()
    {
        if (!active || cam == null || devicePose == null) return;

        Quaternion yawQ = Quaternion.Euler(0f, yawOffset, 0f);

        // 朝向：整体跟手机姿态，只水平偏到锚点 —— 全方向连续平滑
        Quaternion worldRot = yawQ * devicePose.rotation;

        // 位置：设备平移用同一水平偏移对齐（绕 Y 旋转不改变 Y，高低保持竖直）
        Vector3 deltaPos = devicePose.position - baseDevicePos;
        Vector3 worldPos = anchorPos + (yawQ * deltaPos) * moveScale;

        cam.SetPositionAndRotation(worldPos, worldRot);

        if (debugLog)
        {
            logTimer += Time.deltaTime;
            if (logTimer >= 0.3f)
            {
                logTimer = 0f;
                Debug.Log($"[UFO] euler={worldRot.eulerAngles:F1} camPos={cam.position:F1}");
            }
        }
    }

    public void EnterUfo()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam == null || devicePose == null)
        {
            Debug.LogWarning("[UFO] cam 或 devicePose 没接，AR UFO 无法工作。");
            return;
        }

        if (playerController != null) playerController.enabled = false;

        Transform h = head != null ? head : (Camera.main != null ? Camera.main.transform : cam);
        anchorPos = h.position + headOffset;
        float anchorYaw = h.eulerAngles.y;

        baseDevicePos = devicePose.position;
        yawOffset = Mathf.DeltaAngle(HeadingOf(devicePose.rotation), anchorYaw);

        active = true;
        cam.SetPositionAndRotation(anchorPos, Quaternion.Euler(0f, yawOffset, 0f) * devicePose.rotation);
    }

    public void ExitUfo()
    {
        active = false;
        if (playerController != null) playerController.enabled = true;
    }

    /// <summary>设备水平朝向（度）。优先用右轴（与俯仰无关、垂直时也稳），右轴近竖直时退用前向投影。</summary>
    private static float HeadingOf(Quaternion q)
    {
        Vector3 r = q * Vector3.right; r.y = 0f;
        if (r.sqrMagnitude >= 1e-6f) return Mathf.Atan2(-r.z, r.x) * Mathf.Rad2Deg;
        Vector3 f = q * Vector3.forward; f.y = 0f;
        if (f.sqrMagnitude >= 1e-6f) return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        return 0f;
    }
}
