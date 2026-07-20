using UnityEngine;
using UnityEngine.UI;

public class UfoAimFeatureFeedbackController : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;

    [SerializeField]
    private Color normalCrosshairColor =
        new Color(0f, 0.65882355f, 1f, 1f);

    [SerializeField]
    private Color validTargetCrosshairColor =
        new Color(0.23137255f, 0.92941177f, 0.23137255f, 1f);

    [Header("References")]
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
        ClearAllRocketOutlines();
        ApplyCrosshair(UfoFeatureType.None);
    }

    private void ResolveOptionalReferences()
    {
        if (viewToggleButton == null)
        {
            viewToggleButton =
                FindObjectOfType<ViewToggleButton>(true);
        }

        if (siphonController == null)
        {
            siphonController =
                FindObjectOfType<SiphonController>(true);
        }

        if (rocketLauncher == null)
        {
            rocketLauncher =
                FindObjectOfType<RocketLauncher>(true);
        }

        if (heatRayController == null)
        {
            heatRayController =
                FindObjectOfType<Level2HeatRayController>(true);
        }

        if (markController == null)
        {
            markController =
                FindObjectOfType<MarkController>(true);
        }

        featureButtons =
            FindObjectsOfType<UfoFeatureButtonGlow>(true);
    }

    private void LateUpdate()
    {
        UfoFeatureType activeFeature =
            UfoFeatureType.None;

        MonoBehaviour selectedRocketTarget = null;

        bool canShowFeedback =
            IsUfoView() &&
            !GameplayInputLockService.IsBlocked &&
            (
                markController == null ||
                (
                    !markController.IsBusy &&
                    !markController.ActedThisFrame
                )
            );

        if (canShowFeedback &&
            markController != null &&
            markController.TryGetRecallAimTarget(out _))
        {
            // A deployed Mark directly under the reticle should
            // take priority because clicking Mark now means Recall.
            activeFeature = UfoFeatureType.Mark;
        }
        else if (canShowFeedback &&
                 rocketLauncher != null &&
                 rocketLauncher.TryGetAimTarget(
                     out selectedRocketTarget))
        {
            // Rocket stays ahead of Siphon so roots or blocked
            // rocks cannot lose feedback to an object behind them.
            activeFeature = UfoFeatureType.Rocket;
        }
        else if (canShowFeedback &&
                 siphonController != null &&
                 siphonController.TryGetAimTarget(out _))
        {
            activeFeature = UfoFeatureType.Siphon;
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

    private void ApplyCrosshair(
        UfoFeatureType activeFeature)
    {
        if (crosshairImage == null)
            return;

        crosshairImage.color =
            activeFeature == UfoFeatureType.None
                ? normalCrosshairColor
                : validTargetCrosshairColor;
    }

    private void ApplyFeatureGlow(
        UfoFeatureType activeFeature)
    {
        if (featureButtons == null)
            return;

        foreach (UfoFeatureButtonGlow button
                 in featureButtons)
        {
            if (button == null)
                continue;

            bool shouldGlow =
                activeFeature != UfoFeatureType.None &&
                button.FeatureType == activeFeature;

            button.SetGlowing(shouldGlow);
        }
    }

    private void ApplyRocketOutlineState(
        MonoBehaviour selectedTarget)
    {
        if (hoveredRocketTarget == selectedTarget)
            return;

        if (hoveredRocketTarget != null)
        {
            SetTargetOutline(
                hoveredRocketTarget,
                UfoSelectableOutline
                    .OutlineState.Default
            );
        }

        hoveredRocketTarget = selectedTarget;

        if (hoveredRocketTarget != null)
        {
            SetTargetOutline(
                hoveredRocketTarget,
                UfoSelectableOutline
                    .OutlineState.UfoSelected
            );
        }
    }

    private void SetTargetOutline(
        MonoBehaviour target,
        UfoSelectableOutline.OutlineState state)
    {
        if (target is BlastBlockedRockTarget rock)
        {
            rock.SetOutlineState(state);
        }
        else if (target is BlastBlockedRootTarget root)
        {
            root.SetOutlineState(state);
        }
    }

    private void RefreshRocketTargets()
    {
        BlastBlockedRockTarget[] rocks =
            FindObjectsOfType<BlastBlockedRockTarget>(true);

        BlastBlockedRootTarget[] roots =
            FindObjectsOfType<BlastBlockedRootTarget>(true);

        rocketTargets =
            new MonoBehaviour[rocks.Length + roots.Length];

        rocks.CopyTo(rocketTargets, 0);
        roots.CopyTo(rocketTargets, rocks.Length);
    }

    private void ClearAllRocketOutlines()
    {
        if (rocketTargets == null ||
            rocketTargets.Length == 0)
        {
            RefreshRocketTargets();
        }

        if (rocketTargets != null)
        {
            foreach (MonoBehaviour target
                     in rocketTargets)
            {
                if (target == null)
                    continue;

                SetTargetOutline(
                    target,
                    UfoSelectableOutline
                        .OutlineState.Default
                );
            }
        }

        hoveredRocketTarget = null;
    }

    private bool IsUfoView()
    {
        if (viewToggleButton != null)
        {
            return viewToggleButton.IsUfoView();
        }

        return
            SwitchPerspectiveButtonController.Instance != null &&
            SwitchPerspectiveButtonController.Instance.IsUfoView;
    }

    private void ClearFeatureGlow()
    {
        ApplyFeatureGlow(UfoFeatureType.None);
    }

    private void OnDisable()
    {
        ClearFeatureGlow();
        ClearAllRocketOutlines();
        ApplyCrosshair(UfoFeatureType.None);
    }
}
