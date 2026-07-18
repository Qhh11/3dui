using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectivePanelController :
    MonoBehaviour
{
    public static ObjectivePanelController Instance
    {
        get;
        private set;
    }

    [Header("UI")]
    [SerializeField]
    private CanvasGroup panelCanvasGroup;

    [SerializeField]
    private TMP_Text objectiveText;

    [SerializeField]
    private Image completedLine;

    [Header("Animation")]
    [SerializeField]
    private float fadeDuration = 0.25f;

    [SerializeField]
    private float completeLineDuration = 0.25f;

    [SerializeField]
    private float completeHoldDuration = 0.45f;

    private string currentObjective;
    private Coroutine currentRoutine;

    public string CurrentObjective =>
        currentObjective;

    public ObjectiveHintTarget CurrentHintTarget
    {
        get;
        private set;
    }

    public bool HasObjective =>
        !string.IsNullOrWhiteSpace(
            currentObjective
        );

    public bool IsTransitioning
    {
        get;
        private set;
    }

    public event Action<
        string,
        ObjectiveHintTarget> ObjectiveChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ResetCompletedVisual();

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    public void SetObjective(string objective)
    {
        SetObjective(objective, null);
    }

    public void SetObjective(
        string objective,
        ObjectiveHintTarget hintTarget)
    {
        StopCurrentRoutine();

        currentRoutine = StartCoroutine(
            SetObjectiveRoutine(
                objective,
                hintTarget
            )
        );
    }

    public void CompleteCurrentObjective()
    {
        if (!HasObjective)
            return;

        StopCurrentRoutine();

        // 立即取消旧目标，避免淡出期间仍能点击 Hint。
        SetObjectiveData(
            currentObjective,
            null
        );

        currentRoutine = StartCoroutine(
            CompleteCurrentObjectiveRoutine()
        );
    }

    public void CompleteAndSetNextObjective(
        string nextObjective)
    {
        CompleteAndSetNextObjective(
            nextObjective,
            null
        );
    }

    public void CompleteAndSetNextObjective(
        string nextObjective,
        ObjectiveHintTarget nextHintTarget)
    {
        gameObject.SetActive(true);
        StopCurrentRoutine();

        // 旧目标从开始完成动画时就不可 Hint。
        SetObjectiveData(
            currentObjective,
            null
        );

        currentRoutine = StartCoroutine(
            CompleteAndSetNextRoutine(
                nextObjective,
                nextHintTarget
            )
        );
    }

    private IEnumerator SetObjectiveRoutine(
        string objective,
        ObjectiveHintTarget hintTarget)
    {
        IsTransitioning = true;

        SetObjectiveData(
            objective,
            hintTarget
        );

        ApplyObjectiveText(objective);
        ResetCompletedVisual();

        yield return FadePanel(1f);

        IsTransitioning = false;
        currentRoutine = null;
    }

    private IEnumerator
        CompleteCurrentObjectiveRoutine()
    {
        IsTransitioning = true;

        yield return PlayCompleteAnimation();
        yield return FadePanel(0f);

        SetObjectiveData(
            string.Empty,
            null
        );

        ApplyObjectiveText(string.Empty);

        IsTransitioning = false;
        currentRoutine = null;
    }

    private IEnumerator CompleteAndSetNextRoutine(
        string nextObjective,
        ObjectiveHintTarget nextHintTarget)
    {
        IsTransitioning = true;

        if (!string.IsNullOrWhiteSpace(
                currentObjective))
        {
            yield return PlayCompleteAnimation();
            yield return FadePanel(0f);
        }

        SetObjectiveData(
            nextObjective,
            nextHintTarget
        );

        ApplyObjectiveText(nextObjective);
        ResetCompletedVisual();

        yield return FadePanel(1f);

        IsTransitioning = false;
        currentRoutine = null;
    }

    private void SetObjectiveData(
        string objective,
        ObjectiveHintTarget hintTarget)
    {
        currentObjective =
            objective ?? string.Empty;

        CurrentHintTarget = hintTarget;

        ObjectiveChanged?.Invoke(
            currentObjective,
            CurrentHintTarget
        );
    }

    private void ApplyObjectiveText(
        string objective)
    {
        if (objectiveText == null)
            return;

        objectiveText.text =
            objective ?? string.Empty;

        objectiveText.fontStyle &=
            ~FontStyles.Strikethrough;
    }

    private void ResetCompletedVisual()
    {
        if (completedLine == null)
            return;

        completedLine.gameObject.SetActive(false);
        completedLine.fillAmount = 0f;
    }

    private void StopCurrentRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        IsTransitioning = false;
    }

    private IEnumerator PlayCompleteAnimation()
    {
        if (objectiveText != null)
        {
            objectiveText.fontStyle |=
                FontStyles.Strikethrough;
        }

        if (completedLine != null)
        {
            completedLine.gameObject.SetActive(true);
            completedLine.fillAmount = 0f;

            float timer = 0f;

            while (timer < completeLineDuration)
            {
                timer += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.0001f,
                        completeLineDuration
                    )
                );

                completedLine.fillAmount = t;
                yield return null;
            }

            completedLine.fillAmount = 1f;
        }

        yield return new WaitForSecondsRealtime(
            completeHoldDuration
        );
    }

    private IEnumerator FadePanel(
        float targetAlpha)
    {
        if (panelCanvasGroup == null)
            yield break;

        float startAlpha =
            panelCanvasGroup.alpha;

        float timer = 0f;

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer /
                Mathf.Max(
                    0.0001f,
                    fadeDuration
                )
            );

            panelCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
    }
}