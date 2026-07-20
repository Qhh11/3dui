using System.Collections;
using UnityEngine;

public class TowerFlowController : MonoBehaviour
{
    public static TowerFlowController Instance { get; private set; }

    private enum TowerState
    {
        Inactive,
        WaitingForEnergyCore,
        WaitingForReturnToPlayerAfterCore,
        WaitingForGoToBlockedRocks,
        WaitingForSwitchToUfoForBlast,
        WaitingForBlastRocks,
        WaitingForReturnToPlayerAfterBlast,
        PathOpened
    }

    [Header("Scene Objects")]
    [SerializeField] private GameObject energyCoreObject;
    [SerializeField] private DraggableHighlight energyCoreHighlight;
    [SerializeField] private GameObject blockedRocksRoot;
    [SerializeField] private GameObject newlyOpenedPathRoot;

    [Header("Objective Hint Targets")]
    [Tooltip("可手工拖入；留空时会在 HintTargets 下按现有层级格式自动生成。")]
    [SerializeField] private ObjectiveHintTarget energyCoreHintTarget;
    [SerializeField] private ObjectiveHintTarget blockedRocksHintTarget;

    [Header("Shooting")]
    [SerializeField] private GameObject rocketButtonRoot;
    [SerializeField] private RocketLauncher rocketLauncher;

    [Header("Switch")]
    [SerializeField] private ViewToggleButton viewToggleButton;
    [SerializeField] private GameObject switchHighlightObject;

    [Header("Signal Lost")]
    [SerializeField] private SignalLostOverlayController signalLostOverlay;

    [Header("Timing")]
    [SerializeField] private float afterSignalLostDialogueDelay = 0.2f;

    [Header("Level Transition")]
    [SerializeField] private GameObject level2Root;
    [SerializeField] private float explosionVfxDelayBeforeSignalLost = 1.0f;
    [SerializeField] private float level2ActivateDelayAfterSignalLost = 0.1f;

    private TowerState state = TowerState.Inactive;
    private bool hasStartedTowerIntro;
    private bool hasPickedEnergyCore;
    private bool hasStartedBlockedRocksDialogue;
    private bool hasDestroyedRocks;

    private void Awake()
    {
        Instance = this;

        energyCoreHintTarget = ObjectiveHintRuntimeTargetBuilder.ResolveOrCreate(
            energyCoreHintTarget,
            "TowerEnergyCoreHintTarget",
            energyCoreObject
        );

        blockedRocksHintTarget = ObjectiveHintRuntimeTargetBuilder.ResolveOrCreate(
            blockedRocksHintTarget,
            "TowerBlockedRocksHintTarget",
            blockedRocksRoot
        );
    }

    private void Start()
    {
        SetEnergyCoreVisible(false);
        SetNewPathVisible(false);
        SetRocketUnlocked(false);
        SetSwitchHighlight(false);
        SetLevel2Visible(false);
    }

    private void SetLevel2Visible(bool visible)
    {
        if (level2Root != null)
        {
            level2Root.SetActive(visible);
        }
    }

    private void Update()
    {
        if (state == TowerState.WaitingForReturnToPlayerAfterCore)
        {
            CheckReturnToPlayerAfterCoreObjective();
        }

        if (state == TowerState.WaitingForSwitchToUfoForBlast)
        {
            CheckSwitchToUfoForBlastObjective();
        }

        if (state == TowerState.WaitingForReturnToPlayerAfterBlast)
        {
            CheckReturnToPlayerAfterBlastObjective();
        }
    }

