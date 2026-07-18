using Cinemachine;
using TMPro;
using UnityEngine;

public class ViewToggleButton : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera firstPersonCam;
    public CinemachineVirtualCamera secondaryCam;

    [Header("Camera Controllers")]
    public MonoBehaviour firstPersonController;
    public MonoBehaviour secondaryController;

    [Header("View Object Roots")]
    public GameObject[] playerViewObjects;
    public GameObject[] ufoViewObjects;

    [Header("UI")]
    public TextMeshProUGUI switchLabel;

    [Header("Priorities")]
    public int activePriority = 20;
    public int standbyPriority = 10;

    [Header("Start State")]
    public bool startFirstPerson = true;

    [Header("Recenter")]
    public bool recenterOnActivate = true;

    [Header("Optional")]
    public TouchDragger touchDragger;
    public UfoMovement ufoMovement;

    private bool isFirstPerson;

    private void Start()
    {
        isFirstPerson = startFirstPerson;
        Apply();
    }

    public void Toggle()
    {
        isFirstPerson = !isFirstPerson;

        Debug.Log(
            "Switch perspective clicked. " +
            "Is first person: " +
            isFirstPerson
        );

        Apply();
    }

    public void SwitchToPlayer()
    {
        isFirstPerson = true;

        Debug.Log(
            "Switch to Player perspective."
        );

        Apply();
    }

    public void SwitchToUfo()
    {
        isFirstPerson = false;

        Debug.Log(
            "Switch to UFO perspective."
        );

        Apply();
    }

    public bool IsPlayerView()
    {
        return isFirstPerson;
    }

    public bool IsUfoView()
    {
        return !isFirstPerson;
    }

    /// <summary>
    /// 直接切入已经对准目标的 UFO Camera。
    /// 完整 UI/玩法同步仍应经过
    /// SwitchPerspectiveButtonController。
    /// </summary>
    public void SwitchToUfoLookingAt(
        Vector3 targetPosition)
    {
        isFirstPerson = false;

        ApplyCameraControllers();
        ApplyViewObjects();

        if (ufoMovement != null)
        {
            ufoMovement.EnterUfoLookingAt(
                targetPosition
            );
        }
        else
        {
            Debug.LogWarning(
                "[UFO] ufoMovement 未赋值。",
                this
            );
        }

        ApplyCameraPriority();
        UpdateSwitchLabel();
    }

    /// <summary>
    /// 保留旧接口：立即对准。
    /// </summary>
    public void RecenterUfoLookingAt(
        Vector3 targetPosition)
    {
        RecenterUfoLookingAt(
            targetPosition,
            0f
        );
    }

    /// <summary>
    /// 已在 UFO 时可指定平滑转向时间。
    /// 如果当前仍是 User View，则直接预对准 UFO Camera，
    /// 随后由 Cinemachine 完成视角 Blend。
    /// </summary>
    public void RecenterUfoLookingAt(
        Vector3 targetPosition,
        float blendDuration)
    {
        if (isFirstPerson)
        {
            SwitchToUfoLookingAt(
                targetPosition
            );

            return;
        }

        if (ufoMovement != null)
        {
            ufoMovement.RecenterLookingAt(
                targetPosition,
                blendDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "[UFO] ufoMovement 未赋值。",
                this
            );
        }
    }

    private void Apply()
    {
        ApplyCameraControllers();
        ApplyViewObjects();
        ApplyUfoMode();
        ApplyCameraPriority();
        UpdateSwitchLabel();
    }

    private void ApplyCameraPriority()
    {
        if (firstPersonCam != null)
        {
            firstPersonCam.Priority =
                isFirstPerson
                    ? activePriority
                    : standbyPriority;
        }

        if (secondaryCam != null)
        {
            secondaryCam.Priority =
                isFirstPerson
                    ? standbyPriority
                    : activePriority;
        }
    }

    private void ApplyCameraControllers()
    {
        SetController(
            firstPersonController,
            isFirstPerson
        );

        SetController(
            secondaryController,
            !isFirstPerson
        );

        if (touchDragger != null)
            touchDragger.enabled = isFirstPerson;
    }

    private void ApplyUfoMode()
    {
        if (ufoMovement == null)
        {
            Debug.LogWarning(
                "[UFO] ufoMovement 槽为空。",
                this
            );

            return;
        }

        if (isFirstPerson)
            ufoMovement.ExitUfo();
        else
            ufoMovement.EnterUfo();
    }

    private void ApplyViewObjects()
    {
        SetObjectsActive(
            playerViewObjects,
            isFirstPerson
        );

        SetObjectsActive(
            ufoViewObjects,
            !isFirstPerson
        );
    }

    private static void SetObjectsActive(
        GameObject[] objects,
        bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject target in objects)
        {
            if (target != null)
                target.SetActive(active);
        }
    }

    private void SetController(
        MonoBehaviour controller,
        bool controllerActive)
    {
        if (controller == null)
            return;

        controller.enabled = controllerActive;

        if (controllerActive &&
            recenterOnActivate)
        {
            controller.SendMessage(
                "Recenter",
                SendMessageOptions
                    .DontRequireReceiver
            );
        }
    }

    private void UpdateSwitchLabel()
    {
        if (switchLabel == null)
            return;

        switchLabel.text =
            isFirstPerson
                ? "Switch to UFO"
                : "Switch to Player";
    }
}