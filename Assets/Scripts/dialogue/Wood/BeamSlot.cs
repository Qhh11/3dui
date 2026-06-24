using UnityEngine;

/// <summary>
/// A drop target (the translucent "ghost" on the ground) where a wood beam
/// should be placed. WoodBeamPuzzle reads these to decide if a dropped beam
/// is close enough to snap.
/// </summary>
public class BeamSlot : MonoBehaviour
{
    [Tooltip("Where the beam snaps to. Defaults to this object's own transform.")]
    [SerializeField] private Transform snapPoint;

    [Tooltip("The translucent ghost shown while the puzzle is active. Hidden once filled.")]
    [SerializeField] private GameObject ghostVisual;

    [Tooltip("How close (world units) a dropped beam must be to snap into this slot.")]
    [SerializeField] private float acceptRadius = 1.5f;

    public bool IsOccupied { get; private set; }
    public float AcceptRadius => acceptRadius;
    public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;
    public Quaternion SnapRotation => snapPoint != null ? snapPoint.rotation : transform.rotation;

    public void SetGhostVisible(bool visible)
    {
        if (IsOccupied)
        {
            visible = false;
        }

        if (ghostVisual != null)
        {
            ghostVisual.SetActive(visible);
        }
    }

    public void MarkOccupied()
    {
        IsOccupied = true;

        if (ghostVisual != null)
        {
            ghostVisual.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(SnapPosition, acceptRadius);
    }
#endif
}
