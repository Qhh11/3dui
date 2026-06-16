using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    public bool IsDialogueOpen { get; private set; }

    // Keep this for compatibility with older scripts.
    public bool IsOpen => IsDialogueOpen;

    [Header("Dialogue UI")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Fixed choice UI")]
    [Tooltip("Parent object of your three manually created choice buttons.")]
    [SerializeField] private GameObject choicesRoot;

    [Tooltip("Drag your three existing choice buttons here in order: 0, 1, 2.")]
    [SerializeField] private Button[] choiceButtons;

    [Header("In-game UI visibility")]
    [Tooltip("Use CanvasGroup instead of SetActive, especially for OnScreenControl joystick UI.")]
    [SerializeField] private CanvasGroup[] inGameUiCanvasGroups;

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 40f;

    [Header("Optional control lock")]
    [Tooltip("Player movement, gyro camera, switch button scripts, etc. will be disabled during dialogue.")]
    [SerializeField] private MonoBehaviour[] behavioursToDisableWhileOpen;

    [Header("Control lock groups")]
    [SerializeField] private ControlLockGroup[] controlLockGroups;

    private DialogueSequence currentSequence;
    private int currentIndex;
    private Coroutine typingCoroutine;
    private string fullCurrentText;
    private bool isTyping;
    private bool waitingForChoice;
    private bool isWaitingForExternalAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CloseImmediate();
    }

    private void Update()
    {
        if (!IsDialogueOpen)
        {
            return;
        }

        if (waitingForChoice)
        {
            return;
        }

        if (isWaitingForExternalAction)
        {
            return;
        }

        if (TryGetAdvanceInput())
        {
            Advance();
        }
    }

    private bool TryGetAdvanceInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUi(-1))
            {
                return false;
            }

            return true;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverUi(touch.fingerId))
                {
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    private bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    public void WaitForExternalAction()
    {
        isWaitingForExternalAction = true;
    }

    public void ContinueAfterExternalAction(bool endDialogue)
    {
        if (!isWaitingForExternalAction)
        {
            return;
        }

        isWaitingForExternalAction = false;

        if (endDialogue)
        {
            EndDialogue();
        }
        else
        {
            Advance();
        }
    }

    public void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning("Cannot start dialogue: sequence is null.");
            return;
        }

        currentSequence = sequence;
        currentIndex = 0;
        IsDialogueOpen = true;

        SetDialogueVisible(true);
        SetInGameUiVisible(false);
        SetControlsEnabled(false);

        SwitchPerspectiveButtonController.Instance?.NotifyDialogueOpened();

        ClearChoices();
        ShowCurrentEntry();
    }

    public void Advance()
    {
        if (!IsDialogueOpen || waitingForChoice)
        {
            return;
        }

        if (isTyping)
        {
            FinishTypingImmediately();
            return;
        }

        currentIndex++;

        if (currentSequence == null || currentIndex >= currentSequence.entries.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentEntry();
    }

    private void ShowCurrentEntry()
    {
        ClearChoices();

        if (currentSequence == null || currentIndex < 0 || currentIndex >= currentSequence.entries.Count)
        {
            EndDialogue();
            return;
        }

        DialogueEntry entry = currentSequence.entries[currentIndex];

        DialogueEventRouter.Instance?.Trigger(entry.eventOnStart);

        if (entry.speaker != null)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = entry.speaker.displayName;
                speakerNameText.color = entry.speaker.nameColor;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = entry.speaker.portrait;
                portraitImage.enabled = entry.speaker.portrait != null;
            }
        }
        else
        {
            if (speakerNameText != null)
            {
                speakerNameText.text = string.Empty;
            }

            if (portraitImage != null)
            {
                portraitImage.enabled = false;
            }
        }

        fullCurrentText = entry.text ?? string.Empty;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLine(entry));
    }

    private IEnumerator TypeLine(DialogueEntry entry)
    {
        isTyping = true;

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }

        float delay = charactersPerSecond <= 0f ? 0f : 1f / charactersPerSecond;

        for (int i = 0; i < fullCurrentText.Length; i++)
        {
            if (bodyText != null)
            {
                bodyText.text += fullCurrentText[i];
            }

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }
        }

        CompleteCurrentEntry(entry);
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (bodyText != null)
        {
            bodyText.text = fullCurrentText;
        }

        if (currentSequence == null || currentIndex < 0 || currentIndex >= currentSequence.entries.Count)
        {
            EndDialogue();
            return;
        }

        DialogueEntry entry = currentSequence.entries[currentIndex];
        CompleteCurrentEntry(entry);
    }

    private void CompleteCurrentEntry(DialogueEntry entry)
    {
        isTyping = false;
        typingCoroutine = null;

        DialogueEventRouter.Instance?.Trigger(entry.eventOnEnd);

        if (entry.choices != null && entry.choices.Count > 0)
        {
            ShowChoices(entry);
        }
    }

    private void ShowChoices(DialogueEntry entry)
    {
        waitingForChoice = true;

        if (choicesRoot != null)
        {
            choicesRoot.SetActive(true);
        }

        if (choiceButtons == null || choiceButtons.Length == 0)
        {
            Debug.LogWarning("No choice buttons assigned on DialogueRunner.");
            return;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];

            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();

            bool hasChoice = i < entry.choices.Count;
            button.gameObject.SetActive(hasChoice);

            if (!hasChoice)
            {
                continue;
            }

            DialogueChoice choice = entry.choices[i];

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = choice.text;
            }

            DialogueChoice capturedChoice = choice;
            button.onClick.AddListener(() => SelectChoice(capturedChoice));
        }
    }

    private void SelectChoice(DialogueChoice choice)
    {
        waitingForChoice = false;
        ClearChoices();

        if (!string.IsNullOrWhiteSpace(choice.setFlagOnSelect))
        {
            GameStateManager.Instance?.SetFlag(choice.setFlagOnSelect, true);
        }

        DialogueEventRouter.Instance?.Trigger(choice.eventOnSelect);

        if (choice.nextSequence != null)
        {
            StartDialogue(choice.nextSequence);
            return;
        }

        Advance();
    }

    private void ClearChoices()
    {
        waitingForChoice = false;

        if (choiceButtons != null)
        {
            foreach (Button button in choiceButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        if (choicesRoot != null)
        {
            choicesRoot.SetActive(false);
        }
    }

    public void EndDialogue()
    {
        ClearChoices();
        CloseImmediate();
    }

    private void CloseImmediate()
    {
        IsDialogueOpen = false;
        isWaitingForExternalAction = false;

        currentSequence = null;
        currentIndex = 0;
        isTyping = false;
        waitingForChoice = false;
        fullCurrentText = string.Empty;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        SetDialogueVisible(false);
        SetInGameUiVisible(true);
        SetControlsEnabled(true);

        SwitchPerspectiveButtonController.Instance?.NotifyDialogueClosed();
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialogueCanvasGroup == null)
        {
            return;
        }

        dialogueCanvasGroup.alpha = visible ? 1f : 0f;
        dialogueCanvasGroup.interactable = visible;
        dialogueCanvasGroup.blocksRaycasts = visible;
    }

    private void SetInGameUiVisible(bool visible)
    {
        if (inGameUiCanvasGroups == null)
        {
            return;
        }

        foreach (CanvasGroup canvasGroup in inGameUiCanvasGroups)
        {
            if (canvasGroup == null)
            {
                continue;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        if (behavioursToDisableWhileOpen != null)
        {
            foreach (MonoBehaviour behaviour in behavioursToDisableWhileOpen)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = enabled;
                }
            }
        }

        if (controlLockGroups != null)
        {
            foreach (ControlLockGroup group in controlLockGroups)
            {
                if (group != null)
                {
                    group.SetControlsEnabled(enabled);
                }
            }
        }
    }
}