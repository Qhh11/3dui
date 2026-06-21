using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SwitchPerspectiveButtonController : MonoBehaviour
{
    public static SwitchPerspectiveButtonController Instance { get; private set; }
    public bool IsUfoView { get; private set; }

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
    [SerializeField] private string firstUfoSequenceId = "level1_first_ufo_view";
    [SerializeField] private float firstUfoDialogueDelay = 5f;

    [Header("Return Player Flow")]
    [SerializeField] private string afterSwitchBackSequenceId = "level1_after_switch_back_to_player";
    [SerializeField] private float afterSwitchBackDialogueDelay = 0.2f;

    private SwitchRequestMode requestMode = SwitchRequestMode.None;
    private bool switchVisible;

    private void Awake()
    {
        Instance = this;
        IsUfoView = startInUfoView;
    }

    private void Start()
    {
        if (viewToggleButton == null)
            viewToggleButton = FindObjectOfType<ViewToggleButton>();

        if (siphonController == null)
            siphonController = FindObjectOfType<SiphonController>();

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
        if (switchVisible)
            SetRootVisible(true);

        RefreshSwitchButtonAvailability();
    }

    public void NotifyDialogueClosed()
    {
        if (switchVisible)
            SetRootVisible(true);

        RefreshSwitchButtonAvailability();
    }

    public void ShowSwitchToUfoHint()
    {
        switchVisible = true;
        requestMode = SwitchRequestMode.SwitchToUfo;

        SetRootVisible(true);
        SetHighlightVisible(true);
        RefreshSwitchButtonAvailability();

        DialogueRunner.Instance?.WaitForExternalAction();
    }

    public void ShowSwitchToPlayerHint()
    {
        switchVisible = true;
        requestMode = SwitchRequestMode.SwitchToPlayer;

        SetRootVisible(true);
        SetHighlightVisible(true);
        RefreshSwitchButtonAvailability();

        DialogueRunner.Instance?.WaitForExternalAction();
    }

    public void ShowSwitchHint()
    {
        ShowSwitchToUfoHint();
    }

    public void EnableFreeSwitching()
    {
        switchVisible = true;
        requestMode = SwitchRequestMode.FreeSwitching;

        SetRootVisible(true);
        SetHighlightVisible(false);
        RefreshSwitchButtonAvailability();
    }

    public void OnSwitchButtonClicked()
    {
        Debug.Log("Switch button clicked.");

        if (!CanSwitchNow())
        {
            Debug.Log("Switch blocked because another interaction is busy.");
            RefreshSwitchButtonAvailability();
            return;
        }

        if (viewToggleButton == null)
            viewToggleButton = FindObjectOfType<ViewToggleButton>();

        if (viewToggleButton == null)
        {
            Debug.LogWarning("ViewToggleButton is not assigned.");
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
        {
            HandleFreeSwitch();
            return;
        }

        Debug.Log("Switch button ignored because no switch request is active.");
    }

    private void HandleSwitchToUfo()
    {
        IsUfoView = true;
        viewToggleButton.SwitchToUfo();
        siphonController?.OnPerspectiveChanged(IsUfoView);

        requestMode = SwitchRequestMode.None;
        SetHighlightVisible(false);

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        DialogueRunner.Instance?.ContinueAfterExternalAction(true);

        StartCoroutine(StartDialogueAfterDelay(firstUfoSequenceId, firstUfoDialogueDelay));

        RefreshSwitchButtonAvailability();
    }

    private void HandleSwitchToPlayer()
    {
        IsUfoView = false;
        viewToggleButton.SwitchToPlayer();
        siphonController?.OnPerspectiveChanged(IsUfoView);

        requestMode = SwitchRequestMode.FreeSwitching;
        SetHighlightVisible(false);

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        DialogueRunner.Instance?.ContinueAfterExternalAction(true);

        StartCoroutine(StartDialogueAfterDelay(afterSwitchBackSequenceId, afterSwitchBackDialogueDelay));

        RefreshSwitchButtonAvailability();
    }

    private void HandleFreeSwitch()
    {
        viewToggleButton.Toggle();

        IsUfoView = !IsUfoView;
        siphonController?.OnPerspectiveChanged(IsUfoView);

        SetHighlightVisible(false);
        RefreshSwitchButtonAvailability();
    }

    private IEnumerator StartDialogueAfterDelay(string sequenceId, float delay)
    {
        yield return new WaitForSeconds(delay);

        DialogueSequence sequence = Level1DialogueDatabase.Instance.GetSequence(sequenceId);

        if (sequence == null)
        {
            Debug.LogWarning($"Dialogue sequence not found: {sequenceId}");
            yield break;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    private bool CanSwitchNow()
    {
        if (siphonController != null && !siphonController.CanSwitchPerspective)
            return false;

        return true;
    }

    private void RefreshSwitchButtonAvailability()
    {
        if (switchButton == null)
            return;

        if (!switchVisible)
        {
            SetButtonInteractable(false);
            return;
        }

        bool hasSwitchRequest =
            requestMode == SwitchRequestMode.SwitchToUfo ||
            requestMode == SwitchRequestMode.SwitchToPlayer ||
            requestMode == SwitchRequestMode.FreeSwitching;

        bool siphonAllowsSwitch =
            siphonController == null ||
            siphonController.CanSwitchPerspective;

        SetButtonInteractable(hasSwitchRequest && siphonAllowsSwitch);

        if (requestMode == SwitchRequestMode.SwitchToUfo ||
            requestMode == SwitchRequestMode.SwitchToPlayer)
        {
            SetHighlightVisible(siphonAllowsSwitch);
        }
        else
        {
            SetHighlightVisible(false);
        }
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