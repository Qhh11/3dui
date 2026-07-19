using UnityEngine;

public enum UfoFeatureType
{
    None,
    Siphon,
    Rocket,
    Analyze,
    Melt
}

public class UfoFeatureButtonGlow : MonoBehaviour
{
    [SerializeField] private UfoFeatureType featureType;
    [SerializeField] private GameObject glowObject;

    public UfoFeatureType FeatureType => featureType;

    private void Awake()
    {
        SetGlowing(false);
    }

    public void SetGlowing(bool glowing)
    {
        if (glowObject != null)
            glowObject.SetActive(glowing);
    }

    private void OnDisable()
    {
        SetGlowing(false);
    }
}
