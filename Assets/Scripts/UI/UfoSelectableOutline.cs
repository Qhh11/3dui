using UnityEngine;

[DisallowMultipleComponent]
public class UfoSelectableOutline : MonoBehaviour
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
    [SerializeField] private Renderer[] outlineRenderers;

    private void Awake()
    {
        EnsureRenderers();
        SetOutlineState(OutlineState.Default);
    }

    public void SetOutlineState(OutlineState state)
    {
        EnsureRenderers();

        uint targetMask = defaultMask;

        if (state == OutlineState.UfoIdle)
            targetMask = ufoIdleMask;
        else if (state == OutlineState.UfoSelected)
            targetMask = ufoSelectedMask;

        if (outlineRenderers == null)
            return;

        foreach (Renderer target in outlineRenderers)
        {
            if (target != null)
                target.renderingLayerMask = targetMask;
        }
    }

    private void EnsureRenderers()
    {
        if (outlineRenderers == null || outlineRenderers.Length == 0)
            outlineRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnDisable()
    {
        SetOutlineState(OutlineState.Default);
    }
}
