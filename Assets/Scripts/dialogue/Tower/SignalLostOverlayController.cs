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
    [SerializeField] private float holdDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private void Start()
    {
        if (signalText != null)
        {
            signalText.text = signalLostText;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator PlayRoutine()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        yield return Fade(1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(0f, fadeOutDuration);
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}