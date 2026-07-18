using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Objective Hint Button 总控。
///
/// User View：
/// CameraBlockCheck
/// -> 现有视角总控切到 UFO
/// -> 同帧立即预对准 UFO Camera
/// -> Cinemachine 负责平滑 Blend。
///
/// UFO View：
/// 位置不变，短时间平滑转向目标。
/// </summary>
public class ObjectiveHintButtonController :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Button hintButton;

    [Tooltip(
        "建议拖 HintButton 同一 GameObject 上的专用 CanvasGroup。" +
        "不要拖共享的 ObjectivePanel/InGameUI CanvasGroup。"
    )]
    [SerializeField]
    private CanvasGroup availabilityCanvasGroup;

    [Header("Objective")]
    [SerializeField]
    private ObjectivePanelController objectivePanel;

    [Header("Perspective")]
    [SerializeField]
    private SwitchPerspectiveButtonController
        switchPerspectiveController;

    [SerializeField]
    private ViewToggleButton viewToggleButton;

    [SerializeField]
    private SwitchViewCameraGuard cameraGuard;

    [Header("Interaction Guards")]
    [SerializeField]
    private SiphonController siphonController;

    [SerializeField]
    private MarkController markController;

    [SerializeField]
    private RocketLauncher rocketLauncher;

    [Header("Hint Timing")]
    [SerializeField, Range(3f, 5f)]
    private float hintDuration = 5f;

    [Tooltip(
        "已经处于 UFO View 时的平滑转向时间。"
    )]
    [SerializeField, Range(0f, 1f)]
    private float ufoRecenterDuration = 0.4f;

    [Header("Disabled Visual")]
    [Tooltip(
        "一般关闭，由 Button Disabled Color 控制半透明。"
    )]
    [SerializeField]
    private bool driveCanvasGroupAlpha = false;

    [SerializeField, Range(0f, 1f)]
    private float disabledAlpha = 0.5f;

    private ObjectivePanelController boundPanel;
    private ObjectiveHintTarget activeTarget;
    private Coroutine hintRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindObjectivePanel();
        RefreshAvailability();
    }

    private void Update()
    {
        ResolveReferences();
        BindObjectivePanel();

        if (activeTarget != null &&
            objectivePanel != null &&
            objectivePanel.CurrentHintTarget !=
                activeTarget)
        {
            StopActiveHint();
        }

        RefreshAvailability();
    }

    private void OnDisable()
    {
        UnbindObjectivePanel();
        StopActiveHint();
        ClearSelectedUiObject();
    }

    /// <summary>
    /// 只在 Button OnClick 中绑定一次。
    /// </summary>
    public void OnHintButtonClicked()
    {
        ClearSelectedUiObject();

        if (!CanUseHint())
        {
            RefreshAvailability();
            return;
        }

        ObjectiveHintTarget target =
            objectivePanel.CurrentHintTarget;

        if (target == null ||
            !target.TryGetFocusPosition(
                out Vector3 focusPosition))
        {
            RefreshAvailability();
            return;
        }

        bool enteringFromPlayerView =
            viewToggleButton.IsPlayerView();

        if (enteringFromPlayerView &&
            cameraGuard != null &&
            !cameraGuard.TryAllowEnteringUfo())
        {
            RefreshAvailability();
            return;
        }

        if (enteringFromPlayerView)
        {
            // 必须经过现有总控，保证：
            // Cinemachine、Player/UFO UI、Siphon 状态同步。
            switchPerspectiveController
                .ForceSwitchToUfoForCutscene();

            if (!viewToggleButton.IsUfoView())
            {
                Debug.LogWarning(
                    "[ObjectiveHint] 无法切换到 UFO View。",
                    this
                );

                RefreshAvailability();
                return;
            }

            // User -> UFO：
            // 同帧立即预对准目标 Camera，
            // 平滑由 Cinemachine Brain 负责。
            viewToggleButton
                .RecenterUfoLookingAt(
                    focusPosition,
                    0f
                );
        }
        else
        {
            // 已在 UFO：
            // 不移动位置，只平滑旋转。
            viewToggleButton
                .RecenterUfoLookingAt(
                    focusPosition,
                    ufoRecenterDuration
                );
        }

        StartHint(target);
        RefreshAvailability();
    }

    private bool CanUseHint()
    {
        if (!IsUiHierarchyAvailable())
            return false;

        if (objectivePanel == null ||
            objectivePanel.IsTransitioning ||
            !objectivePanel.HasObjective)
        {
            return false;
        }

        ObjectiveHintTarget target =
            objectivePanel.CurrentHintTarget;

        if (target == null ||
            !target.IsAvailable ||
            !target.TryGetFocusPosition(out _))
        {
            return false;
        }

        if (switchPerspectiveController == null ||
            viewToggleButton == null)
        {
            return false;
        }

        if (GameplayInputLockService.IsBlocked)
            return false;

        if (siphonController != null &&
            !siphonController.CanSwitchPerspective)
        {
            return false;
        }

        if (markController != null &&
            (markController.IsBusy ||
             markController.ActedThisFrame))
        {
            return false;
        }

        if (rocketLauncher != null &&
            rocketLauncher.FiredThisFrame)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 防止父 CanvasGroup 已隐藏/禁用，
    /// 但 Button 自身仍显示为可用颜色。
    ///
    /// availabilityCanvasGroup 是本脚本自己控制的，
    /// 检查时跳过，避免 false 后永远无法恢复。
    /// </summary>
    private bool IsUiHierarchyAvailable()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            CanvasGroup[] groups =
                current.GetComponents<CanvasGroup>();

            bool stopAtThisLevel = false;

            foreach (CanvasGroup group in groups)
            {
                if (group == null ||
                    group == availabilityCanvasGroup)
                {
                    continue;
                }

                if (group.alpha <= 0.001f ||
                    !group.interactable ||
                    !group.blocksRaycasts)
                {
                    return false;
                }

                if (group.ignoreParentGroups)
                    stopAtThisLevel = true;
            }

            if (stopAtThisLevel)
                break;

            current = current.parent;
        }

        return true;
    }

    private void StartHint(
        ObjectiveHintTarget target)
    {
        StopActiveHint();

        activeTarget = target;
        activeTarget.SetHintActive(true);

        hintRoutine = StartCoroutine(
            HintDurationRoutine(target)
        );
    }

    private IEnumerator HintDurationRoutine(
        ObjectiveHintTarget target)
    {
        yield return new WaitForSecondsRealtime(
            hintDuration
        );

        if (activeTarget == target)
        {
            target.SetHintActive(false);
            activeTarget = null;
        }

        hintRoutine = null;
        RefreshAvailability();
    }

    private void StopActiveHint()
    {
        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
        }

        if (activeTarget != null)
        {
            activeTarget.SetHintActive(false);
            activeTarget = null;
        }
    }

    private void RefreshAvailability()
    {
        bool interactable = CanUseHint();

        if (hintButton != null)
            hintButton.interactable = interactable;

        if (availabilityCanvasGroup != null)
        {
            availabilityCanvasGroup.interactable =
                interactable;

            availabilityCanvasGroup.blocksRaycasts =
                interactable;

            if (driveCanvasGroupAlpha)
            {
                availabilityCanvasGroup.alpha =
                    interactable
                        ? 1f
                        : disabledAlpha;
            }
        }
    }

    private void HandleObjectiveChanged(
        string objective,
        ObjectiveHintTarget target)
    {
        StopActiveHint();
        RefreshAvailability();
    }

    private void ResolveReferences()
    {
        if (hintButton == null)
            hintButton = GetComponent<Button>();

        if (objectivePanel == null)
        {
            objectivePanel =
                ObjectivePanelController.Instance;
        }

        if (switchPerspectiveController == null)
        {
            switchPerspectiveController =
                SwitchPerspectiveButtonController
                    .Instance;
        }

        if (viewToggleButton == null)
        {
            viewToggleButton =
                FindObjectOfType<ViewToggleButton>();
        }

        if (siphonController == null)
        {
            siphonController =
                FindObjectOfType<SiphonController>();
        }

        if (markController == null)
        {
            markController =
                FindObjectOfType<MarkController>();
        }

        if (rocketLauncher == null)
        {
            rocketLauncher =
                FindObjectOfType<RocketLauncher>();
        }
    }

    private void BindObjectivePanel()
    {
        if (boundPanel == objectivePanel)
            return;

        UnbindObjectivePanel();

        boundPanel = objectivePanel;

        if (boundPanel != null)
        {
            boundPanel.ObjectiveChanged +=
                HandleObjectiveChanged;
        }
    }

    private void UnbindObjectivePanel()
    {
        if (boundPanel != null)
        {
            boundPanel.ObjectiveChanged -=
                HandleObjectiveChanged;
        }

        boundPanel = null;
    }

    private static void ClearSelectedUiObject()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }
    }
}