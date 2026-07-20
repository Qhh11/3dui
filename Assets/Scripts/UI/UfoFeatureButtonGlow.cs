using UnityEngine;

public enum UfoFeatureType
{
    None = 0,
    Siphon = 1,
    Rocket = 2,
    Mark = 3,
    Melt = 4
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
        {
            glowObject.SetActive(glowing);
        }
    }

    private void OnDisable()
    {
        SetGlowing(false);
    }
}