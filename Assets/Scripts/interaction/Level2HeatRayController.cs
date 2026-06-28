using UnityEngine;
using UnityEngine.UI;

public class Level2HeatRayController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool unlocked;

    [Header("References")]
    [SerializeField] private ViewToggleButton viewToggleButton;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private GameObject heatButtonRoot;
    [SerializeField] private Button heatButton;
    [SerializeField] private Level2HeatRayHoldButton holdButton;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 5000f;
    [SerializeField] private LayerMask heatableMask = ~0;

    [Header("Heat")]
    [SerializeField] private float heatPerSecond = 0.25f;

    [Header("Beam Visual")]
    [SerializeField] private Transform beamOrigin;
    [SerializeField] private LineRenderer beamLine;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color normalCrosshairColor = Color.white;
    [SerializeField] private Color targetCrosshairColor = Color.red;

    private Level2HeatMeltableObject hoveredObject;

    private void Start()
    {
        SetUnlocked(unlocked);
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
    }

    private void Update()
    {
        if (!unlocked || !IsUfoView())
        {
            ClearHover();
            HideBeam();
            SetButtonInteractable(false);
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        bool hasTarget = TryGetHeatTarget(out Level2HeatMeltableObject target, out RaycastHit hit);

        UpdateHover(target);

        SetButtonInteractable(hasTarget);

        bool isHolding = holdButton != null && holdButton.IsHeld;

        if (!hasTarget || !isHolding)
        {
            HideBeam();
            return;
        }

        target.AddHeat(heatPerSecond * Time.deltaTime);
        ShowBeam();
        UpdateBeam(hit.point);
    }

    public void SetUnlocked(bool value)
    {
        unlocked = value;

        if (heatButtonRoot != null)
        {
            heatButtonRoot.SetActive(value);
        }

        SetButtonInteractable(false);

        if (!value)
        {
            ClearHover();
            HideBeam();
        }
    }

    private bool TryGetHeatTarget(out Level2HeatMeltableObject target, out RaycastHit hit)
    {
        target = null;
        hit = default;

        Camera cam = aimCamera != null ? aimCamera : Camera.main;

        if (cam == null)
        {
            return false;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out hit, maxDistance, heatableMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        target = hit.collider.GetComponentInParent<Level2HeatMeltableObject>();

        if (target == null || target.IsCompleted)
        {
            target = null;
            return false;
        }

        return true;
    }

    private bool IsUfoView()
    {
        return viewToggleButton != null && viewToggleButton.IsUfoView();
    }

    private void UpdateHover(Level2HeatMeltableObject target)
    {
        if (hoveredObject != null && hoveredObject != target)
        {
            hoveredObject.SetOutlineState(Level2HeatMeltableObject.OutlineState.UfoIdle);
            hoveredObject = null;
        }

        if (target == null)
        {
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        hoveredObject = target;
        hoveredObject.SetOutlineState(Level2HeatMeltableObject.OutlineState.UfoSelected);
        SetCrosshairColor(targetCrosshairColor);
    }

    private void ClearHover()
    {
        if (hoveredObject == null)
        {
            return;
        }

        hoveredObject.SetOutlineState(Level2HeatMeltableObject.OutlineState.Default);
        hoveredObject = null;
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (heatButton != null)
        {
            heatButton.interactable = interactable;
        }
    }

    private void ShowBeam()
    {
        if (beamLine != null)
        {
            beamLine.enabled = true;
        }
    }

    private void HideBeam()
    {
        if (beamLine != null)
        {
            beamLine.enabled = false;
        }
    }

    private void UpdateBeam(Vector3 targetPosition)
    {
        if (beamLine == null)
        {
            return;
        }

        Vector3 start = beamOrigin != null ? beamOrigin.position : transform.position;

        beamLine.positionCount = 2;
        beamLine.SetPosition(0, start);
        beamLine.SetPosition(1, targetPosition);
    }

    private void SetCrosshairColor(Color color)
    {
        if (crosshairImage != null)
        {
            crosshairImage.color = color;
        }
    }
}