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

    private TowerState state = TowerState.Inactive;
    private bool hasStartedTowerIntro;
    private bool hasPickedEnergyCore;
    private bool hasStartedBlockedRocksDialogue;
    private bool hasDestroyedRocks;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetEnergyCoreVisible(false);
        SetNewPathVisible(false);
        SetRocketUnlocked(false);
        SetSwitchHighlight(false);
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
            "Use Siphon to collect the energy core near the tower"
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
            "Go to the blocked stone passage"
        );
    }

    public void TryStartBlockedRocksDialogue()
    {
        if (state != TowerState.WaitingForGoToBlockedRocks)
        {
            return;
        }

        if (hasStartedBlockedRocksDialogue)
        {
            return;
        }

        hasStartedBlockedRocksDialogue = true;

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        DialogueSequence sequence = TowerDialogueDatabase.Instance.GetSequence(
            TowerDialogueIds.Sequences.BlockedRocksFound
        );

        if (sequence == null)
        {
            Debug.LogWarning("Blocked rocks found sequence is missing.");
            return;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
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
            "Aim at the blocked rocks and fire"
        );
    }

    public void OnBlockedRocksDestroyed()
    {
        if (hasDestroyedRocks)
        {
            return;
        }

        if (state != TowerState.WaitingForBlastRocks)
        {
            return;
        }

        hasDestroyedRocks = true;
        state = TowerState.PathOpened;

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        SetRocketUnlocked(false);
        SetSwitchHighlight(false);

        if (blockedRocksRoot != null)
        {
            blockedRocksRoot.SetActive(false);
        }

        SetNewPathVisible(true);

        StartCoroutine(PlaySignalLostThenDialogue());
    }

    private IEnumerator PlaySignalLostThenDialogue()
    {
        if (signalLostOverlay != null)
        {
            yield return signalLostOverlay.PlayRoutine();
        }

        yield return new WaitForSeconds(afterSignalLostDialogueDelay);

        DialogueSequence sequence = TowerDialogueDatabase.Instance.GetSequence(
            TowerDialogueIds.Sequences.AfterRocksDestroyed
        );

        if (sequence == null)
        {
            Debug.LogWarning("After rocks destroyed sequence is missing.");
            yield break;
        }

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

    public void ShowFollowNewPathObjective()
    {
        state = TowerState.PathOpened;

        ObjectivePanelController.Instance?.SetObjective(
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
        if (rocketButtonRoot != null)
        {
            rocketButtonRoot.SetActive(unlocked);
        }

        if (rocketLauncher != null)
        {
            rocketLauncher.enabled = unlocked;
        }
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