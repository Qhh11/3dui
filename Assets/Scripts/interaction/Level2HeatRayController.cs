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

    private Level2HeatMeltableObject[] meltTargets;

    public bool IsUnlocked => unlocked;

    private void Start()
    {
        RefreshMeltTargets();
        SetUnlocked(unlocked);
        HideBeam();
        SetCrosshairColor(normalCrosshairColor);
    }

    private void Update()
    {
        if (!unlocked || !IsUfoView())
        {
            ApplyMeltOutlineState(null, false);
            HideBeam();
            SetButtonInteractable(false);
            SetCrosshairColor(normalCrosshairColor);
            return;
        }

        bool hasTarget = TryGetHeatTarget(out Level2HeatMeltableObject target, out RaycastHit hit);

        ApplyMeltOutlineState(target, true);
        SetCrosshairColor(hasTarget ? targetCrosshairColor : normalCrosshairColor);

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
            ApplyMeltOutlineState(null, false);
            HideBeam();
            return;
        }

        RefreshMeltTargets();
        ApplyMeltOutlineState(null, IsUfoView());
    }

    public bool TryGetAimTarget(out Level2HeatMeltableObject target)
    {
        return TryGetHeatTarget(out target, out _);
    }

    private bool TryGetHeatTarget(out Level2HeatMeltableObject target, out RaycastHit hit)
    {
        target = null;
        hit = default;

        if (!unlocked || !IsUfoView())
        {
            return false;
        }

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

    private void RefreshMeltTargets()
    {
        meltTargets = FindObjectsOfType<Level2HeatMeltableObject>(true);
    }

    private void ApplyMeltOutlineState(
        Level2HeatMeltableObject selectedTarget,
        bool showIdle)
    {
        if (meltTargets == null || meltTargets.Length == 0)
        {
            RefreshMeltTargets();
        }

        if (meltTargets == null)
        {
            return;
        }

        foreach (Level2HeatMeltableObject target in meltTargets)
        {
            if (target == null)
            {
                continue;
            }

            if (target.IsCompleted)
            {
                target.SetOutlineState(Level2HeatMeltableObject.OutlineState.Default);
                continue;
            }

            if (target == selectedTarget)
            {
                target.SetOutlineState(Level2HeatMeltableObject.OutlineState.UfoSelected);
                continue;
            }

            target.SetOutlineState(
                showIdle
                    ? Level2HeatMeltableObject.OutlineState.UfoIdle
                    : Level2HeatMeltableObject.OutlineState.Default
            );
        }
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

    private void OnDisable()
    {
        ApplyMeltOutlineState(null, false);
        HideBeam();
        SetButtonInteractable(false);
    }
}
