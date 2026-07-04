using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Level2BPortalConfirmationUI : MonoBehaviour
{
    public static Level2BPortalConfirmationUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;

    [Header("Ending")]
    [SerializeField] private Level2BEndingUIController endingUI;

    [Header("Text")]
    [SerializeField]
    private string confirmationMessage =
        "Are you sure this is the reality you want to enter?";

    [Header("Typewriter")]
    [SerializeField] private float typewriterSpeed = 0.035f;
    [SerializeField] private float fadeInDuration = 0.25f;

    private Coroutine currentRoutine;
    private bool isVisible;

    private void Awake()
    {
        Instance = this;

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        HideInstant();
    }

    private void Start()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnYesClicked);
            yesButton.onClick.AddListener(OnYesClicked);
        }
    }

    public void Show()
    {
        if (isVisible)
        {
            return;
        }

        isVisible = true;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    public void HideInstant()
    {
        isVisible = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (messageText != null)
        {
            messageText.text = string.Empty;
        }

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowRoutine()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (messageText != null)
        {
            messageText.text = string.Empty;
        }

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            float timer = 0f;

            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / fadeInDuration);
                canvasGroup.alpha = t;
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        yield return TypeMessage();

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(true);
            yesButton.interactable = true;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        currentRoutine = null;
    }

    private IEnumerator TypeMessage()
    {
        if (messageText == null)
        {
            yield break;
        }

        messageText.text = string.Empty;

        for (int i = 0; i < confirmationMessage.Length; i++)
        {
            messageText.text += confirmationMessage[i];
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    private void OnYesClicked()
    {
        HideInstant();

        if (endingUI != null)
        {
            endingUI.Show();
        }
    }
}