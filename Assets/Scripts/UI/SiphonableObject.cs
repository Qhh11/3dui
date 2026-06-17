using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SiphonableObject : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private GameObject highlightObject;

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
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightObject != null)
            highlightObject.SetActive(highlighted);
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