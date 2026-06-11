using System.Collections;
using UnityEngine;

public class UIToggleFade : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 0.3f;
    public bool interactImmediatelyWhenShowing = true;

    Coroutine fadeCoroutine;

    public void ToggleUI()
    {
        float targetAlpha = canvasGroup.alpha > 0.5f ? 0f : 1f;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeTo(targetAlpha));
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        canvasGroup.gameObject.SetActive(true);

        if (targetAlpha == 0f)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (interactImmediatelyWhenShowing)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        bool visible = targetAlpha > 0.9f;

        if (visible && !interactImmediatelyWhenShowing)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (!visible)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        fadeCoroutine = null;
    }
}