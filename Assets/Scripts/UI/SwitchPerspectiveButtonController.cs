using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SwitchPerspectiveButtonController : MonoBehaviour
{
    public static SwitchPerspectiveButtonController Instance { get; private set; }

    private enum SwitchRequestMode
    {
        None,
        SwitchToUfo,
        SwitchToPlayer,
        FreeSwitching
    }

    [Header("References")]
    [SerializeField] private ViewToggleButton viewToggleButton;

    [Header("Root")]
    [SerializeField] private GameObject switchUiRoot;

    [Header("Button")]
    [SerializeField] private Button switchButton;

    [Header("Highlight")]
    [SerializeField] private GameObject highlightObject;

    [Header("Start State")]
    [SerializeField] private bool startHidden = true;

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
    }

    private void Start()
    {
        if (viewToggleButton == null)
        {
            viewToggleButton = FindObjectOfType<ViewToggleButton>();
        }

        switchVisible = !startHidden;

        SetRootVisible(switchVisible);
        SetButtonInteractable(false);
        SetHighlightVisible(false);
    }

    public void NotifyDialogueOpened()
    {
        if (switchVisible)
        {
            SetRootVisible(true);
        }

        if (requestMode == SwitchRequestMode.SwitchToUfo || requestMode == SwitchRequestMode.SwitchToPlayer)
        {
            SetButtonInteractable(true);
            SetHighlightVisible(true);
        }
        else
        {
            SetButtonInteractable(false);
            SetHighlightVisible(false);
        }
    }

    public void NotifyDialogueClosed()
    {
        if (switchVisible)
        {
            SetRootVisible(true);
        }

        if (requestMode == SwitchRequestMode.FreeSwitching)
        {
            SetButtonInteractable(true);
            SetHighlightVisible(false);
            return;
        }

        if (requestMode == SwitchRequestMode.SwitchToUfo || requestMode == SwitchRequestMode.SwitchToPlayer)
        {
            SetButtonInteractable(true);
            SetHighlightVisible(true);
            return;
        }

        SetButtonInteractable(false);
        SetHighlightVisible(false);
    }

    public void ShowSwitchToUfoHint()
    {
        switchVisible = true;
        requestMode = SwitchRequestMode.SwitchToUfo;

        SetRootVisible(true);
        SetButtonInteractable(true);
        SetHighlightVisible(true);

        DialogueRunner.Instance?.WaitForExternalAction();
    }

    public void ShowSwitchToPlayerHint()
    {
        switchVisible = true;
        requestMode = SwitchRequestMode.SwitchToPlayer;

        SetRootVisible(true);
        SetButtonInteractable(true);
        SetHighlightVisible(true);

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
        SetButtonInteractable(true);
        SetHighlightVisible(false);
    }

    public void OnSwitchButtonClicked()
    {
        Debug.Log("Switch button clicked.");

        if (viewToggleButton == null)
        {
            viewToggleButton = FindObjectOfType<ViewToggleButton>();
        }

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
            viewToggleButton.Toggle();
            SetHighlightVisible(false);
            SetButtonInteractable(true);
            return;
        }

        Debug.Log("Switch button ignored because no switch request is active.");
    }

    private void HandleSwitchToUfo()
    {
        viewToggleButton.SwitchToUfo();

        requestMode = SwitchRequestMode.None;
        SetHighlightVisible(false);
        SetButtonInteractable(false);

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        DialogueRunner.Instance?.ContinueAfterExternalAction(true);

        StartCoroutine(StartDialogueAfterDelay(firstUfoSequenceId, firstUfoDialogueDelay));
    }

    private void HandleSwitchToPlayer()
    {
        viewToggleButton.SwitchToPlayer();

        requestMode = SwitchRequestMode.FreeSwitching;
        SetHighlightVisible(false);
        SetButtonInteractable(true);

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        DialogueRunner.Instance?.ContinueAfterExternalAction(true);

        StartCoroutine(StartDialogueAfterDelay(afterSwitchBackSequenceId, afterSwitchBackDialogueDelay));
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

    private void SetRootVisible(bool visible)
    {
        if (switchUiRoot != null)
        {
            switchUiRoot.SetActive(visible);
        }
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (switchButton != null)
        {
            switchButton.interactable = interactable;
        }
    }

    private void SetHighlightVisible(bool visible)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(visible);
        }
    }
}