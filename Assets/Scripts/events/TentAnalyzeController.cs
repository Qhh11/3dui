using UnityEngine;
using UnityEngine.UI;

public class TentAnalyzeController : MonoBehaviour
{
    [Header("Tent UI")]
    [SerializeField] private GameObject tentHighlightObject;
    [SerializeField] private GameObject analyzeButtonRoot;
    [SerializeField] private Button analyzeButton;

    [Header("Dialogue")]
    [SerializeField] private string analyzedSequenceId = "level1_tent_analyzed";

    [Header("Objective")]
    [SerializeField] private string analyzeObjectiveText = "Select the tent to analyze";

    private bool hasAnalyzed;

    private void Start()
    {
        SetAnalyzeUiVisible(false);

        if (analyzeButton != null)
        {
            analyzeButton.onClick.AddListener(OnAnalyzeButtonClicked);
        }
    }

    public void ShowAnalyzeTent()
    {
        if (hasAnalyzed)
        {
            return;
        }

        SetAnalyzeUiVisible(true);

        if (ObjectivePanelController.Instance != null)
        {
            ObjectivePanelController.Instance.CompleteAndSetNextObjective(analyzeObjectiveText);
        }
    }

    private void OnAnalyzeButtonClicked()
    {
        if (hasAnalyzed)
        {
            return;
        }

        hasAnalyzed = true;

        SetAnalyzeUiVisible(false);

        DialogueSequence sequence = Level1DialogueDatabase.Instance.GetSequence(analyzedSequenceId);

        if (sequence == null)
        {
            Debug.LogWarning($"Dialogue sequence not found: {analyzedSequenceId}");
            return;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    private void SetAnalyzeUiVisible(bool visible)
    {
        if (tentHighlightObject != null)
        {
            tentHighlightObject.SetActive(visible);
        }

        if (analyzeButtonRoot != null)
        {
            analyzeButtonRoot.SetActive(visible);
        }

        if (analyzeButton != null)
        {
            analyzeButton.interactable = visible;
        }
    }
}