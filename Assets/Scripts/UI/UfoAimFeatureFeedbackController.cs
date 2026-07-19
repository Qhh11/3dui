using UnityEngine;
using UnityEngine.UI;

public class UfoAimFeatureFeedbackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private ViewToggleButton viewToggleButton;
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private RocketLauncher rocketLauncher;
    [SerializeField] private Level2HeatRayController heatRayController;
    [SerializeField] private MarkController markController;

    [Header("Feature Buttons")]
    [SerializeField] private UfoFeatureButtonGlow[] featureButtons;

    private BlastBlockedRockTarget hoveredRocketTarget;
    private BlastBlockedRockTarget[] rocketTargets;

    private void Start()
    {
        ResolveOptionalReferences();
        RefreshRocketTargets();
        ClearFeatureGlow();
        ApplyRocketOutlineState(null);
    }

    private void ResolveOptionalReferences()
    {
        if (heatRayController == null)
            heatRayController = FindObjectOfType<Level2HeatRayController>(true);

        featureButtons = FindObjectsOfType<UfoFeatureButtonGlow>(true);
    }

    private void LateUpdate()
    {
        UfoFeatureType activeFeature = UfoFeatureType.None;
        BlastBlockedRockTarget selectedRocketTarget = null;

        bool canShowFeedback =
            IsUfoView() &&
            !GameplayInputLockService.IsBlocked &&
            (markController == null ||
             (!markController.IsBusy && !markController.ActedThisFrame));

        if (canShowFeedback &&
            siphonController != null &&
            siphonController.TryGetAimTarget(out _))
        {
            activeFeature = UfoFeatureType.Siphon;
        }
        else if (canShowFeedback &&
                 rocketLauncher != null &&
                 rocketLauncher.TryGetAimTarget(out selectedRocketTarget))
        {
            activeFeature = UfoFeatureType.Rocket;
        }
        else if (canShowFeedback &&
                 heatRayController != null &&
                 heatRayController.TryGetAimTarget(out _))
        {
            activeFeature = UfoFeatureType.Melt;
        }

        ApplyCrosshair(activeFeature);
        ApplyFeatureGlow(activeFeature);
        ApplyRocketOutlineState(selectedRocketTarget);
    }

    private void ApplyCrosshair(UfoFeatureType activeFeature)
    {
        if (crosshairImage == null || siphonController == null)
            return;

        crosshairImage.color =
            activeFeature == UfoFeatureType.None
                ? siphonController.NormalCrosshairColor
                : siphonController.TargetCrosshairColor;
    }

    private void ApplyFeatureGlow(UfoFeatureType activeFeature)
    {
        if (featureButtons == null)
            return;

        foreach (UfoFeatureButtonGlow button in featureButtons)
        {
            if (button == null)
                continue;

            button.SetGlowing(
                activeFeature != UfoFeatureType.None &&
                button.FeatureType == activeFeature
            );
        }
    }

    private void ApplyRocketOutlineState(BlastBlockedRockTarget selectedTarget)
    {
        if (rocketTargets == null || rocketTargets.Length == 0)
            RefreshRocketTargets();

        bool showIdle =
            IsUfoView() &&
            rocketLauncher != null &&
            rocketLauncher.enabled &&
            rocketLauncher.gameObject.activeInHierarchy;

        if (rocketTargets == null)
            return;

        foreach (BlastBlockedRockTarget target in rocketTargets)
        {
            if (target == null)
                continue;

            if (target == selectedTarget)
            {
                target.SetOutlineState(UfoSelectableOutline.OutlineState.UfoSelected);
                continue;
            }

            target.SetOutlineState(
                showIdle && target.CanBeRocketTargetNow()
                    ? UfoSelectableOutline.OutlineState.UfoIdle
                    : UfoSelectableOutline.OutlineState.Default
            );
        }

        hoveredRocketTarget = selectedTarget;
    }

    private void RefreshRocketTargets()
    {
        rocketTargets = FindObjectsOfType<BlastBlockedRockTarget>(true);
    }

    private bool IsUfoView()
    {
        if (viewToggleButton != null)
            return viewToggleButton.IsUfoView();

        return SwitchPerspectiveButtonController.Instance != null &&
               SwitchPerspectiveButtonController.Instance.IsUfoView;
    }

    private void ClearFeatureGlow()
    {
        ApplyFeatureGlow(UfoFeatureType.None);
    }

    private void OnDisable()
    {
        ClearFeatureGlow();

        if (hoveredRocketTarget != null)
            hoveredRocketTarget.SetOutlineState(UfoSelectableOutline.OutlineState.Default);
    }
}
