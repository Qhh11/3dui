using UnityEngine;
using UnityEngine.Events;

public class Level2HeatMeltableObject : MonoBehaviour
{
    public enum OutlineState
    {
        Default,
        UfoIdle,
        UfoSelected
    }

    [Header("Rendering Layer Masks")]
    [SerializeField] private uint defaultMask = 1u << 0;
    [SerializeField] private uint ufoIdleMask = 1u << 2;
    [SerializeField] private uint ufoSelectedMask = 1u << 1;

    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Emission")]
    [SerializeField] private string emissionColorProperty = "_EmissionColor";

    [ColorUsage(false, true)]
    [SerializeField] private Color coldEmissionColor = new Color(1f / 255f, 95f / 255f, 255f / 255f, 1f);

    [ColorUsage(false, true)]
    [SerializeField] private Color hotEmissionColor = new Color(1f, 0.12f, 0f, 1f);

    [Tooltip("Matches Unity HDR Color Picker Intensity. 0 = x1, +1 = x2, -1 = x0.5.")]
    [SerializeField] private float coldEmissionExposure = 0f;

    [Tooltip("Matches Unity HDR Color Picker Intensity. 0 = x1, +1 = x2, -1 = x0.5.")]
    [SerializeField] private float hotEmissionExposure = 0f;

    [SerializeField, Range(0f, 1f)] private float meltProgress = 0f;

    [Header("Events")]
    public UnityEvent onMeltCompleted;

    private bool isCompleted;
    private MaterialPropertyBlock propertyBlock;

    public bool IsCompleted => isCompleted;
    public float MeltProgress => meltProgress;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        propertyBlock = new MaterialPropertyBlock();

        EnableEmissionKeyword();

        SetOutlineState(OutlineState.Default);
        ApplyEmissionColor();
    }

    public void AddHeat(float amount)
    {
        if (isCompleted)
        {
            return;
        }

        meltProgress = Mathf.Clamp01(meltProgress + amount);

        ApplyEmissionColor();

        if (meltProgress >= 1f)
        {
            CompleteMelt();
        }
    }

    public void SetMeltProgress(float progress)
    {
        if (isCompleted)
        {
            return;
        }

        meltProgress = Mathf.Clamp01(progress);

        ApplyEmissionColor();

        if (meltProgress >= 1f)
        {
            CompleteMelt();
        }
    }

    public void SetOutlineState(OutlineState state)
    {
        uint targetMask = defaultMask;

        switch (state)
        {
            case OutlineState.Default:
                targetMask = defaultMask;
                break;

            case OutlineState.UfoIdle:
                targetMask = ufoIdleMask;
                break;

            case OutlineState.UfoSelected:
                targetMask = ufoSelectedMask;
                break;
        }

        if (targetRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.renderingLayerMask = targetMask;
            }
        }
    }

    private void ApplyEmissionColor()
    {
        if (targetRenderers == null)
        {
            return;
        }

        Color emissionColor = Color.Lerp(
            coldEmissionColor,
            hotEmissionColor,
            meltProgress
        );

        float emissionExposure = Mathf.Lerp(
            coldEmissionExposure,
            hotEmissionExposure,
            meltProgress
        );

        // Unity HDR Color Picker Intensity works like exposure:
        // 0 = x1, +1 = x2, +2 = x4, -1 = x0.5
        float emissionMultiplier = Mathf.Pow(2f, emissionExposure);

        Color finalEmissionColor = emissionColor * emissionMultiplier;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(emissionColorProperty, finalEmissionColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void EnableEmissionKeyword()
    {
        if (targetRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            Material[] materials = targetRenderer.materials;

            foreach (Material material in materials)
            {
                if (material != null)
                {
                    material.EnableKeyword("_EMISSION");
                }
            }
        }
    }

    private void CompleteMelt()
    {
        if (isCompleted)
        {
            return;
        }

        isCompleted = true;

        SetOutlineState(OutlineState.Default);

        onMeltCompleted?.Invoke();
    }
}