    public void StartTowerIntro()
    {
        if (hasStartedTowerIntro)
        {
            return;
        }

        hasStartedTowerIntro = true;
        state = TowerState.WaitingForEnergyCore;

        DialogueSequence sequence = TowerDialogueDatabase.Instance.GetSequence(
            TowerDialogueIds.Sequences.TowerIntro
        );

        if (sequence == null)
        {
            Debug.LogWarning("Tower intro sequence is missing.");
            return;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    public void ShowCollectEnergyCoreObjective()
    {
        state = TowerState.WaitingForEnergyCore;

        SetEnergyCoreVisible(true);

        ObjectivePanelController.Instance?.SetObjective(
            "Use Siphon to collect the energy core near the tower",
            energyCoreHintTarget
        );
    }

    public void PickUpEnergyCore()
    {
        if (hasPickedEnergyCore)
        {
            return;
        }

        hasPickedEnergyCore = true;

        Debug.Log("TowerFlowController received energy core pickup.");

        SetEnergyCoreVisible(false);

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        DialogueSequence sequence = TowerDialogueDatabase.Instance.GetSequence(
            TowerDialogueIds.Sequences.BlastUnlocked
        );

        if (sequence == null)
        {
            Debug.LogWarning("Blast unlocked sequence is missing.");
            return;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    public void ShowReturnToPlayerAfterCoreObjective()
    {
        state = TowerState.WaitingForReturnToPlayerAfterCore;

        SwitchPerspectiveButtonController.Instance?.EnableFreeSwitching();

        ObjectivePanelController.Instance?.SetObjective(
            "Switch back to Player perspective"
        );

        SetSwitchHighlight(true);
    }

    private void CheckReturnToPlayerAfterCoreObjective()
    {
        if (viewToggleButton == null)
        {
            return;
        }

        if (!viewToggleButton.IsPlayerView())
        {
            return;
        }

        state = TowerState.WaitingForGoToBlockedRocks;

        SetSwitchHighlight(false);

        ObjectivePanelController.Instance?.CompleteAndSetNextObjective(
            "Go to the blocked stone passage behind the tower",
            blockedRocksHintTarget
        );
    }

    public bool TryStartBlockedRocksDialogue()
    {
        if (state != TowerState.WaitingForGoToBlockedRocks)
        {
            return false;
        }

        if (hasStartedBlockedRocksDialogue)
        {
            return false;
        }

        hasStartedBlockedRocksDialogue = true;

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        DialogueSequence sequence = TowerDialogueDatabase.Instance.GetSequence(
            TowerDialogueIds.Sequences.BlockedRocksFound
        );

        if (sequence == null)
        {
            Debug.LogWarning("Blocked rocks found sequence is missing.");
            return false;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
        return true;
    }

    public void ShowSwitchToUfoForBlastObjective()
    {
        state = TowerState.WaitingForSwitchToUfoForBlast;

        SwitchPerspectiveButtonController.Instance?.EnableFreeSwitching();

        ObjectivePanelController.Instance?.SetObjective(
            "Switch to UFO perspective"
        );

        SetSwitchHighlight(true);
    }

    private void CheckSwitchToUfoForBlastObjective()
    {
        if (viewToggleButton == null)
        {
            return;
        }

        if (!viewToggleButton.IsUfoView())
        {
            return;
        }

        state = TowerState.WaitingForBlastRocks;

        SetSwitchHighlight(false);
        SetRocketUnlocked(true);

        ObjectivePanelController.Instance?.CompleteAndSetNextObjective(
            "Aim at the blocked rocks and fire",
            blockedRocksHintTarget
        );
    }

    public void OnBlockedRocksDestroyed()
    {
        Debug.Log("[TowerFlow] OnBlockedRocksDestroyed called. State = " + state);

        if (hasDestroyedRocks)
        {
            Debug.Log("[TowerFlow] Rocks already destroyed. Ignored.");
            return;
        }

        if (state != TowerState.WaitingForBlastRocks)
        {
            Debug.LogWarning("[TowerFlow] Rejected rock destruction because state is not WaitingForBlastRocks.");
            return;
        }

        hasDestroyedRocks = true;
        state = TowerState.PathOpened;

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        SetRocketUnlocked(false);
        SetSwitchHighlight(false);

        StartCoroutine(ExplosionThenSignalLostFlow());
    }

    private IEnumerator ExplosionThenSignalLostFlow()
    {
        Debug.Log("[TowerFlow] Explosion flow started.");

        // Wait for the explosion VFX to finish before showing Signal Lost.
        if (explosionVfxDelayBeforeSignalLost > 0f)
        {
            yield return new WaitForSeconds(explosionVfxDelayBeforeSignalLost);
        }

        // Hide the blocked rocks after the explosion has had time to play.
        if (blockedRocksRoot != null)
        {
            blockedRocksRoot.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[TowerFlow] Blocked Rocks Root is not assigned.");
        }

        // Show the newly opened path in Level 1.
        SetNewPathVisible(true);

        // Play the Signal Lost overlay.
        if (signalLostOverlay != null)
        {
            Debug.Log("[TowerFlow] Playing Signal Lost overlay.");
            yield return signalLostOverlay.PlayRoutine();
            Debug.Log("[TowerFlow] Signal Lost overlay finished.");
        }
        else
        {
            Debug.LogWarning("[TowerFlow] Signal Lost Overlay is not assigned.");
        }

        // Activate Level 2 objects after Signal Lost.
        if (level2ActivateDelayAfterSignalLost > 0f)
        {
            yield return new WaitForSecondsRealtime(level2ActivateDelayAfterSignalLost);
        }

        SetLevel2Visible(true);

        // Then continue the dialogue.
        yield return new WaitForSecondsRealtime(afterSignalLostDialogueDelay);

        DialogueSequence sequence = TowerDialogueDatabase.Instance.GetSequence(
            TowerDialogueIds.Sequences.AfterRocksDestroyed
        );

        if (sequence == null)
        {
            Debug.LogWarning("[TowerFlow] After rocks destroyed sequence is missing.");
            yield break;
        }

        Debug.Log("[TowerFlow] Starting after rocks destroyed dialogue.");
        DialogueRunner.Instance.StartDialogue(sequence);
    }


    public void ShowReturnToPlayerAfterBlastObjective()
    {
        state = TowerState.WaitingForReturnToPlayerAfterBlast;

        SwitchPerspectiveButtonController.Instance?.EnableFreeSwitching();

        ObjectivePanelController.Instance?.SetObjective(
            "Switch back to Player perspective"
        );

        SetSwitchHighlight(true);
    }

    private void CheckReturnToPlayerAfterBlastObjective()
    {
        if (viewToggleButton == null)
        {
            return;
        }

        if (!viewToggleButton.IsPlayerView())
        {
            return;
        }

        state = TowerState.PathOpened;

        SetSwitchHighlight(false);

        ObjectivePanelController.Instance?.CompleteAndSetNextObjective(
            "Follow the newly opened path"
        );
    }

    private void SetEnergyCoreVisible(bool visible)
    {
        if (energyCoreObject != null)
        {
            energyCoreObject.SetActive(visible);
        }

        if (energyCoreHighlight != null)
        {
            energyCoreHighlight.SetHighlighted(visible);
        }
    }

    private void SetRocketUnlocked(bool unlocked)
    {
        ResolveRocketLauncher();

        if (rocketLauncher != null)
            rocketLauncher.enabled = unlocked;
        else
            Debug.LogError("[TowerFlow] RocketLauncher is not assigned and could not be found under Rocket Button Root.", this);

        if (rocketButtonRoot != null)
            rocketButtonRoot.SetActive(unlocked);
        else
            Debug.LogError("[TowerFlow] Rocket Button Root is not assigned.", this);
    }

    private void ResolveRocketLauncher()
    {
        if (rocketLauncher == null && rocketButtonRoot != null)
            rocketLauncher = rocketButtonRoot.GetComponentInChildren<RocketLauncher>(true);
    }

    private void SetNewPathVisible(bool visible)
    {
        if (newlyOpenedPathRoot != null)
        {
            newlyOpenedPathRoot.SetActive(visible);
        }
    }

    private void SetSwitchHighlight(bool visible)
    {
        if (switchHighlightObject != null)
        {
            switchHighlightObject.SetActive(visible);
        }
    }
}
