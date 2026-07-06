using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BoundaryWarningUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text warningLabel;
    [SerializeField] private float fadeSpeed = 8f;

    [Header("Speaker")]
    [SerializeField] private string fallbackSpeakerName = "UFO";
    [SerializeField] private Color fallbackSpeakerColor =
        new Color(0.55f, 0.85f, 1f);

    private bool targetVisible;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        Image[] images = GetComponentsInChildren<Image>(true);
        if (images.Length > 0)
            portraitImage = images[0];

        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);

        if (labels.Length > 0)
            speakerLabel = labels[0];

        if (labels.Length > 1)
            warningLabel = labels[1];
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (warningLabel == null)
            warningLabel = GetComponentInChildren<TMP_Text>(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        ApplyFallbackSpeaker();
    }

    private void Start()
    {
        ApplyUfoSpeakerFromDatabase();
    }

    private void Update()
    {
        if (canvasGroup == null)
            return;

        float targetAlpha = targetVisible ? 1f : 0f;

        canvasGroup.alpha =
            Mathf.MoveTowards(
                canvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void SetWarningVisible(bool visible, string text)
    {
        targetVisible = visible;

        if (warningLabel != null &&
            !string.IsNullOrWhiteSpace(text))
        {
            warningLabel.text = text;
        }
    }

    private void ApplyFallbackSpeaker()
    {
        if (speakerLabel != null)
        {
            speakerLabel.text = fallbackSpeakerName + ":";
            speakerLabel.color = fallbackSpeakerColor;
        }

        if (portraitImage != null)
            portraitImage.enabled = portraitImage.sprite != null;
    }

    private void ApplyUfoSpeakerFromDatabase()
    {
        Level1DialogueDatabase database =
            Level1DialogueDatabase.Instance;

        if (database == null)
            return;

        DialogueCharacter ufoCharacter =
            database.GetCharacter(
                DialogueIds.Characters.UFOTerminal
            );

        if (ufoCharacter == null)
            return;

        if (speakerLabel != null)
        {
            speakerLabel.text = GetShortSpeakerName(ufoCharacter.displayName) + ":";
            speakerLabel.color = ufoCharacter.nameColor;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = ufoCharacter.portrait;
            portraitImage.enabled = ufoCharacter.portrait != null;
        }
    }

    private string GetShortSpeakerName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return fallbackSpeakerName;

        if (displayName.StartsWith("UFO"))
            return "UFO";

        return displayName;
    }
}