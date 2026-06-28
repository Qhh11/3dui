using System.Collections;
using UnityEngine;

public class WellEnergyDiscoveryAction : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private string ufoViewSequenceId = "well_ufo_view";
    [SerializeField] private float delayAfterSwitch = 2f;

    [Header("Energy Block")]
    [SerializeField] private GameObject energyHighlight;
    [SerializeField] private GameObject outlineEffect;

    [Header("Objective")]
    [SerializeField]
    private string objectiveText =
        "Switch to UFO view near the well";

    private bool hasTriggered;

    public void Trigger()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;

        if (ObjectivePanelController.Instance != null)
        {
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective(objectiveText);
        }

        StartCoroutine(WaitForUfoThenPlay());
    }

    private IEnumerator WaitForUfoThenPlay()
    {
        // 等玩家切到UFO视角
        while (!IsInUfoView())
        {
            yield return null;
        }
        // ← 切到UFO后立刻显示球(UFO视角能看到)
        if (energyHighlight != null)
            energyHighlight.SetActive(true);

        if (outlineEffect != null)
            outlineEffect.SetActive(true);

        yield return new WaitForSecondsRealtime(
            delayAfterSwitch
        );

        // 等对话框关闭
        while (DialogueRunner.Instance != null
            && DialogueRunner.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        DialogueSequence seq =
            Level1DialogueDatabase.Instance
                .GetSequence(ufoViewSequenceId);

        if (seq != null)
        {
            DialogueRunner.Instance.StartDialogue(seq);
        }

        while (DialogueRunner.Instance != null &&
               DialogueRunner.Instance.IsDialogueOpen)
        {
            yield return null;
        }

        if (ObjectivePanelController.Instance != null)
        {
            ObjectivePanelController.Instance
                .CompleteAndSetNextObjective(
                    "Energy signature marked. Switch back and investigate."
                );
        }
    }

    private bool IsInUfoView()
    {
        if (SwitchPerspectiveButtonController.Instance != null)
        {
            return SwitchPerspectiveButtonController
                .Instance
                .IsUfoView;
        }

        return false;
    }
}