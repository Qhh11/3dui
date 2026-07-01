using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectivePanelController : MonoBehaviour
{
    public static ObjectivePanelController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private Image completedLine;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float completeLineDuration = 0.25f;
    [SerializeField] private float completeHoldDuration = 0.45f;

    private string currentObjective;
    private Coroutine currentRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (completedLine != null)
        {
            completedLine.gameObject.SetActive(false);
            completedLine.fillAmount = 0f;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    public void SetObjective(string objective)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(SetObjectiveRoutine(objective));
    }

    public void CompleteCurrentObjective()
    {
        if (string.IsNullOrWhiteSpace(currentObjective))
        {
            return;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(CompleteCurrentObjectiveRoutine());
    }

    public void CompleteAndSetNextObjective(string nextObjective)
    {
        gameObject.SetActive(true);
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(CompleteAndSetNextRoutine(nextObjective));
    }

    private IEnumerator SetObjectiveRoutine(string objective)
    {
        currentObjective = objective;

        if (objectiveText != null)
        {
            objectiveText.text = objective;
            objectiveText.fontStyle &= ~FontStyles.Strikethrough;
        }

        if (completedLine != null)
        {
            completedLine.gameObject.SetActive(false);
            completedLine.fillAmount = 0f;
        }

        yield return FadePanel(1f);
        currentRoutine = null;
    }

    private IEnumerator CompleteCurrentObjectiveRoutine()
    {
        yield return PlayCompleteAnimation();
        yield return FadePanel(0f);

        currentObjective = string.Empty;

        if (objectiveText != null)
        {
            objectiveText.text = string.Empty;
            objectiveText.fontStyle &= ~FontStyles.Strikethrough;
        }

        currentRoutine = null;
    }

    private IEnumerator CompleteAndSetNextRoutine(string nextObjective)
    {
        if (!string.IsNullOrWhiteSpace(currentObjective))
        {
            yield return PlayCompleteAnimation();
            yield return FadePanel(0f);
        }

        currentObjective = nextObjective;

        if (objectiveText != null)
        {
            objectiveText.text = nextObjective;
            objectiveText.fontStyle &= ~FontStyles.Strikethrough;
        }

        if (completedLine != null)
        {
            completedLine.gameObject.SetActive(false);
            completedLine.fillAmount = 0f;
        }

        yield return FadePanel(1f);
        currentRoutine = null;
    }

    private IEnumerator PlayCompleteAnimation()
    {
        if (objectiveText != null)
        {
            objectiveText.fontStyle |= FontStyles.Strikethrough;
        }

        if (completedLine != null)
        {
            completedLine.gameObject.SetActive(true);
            completedLine.fillAmount = 0f;

            float timer = 0f;

            while (timer < completeLineDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / completeLineDuration);
                completedLine.fillAmount = t;
                yield return null;
            }

            completedLine.fillAmount = 1f;
        }

        yield return new WaitForSecondsRealtime(completeHoldDuration);
    }

    private IEnumerator FadePanel(float targetAlpha)
    {
        if (panelCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = panelCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
    }
}
