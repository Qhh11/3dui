using UnityEngine;

public class ReturnToPlayerPerspectiveAction : MonoBehaviour
{
    [Header("Objective")]
    [SerializeField] private bool setNextObjective = true;
    [SerializeField] private string nextObjectiveText = "Investigate the strange tent";

    private bool hasTriggered;

    public void Trigger()
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;

        if (ObjectivePanelController.Instance != null)
        {
            if (setNextObjective)
            {
                ObjectivePanelController.Instance.CompleteAndSetNextObjective(nextObjectiveText);
            }
            else
            {
                ObjectivePanelController.Instance.CompleteCurrentObjective();
            }
        }

        SwitchPerspectiveButtonController.Instance?.EnableFreeSwitching();
    }
}