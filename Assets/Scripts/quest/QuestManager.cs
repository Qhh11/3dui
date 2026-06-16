using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup objectiveCanvasGroup;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Wayfinding")]
    [SerializeField] private WayfindingManager wayfindingManager;

    public string CurrentObjectiveId { get; private set; }
    public Transform CurrentTarget { get; private set; }

    public UnityEvent<string> OnObjectiveChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        HideObjective();
    }

    public void SetObjective(string objectiveId, string text, Transform target)
    {
        CurrentObjectiveId = objectiveId;
        CurrentTarget = target;

        if (objectiveText != null)
            objectiveText.text = text;

        SetObjectiveVisible(!string.IsNullOrWhiteSpace(text));

        if (wayfindingManager == null)
            wayfindingManager = WayfindingManager.Instance;

        if (wayfindingManager != null)
            wayfindingManager.SetTarget(target);

        OnObjectiveChanged?.Invoke(objectiveId);
        Debug.Log($"Objective changed: {objectiveId} -> {text}");
    }

    public void CompleteObjective(string objectiveId)
    {
        if (!string.IsNullOrWhiteSpace(objectiveId) && objectiveId != CurrentObjectiveId)
            return;

        HideObjective();
    }

    public void HideObjective()
    {
        CurrentObjectiveId = null;
        CurrentTarget = null;

        if (objectiveText != null)
            objectiveText.text = string.Empty;

        SetObjectiveVisible(false);

        if (wayfindingManager == null)
            wayfindingManager = WayfindingManager.Instance;

        if (wayfindingManager != null)
            wayfindingManager.SetTarget(null);
    }

    private void SetObjectiveVisible(bool visible)
    {
        if (objectiveCanvasGroup == null) return;

        objectiveCanvasGroup.alpha = visible ? 1f : 0f;
        objectiveCanvasGroup.interactable = visible;
        objectiveCanvasGroup.blocksRaycasts = visible;
    }
}
