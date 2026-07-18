using System.Collections;
using UnityEngine;

/// <summary>
/// 清理堵住门口的木梁。
///
/// TouchDragger 负责拖拽；
/// 本脚本负责：
/// - 显示 Slot Ghost
/// - 自动吸附
/// - 更新中央 Objective
/// - 动态更新 Hint 可用木梁
/// - 完成后播放后续对话
/// </summary>
public class WoodBeamPuzzle : MonoBehaviour
{
    [Header("Drag System")]
    [Tooltip(
        "场景里的 TouchDragger。" +
        "留空时自动查找。"
    )]
    [SerializeField]
    private TouchDragger touchDragger;

    [Header("Puzzle Pieces")]
    [SerializeField]
    private DraggableBeam[] beams;

    [SerializeField]
    private BeamSlot[] slots;

    [Header("Objective")]
    [SerializeField]
    private ObjectiveHintTarget
        objectiveHintTarget;

    [Tooltip(
        "{0} = 已归位数量，{1} = 总数量"
    )]
    [SerializeField]
    private string objectiveFormat =
        "Clear the wooden beams blocking the doorway   {0}/{1}";

    [Header("Tutorial UI")]
    [Tooltip(
        "例如“按住木梁，拖到发光位置”。" +
        "第一根归位后自动隐藏。"
    )]
    [SerializeField]
    private GameObject dragHint;

    [Tooltip(
        "例如“已学会：拖拽”的提示。"
    )]
    [SerializeField]
    private GameObject learnedToast;

    [SerializeField]
    private float learnedToastSeconds = 2f;

    [Header("On Complete")]
    [SerializeField]
    private string nextSequenceId =
        DialogueIds.Sequences
            .VillagerCFirstHouseAfterBeams;

    [Tooltip(
        "建议至少 1 秒，让 Objective 完成动画结束后再播放对话。"
    )]
    [SerializeField]
    private float delayBeforeNextDialogue = 1.1f;

    private ObjectiveHintOutline[]
        beamHintOutlines;

    private bool armed;
    private bool revealed;
    private bool solved;

    private int placedCount;

    private DraggableBeam currentBeam;

    private void Start()
    {
        ResolveTouchDragger();
        CacheBeamHintOutlines();

        SetSlotGhostsVisible(false);

        if (dragHint != null)
            dragHint.SetActive(false);

        if (learnedToast != null)
            learnedToast.SetActive(false);

        BindBeams();

        // 根据场景初始状态同步一次。
        RefreshPlacedCountAndEligibility();
    }

    /// <summary>
    /// 绑定到 DialogueEventRouter：
    /// quest_clear_wood_beams
    /// </summary>
    public void BeginPuzzle()
    {
        if (revealed || solved)
            return;

        armed = true;
    }

    private void Update()
    {
        bool dialogueOpen =
            DialogueRunner.Instance != null &&
            DialogueRunner.Instance
                .IsDialogueOpen;

        // 对话结束后才正式显示玩法。
        if (armed &&
            !revealed &&
            !dialogueOpen)
        {
            Reveal();
        }

        if (!revealed ||
            solved ||
            dialogueOpen)
        {
            return;
        }

        TryAutoSnap();
    }

    private void Reveal()
    {
        revealed = true;

        RefreshPlacedCountAndEligibility();
        SetSlotGhostsVisible(true);

        if (beams != null)
        {
            foreach (DraggableBeam beam in beams)
            {
                if (beam == null ||
                    beam.IsPlaced)
                {
                    continue;
                }

                beam.SetGrabbable(true);
            }
        }

        if (dragHint != null &&
            placedCount == 0)
        {
            dragHint.SetActive(true);
        }

        PublishObjective();
    }

    public void OnBeamGrabbed(
        DraggableBeam beam)
    {
        if (!revealed ||
            solved ||
            beam == null ||
            beam.IsPlaced)
        {
            return;
        }

        currentBeam = beam;
    }

    public void OnBeamReleased(
        DraggableBeam beam)
    {
        if (currentBeam == beam)
            currentBeam = null;
    }

    private void TryAutoSnap()
    {
        if (currentBeam == null ||
            currentBeam.IsPlaced)
        {
            return;
        }

        BeamSlot slot =
            FindNearestFreeSlot(
                currentBeam.transform.position
            );

        if (slot == null)
            return;

        DraggableBeam beam = currentBeam;
        currentBeam = null;

        if (touchDragger != null)
            touchDragger.ForceRelease();

        Place(beam, slot);
    }

    private void Place(
        DraggableBeam beam,
        BeamSlot slot)
    {
        if (beam == null ||
            slot == null ||
            beam.IsPlaced)
        {
            return;
        }

        beam.PlaceAt(
            slot.SnapPosition,
            slot.SnapRotation
        );

        slot.MarkOccupied();

        SetBeamHintEligible(
            beam,
            false
        );

        if (dragHint != null)
            dragHint.SetActive(false);

        RefreshPlacedCount();
        PublishObjective();

        int total =
            beams != null
                ? beams.Length
                : 0;

        if (total > 0 &&
            placedCount >= total)
        {
            StartCoroutine(
                CompleteRoutine()
            );
        }
    }

