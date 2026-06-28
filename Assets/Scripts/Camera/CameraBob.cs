using UnityEngine;

/// <summary>
/// 第一人称镜头晃动（head/camera bob）。
/// 挂在【第一人称相机】上（GyroFpsController.cam 指向的那个 vcam；若该 vcam 的 Body 不是
/// Do Nothing 而是跟随某个头节点，则改挂到那个头节点上）。
///
/// 原理：陀螺仪只写镜头的 rotation，localPosition 空闲；这里在 localPosition 上叠加一个
/// 随移动速度缩放的正弦偏移：竖直按步频上下，水平按半频左右（走出自然的 8 字轨迹）。
/// 站住时平滑归位、相位复位，起步不跳。
/// </summary>
public class CameraBob : MonoBehaviour
{
    [Header("速度来源（优先用 CharacterController 的真实速度）")]
    [Tooltip("角色的 CharacterController；填了就用真实水平速度（撞墙不动时不晃）")]
    public CharacterController controller;
    [Tooltip("没有 controller 时，用输入摇杆强度近似速度")]
    public bool useInputIfNoController = true;
    [Tooltip("达到该水平速度时晃到满幅（建议=你的 playerSpeed，目前 30）")]
    public float referenceSpeed = 30f;

    [Header("幅度 / 频率")]
    [Tooltip("上下起伏幅度（世界单位/米）")]
    public float verticalAmount = 0.08f;
    [Tooltip("左右摆动幅度（世界单位/米）")]
    public float horizontalAmount = 0.05f;
    [Tooltip("步频基数（越大晃得越快）")]
    public float frequency = 12f;

    [Header("平滑")]
    [Tooltip("进入/退出晃动的平滑速度（越大越跟手）")]
    public float smooth = 10f;

    private Vector3 restLocalPos;
    private float bobTimer;
    private Vector3 currentOffset;

    void OnEnable()
    {
        // 以当前 localPosition 为静止基准（避免被上次的偏移污染）
        restLocalPos = transform.localPosition - currentOffset;
    }

    void Start()
    {
        restLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        float speed01 = GetSpeed01();

        Vector3 target = Vector3.zero;
        if (speed01 > 0.01f)
        {
            bobTimer += Time.deltaTime * frequency * Mathf.Max(speed01, 0.3f);
            float v = Mathf.Sin(bobTimer) * verticalAmount * speed01;            // 上下：全频
            float h = Mathf.Sin(bobTimer * 0.5f) * horizontalAmount * speed01;   // 左右：半频
            target = new Vector3(h, v, 0f);
        }
        else
        {
            bobTimer = 0f; // 站住复位相位，重新起步从 0 开始不跳
        }

        // 帧率无关平滑
        float t = 1f - Mathf.Exp(-smooth * Time.deltaTime);
        currentOffset = Vector3.Lerp(currentOffset, target, t);
        transform.localPosition = restLocalPos + currentOffset;
    }

    private float GetSpeed01()
    {
        if (controller != null)
        {
            Vector3 v = controller.velocity; v.y = 0f;
            return Mathf.Clamp01(v.magnitude / Mathf.Max(0.01f, referenceSpeed));
        }
        if (useInputIfNoController)
        {
            var im = InputManager.Instance;
            if (im != null) return Mathf.Clamp01(im.GetLeftStick().magnitude);
        }
        return 0f;
    }
}
