using UnityEngine;
 
/// <summary>
/// 第三人称陀螺仪轨道相机：相机围绕 pivot 公转并始终看向它。
/// 仰角由手机倾斜决定，手机竖直时为 restAngle(默认 45°)，夹在 minAngle / maxAngle 之间；
/// 水平方向随手机左右转动。
///
/// 关键设计(与第一人称一致)：朝向用设备「右轴」的水平投影算，和俯仰解耦；
/// 仰角用 forward 绕右轴的有符号连续夹角 —— 前倾过头(超过 90°)时继续增大、被 clamp 摁在
/// 上/下限上，不会折返、不会翻到对侧。
///
/// Cinemachine：vcam 的 Body 和 Aim 都设 Do Nothing，脚本挂到 vcam 上。
/// 依赖 GyroAttitude.cs(按屏幕方向自适应的姿态读取)。
/// </summary>
public class GyroOrbitController : MonoBehaviour
{
    [Header("轨道")]
    [Tooltip("围绕公转的中心点/物体")]
    public Transform pivot;
    [Tooltip("相机到中心的距离；设为 0 则自动取启动时的当前距离")]
    public float radius = 0f;
    [Tooltip("要移动的相机；留空则用脚本所在物体自身")]
    public Transform cam;
 
    [Header("仰角(度)")]
    [Tooltip("手机竖直时的相机仰角")]
    public float restAngle = 45f;
    [Tooltip("最低仰角")]
    public float minAngle = 10f;
    [Tooltip("最高仰角(留点余量，别到 90)")]
    public float maxAngle = 85f;
    [Tooltip("手机倾斜对仰角的灵敏度")]
    public float pitchSensitivity = 1f;
    [Tooltip("仰角方向反了就勾上")]
    public bool invertPitch = false;
 
    [Header("其它")]
    [Range(1f, 30f)]
    public float smooth = 12f;
    public bool recenterOnStart = true;
 
    private float baseYaw;
    private float recenterYaw;
    private float lastPitch;   // 上一次有效的俯仰(手机侧倒时沿用)
    private float lastYaw;     // 上一次有效的水平朝向
    private bool gyroAvailable;
 
    void Start()
    {
        if (cam == null) cam = transform;
        if (pivot != null && radius <= 0f)
            radius = Vector3.Distance(cam.position, pivot.position);
 
        baseYaw = CurrentOrbitYaw();
 
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
        if (!gyroAvailable || pivot == null) return;
 
        SampleDevice(out float devicePitch, out float rawYaw);
 
        // 仰角：手机竖直(devicePitch≈0)时为 restAngle；连续 pitch 过 90° 不折返
        if (invertPitch) devicePitch = -devicePitch;
        float pitch = Mathf.Clamp(restAngle + devicePitch * pitchSensitivity, minAngle, maxAngle);
 
        // 水平：相对 recenter
        float yaw = baseYaw + Mathf.DeltaAngle(recenterYaw, rawYaw);
 
        Quaternion desired = Quaternion.Euler(pitch, yaw, 0f);
        // 相机绕 pivot 公转：位置 = 中心 - 朝向前方 × 半径，始终对着中心
        Vector3 targetPos = pivot.position - (desired * Vector3.forward) * radius;
 
        float dt = Time.deltaTime * smooth;
        cam.rotation = Quaternion.Slerp(cam.rotation, desired, dt);
        cam.position = Vector3.Lerp(cam.position, targetPos, dt);
    }
 
    /// <summary>以当前手机水平朝向为基准(可绑到 UI 按钮)。</summary>
    public void Recenter()
    {
        SampleDevice(out _, out float yawNow);
        recenterYaw = yawNow;
        baseYaw = CurrentOrbitYaw();
    }
 
    /// <summary>当前相机相对 pivot 的水平方位角</summary>
    private float CurrentOrbitYaw()
    {
        if (pivot == null) return cam.eulerAngles.y;
        Vector3 dir = pivot.position - cam.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg
            : cam.eulerAngles.y;
    }
 
    /// <summary>
    /// 从手机姿态取俯仰和水平朝向(防翻面)。
    /// 朝向用「右轴」的水平投影 —— 俯仰绕右轴转，右轴不随俯仰变，yaw/pitch 解耦；
    /// 俯仰用 forward 绕右轴的有符号夹角 —— 连续，过 90° 不折返。
    /// </summary>
    private void SampleDevice(out float pitch, out float yaw)
    {
        Quaternion att = GyroAttitude.Read();
        Vector3 fwd = att * Vector3.forward;
        Vector3 right = att * Vector3.right;
 
        Vector3 rightFlat = new Vector3(right.x, 0f, right.z);
        if (rightFlat.sqrMagnitude > 1e-4f)
        {
            rightFlat.Normalize();
            Vector3 lookFlat = Vector3.Cross(rightFlat, Vector3.up); // 对应的水平前方
            lastYaw = Mathf.Atan2(lookFlat.x, lookFlat.z) * Mathf.Rad2Deg;
            lastPitch = Vector3.SignedAngle(lookFlat, fwd, rightFlat);
        }
        // 手机侧倒时右轴竖直、朝向无意义，沿用上一次的值
        pitch = lastPitch;
        yaw = lastYaw;
    }
}