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
    private DialogueRunner gameplayLockedRunner;

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

        focusCamera.Priority =
            activePriority;
    }

    public void EndFocus()
    {
        ReleaseDialogueAdvanceLock();

        if (state == FocusState.Idle)
        {
            if (focusCamera != null)
            {
                focusCamera.Priority =
                    inactivePriority;
            }

            return;
        }

        ResolveSceneReferences();

        waitingForForwardBlend = false;
        forwardBlendStartedFrame = -1;

        if (focusCamera != null)
        {
            focusCamera.Priority =
                inactivePriority;
        }

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
            if (brain.IsBlending)
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
            if (brain.IsBlending)
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
        if (focusCamera != null)
        {
            focusCamera.Priority =
                inactivePriority;
        }

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

        advanceLockedRunner
            ?.WaitForExternalAction();
    }

    private void ReleaseDialogueAdvanceLock()
    {
        if (advanceLockedRunner == null)
        {
            return;
        }

        advanceLockedRunner
            .ResumeAfterExternalAction();

        advanceLockedRunner = null;
    }

    private void AcquireGameplayLock()
    {
        if (gameplayLockedRunner != null)
        {
            return;
        }

        gameplayLockedRunner =
            DialogueRunner.Instance;

        gameplayLockedRunner
            ?.SetExternalGameplayLock(true);
    }

    private void ReleaseGameplayLock()
    {
        if (gameplayLockedRunner == null)
        {
            return;
        }

        gameplayLockedRunner
            .SetExternalGameplayLock(false);

        gameplayLockedRunner = null;
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
}