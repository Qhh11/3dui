using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SiphonableObject : MonoBehaviour
{
    public enum OutlineState
    {
        Default,
        UfoIdle,
        UfoSelected
    }

    [Header("Rendering Layer Masks")]
    [SerializeField] private uint defaultMask = 1u << 0;
    [SerializeField] private uint ufoIdleMask = 1u << 2;      // Outline_2
    [SerializeField] private uint ufoSelectedMask = 1u << 1;  // Outline_1

    [Header("Renderers")]
    [SerializeField] private Renderer[] outlineRenderers;

    [Header("Place")]
    [SerializeField] private float skinWidth = 0.03f;

    [Header("Events")]
    public UnityEvent onPickedUp;
    public UnityEvent onPlaced;
    public UnityEvent onPlaceFailed;

    private Rigidbody rb;
    private Transform originalParent;

    public float SkinWidth => skinWidth;
    public Rigidbody Rigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (outlineRenderers == null || outlineRenderers.Length == 0)
            outlineRenderers = GetComponentsInChildren<Renderer>(true);

        SetOutlineState(OutlineState.Default);
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

        if (outlineRenderers == null)
            return;

        foreach (Renderer r in outlineRenderers)
        {
            if (r != null)
                r.renderingLayerMask = targetMask;
        }
    }

    public void PickUp(Transform holdPoint)
    {
        originalParent = transform.parent;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        transform.SetParent(holdPoint, true);
        SetOutlineState(OutlineState.Default);

        onPickedUp?.Invoke();
    }

    public void Place(Vector3 position, Quaternion rotation)
    {
        transform.SetParent(originalParent, true);
        transform.SetPositionAndRotation(position, rotation);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.useGravity = true;
        rb.isKinematic = false;

        StartCoroutine(FreezeAfterSettle(20f));

        onPlaced?.Invoke();
    }

    private System.Collections.IEnumerator FreezeAfterSettle(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    public void NotifyPlaceFailed()
    {
        onPlaceFailed?.Invoke();
    }
}