using System.Collections;
using UnityEngine;

public class Level2BFlowController : MonoBehaviour
{
    public static Level2BFlowController Instance { get; private set; }

    private enum Level2BState
    {
        Inactive,
        WaitingForPortalApproach,
        WaitingForSwitchToUfoForHeat,
        WaitingForMeltMountains,
        WaitingForReturnToPlayerForPortal,
        WaitingForPortalEntry,
        Ending
    }

    [Header("References")]
    [SerializeField] private ViewToggleButton viewToggleButton;
    [SerializeField] private Level2HeatRayController heatRayController;
    [SerializeField] private SignalLostOverlayController signalLostOverlay;

    [Header("Portal Objects")]
    [SerializeField] private GameObject grayPortalRoot;
    [SerializeField] private GameObject pinkPortalRoot;
    [SerializeField] private GameObject planetBackdropRoot;

    [Header("Triggers")]
    [SerializeField] private GameObject portalEntryTriggerRoot;

    [Header("Mountains")]
    [SerializeField] private Level2HeatMeltableObject[] iceMountains;

    [Header("Switch")]
    [SerializeField] private GameObject switchHighlightObject;

    [Header("Timing")]
    [SerializeField] private float afterSignalLostDialogueDelay = 0.2f;

    private Level2BState state = Level2BState.WaitingForPortalApproach;
    private bool hasStartedPortalDialogue;
    private bool hasActivatedPortal;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetPortalActivated(false);
        SetPortalEntryTrigger(false);
        SetSwitchHighlight(false);

        if (heatRayController != null)
        {
            heatRayController.SetUnlocked(false);
        }
    }

    private void Update()
    {
        if (state == Level2BState.WaitingForSwitchToUfoForHeat)
        {
            CheckSwitchToUfoForHeat();
        }

        if (state == Level2BState.WaitingForReturnToPlayerForPortal)
        {
            CheckReturnToPlayerForPortal();
        }
    }

    public void TryStartPortalInactiveDialogue()
    {
        if (hasStartedPortalDialogue)
        {
            return;
        }

        hasStartedPortalDialogue = true;
        state = Level2BState.Inactive;

        DialogueSequence sequence = Level2BDialogueDatabase.Instance.GetSequence(
            Level2BDialogueIds.Sequences.PortalInactive
        );

        if (sequence == null)
        {
            Debug.LogWarning("Level2B portal inactive sequence is missing.");
            return;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    public void ShowSwitchToUfoForHeatObjective()
    {
        state = Level2BState.WaitingForSwitchToUfoForHeat;

        SwitchPerspectiveButtonController.Instance?.EnableFreeSwitching();

        ObjectivePanelController.Instance?.SetObjective(
            "Switch to UFO perspective"
        );

        SetSwitchHighlight(true);
    }

    private void CheckSwitchToUfoForHeat()
    {
        if (viewToggleButton == null)
        {
            return;
        }

        if (!viewToggleButton.IsUfoView())
        {
            return;
        }

        state = Level2BState.WaitingForMeltMountains;

        SetSwitchHighlight(false);

        if (heatRayController != null)
        {
            heatRayController.SetUnlocked(true);
        }

        ObjectivePanelController.Instance?.CompleteAndSetNextObjective(
            "Melt the frozen mountains"
        );
    }

    public void NotifyIceMountainMelted()
    {
        if (state != Level2BState.WaitingForMeltMountains)
        {
            return;
        }

        if (!AreAllIceMountainsMelted())
        {
            return;
        }

        StartCoroutine(ActivatePortalFlow());
    }

    private IEnumerator ActivatePortalFlow()
    {
        if (hasActivatedPortal)
        {
            yield break;
        }

        hasActivatedPortal = true;
        state = Level2BState.Inactive;

        if (heatRayController != null)
        {
            heatRayController.SetUnlocked(false);
        }

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        if (signalLostOverlay != null)
        {
            yield return signalLostOverlay.PlayRoutine();
        }

        SetPortalActivated(true);

        yield return new WaitForSeconds(afterSignalLostDialogueDelay);

        DialogueSequence sequence = Level2BDialogueDatabase.Instance.GetSequence(
            Level2BDialogueIds.Sequences.PortalActivated
        );

        if (sequence == null)
        {
            Debug.LogWarning("Level2B portal activated sequence is missing.");
            yield break;
        }

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    public void ShowReturnToPlayerForPortalObjective()
    {
        state = Level2BState.WaitingForReturnToPlayerForPortal;

        SwitchPerspectiveButtonController.Instance?.EnableFreeSwitching();

        ObjectivePanelController.Instance?.SetObjective(
            "Switch back to Player perspective"
        );

        SetSwitchHighlight(true);
    }

    private void CheckReturnToPlayerForPortal()
    {
        if (viewToggleButton == null)
        {
            return;
        }

        if (!viewToggleButton.IsPlayerView())
        {
            return;
        }

        state = Level2BState.WaitingForPortalEntry;

        SetSwitchHighlight(false);
        SetPortalEntryTrigger(true);

        ObjectivePanelController.Instance?.CompleteAndSetNextObjective(
            "Enter the activated portal"
        );
    }

    public bool TryEnterPortal()
    {
        if (state != Level2BState.WaitingForPortalEntry)
        {
            return false;
        }

        state = Level2BState.Ending;

        ObjectivePanelController.Instance?.CompleteCurrentObjective();

        Level2BPortalConfirmationUI.Instance?.Show();

        return true;
    }

    private bool AreAllIceMountainsMelted()
    {
        if (iceMountains == null || iceMountains.Length == 0)
        {
            return false;
        }

        foreach (Level2HeatMeltableObject mountain in iceMountains)
        {
            if (mountain == null || !mountain.IsCompleted)
            {
                return false;
            }
        }

        return true;
    }

    private void SetPortalActivated(bool activated)
    {
        if (grayPortalRoot != null)
        {
            grayPortalRoot.SetActive(!activated);
        }

        if (pinkPortalRoot != null)
        {
            pinkPortalRoot.SetActive(activated);
        }

        if (planetBackdropRoot != null)
        {
            planetBackdropRoot.SetActive(activated);
        }
    }

    private void SetPortalEntryTrigger(bool active)
    {
        if (portalEntryTriggerRoot != null)
        {
            portalEntryTriggerRoot.SetActive(active);
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