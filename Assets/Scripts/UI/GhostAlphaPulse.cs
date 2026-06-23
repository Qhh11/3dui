using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class GhostAlphaPulse : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Visibility")]
    [SerializeField] private bool startHidden = true;

    [Header("Pulse")]
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.15f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.45f;
    [SerializeField, Min(0f)] private float pulseSpeed = 2f;
    [SerializeField] private bool useUnscaledTime;

    private readonly HashSet<Material> runtimeMaterials = new();

    private bool initialized;
    private bool visible;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private static readonly int SurfaceId =
        Shader.PropertyToID("_Surface");

    private static readonly int BlendId =
        Shader.PropertyToID("_Blend");

    private static readonly int SrcBlendId =
        Shader.PropertyToID("_SrcBlend");

    private static readonly int DstBlendId =
        Shader.PropertyToID("_DstBlend");

    private static readonly int SrcBlendAlphaId =
        Shader.PropertyToID("_SrcBlendAlpha");

    private static readonly int DstBlendAlphaId =
        Shader.PropertyToID("_DstBlendAlpha");

    private static readonly int ZWriteId =
        Shader.PropertyToID("_ZWrite");

    private void Awake()
    {
        Initialize();
        SetVisible(!startHidden);
    }

    private void Update()
    {
        if (!visible)
        {
            return;
        }

        float time = useUnscaledTime
            ? Time.unscaledTime
            : Time.time;

        float wave = (Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);

        ApplyAlpha(alpha);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void SetVisible(bool value)
    {
        Initialize();

        visible = value;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = value;
            }
        }

        if (value)
        {
            ApplyAlpha(minAlpha);
        }
    }

    public bool IsVisible()
    {
        return visible;
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers =
                GetComponentsInChildren<Renderer>(true);
        }

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            // materials 会为当前运行实例创建独立材质，
            // 不会改变 Project 中的共享材质。
            Material[] materials = targetRenderer.materials;

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                ConfigureTransparentMaterial(material);
                runtimeMaterials.Add(material);
            }
        }
    }

    private void ConfigureTransparentMaterial(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");

        if (material.HasProperty(SurfaceId))
        {
            material.SetFloat(SurfaceId, 1f);
        }

        if (material.HasProperty(BlendId))
        {
            material.SetFloat(BlendId, 0f);
        }

        if (material.HasProperty(SrcBlendId))
        {
            material.SetFloat(
                SrcBlendId,
                (float)BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty(DstBlendId))
        {
            material.SetFloat(
                DstBlendId,
                (float)BlendMode.OneMinusSrcAlpha
            );
        }

        if (material.HasProperty(SrcBlendAlphaId))
        {
            material.SetFloat(
                SrcBlendAlphaId,
                (float)BlendMode.One
            );
        }

        if (material.HasProperty(DstBlendAlphaId))
        {
            material.SetFloat(
                DstBlendAlphaId,
                (float)BlendMode.OneMinusSrcAlpha
            );
        }

        if (material.HasProperty(ZWriteId))
        {
            material.SetFloat(ZWriteId, 0f);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private void ApplyAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        foreach (Material material in runtimeMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(BaseColorId))
            {
                Color color = material.GetColor(BaseColorId);
                color.a = alpha;
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(ColorId))
            {
                Color color = material.GetColor(ColorId);
                color.a = alpha;
                material.SetColor(ColorId, color);
            }
        }
    }

    private void OnValidate()
    {
        minAlpha = Mathf.Clamp01(minAlpha);
        maxAlpha = Mathf.Clamp01(maxAlpha);

        if (maxAlpha < minAlpha)
        {
            maxAlpha = minAlpha;
        }

        pulseSpeed = Mathf.Max(0f, pulseSpeed);
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        runtimeMaterials.Clear();
    }
}