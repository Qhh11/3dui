using UnityEngine;
using UnityEngine.EventSystems;

public class MarkInstance :
    MonoBehaviour,
    IPointerClickHandler
{
    public enum MarkState
    {
        Selector,
        Launching,
        Deployed,
        Returning
    }

    [Header("Home")]
    [Tooltip("This Mark's own home anchor under the UFO camera.")]
    [SerializeField] private Transform homeAnchor;

    [Header("References")]
    [Tooltip("Prefer a child visual object, not this root Transform.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Renderer[] markRenderers;
    [SerializeField] private Collider[] markColliders;

    [Header("Layers")]
    [SerializeField] private string selectorLayerName =
        "MarkSelector3D";

    [SerializeField] private string deployedLayerName =
        "UfoMark";

    [SerializeField] private string transitLayerName =
        "Ignore Raycast";

    [Header("Selector Visual")]
    [SerializeField] private float unselectedAlpha =
        0.7f;

    [SerializeField] private float selectedAlpha =
        1f;

    [SerializeField] private float selectedScale =
        1.1f;

    private MarkController controller;
    private int slotIndex;
    private Color markColor = Color.white;

    private MarkState state =
        MarkState.Selector;

    private Transform attachedTarget;
    private Vector3 attachedLocalPoint;
    private Vector3 launchTargetPosition;

    private float launchSpeed;
    private float returnSpeed;
    private float transitionDuration = 0.08f;
    private float aimedScale = 1.12f;

    private bool initialized;
    private bool selectorVisible = true;
    private bool selectorInputEnabled = true;
    private bool selected;
    private bool aimed;

    private Vector3 homeLocalPosition;
    private Quaternion homeLocalRotation;
    private Vector3 homeLocalScale;
    private Vector3 baseVisualLocalScale;

    private float currentAlpha = 0.7f;
    private float currentScale = 1f;

    private MaterialPropertyBlock propertyBlock;

    public MarkState State => state;

    public bool IsDeployed =>
        state == MarkState.Deployed;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        UpdateMovement();
        UpdateVisual();
    }

    public void Bind(
        MarkController owner,
        int index,
        Color color,
        float newTransitionDuration,
        float newAimedScale)
    {
        EnsureInitialized();

        controller = owner;
        slotIndex = index;
        markColor = color;

        transitionDuration =
            newTransitionDuration;

        aimedScale =
            newAimedScale;

        state = MarkState.Selector;
        selected = false;
        aimed = false;
        attachedTarget = null;

        ReturnToHomeImmediately();
        SetLayerRecursively(selectorLayerName);
        RefreshPresentation();
        ApplyVisual();
    }

    public void SetSelectorPresentation(
        bool visible,
        bool inputEnabled)
    {
        selectorVisible = visible;
        selectorInputEnabled = inputEnabled;

        RefreshPresentation();
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    public void SetAimed(bool value)
    {
        aimed =
            value &&
            state == MarkState.Deployed;
    }

    public bool Launch(
        Vector3 targetPosition,
        Transform targetTransform,
        float newLaunchSpeed,
        float newReturnSpeed)
    {
        if (state != MarkState.Selector)
            return false;

        EnsureInitialized();

        launchTargetPosition =
            targetPosition;

        attachedTarget =
            targetTransform;

        if (attachedTarget != null)
        {
            attachedLocalPoint =
                attachedTarget.InverseTransformPoint(
                    launchTargetPosition
                );
        }

        launchSpeed = newLaunchSpeed;
        returnSpeed = newReturnSpeed;

        selected = false;
        aimed = false;

        // Keep the current world position. The Sphere starts flying
        // directly from its screen-bottom selector position.
        transform.SetParent(null, true);

        state = MarkState.Launching;

        SetLayerRecursively(
            transitLayerName
        );

        RefreshPresentation();

        return true;
    }

    public bool BeginReturn()
    {
        if (state != MarkState.Deployed)
            return false;

        aimed = false;
        attachedTarget = null;
        state = MarkState.Returning;

        SetLayerRecursively(
            transitLayerName
        );

        RefreshPresentation();

        return true;
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (state != MarkState.Selector ||
            !selectorVisible ||
            !selectorInputEnabled)
        {
            return;
        }

        controller?.SelectSlot(slotIndex);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (homeAnchor == null)
            homeAnchor = transform.parent;

        if (visualRoot == null)
            visualRoot = transform;

        if (markRenderers == null ||
            markRenderers.Length == 0)
        {
            markRenderers =
                GetComponentsInChildren<Renderer>(
                    true
                );
        }

        if (markColliders == null ||
            markColliders.Length == 0)
        {
            markColliders =
                GetComponentsInChildren<Collider>(
                    true
                );
        }

        if (homeAnchor != null &&
            transform.parent != homeAnchor)
        {
            transform.SetParent(
                homeAnchor,
                true
            );
        }

        homeLocalPosition =
            transform.localPosition;

        homeLocalRotation =
            transform.localRotation;

        homeLocalScale =
            transform.localScale;

        baseVisualLocalScale =
            visualRoot.localScale;

        propertyBlock =
            new MaterialPropertyBlock();

        initialized = true;
    }

    private void UpdateMovement()
    {
        if (state == MarkState.Launching)
        {
            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    launchTargetPosition,
                    launchSpeed * Time.deltaTime
                );

            if (Vector3.Distance(
                    transform.position,
                    launchTargetPosition) <= 0.01f)
            {
                transform.position =
                    launchTargetPosition;

                state = MarkState.Deployed;

                SetLayerRecursively(
                    deployedLayerName
                );

                RefreshPresentation();

                controller
                    ?.NotifyDeploymentCompleted(
                        slotIndex,
                        this
                    );
            }

            return;
        }

        if (state == MarkState.Deployed)
        {
            if (attachedTarget != null)
            {
                transform.position =
                    attachedTarget.TransformPoint(
                        attachedLocalPoint
                    );
            }

            return;
        }

        if (state == MarkState.Returning)
        {
            if (homeAnchor == null)
            {
                CompleteReturn();
                return;
            }

            Vector3 targetPosition =
                homeAnchor.TransformPoint(
                    homeLocalPosition
                );

            transform.position =
                Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    returnSpeed * Time.deltaTime
                );

            if (Vector3.Distance(
                    transform.position,
                    targetPosition) <= 0.01f)
            {
                CompleteReturn();
            }
        }
    }

    private void CompleteReturn()
    {
        state = MarkState.Selector;
        aimed = false;
        attachedTarget = null;

        ReturnToHomeImmediately();

        SetLayerRecursively(
            selectorLayerName
        );

        RefreshPresentation();

        controller?.NotifyReturnCompleted(
            slotIndex,
            this
        );
    }

    private void ReturnToHomeImmediately()
    {
        if (homeAnchor == null)
            return;

        transform.SetParent(
            homeAnchor,
            false
        );

        transform.localPosition =
            homeLocalPosition;

        transform.localRotation =
            homeLocalRotation;

        transform.localScale =
            homeLocalScale;
    }

    private void UpdateVisual()
    {
        float targetAlpha;
        float targetScale;

        if (state == MarkState.Selector)
        {
            targetAlpha =
                selected
                    ? selectedAlpha
                    : unselectedAlpha;

            targetScale =
                selected
                    ? selectedScale
                    : 1f;
        }
        else if (state == MarkState.Deployed)
        {
            targetAlpha = 1f;

            targetScale =
                aimed
                    ? aimedScale
                    : 1f;
        }
        else if (state == MarkState.Returning)
        {
            targetAlpha =
                unselectedAlpha;

            targetScale = 1f;
        }
        else
        {
            targetAlpha = 1f;
            targetScale = 1f;
        }

        float alphaDistance =
            Mathf.Abs(
                selectedAlpha -
                unselectedAlpha
            );

        float scaleDistance =
            Mathf.Max(
                Mathf.Abs(
                    selectedScale - 1f
                ),
                Mathf.Abs(
                    aimedScale - 1f
                )
            );

        float alphaSpeed =
            transitionDuration <= 0f
                ? float.MaxValue
                : alphaDistance /
                  transitionDuration;

        float scaleSpeed =
            transitionDuration <= 0f
                ? float.MaxValue
                : scaleDistance /
                  transitionDuration;

        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            alphaSpeed * Time.unscaledDeltaTime
        );

        currentScale = Mathf.MoveTowards(
            currentScale,
            targetScale,
            scaleSpeed * Time.unscaledDeltaTime
        );

        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (visualRoot != null)
        {
            visualRoot.localScale =
                baseVisualLocalScale *
                currentScale;
        }

        if (markRenderers == null)
            return;

        Color appliedColor = markColor;
        appliedColor.a = currentAlpha;

        foreach (Renderer targetRenderer
                 in markRenderers)
        {
            if (targetRenderer == null)
                continue;

            propertyBlock.Clear();

            targetRenderer.GetPropertyBlock(
                propertyBlock
            );

            propertyBlock.SetColor(
                "_BaseColor",
                appliedColor
            );

            propertyBlock.SetColor(
                "_Color",
                appliedColor
            );

            targetRenderer.SetPropertyBlock(
                propertyBlock
            );
        }
    }

    private void RefreshPresentation()
    {
        bool shouldRender =
            state != MarkState.Selector ||
            selectorVisible;

        bool shouldReceiveInput =
            state == MarkState.Deployed ||
            (state == MarkState.Selector &&
             selectorVisible &&
             selectorInputEnabled);

        SetRenderersEnabled(
            shouldRender
        );

        SetCollidersEnabled(
            shouldReceiveInput
        );
    }

    private void SetRenderersEnabled(
        bool value)
    {
        if (markRenderers == null)
            return;

        foreach (Renderer targetRenderer
                 in markRenderers)
        {
            if (targetRenderer != null)
                targetRenderer.enabled = value;
        }
    }

    private void SetCollidersEnabled(
        bool value)
    {
        if (markColliders == null)
            return;

        foreach (Collider targetCollider
                 in markColliders)
        {
            if (targetCollider != null)
                targetCollider.enabled = value;
        }
    }

    private void SetLayerRecursively(
        string layerName)
    {
        int layer =
            LayerMask.NameToLayer(layerName);

        if (layer < 0)
        {
            Debug.LogWarning(
                $"Layer not found: {layerName}",
                this
            );

            return;
        }

        SetLayerRecursively(
            transform,
            layer
        );
    }

    private static void SetLayerRecursively(
        Transform root,
        int layer)
    {
        root.gameObject.layer = layer;

        for (int i = 0;
             i < root.childCount;
             i++)
        {
            SetLayerRecursively(
                root.GetChild(i),
                layer
            );
        }
    }
}