using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SiphonableObject : MonoBehaviour
{
    [Header("Outline")]
    [SerializeField] private Renderer[] outlineRenderers;
    
    [SerializeField] private uint outlineMask = 1u << 1;

    [Header("Place")]
    [SerializeField] private float skinWidth = 0.03f;

    [Header("Events")]
    public UnityEvent onPickedUp;
    public UnityEvent onPlaced;
    public UnityEvent onPlaceFailed;

    private Rigidbody rb;
    private Transform originalParent;
    private uint[] originalRenderingLayerMasks;

    public float SkinWidth => skinWidth;
    public Rigidbody Rigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (outlineRenderers == null || outlineRenderers.Length == 0)
        {
            outlineRenderers = GetComponentsInChildren<Renderer>(true);
        }

        originalRenderingLayerMasks = new uint[outlineRenderers.Length];

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            if (outlineRenderers[i] != null)
            {
                originalRenderingLayerMasks[i] = outlineRenderers[i].renderingLayerMask;
            }
        }

        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outlineRenderers == null)
        {
            return;
        }

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            Renderer r = outlineRenderers[i];

            if (r == null)
            {
                continue;
            }

            if (highlighted)
            {
                r.renderingLayerMask = originalRenderingLayerMasks[i] | outlineMask;
            }
            else
            {
                r.renderingLayerMask = originalRenderingLayerMasks[i];
            }
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
        SetHighlighted(false);

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