using UnityEngine;

public class WayfindingManager : MonoBehaviour
{
    public static WayfindingManager Instance { get; private set; }

    [Header("Proxy target")]
    [Tooltip("Assign this proxy as the target of HologramPath, CompassBarElement, and GiantArrowCompass.")]
    [SerializeField] private Transform targetProxy;

    [Header("Visual roots")]
    [SerializeField] private GameObject[] wayfindingVisuals;

    private Transform activeTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void LateUpdate()
    {
        if (activeTarget != null && targetProxy != null)
            targetProxy.position = activeTarget.position;
    }

    public void SetTarget(Transform target)
    {
        activeTarget = target;

        bool visible = activeTarget != null;
        foreach (GameObject visual in wayfindingVisuals)
        {
            if (visual != null)
                visual.SetActive(visible);
        }

        if (visible && targetProxy != null)
            targetProxy.position = activeTarget.position;
    }
}
