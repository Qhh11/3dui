using System.Collections;
using TMPro;
using UnityEngine;

public class SignalLostOverlayController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text signalText;

    [Header("Text")]
    [SerializeField] private string signalLostText = "SIGNAL LOST";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Visibility")]
    [SerializeField] private bool deactivateWhenHidden = true;

    private void Awake()
    {
        InitializeIfNeeded();
        HideInstant();
    }

    public IEnumerator PlayRoutine()
    {
        InitializeIfNeeded();

        gameObject.SetActive(true);

        if (signalText != null)
        {
            signalText.text = signalLostText;
        }

        SetInteractable(false);

        canvasGroup.alpha = 0f;

        yield return FadeTo(1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return FadeTo(0f, fadeOutDuration);

        HideInstant();
    }

    public void ShowInstant()
    {
        InitializeIfNeeded();

        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        SetInteractable(false);
    }

    public void HideInstant()
    {
        InitializeIfNeeded();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            SetInteractable(false);
        }

        if (deactivateWhenHidden)
        {
            gameObject.SetActive(false);
        }
    }

    private void InitializeIfNeeded()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (signalText != null)
        {
            signalText.text = signalLostText;
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void SetInteractable(bool interactable)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }
}