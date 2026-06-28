using UnityEngine;

public class NPCDialogueTriggerById : MonoBehaviour
{
    [SerializeField] private MonoBehaviour dialogueDatabase;

    [Header("Dialogue")]
    [SerializeField] private string sequenceId = "villager_a_first_meet";
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许触发（空=无前置要求）。用于让本步只能在上一步完成后触发。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮，则不再触发（可选，用来在更晚的阶段关闭它）。")]
    [SerializeField] private string blockedByFlag = "";

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("3D Dialogue Indicator")]
    [SerializeField] private GameObject dialogueIndicator;

    private IDialogueDatabase database;

    private bool hasTriggered;

    private void Start()
    {
        database = dialogueDatabase as IDialogueDatabase;

        // 如果 cast 失败，尝试从同一个 GameObject 上找
        if (database == null && dialogueDatabase != null)
            database = dialogueDatabase.GetComponent<IDialogueDatabase>() as IDialogueDatabase;


        if (database == null)
        {
            Debug.LogError($"{name}: Dialogue Database not assigned or doesn't implement IDialogueDatabase.");
        }
        RefreshIndicator();
    }

    private void Update()
    {
        // flag 是动态变化的，指示器状态也要随之刷新（解锁后才冒出"!"）
        RefreshIndicator();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        TryStartDialogue();
    }

    private void TryStartDialogue()
    {
        if (!CanStartDialogue())
        {
            return;
        }

        DialogueSequence sequence = database?.GetSequence(sequenceId);
        
        if (sequence == null)
        {
            Debug.LogWarning($"Dialogue sequence not found: {sequenceId}");
            return;
        }

        hasTriggered = true;
        RefreshIndicator();

        DialogueRunner.Instance.StartDialogue(sequence);
    }

    private bool CanStartDialogue()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return false;
        }

        // —— 进度门控 ——
        if (!QuestProgress.Has(requiredFlag))
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(blockedByFlag) && QuestProgress.Has(blockedByFlag))
        {
            return false;
        }

        if (DialogueRunner.Instance != null && DialogueRunner.Instance.IsDialogueOpen)
        {
            return false;
        }

        return true;
    }

    private void RefreshIndicator()
    {
        if (dialogueIndicator == null)
        {
            return;
        }

        bool show = dialogueIndicator.activeSelf;
        bool wanted = CanShowIndicator();
        if (show != wanted)
            dialogueIndicator.SetActive(wanted);
    }

    private bool CanShowIndicator()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return false;
        }
        // 没解锁 / 已被关闭时也不显示指示器
        if (!QuestProgress.Has(requiredFlag))
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(blockedByFlag) && QuestProgress.Has(blockedByFlag))
        {
            return false;
        }

        return true;
    }
}
