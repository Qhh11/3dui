using UnityEngine;
using Cinemachine;   // Cinemachine 3.x；如果是 2.x 改成 using Cinemachine;
using TMPro;        // TextMeshPro，如果不显示标签可以删掉这一行和 label 字段
/// <summary>
/// 虚拟按钮切换视角：每次点击在两台虚拟相机之间来回切换，
/// 并且只让当前生效视角的陀螺仪脚本运行，停掉另一个视角的转动。
/// 用法：
///   1. 把这个脚本挂到一个空物体(例如 CameraManager)上；
///   2. 拖入两台 vcam，以及各自的控制脚本；
///   3. 把 UI Button 的 OnClick 绑到本脚本的 Toggle()。
/// </summary>
public class ViewToggleButton : MonoBehaviour
{
    [Header("两个视角")]
    [Tooltip("第一人称相机")]
    public CinemachineVirtualCamera firstPersonCam;
    [Tooltip("另一个视角(总览 / 轨道 / 第三人称等)")]
    public CinemachineVirtualCamera secondaryCam;

    [Header("各视角的陀螺仪控制脚本")]
    [Tooltip("第一人称的控制脚本，例如 GyroFpsController")]
    public MonoBehaviour firstPersonController;
    [Tooltip("另一个视角的控制脚本，例如 GyroOrbitController")]
    public MonoBehaviour secondaryController;

    public TMPro.TextMeshProUGUI switchLabel;   // 可选：显示当前视角的文本标签
    [Header("优先级")]
    [Tooltip("生效相机的优先级")]
    public int activePriority = 20;
    [Tooltip("待命相机的优先级")]
    public int standbyPriority = 10;

    [Tooltip("启动时是否默认第一人称")]
    public bool startFirstPerson = true;

    [Tooltip("切回某个视角时，是否以当前手机朝向重新校准(避免恢复时朝向错乱)")]
    public bool recenterOnActivate = true;

    private bool isFirstPerson;

    void Start()
    {
        isFirstPerson = startFirstPerson;
        Apply();
    }

    /// <summary>把这个方法绑到按钮的 OnClick()</summary>
    public void Toggle()
    {
        isFirstPerson = !isFirstPerson;
        Apply();
    }

    private void Apply()
    {
        // 1) 切相机优先级，Brain 自动混合
        firstPersonCam.Priority = isFirstPerson ? activePriority : standbyPriority;
        secondaryCam.Priority   = isFirstPerson ? standbyPriority : activePriority;

        // 2) 只启用当前视角的控制脚本，另一个停掉 -> 它的 Update 不再跑，转动停止
        SetController(firstPersonController, isFirstPerson);
        SetController(secondaryController, !isFirstPerson);
    }

    private void SetController(MonoBehaviour controller, bool active)
    {
        if (controller == null) return;
        controller.enabled = active;
        switchLabel.text = isFirstPerson ? "switch to UFO" : "switch to Player";   // 可选：更新标签显示

        // 重新激活时，可选地按当前手机朝向重新校准，避免恢复时方向是停下时的旧值
        if (active && recenterOnActivate)
            controller.SendMessage("Recenter", SendMessageOptions.DontRequireReceiver);
    }
}