    private BeamSlot FindNearestFreeSlot(
        Vector3 position)
    {
        BeamSlot best = null;
        float bestDistance =
            float.MaxValue;

        if (slots == null)
            return null;

        foreach (BeamSlot slot in slots)
        {
            if (slot == null ||
                slot.IsOccupied)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    position,
                    slot.SnapPosition
                );

            if (distance <=
                    slot.AcceptRadius &&
                distance < bestDistance)
            {
                best = slot;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void PublishObjective()
    {
        ObjectivePanelController panel =
            ObjectivePanelController.Instance;

        if (panel == null)
        {
            Debug.LogWarning(
                "WoodBeamPuzzle: " +
                "ObjectivePanelController is missing.",
                this
            );

            return;
        }

        if (!panel.gameObject.activeSelf)
            panel.gameObject.SetActive(true);

        panel.SetObjective(
            BuildObjectiveText(),
            objectiveHintTarget
        );
    }

    private string BuildObjectiveText()
    {
        int total =
            beams != null
                ? beams.Length
                : 0;

        return string.Format(
            objectiveFormat,
            placedCount,
            total
        );
    }

    private IEnumerator CompleteRoutine()
    {
        if (solved)
            yield break;

        solved = true;
        currentBeam = null;

        if (touchDragger != null)
            touchDragger.ForceRelease();

        DisableAllBeamHints();

        ObjectivePanelController.Instance
            ?.CompleteCurrentObjective();

        if (dragHint != null)
            dragHint.SetActive(false);

        if (learnedToast != null)
        {
            learnedToast.SetActive(true);

            yield return
                new WaitForSecondsRealtime(
                    learnedToastSeconds
                );

            learnedToast.SetActive(false);
        }

        if (delayBeforeNextDialogue > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    delayBeforeNextDialogue
                );
        }

        StartNextDialogue();
    }

    private void StartNextDialogue()
    {
        if (string.IsNullOrWhiteSpace(
                nextSequenceId))
        {
            return;
        }

        DialogueSequence sequence =
            Level1DialogueDatabase.Instance
                ?.GetSequence(
                    nextSequenceId
                );

        if (sequence == null)
        {
            Debug.LogWarning(
                "WoodBeamPuzzle: " +
                "next sequence not found: " +
                nextSequenceId,
                this
            );

            return;
        }

        DialogueRunner.Instance
            ?.StartDialogue(sequence);
    }

    private void ResolveTouchDragger()
    {
        if (touchDragger == null)
        {
            touchDragger =
                FindObjectOfType
                    <TouchDragger>();
        }
    }

    private void BindBeams()
    {
        if (beams == null)
            return;

        foreach (DraggableBeam beam in beams)
        {
            if (beam != null)
                beam.Bind(this);
        }
    }

    private void CacheBeamHintOutlines()
    {
        int count =
            beams != null
                ? beams.Length
                : 0;

        beamHintOutlines =
            new ObjectiveHintOutline[count];

        for (int i = 0;
             i < count;
             i++)
        {
            DraggableBeam beam =
                beams[i];

            if (beam == null)
                continue;

            beamHintOutlines[i] =
                beam.GetComponentInChildren
                    <ObjectiveHintOutline>(
                        true
                    );

            if (beamHintOutlines[i] == null)
            {
                Debug.LogWarning(
                    "WoodBeamPuzzle: " +
                    beam.name +
                    " 没有 ObjectiveHintOutline。",
                    beam
                );
            }
        }
    }

    private void RefreshPlacedCountAndEligibility()
    {
        placedCount = 0;

        if (beams == null)
            return;

        for (int i = 0;
             i < beams.Length;
             i++)
        {
            DraggableBeam beam =
                beams[i];

            if (beam == null)
                continue;

            bool isPlaced =
                beam.IsPlaced;

            if (isPlaced)
                placedCount++;

            if (beamHintOutlines != null &&
                i < beamHintOutlines.Length &&
                beamHintOutlines[i] != null)
            {
                beamHintOutlines[i]
                    .SetHintEligible(
                        !isPlaced
                    );
            }
        }
    }

    private void RefreshPlacedCount()
    {
        placedCount = 0;

        if (beams == null)
            return;

        foreach (DraggableBeam beam in beams)
        {
            if (beam != null &&
                beam.IsPlaced)
            {
                placedCount++;
            }
        }
    }

    private void SetBeamHintEligible(
        DraggableBeam beam,
        bool eligible)
    {
        if (beam == null ||
            beams == null ||
            beamHintOutlines == null)
        {
            return;
        }

        for (int i = 0;
             i < beams.Length;
             i++)
        {
            if (beams[i] != beam)
                continue;

            ObjectiveHintOutline outline =
                i < beamHintOutlines.Length
                    ? beamHintOutlines[i]
                    : null;

            if (outline != null)
            {
                outline.SetHintEligible(
                    eligible
                );
            }

            return;
        }
    }

    private void DisableAllBeamHints()
    {
        if (beamHintOutlines == null)
            return;

        foreach (ObjectiveHintOutline outline
                 in beamHintOutlines)
        {
            if (outline != null)
            {
                outline.SetHintEligible(false);
            }
        }
    }

    private void SetSlotGhostsVisible(
        bool visible)
    {
        if (slots == null)
            return;

        foreach (BeamSlot slot in slots)
        {
            if (slot != null)
                slot.SetGhostVisible(visible);
        }
    }
}