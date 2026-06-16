using UnityEngine;

public class ObjectiveEventAction : MonoBehaviour
{
    [Header("Objective")]
    [SerializeField] private string objectiveText;

    [Header("Wayfinding")]
    [SerializeField] private Transform wayfindingTarget;

    [Header("Behavior")]
    [SerializeField] private bool completePreviousObjective = true;

    public void Apply()
    {
        if (ObjectivePanelController.Instance == null)
        {
            Debug.LogWarning("ObjectivePanelController is missing.");
            return;
        }

        if (completePreviousObjective)
        {
            ObjectivePanelController.Instance.CompleteAndSetNextObjective(objectiveText);
        }
        else
        {
            ObjectivePanelController.Instance.SetObjective(objectiveText);
        }

        //if (WayfindingManager.Instance != null && wayfindingTarget != null)
        //{
        //    WayfindingManager.Instance.SetTarget(wayfindingTarget);
        //}
    }

    public void CompleteOnly()
    {
        if (ObjectivePanelController.Instance == null)
        {
            Debug.LogWarning("ObjectivePanelController is missing.");
            return;
        }

        ObjectivePanelController.Instance.CompleteCurrentObjective();
    }
}