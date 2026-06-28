using UnityEngine;

public class TowerEnergyCoreSiphonPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private SiphonableObject siphonableObject;

    [Header("Collect Condition")]
    [SerializeField] private float collectLocalDistance = 0.6f;
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许被吸取拾取（空=无前置）。建议填塔 intro 结束后的 tower_core_quest，防止提前吸走核心。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮则不再允许拾取（可选）。")]
    [SerializeField] private string blockedByFlag = "";

    private bool hasCollected;

    private void Awake()
    {
        if (siphonableObject == null)
        {
            siphonableObject = GetComponent<SiphonableObject>();
        }
    }

    private void Update()
    {
        if (triggerOnlyOnce && hasCollected)
        {
            return;
        }

        if (!QuestProgress.Has(requiredFlag))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(blockedByFlag) && QuestProgress.Has(blockedByFlag))
        {
            return;
        }

        if (siphonController == null || siphonableObject == null)
        {
            return;
        }

        if (!siphonController.IsHolding(siphonableObject))
        {
            return;
        }

        if (transform.localPosition.magnitude > collectLocalDistance)
        {
            return;
        }

        Collect();
    }

    private void Collect()
    {
        if (triggerOnlyOnce && hasCollected)
        {
            return;
        }

        hasCollected = true;

        Debug.Log("Tower energy core collected by siphon.");

        siphonController.ConsumeHeldObject(siphonableObject, true);

        TowerFlowController.Instance?.PickUpEnergyCore();
    }
}
