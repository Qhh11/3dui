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

    private MonoBehaviour hoveredRocketTarget;
    private MonoBehaviour[] rocketTargets;

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
        MonoBehaviour selectedRocketTarget = null;

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

    private void ApplyRocketOutlineState(MonoBehaviour selectedTarget)
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

        foreach (MonoBehaviour target in rocketTargets)
        {
            if (target == null)
                continue;

            if (target == selectedTarget)
            {
                SetTargetOutline(target, UfoSelectableOutline.OutlineState.UfoSelected);
                continue;
            }

            bool canTarget = GetCanBeRocketTargetNow(target);

            SetTargetOutline(
                target,
                showIdle && canTarget
                    ? UfoSelectableOutline.OutlineState.UfoIdle
                    : UfoSelectableOutline.OutlineState.Default
            );
        }

        hoveredRocketTarget = selectedTarget;
    }

    private bool GetCanBeRocketTargetNow(MonoBehaviour target)
    {
        if (target is BlastBlockedRockTarget rock)
            return rock.CanBeRocketTargetNow();

        if (target is BlastBlockedRootTarget root)
            return root.CanBeRocketTargetNow();

        return false;
    }

    private void SetTargetOutline(MonoBehaviour target, UfoSelectableOutline.OutlineState state)
    {
        if (target is BlastBlockedRockTarget rock)
            rock.SetOutlineState(state);
        else if (target is BlastBlockedRootTarget root)
            root.SetOutlineState(state);
    }

    private void RefreshRocketTargets()
    {
        BlastBlockedRockTarget[] rocks = FindObjectsOfType<BlastBlockedRockTarget>(true);
        BlastBlockedRootTarget[] roots = FindObjectsOfType<BlastBlockedRootTarget>(true);

        rocketTargets = new MonoBehaviour[rocks.Length + roots.Length];
        rocks.CopyTo(rocketTargets, 0);
        roots.CopyTo(rocketTargets, rocks.Length);
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
            SetTargetOutline(hoveredRocketTarget, UfoSelectableOutline.OutlineState.Default);
    }
}