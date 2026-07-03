using System.Collections;
using System.Collections.Generic;
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

    public bool IsGameplayInputBlocked =>
        GameplayInputLockService.IsBlocked;

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

    private int nextExternalWaitToken = 1;
    private int activeExternalWaitToken;
    private readonly HashSet<int> activeExternalWaitTokens =
        new HashSet<int>();

    private GameplayInputLockService.LockHandle dialogueGameplayLockHandle;
    private GameplayInputLockService.LockHandle externalGameplayLockHandle;

    private bool gameplayAvailabilityInitialized;
    private bool gameplayControlsEnabled;

    private readonly List<RaycastResult> uiRaycastResults =
        new List<RaycastResult>();

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
        if (!IsDialogueOpen)
            CloseImmediate();
    }

    private void Update()
    {
        if (!IsDialogueOpen)
            return;

        if (waitingForChoice)
            return;

        if (isWaitingForExternalAction)
            return;

        if (TryGetAdvanceInput())
            Advance();
    }

    private bool TryGetAdvanceInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return))
        {
            return true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerBlockedForAdvance(
                    -1,
                    Input.mousePosition))
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
                if (IsPointerBlockedForAdvance(
                        touch.fingerId,
                        touch.position))
                {
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    private bool IsPointerBlockedForAdvance(
        int pointerId,
        Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        uiRaycastResults.Clear();

        PointerEventData eventData =
            new PointerEventData(EventSystem.current)
            {
                pointerId = pointerId,
                position = screenPosition
            };

        EventSystem.current.RaycastAll(
            eventData,
            uiRaycastResults);

        if (uiRaycastResults.Count == 0)
            return false;

        Transform dialogueRoot =
            dialogueCanvasGroup != null
                ? dialogueCanvasGroup.transform
                : null;

        foreach (RaycastResult result in uiRaycastResults)
        {
            if (!(result.module is GraphicRaycaster))
                continue;

            GameObject hitObject = result.gameObject;

            if (hitObject == null)
                continue;

            if (dialogueRoot != null &&
                hitObject.transform.IsChildOf(dialogueRoot))
            {
                return HasInteractiveUiControl(
                    hitObject.transform);
            }

            return true;
        }

        return false;
    }

    private static bool HasInteractiveUiControl(
        Transform transform)
    {
        Selectable selectable =
            transform.GetComponentInParent<Selectable>();

        if (selectable != null &&
            selectable.IsActive() &&
            selectable.IsInteractable())
        {
            return true;
        }

        GameObject gameObject = transform.gameObject;

        return ExecuteEvents
                .GetEventHandler<IPointerClickHandler>(
                    gameObject) != null ||
            ExecuteEvents
                .GetEventHandler<IPointerDownHandler>(
                    gameObject) != null ||
            ExecuteEvents
                .GetEventHandler<IDragHandler>(
                    gameObject) != null ||
            ExecuteEvents
                .GetEventHandler<IScrollHandler>(
                    gameObject) != null;
    }

    public int WaitForExternalAction(string reason = null)
    {
        int token = nextExternalWaitToken++;

        if (nextExternalWaitToken == int.MaxValue)
            nextExternalWaitToken = 1;

        activeExternalWaitToken = token;
        activeExternalWaitTokens.Add(token);
        isWaitingForExternalAction = true;

        return token;
    }

    public void ResumeAfterExternalAction(int token)
    {
        if (token == 0 ||
            !activeExternalWaitTokens.Contains(token))
        {
            return;
        }

        ClearExternalWait(token);
    }

    // Legacy API. Keep temporarily for old scene bindings/scripts.
    public void ResumeAfterExternalAction()
    {
        ClearExternalWait();
    }

    public void SetExternalGameplayLock(bool locked)
    {
        if (locked)
        {
            if (!externalGameplayLockHandle.IsValid)
            {
                externalGameplayLockHandle =
                    GameplayInputLockService.Acquire(
                        "DialogueRunner external gameplay lock"
                    );
            }
        }
        else
        {
            ReleaseExternalGameplayLock();
        }

        RefreshGameplayAvailability();
    }

    public void ContinueAfterExternalAction(
        int token,
        bool endDialogue)
    {
        if (token == 0 ||
            !activeExternalWaitTokens.Contains(token))
        {
            return;
        }

        ClearExternalWait(token);

        if (endDialogue)
        {
            EndDialogue();
        }
        else if (!isWaitingForExternalAction)
        {
            Advance();
        }
    }

    // Legacy API. Keep temporarily for old scene bindings/scripts.
    public void ContinueAfterExternalAction(bool endDialogue)
    {
        if (!isWaitingForExternalAction)
            return;

        ClearExternalWait();

        if (endDialogue)
            EndDialogue();
        else
            Advance();
    }

    public void StartDialogue(DialogueSequence sequence)
    {
        if (sequence == null)
        {
            Debug.LogWarning(
                "Cannot start dialogue: sequence is null."
            );

            return;
        }

        StopTypingCoroutine();
        ClearChoices();
        ClearExternalWait();

        currentSequence = sequence;
        currentIndex = 0;
        IsDialogueOpen = true;

        AcquireDialogueGameplayLock();
        SetDialogueVisible(true);
        RefreshGameplayAvailability();

        SwitchPerspectiveButtonController
            .Instance
            ?.NotifyDialogueOpened();

        ShowCurrentEntry();
    }

    public void Advance()
    {
        if (!IsDialogueOpen ||
            waitingForChoice)
        {
            return;
        }

        if (isTyping)
        {
            FinishTypingImmediately();
            return;
        }

        currentIndex++;

        if (currentSequence == null ||
            currentIndex >= currentSequence.entries.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentEntry();
    }

    private void ShowCurrentEntry()
    {
        ClearChoices();

        if (currentSequence == null ||
            currentIndex < 0 ||
            currentIndex >= currentSequence.entries.Count)
        {
            EndDialogue();
            return;
        }

        DialogueEntry entry =
            currentSequence.entries[currentIndex];

        DialogueEventRouter.Instance
            ?.Trigger(entry.eventOnStart);

        if (entry.speaker != null)
        {
            if (speakerNameText != null)
            {
                speakerNameText.text =
                    entry.speaker.displayName;

                speakerNameText.color =
                    entry.speaker.nameColor;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite =
                    entry.speaker.portrait;

                portraitImage.enabled =
                    entry.speaker.portrait != null;
            }
        }
        else
        {
            if (speakerNameText != null)
                speakerNameText.text = string.Empty;

            if (portraitImage != null)
                portraitImage.enabled = false;
        }

        fullCurrentText =
            entry.text ?? string.Empty;

        StopTypingCoroutine();

        typingCoroutine =
            StartCoroutine(TypeLine(entry));
    }

    private IEnumerator TypeLine(DialogueEntry entry)
    {
        isTyping = true;

        if (bodyText != null)
            bodyText.text = string.Empty;

        float delay =
            charactersPerSecond <= 0f
                ? 0f
                : 1f / charactersPerSecond;

        for (int i = 0;
             i < fullCurrentText.Length;
             i++)
        {
            if (bodyText != null)
                bodyText.text += fullCurrentText[i];

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            else
                yield return null;
        }

        CompleteCurrentEntry(entry);
    }

    private void FinishTypingImmediately()
    {
        StopTypingCoroutine();

        if (bodyText != null)
            bodyText.text = fullCurrentText;

        if (currentSequence == null ||
            currentIndex < 0 ||
            currentIndex >= currentSequence.entries.Count)
        {
            EndDialogue();
            return;
        }

        DialogueEntry entry =
            currentSequence.entries[currentIndex];

        CompleteCurrentEntry(entry);
    }

    private void CompleteCurrentEntry(DialogueEntry entry)
    {
        isTyping = false;
        typingCoroutine = null;

        DialogueEventRouter.Instance
            ?.Trigger(entry.eventOnEnd);

        if (entry.choices != null &&
            entry.choices.Count > 0)
        {
            ShowChoices(entry);
        }
    }

    private void ShowChoices(DialogueEntry entry)
    {
        waitingForChoice = true;

        if (choicesRoot != null)
            choicesRoot.SetActive(true);

        if (choiceButtons == null ||
            choiceButtons.Length == 0)
        {
            Debug.LogWarning(
                "No choice buttons assigned on DialogueRunner."
            );

            return;
        }

        for (int i = 0;
             i < choiceButtons.Length;
             i++)
        {
            Button button = choiceButtons[i];

            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();

            bool hasChoice =
                i < entry.choices.Count;

            button.gameObject.SetActive(hasChoice);

            if (!hasChoice)
                continue;

            DialogueChoice choice =
                entry.choices[i];

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = choice.text;

            DialogueChoice capturedChoice = choice;

            button.onClick.AddListener(
                () => SelectChoice(capturedChoice)
            );
        }
    }

    private void SelectChoice(DialogueChoice choice)
    {
        waitingForChoice = false;
        ClearChoices();

        if (!string.IsNullOrWhiteSpace(
                choice.setFlagOnSelect))
        {
            GameStateManager.Instance
                ?.SetFlag(
                    choice.setFlagOnSelect,
                    true
                );
        }

        DialogueEventRouter.Instance
            ?.Trigger(choice.eventOnSelect);

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
                    continue;

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        if (choicesRoot != null)
            choicesRoot.SetActive(false);
    }

    public void EndDialogue()
    {
        ClearChoices();
        CloseImmediate();
    }

    private void CloseImmediate()
    {
        IsDialogueOpen = false;
        ClearExternalWait();
        ReleaseExternalGameplayLock();
        ReleaseDialogueGameplayLock();

        currentSequence = null;
        currentIndex = 0;
        isTyping = false;
        waitingForChoice = false;
        fullCurrentText = string.Empty;

        StopTypingCoroutine();

        SetDialogueVisible(false);
        RefreshGameplayAvailability();

        SwitchPerspectiveButtonController
            .Instance
            ?.NotifyDialogueClosed();
    }

    public void RefreshGameplayAvailability()
    {
        bool controlsEnabled =
            !IsGameplayInputBlocked;

        if (gameplayAvailabilityInitialized &&
            gameplayControlsEnabled == controlsEnabled)
        {
            return;
        }

        gameplayAvailabilityInitialized = true;
        gameplayControlsEnabled = controlsEnabled;

        SetInGameUiVisible(controlsEnabled);
        SetControlsEnabled(controlsEnabled);
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialogueCanvasGroup == null)
            return;

        dialogueCanvasGroup.alpha =
            visible ? 1f : 0f;

        dialogueCanvasGroup.interactable =
            visible;

        dialogueCanvasGroup.blocksRaycasts =
            visible;
    }

    private void SetInGameUiVisible(bool visible)
    {
        if (inGameUiCanvasGroups == null)
            return;

        foreach (CanvasGroup canvasGroup
                 in inGameUiCanvasGroups)
        {
            if (canvasGroup == null)
                continue;

            canvasGroup.alpha =
                visible ? 1f : 0f;

            canvasGroup.interactable =
                visible;

            canvasGroup.blocksRaycasts =
                visible;
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        if (behavioursToDisableWhileOpen != null)
        {
            foreach (MonoBehaviour behaviour
                     in behavioursToDisableWhileOpen)
            {
                if (behaviour != null)
                    behaviour.enabled = enabled;
            }
        }

        if (controlLockGroups != null)
        {
            foreach (ControlLockGroup group
                     in controlLockGroups)
            {
                if (group != null)
                    group.SetControlsEnabled(enabled);
            }
        }
    }

    private void StopTypingCoroutine()
    {
        if (typingCoroutine == null)
            return;

        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
        isTyping = false;
    }

    private void ClearExternalWait()
    {
        activeExternalWaitToken = 0;
        activeExternalWaitTokens.Clear();
        isWaitingForExternalAction = false;
    }

    private void ClearExternalWait(int token)
    {
        activeExternalWaitTokens.Remove(token);

        if (activeExternalWaitToken == token)
        {
            activeExternalWaitToken = 0;

            foreach (int activeToken in activeExternalWaitTokens)
            {
                activeExternalWaitToken = activeToken;
                break;
            }
        }

        isWaitingForExternalAction =
            activeExternalWaitTokens.Count > 0;
    }

    private void AcquireDialogueGameplayLock()
    {
        if (dialogueGameplayLockHandle.IsValid)
            return;

        dialogueGameplayLockHandle =
            GameplayInputLockService.Acquire(
                "Dialogue open"
            );
    }

    private void ReleaseDialogueGameplayLock()
    {
        if (!dialogueGameplayLockHandle.IsValid)
            return;

        GameplayInputLockService.Release(
            dialogueGameplayLockHandle
        );

        dialogueGameplayLockHandle = default;
    }

    private void ReleaseExternalGameplayLock()
    {
        if (!externalGameplayLockHandle.IsValid)
            return;

        GameplayInputLockService.Release(
            externalGameplayLockHandle
        );

        externalGameplayLockHandle = default;
    }
}
