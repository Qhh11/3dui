using System.Collections;
using UnityEngine;

public class ScreenFadeController : MonoBehaviour
{
    public static ScreenFadeController Instance { get; private set; }

    [Header("Fade UI")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Wake Up")]
    [SerializeField] private float wakeUpDelay = 0.4f;
    [SerializeField] private float wakeUpFadeDuration = 2.0f;
    [SerializeField] private bool startBlack = true;

    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (fadeCanvasGroup == null)
        {
            return;
        }

        fadeCanvasGroup.alpha = startBlack ? 1f : 0f;
        fadeCanvasGroup.blocksRaycasts = startBlack;
        fadeCanvasGroup.interactable = false;
    }

    public void PlayWakeUpFade()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(WakeUpRoutine());
    }

    public void FadeToBlack(float duration)
    {
        StartFade(1f, duration, true);
    }

    public void FadeFromBlack(float duration)
    {
        StartFade(0f, duration, false);
    }

    private void StartFade(float targetAlpha, float duration, bool blockRaycasts)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration, blockRaycasts));
    }

    private IEnumerator WakeUpRoutine()
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        yield return new WaitForSecondsRealtime(wakeUpDelay);

        yield return FadeRoutine(0f, wakeUpFadeDuration, false);
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool blockRaycasts)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        fadeCanvasGroup.blocksRaycasts = true;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = blockRaycasts;
    }
}
