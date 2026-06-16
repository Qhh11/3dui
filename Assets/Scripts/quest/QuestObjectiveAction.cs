using UnityEngine;

public class QuestObjectiveAction : MonoBehaviour
{
    [SerializeField] private string objectiveId;

    [TextArea(2, 4)]
    [SerializeField] private string objectiveText;

    [SerializeField] private Transform target;

    public void Apply()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestManager is missing from the scene.");
            return;
        }

        QuestManager.Instance.SetObjective(objectiveId, objectiveText, target);
    }
}
