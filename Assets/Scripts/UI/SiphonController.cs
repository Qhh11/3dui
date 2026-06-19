using UnityEngine;
using UnityEngine.UI;

public class SiphonController : MonoBehaviour
{
    private enum SiphonState
    {
        Disabled,
        Idle,
        PullingToHold,
        Holding,
        Placing
    }

    [Header("Ability")]
    [SerializeField] private bool startUnlocked = false;

    [Header("References")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Button siphonButton;
    [SerializeField] private GameObject siphonButtonRoot;
    [SerializeField] private Transform holdPoint;

    [Header("Pick Up Raycast")]
    [SerializeField] private float pickUpMaxDistance = 5000000f;
    [SerializeField] private LayerMask siphonableMask = ~0;

    [Header("Pull To Hold")]
    [SerializeField] private float moveToHoldSpeed = 5f;
    [SerializeField] private float rotateToHoldSpeed = 12f;
    [SerializeField] private float readyPositionThreshold = 5f;
    [SerializeField] private float readyRotationThreshold = 3f;

    [Header("Place Aim Raycast")]
    [SerializeField] private float placeAimMaxDistance = 50000f;
    [SerializeField] private LayerMask placeSurfaceMask = ~0;

    [Header("Place Motion")]
    [SerializeField] private float moveToPlaceSpeed = 3000f;
    [SerializeField] private float placePositionThreshold = 5f;
    [SerializeField] private LayerMask placeCollisionMask = ~0;

    [Header("Beam Visual")]
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private LineRenderer beamLine;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color normalCrosshairColor = Color.white;
    [SerializeField] private Color targetCrosshairColor = Color.cyan;

    private SiphonState state = SiphonState.Disabled;

    private SiphonableObject hoveredObject;
    private SiphonableObject heldObject;

    private Vector3 targetPlacePosition;
    private Quaternion targetPlaceRotation;

    private bool forceRefreshHoverNextFrame;

    public bool IsBusy =>
        state == SiphonState.PullingToHold ||
        state == SiphonState.Holding ||
        state == SiphonState.Placing;

    public bool IsHoldingObject =>
        state == SiphonState.Holding && heldObject != null;

    public bool CanSwitchPerspective =>
        state == SiphonState.Disabled ||
        state == SiphonState.Idle;

    private void Start()
    {
        SetUnlocked(startUnlocked);
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
    }

    private void Update()
    {
        if (state == SiphonState.Disabled)
            return;

        if (state == SiphonState.Idle)
        {
            UpdateHover();
            forceRefreshHoverNextFrame = false;
            return;
        }

        if (state == SiphonState.PullingToHold)
        {
            UpdatePullingToHold();
            return;
        }

        if (state == SiphonState.Holding)
        {
            UpdateHolding();
            return;
        }

        if (state == SiphonState.Placing)
        {
            UpdatePlacing();
        }
    }

    public void UnlockSiphon()
    {
        SetUnlocked(true);
    }

    public void SetUnlocked(bool unlocked)
    {
        state = unlocked ? SiphonState.Idle : SiphonState.Disabled;

        if (siphonButtonRoot != null)
            siphonButtonRoot.SetActive(unlocked);

        ClearHover();
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
        SetButtonInteractable(unlocked);

        forceRefreshHoverNextFrame = unlocked;
    }

    public void ResetForPerspectiveSwitch()
    {
        ClearHover();
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);

        if (state == SiphonState.Disabled)
        {
            SetButtonInteractable(false);
            return;
        }

        if (state == SiphonState.Idle)
        {
            SetButtonInteractable(true);
            forceRefreshHoverNextFrame = true;
            return;
        }

        SetButtonInteractable(false);
    }

    public void OnSiphonButtonClicked()
    {
        if (state == SiphonState.Disabled)
            return;

        if (state == SiphonState.PullingToHold || state == SiphonState.Placing)
            return;

        if (state == SiphonState.Idle)
        {
            TryPickUpObject();
            return;
        }

        if (state == SiphonState.Holding)
        {
            TryStartPlaceObject();
        }
    }

    private void TryPickUpObject()
    {
        Camera cam = GetCamera();

        if (cam == null || holdPoint == null)
        {
            Debug.LogWarning("SiphonController missing Camera or HoldPoint.");
            ClearHover();
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, pickUpMaxDistance, siphonableMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Siphon pick up failed: hit nothing.");
            ClearHover();
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        SiphonableObject target = hit.collider.GetComponentInParent<SiphonableObject>();

        if (target == null || target.Rigidbody == null)
        {
            Debug.Log("Siphon pick up failed: target missing SiphonableObject or Rigidbody.");
            ClearHover();
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        ClearHover();

        heldObject = target;
        heldObject.PickUp(holdPoint);

        state = SiphonState.PullingToHold;
        SetCrosshairColor(normalCrosshairColor);
        SetButtonInteractable(false);
        ShowBeam();
    }

    private void UpdatePullingToHold()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            SetButtonInteractable(true);
            HideBeam();
            SetCrosshairColor(normalCrosshairColor);
            forceRefreshHoverNextFrame = true;
            return;
        }

        heldObject.transform.localPosition = Vector3.Lerp(
            heldObject.transform.localPosition,
            Vector3.zero,
            Time.deltaTime * moveToHoldSpeed
        );

        heldObject.transform.localRotation = Quaternion.Slerp(
            heldObject.transform.localRotation,
            Quaternion.identity,
            Time.deltaTime * rotateToHoldSpeed
        );

        UpdateBeam(heldObject.transform.position);

        if (IsHeldObjectReady())
        {
            state = SiphonState.Holding;
            SetButtonInteractable(true);
        }
    }

