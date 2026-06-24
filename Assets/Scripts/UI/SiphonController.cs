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

    [SerializeField] private MarkController markController;
    [SerializeField] private RocketLauncher rocketLauncher;

    private SiphonState state = SiphonState.Disabled;

    private bool isUfoView;
    private SiphonableObject hoveredObject;
    private SiphonableObject heldObject;
    private SiphonableObject[] allSiphonableObjects;

    private Vector3 targetPlacePosition;
    private Quaternion targetPlaceRotation;

    public bool IsBusy =>
        state == SiphonState.PullingToHold ||
        state == SiphonState.Holding ||
        state == SiphonState.Placing;

    public bool IsHoldingObject =>
        state == SiphonState.Holding &&
        heldObject != null;

    public bool CanSwitchPerspective =>
        state == SiphonState.Disabled ||
        state == SiphonState.Idle;

    private void Start()
    {
        RefreshSiphonableObjectList();
        SetUnlocked(startUnlocked);
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
        ApplyPerspectiveOutlineState();
    }

    private void Update()
    {
        if (state == SiphonState.Disabled)
        {
            SetButtonInteractable(false);
            return;
        }

        if (!isUfoView)
        {
            ClearHoverToDefault();
            SetCrosshairColor(normalCrosshairColor);
            SetButtonInteractable(false);
            return;
        }

        if (state == SiphonState.Idle)
        {
            if (IsExternalInputBlocked())
            {
                ClearHoverToIdle();
                SetCrosshairColor(normalCrosshairColor);
                SetButtonInteractable(false);
                return;
            }

            bool hasValidTarget = UpdateHover();

            SetButtonInteractable(
                holdPoint != null &&
                hasValidTarget
            );

            return;
        }

        if (state == SiphonState.PullingToHold)
        {
            SetButtonInteractable(false);
            UpdatePullingToHold();
            return;
        }

        if (state == SiphonState.Holding)
        {
            UpdateHolding();
            SetButtonInteractable(CanTriggerNow());
            return;
        }

        if (state == SiphonState.Placing)
        {
            SetButtonInteractable(false);
            UpdatePlacing();
        }
    }

    public void UnlockSiphon()
    {
        SetUnlocked(true);
    }

    public void SetUnlocked(bool unlocked)
    {
        state = unlocked
            ? SiphonState.Idle
            : SiphonState.Disabled;

        if (siphonButtonRoot != null)
            siphonButtonRoot.SetActive(unlocked);

        ClearHoverToDefault();
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
        SetButtonInteractable(CanTriggerNow());

        ApplyPerspectiveOutlineState();
    }

    public void OnPerspectiveChanged(bool newIsUfoView)
    {
        isUfoView = newIsUfoView;

        ClearHoverToDefault();
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
        ApplyPerspectiveOutlineState();
        SetButtonInteractable(CanTriggerNow());
    }

    public void ResetForPerspectiveSwitch()
    {
        bool currentIsUfoView =
            SwitchPerspectiveButtonController.Instance != null &&
            SwitchPerspectiveButtonController.Instance.IsUfoView;

        OnPerspectiveChanged(currentIsUfoView);
    }

    public void OnSiphonButtonClicked()
    {
        if (!CanTriggerNow())
            return;

        if (state == SiphonState.Idle)
        {
            TryPickUpObject();
            return;
        }

        if (state == SiphonState.Holding)
            TryStartPlaceObject();
    }

    private bool CanTriggerNow()
    {
        if (state == SiphonState.Disabled)
            return false;

        if (!isUfoView)
            return false;

        if (IsExternalInputBlocked())
            return false;

        if (state == SiphonState.Idle)
        {
            return
                holdPoint != null &&
                TryGetPickUpTarget(out _);
        }

        if (state == SiphonState.Holding)
        {
            return
                heldObject != null &&
                TryGetPlaceHit(out _);
        }

        return false;
    }

    private bool IsExternalInputBlocked()
    {
        if (markController != null &&
            (markController.IsBusy ||
             markController.ActedThisFrame))
        {
            return true;
        }

        if (rocketLauncher != null &&
            rocketLauncher.FiredThisFrame)
        {
            return true;
        }

        return
            DialogueRunner.Instance != null &&
            DialogueRunner.Instance.IsDialogueOpen;
    }

    private bool TryGetPickUpTarget(
        out SiphonableObject target)
    {
        target = null;

        Camera cam = GetCamera();

        if (cam == null)
            return false;

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                pickUpMaxDistance,
                siphonableMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        target =
            hit.collider.GetComponentInParent<
                SiphonableObject>();

        if (target == null ||
            target == heldObject ||
            target.Rigidbody == null)
        {
            target = null;
            return false;
        }

        return true;
    }

    private bool TryGetPlaceHit(out RaycastHit hit)
    {
        hit = default;

        Camera cam = GetCamera();

        if (cam == null)
            return false;

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        return Physics.Raycast(
            ray,
            out hit,
            placeAimMaxDistance,
            placeSurfaceMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void TryPickUpObject()
    {
        if (holdPoint == null)
        {
            Debug.LogWarning(
                "SiphonController missing HoldPoint."
            );
            return;
        }

        if (!TryGetPickUpTarget(
                out SiphonableObject target))
        {
            Debug.Log(
                "Siphon pick up failed: no valid target."
            );

            ClearHoverToIdle();
            SetCrosshairColor(normalCrosshairColor);
            SetButtonInteractable(false);
            return;
        }

        ClearHoverToIdle();

        heldObject = target;
        heldObject.PickUp(holdPoint);

        state = SiphonState.PullingToHold;
        SetCrosshairColor(normalCrosshairColor);
        SetButtonInteractable(false);
        ShowBeam();

        ApplyPerspectiveOutlineState();
    }

    private void UpdatePullingToHold()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            SetButtonInteractable(CanTriggerNow());
            HideBeam();
            ApplyPerspectiveOutlineState();
            return;
        }

        heldObject.transform.localPosition =
            Vector3.Lerp(
                heldObject.transform.localPosition,
                Vector3.zero,
                Time.deltaTime * moveToHoldSpeed
            );

        heldObject.transform.localRotation =
            Quaternion.Slerp(
                heldObject.transform.localRotation,
                Quaternion.identity,
                Time.deltaTime * rotateToHoldSpeed
            );

        UpdateBeam(heldObject.transform.position);

        if (IsHeldObjectReady())
        {
            state = SiphonState.Holding;
            SetButtonInteractable(CanTriggerNow());
        }
    }

    private void UpdateHolding()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            HideBeam();
            SetButtonInteractable(CanTriggerNow());
            ApplyPerspectiveOutlineState();
            return;
        }

        heldObject.transform.localPosition =
            Vector3.zero;

        heldObject.transform.localRotation =
            Quaternion.identity;

        UpdateBeam(heldObject.transform.position);
    }

    private void TryStartPlaceObject()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            HideBeam();
            SetButtonInteractable(CanTriggerNow());
            ApplyPerspectiveOutlineState();
            return;
        }

        if (!TryGetPlaceHit(out RaycastHit hit))
        {
            Debug.Log(
                "Siphon place failed: no place surface."
            );

            heldObject.NotifyPlaceFailed();
            SetButtonInteractable(false);
            return;
        }

        targetPlacePosition = hit.point;
        targetPlaceRotation =
            heldObject.transform.rotation;

        heldObject.transform.SetParent(null, true);

        state = SiphonState.Placing;
        SetButtonInteractable(false);
    }

    private void UpdatePlacing()
    {
        SetCrosshairColor(normalCrosshairColor);

        if (heldObject == null)
        {
            state = SiphonState.Idle;
            SetButtonInteractable(CanTriggerNow());
            HideBeam();
            ApplyPerspectiveOutlineState();
            return;
        }

        Vector3 current =
            heldObject.transform.position;

        Vector3 toTarget =
            targetPlacePosition - current;

        if (toTarget.magnitude <=
            placePositionThreshold)
        {
            FinishPlace(targetPlacePosition);
            return;
        }

        Vector3 direction = toTarget.normalized;

        float stepDistance =
            moveToPlaceSpeed * Time.deltaTime;

        Rigidbody rb = heldObject.Rigidbody;

        if (rb != null &&
            rb.SweepTest(
                direction,
                out RaycastHit hit,
                stepDistance + heldObject.SkinWidth,
                QueryTriggerInteraction.Ignore))
        {
            if (IsLayerInMask(
                    hit.collider.gameObject.layer,
                    placeCollisionMask) &&
                hit.collider.GetComponentInParent<
                    SiphonableObject>() != heldObject)
            {
                Vector3 stopPosition =
                    current +
                    direction *
                    Mathf.Max(
                        0f,
                        hit.distance -
                        heldObject.SkinWidth
                    );

                FinishPlace(stopPosition);
                return;
            }
        }

        heldObject.transform.position =
            Vector3.MoveTowards(
                current,
                targetPlacePosition,
                stepDistance
            );

        heldObject.transform.rotation =
            targetPlaceRotation;

        UpdateBeam(heldObject.transform.position);
    }

    private void FinishPlace(Vector3 finalPosition)
    {
        heldObject.Place(
            finalPosition,
            targetPlaceRotation
        );

        heldObject = null;
        state = SiphonState.Idle;

        HideBeam();
        SetCrosshairColor(normalCrosshairColor);

        RefreshSiphonableObjectList();
        ApplyPerspectiveOutlineState();
        SetButtonInteractable(CanTriggerNow());
    }

    private bool UpdateHover()
    {
        ClearHoverToIdle();
        SetCrosshairColor(normalCrosshairColor);

        if (!TryGetPickUpTarget(
                out SiphonableObject target))
        {
            return false;
        }

        hoveredObject = target;

        hoveredObject.SetOutlineState(
            SiphonableObject.OutlineState.UfoSelected
        );

        SetCrosshairColor(targetCrosshairColor);
        return true;
    }

    private void ApplyPerspectiveOutlineState()
    {
        if (allSiphonableObjects == null ||
            allSiphonableObjects.Length == 0)
        {
            RefreshSiphonableObjectList();
        }

        SiphonableObject.OutlineState targetState =
            state != SiphonState.Disabled &&
            isUfoView
                ? SiphonableObject.OutlineState.UfoIdle
                : SiphonableObject.OutlineState.Default;

        foreach (SiphonableObject obj
                 in allSiphonableObjects)
        {
            if (obj == null || obj == heldObject)
                continue;

            obj.SetOutlineState(targetState);
        }
    }

    private void RefreshSiphonableObjectList()
    {
        allSiphonableObjects =
            FindObjectsOfType<SiphonableObject>(true);
    }

    private void ClearHoverToIdle()
    {
        if (hoveredObject == null)
            return;

        hoveredObject.SetOutlineState(
            isUfoView &&
            state != SiphonState.Disabled
                ? SiphonableObject
                    .OutlineState.UfoIdle
                : SiphonableObject
                    .OutlineState.Default
        );

        hoveredObject = null;
    }

    private void ClearHoverToDefault()
    {
        if (hoveredObject == null)
            return;

        hoveredObject.SetOutlineState(
            SiphonableObject.OutlineState.Default
        );

        hoveredObject = null;
    }

    private bool IsHeldObjectReady()
    {
        if (heldObject == null)
            return false;

        float positionError =
            heldObject.transform.localPosition
                .magnitude;

        float rotationError =
            Quaternion.Angle(
                heldObject.transform.localRotation,
                Quaternion.identity
            );

        return
            positionError <= readyPositionThreshold &&
            rotationError <= readyRotationThreshold;
    }

    private Camera GetCamera()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        return aimCamera;
    }

    private bool IsLayerInMask(
        int layer,
        LayerMask mask)
    {
        return
            (mask.value & (1 << layer)) != 0;
    }

    private void SetButtonInteractable(
        bool interactable)
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

        Vector3 start =
            beamOrigin != null
                ? beamOrigin.position
                : transform.position;

        beamLine.positionCount = 2;
        beamLine.SetPosition(0, start);
        beamLine.SetPosition(1, targetPosition);
    }
}