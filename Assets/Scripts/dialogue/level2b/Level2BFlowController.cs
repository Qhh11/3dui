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

    [Header("Objective Hint Target")]
    [Tooltip("可手工拖入；留空时会在 HintTargets 下按现有层级格式自动生成。")]
    [SerializeField] private ObjectiveHintTarget iceMountainsHintTarget;

    [Header("Switch")]
    [SerializeField] private GameObject switchHighlightObject;

    [Header("Timing")]
    [SerializeField] private float afterSignalLostDialogueDelay = 0.2f;

    private Level2BState state = Level2BState.WaitingForPortalApproach;
    private bool hasStartedPortalDialogue;
    private bool hasActivatedPortal;
    private ObjectiveHintOutline[] iceMountainHintOutlines;

    public GameObject InactivePortalRoot => grayPortalRoot;

    private void Awake()
    {
        Instance = this;

        iceMountainsHintTarget = ObjectiveHintRuntimeTargetBuilder.ResolveOrCreate(
            iceMountainsHintTarget,
            "Level2IceMountainsHintTarget",
            BuildIceMountainRoots(),
            out iceMountainHintOutlines
        );

        RefreshIceMountainHintEligibility();
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
            GetMeltObjectiveText(),
            iceMountainsHintTarget
        );
    }

    public void NotifyIceMountainMelted()
    {
        if (state != Level2BState.WaitingForMeltMountains)
        {
            return;
        }

        RefreshIceMountainHintEligibility();

        ObjectivePanelController.Instance?.SetObjective(
            GetMeltObjectiveText(),
            iceMountainsHintTarget
        );

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

    private string GetMeltObjectiveText()
    {
        int completedCount = GetCompletedIceMountainCount();
        int totalCount = GetTotalIceMountainCount();

        return $"Melt the frozen mountains ({completedCount}/{totalCount})";
    }

    private int GetCompletedIceMountainCount()
    {
        if (iceMountains == null)
        {
            return 0;
        }

        int count = 0;

        foreach (Level2HeatMeltableObject mountain in iceMountains)
        {
            if (mountain != null && mountain.IsCompleted)
            {
                count++;
            }
        }

        return count;
    }

    private int GetTotalIceMountainCount()
    {
        if (iceMountains == null)
        {
            return 0;
        }

        return iceMountains.Length;
    }

    private GameObject[] BuildIceMountainRoots()
    {
        if (iceMountains == null)
            return new GameObject[0];

        GameObject[] roots = new GameObject[
            iceMountains.Length
        ];

        for (int i = 0; i < iceMountains.Length; i++)
        {
            roots[i] = iceMountains[i] != null
                ? iceMountains[i].gameObject
                : null;
        }

        return roots;
    }

    private void RefreshIceMountainHintEligibility()
    {
        if (iceMountains == null ||
            iceMountainHintOutlines == null)
        {
            return;
        }

        int count = Mathf.Min(
            iceMountains.Length,
            iceMountainHintOutlines.Length
        );

        for (int i = 0; i < count; i++)
        {
            ObjectiveHintOutline outline =
                iceMountainHintOutlines[i];

            if (outline == null)
                continue;

            Level2HeatMeltableObject mountain =
                iceMountains[i];

            outline.SetHintEligible(
                mountain != null &&
                !mountain.IsCompleted
            );
        }
    }
}
