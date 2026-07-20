using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SwitchPerspectiveButtonController : MonoBehaviour
{
    public static SwitchPerspectiveButtonController Instance
    {
        get;
        private set;
    }

    public bool IsUfoView
    {
        get;
        private set;
    }

    /// <summary>
    /// Switch 当前是否真的能被玩家操作。
    ///
    /// 同时检查界面实际状态和 OnSwitchButtonClicked
    /// 会执行的业务判断，供自动恢复等外部逻辑使用。
    /// </summary>
    public bool CanPlayerSwitchNow
    {
        get
        {
            if (!isActiveAndEnabled ||
                switchUiRoot == null ||
                !switchUiRoot.activeInHierarchy ||
                switchButton == null ||
                !switchButton.isActiveAndEnabled ||
                !switchButton.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (switchUiCanvasGroup != null &&
                (!switchUiCanvasGroup.interactable ||
                 !switchUiCanvasGroup.blocksRaycasts))
            {
                return false;
            }

            return
                CanSwitchNow() &&
                switchButton.interactable &&
                switchButton.IsInteractable();
        }
    }

    private enum SwitchRequestMode
    {
        None,
        SwitchToUfo,
        SwitchToPlayer,
        FreeSwitching
    }

    [Header("References")]
    [SerializeField] private ViewToggleButton viewToggleButton;
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private MarkController markController;
    [SerializeField] private RocketLauncher rocketLauncher;

    [Header("View UI")]
    [SerializeField] private UIToggleFade playerViewFade;
    [SerializeField] private UIToggleFade ufoViewFade;

    [Header("Root")]
    [SerializeField] private GameObject switchUiRoot;

    [Header("Button")]
    [SerializeField] private Button switchButton;

    [Header("Highlight")]
    [SerializeField] private GameObject highlightObject;

    [Header("Start State")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private bool startInUfoView = false;

    [Header("First UFO Flow")]
    [SerializeField] private string firstUfoSequenceId =
        "level1_first_ufo_view";

    [SerializeField] private float firstUfoDialogueDelay = 5f;

    [Header("Return Player Flow")]
    [SerializeField] private string afterSwitchBackSequenceId =
        "level1_after_switch_back_to_player";

    [SerializeField] private float afterSwitchBackDialogueDelay = 0.2f;

    private SwitchRequestMode requestMode =
        SwitchRequestMode.None;

    private bool switchVisible;
    private CanvasGroup switchUiCanvasGroup;
    private int switchWaitToken;
    private Coroutine pendingDialogueCoroutine;
    private int delayedDialogueVersion;

    private void Awake()
    {
        Instance = this;
        IsUfoView = startInUfoView;

        switchUiCanvasGroup =
            GetComponent<CanvasGroup>();
    }

    private void Start()
    {
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

        ApplyViewUiImmediate();

        switchVisible = !startHidden;

        SetRootVisible(switchVisible);
        SetButtonInteractable(false);
        SetHighlightVisible(false);

        siphonController?.OnPerspectiveChanged(IsUfoView);
    }

    private void Update()
    {
        RefreshSwitchButtonAvailability();
    }

    public void NotifyDialogueOpened()
    {
        CancelStaleGuidedSwitchWait();

        if (switchVisible)
            SetRootVisible(true);

        RefreshSwitchButtonAvailability();
    }

    public void NotifyDialogueClosed()
    {
        CancelStaleGuidedSwitchWait();

        if (switchVisible)
            SetRootVisible(true);

        RefreshSwitchButtonAvailability();
    }

    public void ShowSwitchToUfoHint()
    {
        RequestSwitchToUfo();
    }

    public void ShowSwitchToPlayerHint()
    {
        RequestSwitchToPlayer();
    }

    public void ShowSwitchHint()
    {
        RequestSwitchToUfo();
    }

    public void RequestSwitchToUfo()
    {
        BeginGuidedSwitch(
            SwitchRequestMode.SwitchToUfo,
            "Switch to UFO hint"
        );
    }

    public void RequestSwitchToPlayer()
    {
        BeginGuidedSwitch(
            SwitchRequestMode.SwitchToPlayer,
            "Switch to player hint"
        );
    }

    public void EnableFreeSwitching()
    {
        switchVisible = true;
        requestMode = SwitchRequestMode.FreeSwitching;

        SetRootVisible(true);
        RefreshSwitchButtonAvailability();
    }

    public void OnSwitchButtonClicked()
    {
        Debug.Log("Switch button clicked.");

        if (!CanSwitchNow())
        {
            Debug.Log(
                "Switch blocked because another interaction is busy."
            );

            RefreshSwitchButtonAvailability();
            return;
        }

        if (requestMode == SwitchRequestMode.SwitchToUfo)
        {
            HandleSwitchToUfo();
            return;
        }

        if (requestMode == SwitchRequestMode.SwitchToPlayer)
        {
            HandleSwitchToPlayer();
            return;
        }

        if (requestMode == SwitchRequestMode.FreeSwitching)
            HandleFreeSwitch();
    }

    private void HandleSwitchToUfo()
    {
        viewToggleButton.SwitchToUfo();
        IsUfoView = viewToggleButton.IsUfoView();

        ApplyViewUi();

        siphonController?.OnPerspectiveChanged(IsUfoView);

        requestMode = SwitchRequestMode.None;
        SetHighlightVisible(false);

        ObjectivePanelController.Instance
            ?.CompleteCurrentObjective();

        DialogueRunner.Instance
            ?.ContinueAfterExternalAction(
                switchWaitToken,
                true
            );

        ClearGuidedSwitchWait();

        StartDelayedDialogue(
            firstUfoSequenceId,
            firstUfoDialogueDelay
        );

        RefreshSwitchButtonAvailability();
    }

    private void HandleSwitchToPlayer()
    {
        viewToggleButton.SwitchToPlayer();
        IsUfoView = viewToggleButton.IsUfoView();

        ApplyViewUi();

        siphonController?.OnPerspectiveChanged(IsUfoView);

        requestMode = SwitchRequestMode.FreeSwitching;
        SetHighlightVisible(false);

        ObjectivePanelController.Instance
            ?.CompleteCurrentObjective();

        DialogueRunner.Instance
            ?.ContinueAfterExternalAction(
                switchWaitToken,
                true
            );

        ClearGuidedSwitchWait();

        StartDelayedDialogue(
            afterSwitchBackSequenceId,
            afterSwitchBackDialogueDelay
        );

        RefreshSwitchButtonAvailability();
    }

    private void HandleFreeSwitch()
    {
        viewToggleButton.Toggle();
        IsUfoView = viewToggleButton.IsUfoView();

        ApplyViewUi();

        siphonController?.OnPerspectiveChanged(IsUfoView);

        SetHighlightVisible(false);
        RefreshSwitchButtonAvailability();
    }

    public void ForceSwitchToUfo()
    {
        ForceSwitchToUfoForCutscene();
    }

    public void ForceSwitchToPlayer()
    {
        ForceSwitchToPlayerForCutscene();
    }

    public void ForceSwitchToUfoForCutscene()
    {
        if (viewToggleButton == null)
            return;

        viewToggleButton.SwitchToUfo();
        SyncAfterPerspectiveChanged();
    }

    public void ForceSwitchToPlayerForCutscene()
    {
        if (viewToggleButton == null)
            return;

        viewToggleButton.SwitchToPlayer();
        SyncAfterPerspectiveChanged();
    }

    private void BeginGuidedSwitch(
        SwitchRequestMode mode,
        string waitReason)
    {
        if (mode != SwitchRequestMode.SwitchToUfo &&
            mode != SwitchRequestMode.SwitchToPlayer)
        {
            return;
        }

        if (switchWaitToken != 0)
        {
            Debug.LogWarning(
                "Switch hint ignored because another " +
                "guided switch request is already active.",
                this
            );

            return;
        }

        switchVisible = true;
        requestMode = mode;

        SetRootVisible(true);
        RefreshSwitchButtonAvailability();

        switchWaitToken =
            DialogueRunner.Instance != null
                ? DialogueRunner.Instance
                    .WaitForExternalAction(waitReason)
                : 0;
    }

    private void StartDelayedDialogue(
        string sequenceId,
        float delay)
    {
        delayedDialogueVersion++;

        if (pendingDialogueCoroutine != null)
        {
            StopCoroutine(pendingDialogueCoroutine);
        }

        pendingDialogueCoroutine =
            StartCoroutine(
                StartDialogueAfterDelay(
                    sequenceId,
                    delay,
                    delayedDialogueVersion
                )
            );
    }

    private IEnumerator StartDialogueAfterDelay(
        string sequenceId,
        float delay,
        int version)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (version != delayedDialogueVersion)
            yield break;

        while (DialogueRunner.Instance != null &&
               DialogueRunner.Instance.IsDialogueOpen)
        {
            if (version != delayedDialogueVersion)
                yield break;

            yield return null;
        }

        if (version != delayedDialogueVersion)
            yield break;

        pendingDialogueCoroutine = null;

        DialogueSequence sequence =
            Level1DialogueDatabase.Instance
                .GetSequence(sequenceId);

        if (sequence == null)
        {
            Debug.LogWarning(
                $"Dialogue sequence not found: {sequenceId}"
            );
            yield break;
        }

        DialogueRunner.Instance?.StartDialogue(sequence);
    }

    private void ClearGuidedSwitchWait()
    {
        switchWaitToken = 0;
    }

    private void CancelStaleGuidedSwitchWait()
    {
        if (switchWaitToken == 0)
        {
            return;
        }

        switchWaitToken = 0;

        if (requestMode == SwitchRequestMode.SwitchToUfo ||
            requestMode == SwitchRequestMode.SwitchToPlayer)
        {
            requestMode = SwitchRequestMode.None;
            SetHighlightVisible(false);
        }
    }

    private bool CanSwitchNow()
    {
        if (!switchVisible)
            return false;

        if (requestMode == SwitchRequestMode.None)
            return false;

        if (requestMode == SwitchRequestMode.FreeSwitching &&
            GameplayInputLockService.IsBlocked)
        {
            return false;
        }

        if (viewToggleButton == null)
            return false;

        if (switchUiCanvasGroup != null &&
            !switchUiCanvasGroup.interactable)
        {
            return false;
        }

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

    private void RefreshSwitchButtonAvailability()
    {
        if (switchButton == null)
            return;

        bool canSwitch = CanSwitchNow();

        SetButtonInteractable(canSwitch);

        bool guidedRequest =
            requestMode == SwitchRequestMode.SwitchToUfo ||
            requestMode == SwitchRequestMode.SwitchToPlayer;

        SetHighlightVisible(
            guidedRequest &&
            canSwitch
        );
    }

    private void ApplyViewUi()
    {
        playerViewFade?.SetVisible(!IsUfoView);
        ufoViewFade?.SetVisible(IsUfoView);
    }

    private void ApplyViewUiImmediate()
    {
        playerViewFade?.SetVisibleImmediate(!IsUfoView);
        ufoViewFade?.SetVisibleImmediate(IsUfoView);
    }

    private void SyncAfterPerspectiveChanged()
    {
        IsUfoView = viewToggleButton.IsUfoView();

        ApplyViewUi();

        siphonController?.OnPerspectiveChanged(IsUfoView);

        SetHighlightVisible(false);
        RefreshSwitchButtonAvailability();
    }

    private void SetRootVisible(bool visible)
    {
        if (switchUiRoot != null)
            switchUiRoot.SetActive(visible);
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (switchButton != null)
            switchButton.interactable = interactable;
    }

    private void SetHighlightVisible(bool visible)
    {
        if (highlightObject != null)
            highlightObject.SetActive(visible);
    }
}
