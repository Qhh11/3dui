using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// "清理堵住门口的木梁" 小玩法。
/// 拖拽由 TouchDragger 负责；本脚本负责：露虚影 + 计数 + 目标UI + 完成接对话。
///
/// 吸附方式：木梁被拖进某个空 slot 的 AcceptRadius 内就【自动吸附】，
/// 不需要松手。吸附瞬间会让 TouchDragger.ForceRelease() 立刻放手。
///
/// 把这个物体保持在所有 ControlLockGroup / Behaviours To Disable While Open 之外，
/// 它才能在对话期间继续运行、并察觉对话框关闭。
/// </summary>
public class WoodBeamPuzzle : MonoBehaviour
{
    [Header("Drag system")]
    [Tooltip("场景里的 TouchDragger，自动吸附时叫它松手。留空会自动 FindObjectOfType。")]
    [SerializeField] private TouchDragger touchDragger;

    [Header("Puzzle pieces")]
    [SerializeField] private DraggableBeam[] beams;
    [SerializeField] private BeamSlot[] slots;

    [Header("Objective UI")]
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private TMP_Text objectiveText;

    [Tooltip("{0} = 已放, {1} = 总数")]
    [SerializeField] private string objectiveFormat = "清理堵住门口的木梁   {0}/{1}";

    [Header("Hints")]
    [Tooltip("\"按住木梁，拖到发光位置\". 第一根放好后自动隐藏")]
    [SerializeField] private GameObject dragHint;

    [Tooltip("\"已学会 — 拖拽\" toast，解谜完成后显示")]
    [SerializeField] private GameObject learnedToast;
    [SerializeField] private float learnedToastSeconds = 2f;

    [Header("On complete")]
    [SerializeField] private string nextSequenceId = DialogueIds.Sequences.VillagerCFirstHouseAfterBeams;
    [SerializeField] private float delayBeforeNextDialogue = 0.3f;

    private bool armed;        // BeginPuzzle 已调用，等对话框关闭
    private bool revealed;     // 虚影+目标已显示
    private bool solved;
    private int placedCount;

    private DraggableBeam currentBeam;   // 当前正被 TouchDragger 拖着的木梁

    private void Start()
    {
        if (touchDragger == null)
        {
            touchDragger = FindObjectOfType<TouchDragger>();
        }

        // 解谜开始前保持隐藏
        SetSlotGhostsVisible(false);
        if (objectivePanel != null) objectivePanel.SetActive(false);
        if (dragHint != null) dragHint.SetActive(false);
        if (learnedToast != null) learnedToast.SetActive(false);

        // 让每根木梁拾起/松手/释放时回调本脚本
        if (beams != null)
        {
            foreach (DraggableBeam beam in beams)
            {
                if (beam != null) beam.Bind(this);
            }
        }
    }

    /// <summary>绑到 DialogueEventRouter 的 "quest_clear_wood_beams"。</summary>
    public void BeginPuzzle()
    {
        if (revealed || solved) return;
        armed = true;
    }

    private void Update()
    {
        bool dialogueOpen = DialogueRunner.Instance != null && DialogueRunner.Instance.IsDialogueOpen;

        // 对话框关闭后才露出玩法
        if (armed && !revealed && !dialogueOpen)
        {
            Reveal();
        }

        if (!revealed || solved || dialogueOpen)
        {
            return;
        }

        // 每帧检查：当前拖着的木梁是否已进入某个空 slot 的吸附范围
        TryAutoSnap();
    }

    private void Reveal()
    {
        revealed = true;

        SetSlotGhostsVisible(true);

        if (beams != null)
        {
            foreach (DraggableBeam beam in beams)
            {
                if (beam != null) beam.SetGrabbable(true);
            }
        }

        if (objectivePanel != null) objectivePanel.SetActive(true);
        if (dragHint != null) dragHint.SetActive(true);

        UpdateObjective();
    }

    // ----- DraggableBeam 通过这两个方法告诉我"谁正被拖" -----

    public void OnBeamGrabbed(DraggableBeam beam)
    {
        currentBeam = beam;
    }

    public void OnBeamReleased(DraggableBeam beam)
    {
        // 玩家自己松手(没吸附到)：清空当前拖拽，木梁留在原地不动。
        if (currentBeam == beam)
        {
            currentBeam = null;
        }
    }

    // ----- 自动吸附核心 -----

    private void TryAutoSnap()
    {
        if (currentBeam == null || currentBeam.IsPlaced) return;

        BeamSlot slot = FindNearestFreeSlot(currentBeam.transform.position);
        if (slot == null) return;   // 还没拖到任何 slot 的范围内

        DraggableBeam beam = currentBeam;
        currentBeam = null;

        // 先让拖拽系统立刻松手，避免它继续把已吸附的木梁拽走
        if (touchDragger != null) touchDragger.ForceRelease();

        Place(beam, slot);
    }

    private void Place(DraggableBeam beam, BeamSlot slot)
    {
        beam.PlaceAt(slot.SnapPosition, slot.SnapRotation);
        slot.MarkOccupied();
        placedCount++;

        if (dragHint != null) dragHint.SetActive(false);

        UpdateObjective();

        if (beams != null && placedCount >= beams.Length)
        {
            StartCoroutine(CompleteRoutine());
        }
    }

    private BeamSlot FindNearestFreeSlot(Vector3 position)
    {
        BeamSlot best = null;
        float bestDist = float.MaxValue;

        if (slots == null) return null;

        foreach (BeamSlot slot in slots)
        {
            if (slot == null || slot.IsOccupied) continue;

            float dist = Vector3.Distance(position, slot.SnapPosition);
            if (dist <= slot.AcceptRadius && dist < bestDist)
            {
                bestDist = dist;
                best = slot;
            }
        }

        return best;
    }

    private void UpdateObjective()
    {
        if (objectiveText != null)
        {
            int total = beams != null ? beams.Length : 0;
            objectiveText.text = string.Format(objectiveFormat, placedCount, total);
        }
    }

    private IEnumerator CompleteRoutine()
    {
        solved = true;

        if (objectivePanel != null) objectivePanel.SetActive(false);
        if (dragHint != null) dragHint.SetActive(false);

        if (learnedToast != null)
        {
            learnedToast.SetActive(true);
            yield return new WaitForSecondsRealtime(learnedToastSeconds);
            learnedToast.SetActive(false);
        }

        if (delayBeforeNextDialogue > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeNextDialogue);
        }

        StartNextDialogue();
    }

    private void StartNextDialogue()
    {
        if (string.IsNullOrWhiteSpace(nextSequenceId)) return;

        DialogueSequence sequence = Level1DialogueDatabase.Instance?.GetSequence(nextSequenceId);
        if (sequence == null)
        {
            Debug.LogWarning($"WoodBeamPuzzle: next sequence not found: {nextSequenceId}");
            return;
        }

        DialogueRunner.Instance?.StartDialogue(sequence);
    }

    private void SetSlotGhostsVisible(bool visible)
    {
        if (slots == null) return;

        foreach (BeamSlot slot in slots)
        {
            if (slot != null) slot.SetGhostVisible(visible);
        }
    }
}
