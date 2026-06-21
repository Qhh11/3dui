using UnityEngine;

public class TowerEnergyCoreSiphonPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private SiphonableObject siphonableObject;

    [Header("Collect Condition")]
    [SerializeField] private float collectLocalDistance = 0.6f;
    [SerializeField] private bool triggerOnlyOnce = true;

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