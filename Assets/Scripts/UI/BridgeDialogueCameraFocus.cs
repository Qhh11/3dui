using Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BridgeDialogueCameraFocus :
    MonoBehaviour
{
    private enum FocusState
    {
        Idle,
        Focused,
        Returning
    }

    [Header("Camera")]
    [SerializeField]
    private CinemachineVirtualCamera focusCamera;

    [Tooltip(
        "Optional. If empty, the CinemachineBrain " +
        "on Camera.main is found at runtime."
    )]
    [SerializeField]
    private CinemachineBrain brain;

    [SerializeField]
    private int activePriority = 30;

    [SerializeField]
    private int inactivePriority = 0;

    [Tooltip(
        "Used if no CinemachineBrain can be found."
    )]
    [SerializeField]
    private float fallbackReturnLockDuration = 2f;

    [Header("Optional UI Lock")]
    [Tooltip(
        "Optional. If empty, the CanvasGroup on " +
        "SwitchPerspectiveButtonController is found at runtime."
    )]
    [SerializeField]
    private CanvasGroup switchUiCanvasGroup;

    private FocusState state = FocusState.Idle;

    private bool previousSwitchInteractable;
    private bool previousSwitchBlocksRaycasts;
    private bool switchStateCaptured;

    private bool waitingForForwardBlend;
    private int forwardBlendStartedFrame = -1;
    private float forwardBlendStartedTime;

    private int returnStartedFrame = -1;
    private float returnStartedTime;

    private DialogueRunner advanceLockedRunner;
    private int advanceWaitToken;

    private GameplayInputLockService.LockHandle gameplayLockHandle;

    private int previousFocusPriority;
    private bool focusPriorityCaptured;

    private void Awake()
    {
        ResolveSceneReferences();

        if (focusCamera != null)
        {
            focusCamera.Priority =
                inactivePriority;
        }
    }

    private void Update()
    {
        if (state == FocusState.Idle)
        {
            return;
        }

        EnforceSwitchLock();

        if (state == FocusState.Focused)
        {
            UpdateForwardBlendWait();

            if (DialogueRunner.Instance != null &&
                !DialogueRunner.Instance
                    .IsDialogueOpen)
            {
                EndFocus();
            }

            return;
        }

        if (state != FocusState.Returning)
        {
            return;
        }

        UpdateReturnBlend();
    }

    private void OnDisable()
    {
        CancelFocusImmediate();
    }

    public void BeginFocus()
    {
        ResolveSceneReferences();

        if (focusCamera == null)
        {
            Debug.LogWarning(
                "Bridge focus camera " +
                "is not assigned.",
                this
            );

            return;
        }

        if (state == FocusState.Focused)
        {
            return;
        }

        if (state == FocusState.Idle)
        {
            CaptureSwitchState();
            AcquireGameplayLock();
        }

        AcquireDialogueAdvanceLock();

        state = FocusState.Focused;

        waitingForForwardBlend = true;

        forwardBlendStartedFrame =
            Time.frameCount;

        forwardBlendStartedTime =
            Time.unscaledTime;

        EnforceSwitchLock();

        CaptureFocusCameraPriority();

        focusCamera.Priority =
            activePriority;
    }

    public void EndFocus()
    {
        ReleaseDialogueAdvanceLock();

        if (state == FocusState.Idle)
        {
            RestoreFocusCameraPriority();

            return;
        }

        ResolveSceneReferences();

        waitingForForwardBlend = false;
        forwardBlendStartedFrame = -1;

        RestoreFocusCameraPriority();

        state = FocusState.Returning;

        returnStartedFrame =
            Time.frameCount;

        returnStartedTime =
            Time.unscaledTime;

        EnforceSwitchLock();
    }

    private void UpdateForwardBlendWait()
    {
        if (!waitingForForwardBlend)
        {
            return;
        }

        if (Time.frameCount <=
            forwardBlendStartedFrame)
        {
            return;
        }

        if (brain != null)
        {
            float elapsed =
                Time.unscaledTime -
                forwardBlendStartedTime;

            if (brain.IsBlending &&
                elapsed < fallbackReturnLockDuration)
            {
                return;
            }
        }
        else
        {
            float elapsed =
                Time.unscaledTime -
                forwardBlendStartedTime;

            if (elapsed <
                fallbackReturnLockDuration)
            {
                return;
            }
        }

        waitingForForwardBlend = false;
        forwardBlendStartedFrame = -1;

        ReleaseDialogueAdvanceLock();
    }

    private void UpdateReturnBlend()
    {
        if (Time.frameCount <=
            returnStartedFrame)
        {
            return;
        }

        if (brain != null)
        {
            float elapsed =
                Time.unscaledTime -
                returnStartedTime;

            if (brain.IsBlending &&
                elapsed < fallbackReturnLockDuration)
            {
                return;
            }
        }
        else
        {
            float elapsed =
                Time.unscaledTime -
                returnStartedTime;

            if (elapsed <
                fallbackReturnLockDuration)
            {
                return;
            }
        }

        CompleteReturn();
    }

    private void CompleteReturn()
    {
        state = FocusState.Idle;

        waitingForForwardBlend = false;
        forwardBlendStartedFrame = -1;
        returnStartedFrame = -1;

        ReleaseDialogueAdvanceLock();
        RestoreSwitchState();
        ReleaseGameplayLock();
    }

    private void CancelFocusImmediate()
    {
        RestoreFocusCameraPriority();

        state = FocusState.Idle;

        waitingForForwardBlend = false;
        forwardBlendStartedFrame = -1;
        returnStartedFrame = -1;

        ReleaseDialogueAdvanceLock();
        RestoreSwitchState();
        ReleaseGameplayLock();
    }

    private void AcquireDialogueAdvanceLock()
    {
        if (advanceLockedRunner != null)
        {
            return;
        }

        advanceLockedRunner =
            DialogueRunner.Instance;

        if (advanceLockedRunner != null)
        {
            advanceWaitToken =
                advanceLockedRunner.WaitForExternalAction(
                    "Bridge camera focus"
                );
        }
    }

    private void ReleaseDialogueAdvanceLock()
    {
        if (advanceLockedRunner == null)
        {
            return;
        }

        advanceLockedRunner
            .ResumeAfterExternalAction(
                advanceWaitToken
            );

        advanceLockedRunner = null;
        advanceWaitToken = 0;
    }

    private void AcquireGameplayLock()
    {
        if (gameplayLockHandle.IsValid)
        {
            return;
        }

        gameplayLockHandle =
            GameplayInputLockService.Acquire(
                "Bridge camera focus"
            );

        DialogueRunner.Instance
            ?.RefreshGameplayAvailability();
    }

    private void ReleaseGameplayLock()
    {
        if (!gameplayLockHandle.IsValid)
        {
            return;
        }

        GameplayInputLockService.Release(
            gameplayLockHandle
        );

        gameplayLockHandle = default;

        DialogueRunner.Instance
            ?.RefreshGameplayAvailability();
    }

    private void ResolveSceneReferences()
    {
        if (brain == null)
        {
            Camera mainCamera =
                Camera.main;

            if (mainCamera != null)
            {
                brain =
                    mainCamera.GetComponent<
                        CinemachineBrain
                    >();
            }
        }

        if (switchUiCanvasGroup != null)
        {
            return;
        }

        SwitchPerspectiveButtonController
            switchController =
                SwitchPerspectiveButtonController
                    .Instance;

        if (switchController == null)
        {
            switchController =
                FindObjectOfType<
                    SwitchPerspectiveButtonController
                >();
        }

        if (switchController != null)
        {
            switchUiCanvasGroup =
                switchController.GetComponent<
                    CanvasGroup
                >();
        }
    }

    private void CaptureSwitchState()
    {
        ResolveSceneReferences();

        if (switchUiCanvasGroup == null ||
            switchStateCaptured)
        {
            return;
        }

        previousSwitchInteractable =
            switchUiCanvasGroup.interactable;

        previousSwitchBlocksRaycasts =
            switchUiCanvasGroup.blocksRaycasts;

        switchStateCaptured = true;
    }

    private void EnforceSwitchLock()
    {
        if (switchUiCanvasGroup == null)
        {
            return;
        }

        switchUiCanvasGroup.interactable =
            false;

        switchUiCanvasGroup.blocksRaycasts =
            true;
    }

    private void RestoreSwitchState()
    {
        if (switchUiCanvasGroup == null ||
            !switchStateCaptured)
        {
            return;
        }

        switchUiCanvasGroup.interactable =
            previousSwitchInteractable;

        switchUiCanvasGroup.blocksRaycasts =
            previousSwitchBlocksRaycasts;

        switchStateCaptured = false;
    }

    private void CaptureFocusCameraPriority()
    {
        if (focusCamera == null ||
            focusPriorityCaptured)
        {
            return;
        }

        previousFocusPriority =
            focusCamera.Priority;

        focusPriorityCaptured = true;
    }

    private void RestoreFocusCameraPriority()
    {
        if (focusCamera == null)
        {
            return;
        }

        focusCamera.Priority =
            focusPriorityCaptured
                ? previousFocusPriority
                : inactivePriority;

        focusPriorityCaptured = false;
    }
}
