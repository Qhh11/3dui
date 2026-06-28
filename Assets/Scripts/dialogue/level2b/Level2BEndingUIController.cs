using TMPro;
using UnityEngine;

public class Level2BEndingUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text endingText;

    [Header("Text")]
    [SerializeField]
    private string message =
        "Thanks for playing the demo.\nMore content is under development.";

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        HideInstant();
    }

    public void Show()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (endingText != null)
        {
            endingText.text = message;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void HideInstant()
    {
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
    }
}