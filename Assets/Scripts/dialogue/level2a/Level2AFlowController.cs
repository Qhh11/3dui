using System.Collections;
using UnityEngine;

public class Level2AFlowController : MonoBehaviour
{
    public static Level2AFlowController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        energyBlockHintTarget = ObjectiveHintRuntimeTargetBuilder.ResolveOrCreate(
            energyBlockHintTarget,
            "Level2EnergyBlockHintTarget",
            energyBlock
        );

        treeRootsHintTarget = ObjectiveHintRuntimeTargetBuilder.ResolveOrCreate(
            treeRootsHintTarget,
            "Level2TreeRootsHintTarget",
            treeRoots
        );

        if (portalHintRendererRoot != null)
        {
            portalHintTarget = ObjectiveHintRuntimeTargetBuilder.ResolveOrCreate(
                portalHintTarget,
                "Level2PortalHintTarget",
                portalHintRendererRoot
            );
        }
    }
    [Header("References")]
    [SerializeField] private ViewToggleButton viewToggleButton;
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private SignalLostOverlayController signalLostOverlay;

    [Header("Energy Block")]
    [SerializeField] private GameObject energyBlock;
    [SerializeField] private GameObject treeRoots;

    [Header("Objective Hint Targets")]
    [Tooltip("可手工拖入；留空时会在 HintTargets 下按现有层级格式自动生成。")]
    [SerializeField] private ObjectiveHintTarget energyBlockHintTarget;
    [SerializeField] private ObjectiveHintTarget treeRootsHintTarget;
    [SerializeField] private ObjectiveHintTarget portalHintTarget;

    [Tooltip("可选。留空时从 Level2BFlowController 取得未激活 Portal。")]
    [SerializeField] private GameObject portalHintRendererRoot;

    [Header("Triggers")]
    [SerializeField] private GameObject energyTrigger;
    [SerializeField] private GameObject preludeTrigger;

    [Header("Shooting")]
    [SerializeField] private GameObject shootingButtonRoot;

    [Header("Timing")]
    [SerializeField] private float afterSignalLostDialogueDelay = 0.2f;

    private bool siphonAttempted = false;
    private bool rootsDestroyed = false;
    private bool energyCollected = false;
    private bool readyForUfoCheck = false;  // 加这个
    private bool firstUfoViewPlayed = false;
    private bool wasUfoView = false;

    private void Update()
    {
        if (!readyForUfoCheck || firstUfoViewPlayed) return;

        if (viewToggleButton != null && viewToggleButton.IsUfoView() && !wasUfoView)
        {
            firstUfoViewPlayed = true;

            DialogueSequence seq = Level2ADialogueDatabase.Instance?
                .GetSequence(Level2ADialogueIds.Sequences.FirstUfoView);
            if (seq != null)
                DialogueRunner.Instance?.StartDialogue(seq);
        }

        wasUfoView = viewToggleButton != null && viewToggleButton.IsUfoView();
    }

    // Called by DialogueEventRouter: level2a_quest_explore_ufo
    public void OnQuestExploreUfo()
    {
        // 不再需要 objective 提示探索
        // 不再需要激活 trigger

        // 短暂延迟后自动触发发现能源块的对话
        StartCoroutine(AutoDetectEnergy());
    }

    private IEnumerator AutoDetectEnergy()
    {
        // 显示扫描提示
        if (ObjectivePanelController.Instance != null)
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective("UFO scanning the area...");

        yield return new WaitForSeconds(2f);

        DialogueSequence seq = Level2ADialogueDatabase.Instance?
            .GetSequence(Level2ADialogueIds.Sequences.EnergyFound);
        if (seq != null)
            DialogueRunner.Instance?.StartDialogue(seq);
    }

    // Called by DialogueEventRouter: level2a_quest_siphon_energy
    public void OnQuestSiphonEnergy()
    {
        if (ObjectivePanelController.Instance != null)
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective(
                    "Use siphon on the energy block",
                    energyBlockHintTarget
                );

        // Unlock siphon if not already
        if (siphonController != null)
            siphonController.UnlockSiphon();
    }

    // Called when player tries to siphon the energy block but roots block it
    public void OnSiphonAttemptFailed()
    {
        if (siphonAttempted) return;
        siphonAttempted = true;

        // Play siphon failed dialogue
        DialogueSequence seq = Level2ADialogueDatabase.Instance?
            .GetSequence(Level2ADialogueIds.Sequences.SiphonFailed);
        if (seq != null)
            DialogueRunner.Instance?.StartDialogue(seq);
    }

    // Called by DialogueEventRouter: level2a_quest_shoot_roots
    public void OnQuestShootRoots()
    {
        if (ObjectivePanelController.Instance != null)
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective(
                    "Use Shooting to clear the roots",
                    treeRootsHintTarget
                );

        if (shootingButtonRoot != null)
            shootingButtonRoot.SetActive(true);
    }

    // Called when shooting destroys the tree roots
    public void OnRootsDestroyed()
    {
        if (rootsDestroyed) return;
        rootsDestroyed = true;

        if (treeRoots != null)
            treeRoots.SetActive(false);

        // 解除封锁
        SiphonableObject so = energyBlock?.GetComponent<SiphonableObject>();
        if (so != null)
            so.ClearBlocked();

        // Play roots cleared dialogue
        DialogueSequence seq = Level2ADialogueDatabase.Instance?
            .GetSequence(Level2ADialogueIds.Sequences.RootsCleared);
        if (seq != null)
            DialogueRunner.Instance?.StartDialogue(seq);
    }

    // Called by DialogueEventRouter: level2a_quest_siphon_after_roots
    public void OnQuestSiphonAfterRoots()
    {
        if (ObjectivePanelController.Instance != null)
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective(
                    "Use siphon on the energy block",
                    energyBlockHintTarget
                );
    }

    // Called when siphon successfully picks up the energy block
    public void OnEnergyCollected()
    {
        if (energyCollected) return;
        energyCollected = true;

        // 先让 SiphonController 释放，再隐藏物体
        SiphonableObject so = energyBlock?.GetComponent<SiphonableObject>();
        if (so != null && siphonController != null)
            siphonController.ConsumeHeldObject(so, true);  // 这会自动 SetActive(false)

        // Play siphon success dialogue → triggers Signal Lost
        DialogueSequence seq = Level2ADialogueDatabase.Instance?
            .GetSequence(Level2ADialogueIds.Sequences.SiphonSuccess);
        if (seq != null)
            DialogueRunner.Instance?.StartDialogue(seq);
    }

    // Called by DialogueEventRouter: level2a_play_signal_lost
    public void PlaySignalLost()
    {
        if (signalLostOverlay != null)
        {
            StartCoroutine(SignalLostSequence());
        }
    }

    private IEnumerator SignalLostSequence()
    {
        // Play Signal Lost animation
        // (use whatever method your teammate's SignalLostOverlayController has)
        signalLostOverlay.gameObject.SetActive(true);

        // Wait for animation to finish
        yield return new WaitForSeconds(3f);

        signalLostOverlay.gameObject.SetActive(false);

        yield return new WaitForSeconds(afterSignalLostDialogueDelay);

        // Play portal discovered dialogue
        DialogueSequence seq = Level2ADialogueDatabase.Instance?
            .GetSequence(Level2ADialogueIds.Sequences.PortalDiscovered);
        if (seq != null)
            DialogueRunner.Instance?.StartDialogue(seq);
    }

    // Called by DialogueEventRouter: level2a_quest_go_to_portal
    public void OnQuestGoToPortal()
    {
        EnsurePortalHintTarget();

        if (ObjectivePanelController.Instance != null)
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective(
                    "Go to the portal",
                    portalHintTarget
                );
    }

    private void EnsurePortalHintTarget()
    {
        if (portalHintTarget != null)
            return;

        if (portalHintRendererRoot == null &&
            Level2BFlowController.Instance != null)
        {
            portalHintRendererRoot =
                Level2BFlowController.Instance
                    .InactivePortalRoot;
        }

        portalHintTarget = ObjectiveHintRuntimeTargetBuilder.ResolveOrCreate(
            null,
            "Level2PortalHintTarget",
            portalHintRendererRoot
        );
    }

    public void OnShowSwitchHint()
    {
        if (viewToggleButton != null)
        {
            viewToggleButton.gameObject.SetActive(true);
        }
        
        readyForUfoCheck = true;  
        
        if (ObjectivePanelController.Instance != null)
        ObjectivePanelController.Instance
            .CompleteAndSetNextObjective("Switch to UFO view");
    }
}
