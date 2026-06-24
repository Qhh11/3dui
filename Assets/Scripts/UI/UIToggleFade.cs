using System.Collections;
using UnityEngine;

public class UIToggleFade : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.3f;
    public bool interactImmediatelyWhenShowing = true;

    private Coroutine fadeCoroutine;
    private bool targetVisible;

    private void Awake()
    {
        if (canvasGroup == null)
            return;

        targetVisible = canvasGroup.alpha > 0.5f;
        SetInteraction(targetVisible);
    }

    public void ToggleUI()
    {
        SetVisible(!targetVisible);
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        targetVisible = visible;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (fadeDuration <= 0f)
        {
            SetVisibleImmediate(visible);
            return;
        }

        canvasGroup.gameObject.SetActive(true);
        fadeCoroutine = StartCoroutine(FadeTo(visible));
    }

    public void SetVisibleImmediate(bool visible)
    {
        if (canvasGroup == null)
            return;

        targetVisible = visible;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = visible ? 1f : 0f;
        SetInteraction(visible);
    }

    private IEnumerator FadeTo(bool visible)
    {
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;
        float time = 0f;

        if (!visible)
        {
            SetInteraction(false);
        }
        else if (interactImmediatelyWhenShowing)
        {
            SetInteraction(true);
        }

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                time / fadeDuration
            );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        SetInteraction(visible);
        fadeCoroutine = null;
    }

    private void SetInteraction(bool interactable)
    {
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }
}