using UnityEngine;
 
/// <summary>
/// 第一人称陀螺仪视角：用手机陀螺仪控制人物的朝向(yaw)和视角(pitch)。
/// 手机朝上 levelDeviceAngle(默认 45°)时平视，俯仰有上下限。
///
/// 关键设计：朝向用设备「右轴」的水平投影来算，而不是 forward。
/// 俯仰是绕右轴转的，所以右轴不随俯仰变化 —— yaw 与 pitch 解耦，
/// 手机俯仰超过 90° 也不会被当成翻面(不翻 180°、不折返)；
/// 俯仰用有符号的连续角度，过 90° 继续增大，clamp 后稳稳停在上限。
///
/// 依赖 GyroAttitude.cs(按屏幕方向自适应的姿态读取)。
/// </summary>
public class GyroFpsController : MonoBehaviour
{
    [Header("对象")]
    [Tooltip("人物身体：绕 Y 轴转，控制朝向")]
    public Transform body;
    [Tooltip("眼睛/相机：绕 X 轴转，控制俯仰。留空则用脚本自身")]
    public Transform cam;
 
    [Header("视角(俯仰)")]
    [Tooltip("手机朝上多少度时人物平视")]
    public float levelDeviceAngle = -50f;
    [Tooltip("最高视角：最多往上看多少度")]
    public float maxLookUp = 80f;
    [Tooltip("最低视角：最多往下看多少度")]
    public float maxLookDown = 50f;
    [Tooltip("俯仰灵敏度")]
    public float pitchSensitivity = 1f;
    [Tooltip("抬头/低头方向反了就勾上")]
    public bool invertPitch = false;
 
    [Header("朝向(偏航)")]
    [Tooltip("左右转向反了就勾上")]
    public bool invertYaw = false;
 
    [Header("其它")]
    [Range(1f, 30f)]
    public float smooth = 12f;
    public bool recenterOnStart = true;
 
    private float baseYaw;
    private float recenterYaw;
    private float lastYaw;     // 上一次有效的水平朝向(手机侧倒时沿用)
    private float lastPitch;   // 上一次有效的俯仰
    private bool gyroAvailable;
 
    void Start()
    {
        if (cam == null) cam = transform;
        baseYaw = (body != null ? body.eulerAngles.y : cam.eulerAngles.y);
 
        gyroAvailable = SystemInfo.supportsGyroscope;
        if (gyroAvailable)
        {
            Input.gyro.enabled = true;
            SampleDevice(out lastPitch, out lastYaw); // 初始化，避免开头跳变
            if (recenterOnStart) Invoke(nameof(Recenter), 0.5f);
        }
        else
        {
            Debug.LogWarning("本设备没有陀螺仪。");
        }
    }
 
    void Update()
    {
        if (!gyroAvailable) return;
 
        SampleDevice(out float devicePitch, out float rawYaw);
 
        // 俯仰：手机朝上 levelDeviceAngle 时平视；devicePitch 是连续值，过 90° 也不折返
        if (invertPitch) devicePitch = -devicePitch;
        float lookPitch = (devicePitch - levelDeviceAngle) * pitchSensitivity;
        lookPitch = Mathf.Clamp(lookPitch, -maxLookDown, maxLookUp);
        float unityPitch = -lookPitch;  // Unity 欧拉 X：正 = 朝下
 
        // 朝向：相对 recenter
        float yawDelta = Mathf.DeltaAngle(recenterYaw, rawYaw);
        if (invertYaw) yawDelta = -yawDelta;
        float yaw = baseYaw + yawDelta;
 
        float dt = Time.deltaTime * smooth;
        if (body != null)
        {
            body.rotation = Quaternion.Slerp(body.rotation,
                Quaternion.Euler(0f, yaw, 0f), dt);
            cam.localRotation = Quaternion.Slerp(cam.localRotation,
                Quaternion.Euler(unityPitch, 0f, 0f), dt);
        }
        else
        {
            cam.rotation = Quaternion.Slerp(cam.rotation,
                Quaternion.Euler(unityPitch, yaw, 0f), dt);
        }
    }
 
    /// <summary>
    /// 从手机姿态取俯仰和水平朝向。
    /// 朝向用「右轴」的水平投影 —— 俯仰绕右轴转，所以右轴不随俯仰变，yaw/pitch 解耦；
    /// 俯仰用 forward 相对水平前方、绕右轴的有符号夹角 —— 连续，过 90° 不折返。
    /// </summary>
    private void SampleDevice(out float pitch, out float yaw)
    {
        Quaternion att = GyroAttitude.Read();
        Vector3 fwd = att * Vector3.forward;
        Vector3 right = att * Vector3.right;
 
        // 把右轴压到水平面。只有手机侧倒(右轴接近竖直)时它才会变得很小。
        Vector3 rightFlat = new Vector3(right.x, 0f, right.z);
        if (rightFlat.sqrMagnitude > 1e-4f)
        {
            rightFlat.Normalize();
            // 与右轴对应的水平前方：forward = Cross(right, up)
            Vector3 lookFlat = Vector3.Cross(rightFlat, Vector3.up);
            lastYaw = Mathf.Atan2(lookFlat.x, lookFlat.z) * Mathf.Rad2Deg;
            // 有符号俯仰：fwd 相对 lookFlat、绕右轴的夹角，连续不折返
            lastPitch = Vector3.SignedAngle(lookFlat, fwd, rightFlat);
        }
        // 手机侧倒时右轴竖直、朝向无意义，沿用上一次的值
        pitch = lastPitch;
        yaw = lastYaw;
    }
 
    /// <summary>校准水平朝向。建议手机举到舒适握姿(朝上约 45°)时调用。</summary>
    public void Recenter()
    {
        SampleDevice(out _, out float yawNow);
        recenterYaw = yawNow;
        baseYaw = (body != null ? body.eulerAngles.y : cam.eulerAngles.y);
    }
}
 