    private void UpdateHolding()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            HideBeam();
            SetCrosshairColor(normalCrosshairColor);
            SetButtonInteractable(true);
            forceRefreshHoverNextFrame = true;
            return;
        }

        heldObject.transform.localPosition = Vector3.zero;
        heldObject.transform.localRotation = Quaternion.identity;

        UpdateBeam(heldObject.transform.position);
    }

    private void TryStartPlaceObject()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            HideBeam();
            SetCrosshairColor(normalCrosshairColor);
            SetButtonInteractable(true);
            forceRefreshHoverNextFrame = true;
            return;
        }

        Camera cam = GetCamera();

        if (cam == null)
        {
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, placeAimMaxDistance, placeSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Siphon place failed: no place surface.");
            heldObject.NotifyPlaceFailed();
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        targetPlacePosition = hit.point;
        targetPlaceRotation = heldObject.transform.rotation;

        heldObject.transform.SetParent(null, true);

        state = SiphonState.Placing;
        SetCrosshairColor(normalCrosshairColor);
        SetButtonInteractable(false);
    }

    private void UpdatePlacing()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            SetButtonInteractable(true);
            HideBeam();
            SetCrosshairColor(normalCrosshairColor);
            forceRefreshHoverNextFrame = true;
            return;
        }

        Vector3 current = heldObject.transform.position;
        Vector3 toTarget = targetPlacePosition - current;

        if (toTarget.magnitude <= placePositionThreshold)
        {
            FinishPlace(targetPlacePosition);
            return;
        }

        Vector3 direction = toTarget.normalized;
        float stepDistance = moveToPlaceSpeed * Time.deltaTime;

        Rigidbody rb = heldObject.Rigidbody;

        if (rb != null && rb.SweepTest(direction, out RaycastHit hit, stepDistance + heldObject.SkinWidth, QueryTriggerInteraction.Ignore))
        {
            if (IsLayerInMask(hit.collider.gameObject.layer, placeCollisionMask) &&
                hit.collider.GetComponentInParent<SiphonableObject>() != heldObject)
            {
                Vector3 stopPosition = current + direction * Mathf.Max(0f, hit.distance - heldObject.SkinWidth);
                FinishPlace(stopPosition);
                return;
            }
        }

        heldObject.transform.position = Vector3.MoveTowards(
            current,
            targetPlacePosition,
            stepDistance
        );

        heldObject.transform.rotation = targetPlaceRotation;

        UpdateBeam(heldObject.transform.position);
    }

    private void FinishPlace(Vector3 finalPosition)
    {
        heldObject.Place(finalPosition, targetPlaceRotation);
        heldObject = null;

        state = SiphonState.Idle;
        SetButtonInteractable(true);
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
        forceRefreshHoverNextFrame = true;
    }

    private bool IsHeldObjectReady()
    {
        if (heldObject == null)
            return false;

        float positionError = heldObject.transform.localPosition.magnitude;
        float rotationError = Quaternion.Angle(
            heldObject.transform.localRotation,
            Quaternion.identity
        );

        return positionError <= readyPositionThreshold &&
               rotationError <= readyRotationThreshold;
    }

    private void UpdateHover()
    {
        ClearHover();
        SetCrosshairColor(normalCrosshairColor);

        Camera cam = GetCamera();
        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, pickUpMaxDistance, siphonableMask, QueryTriggerInteraction.Ignore))
        {
            SiphonableObject target = hit.collider.GetComponentInParent<SiphonableObject>();

            if (target != null)
            {
                hoveredObject = target;
                hoveredObject.SetHighlighted(true);
                SetCrosshairColor(targetCrosshairColor);
            }
        }
    }

    private Camera GetCamera()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        return aimCamera;
    }

    private void ClearHover()
    {
        if (hoveredObject != null)
        {
            hoveredObject.SetHighlighted(false);
            hoveredObject = null;
        }
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (siphonButton != null)
            siphonButton.interactable = interactable;
    }

    private void SetCrosshairColor(Color color)
    {
        if (crosshairImage != null)
            crosshairImage.color = color;
    }

    private void ShowBeam()
    {
        if (beamLine != null)
            beamLine.enabled = true;
    }

    private void HideBeam()
    {
        if (beamLine != null)
            beamLine.enabled = false;
    }

    private void UpdateBeam(Vector3 targetPosition)
    {
        if (beamLine == null)
            return;

        Vector3 start = beamOrigin != null ? beamOrigin.position : transform.position;

        beamLine.positionCount = 2;
        beamLine.SetPosition(0, start);
        beamLine.SetPosition(1, targetPosition);
    }
}