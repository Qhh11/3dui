using System.Collections;
using UnityEngine;

public class NPCDialogueReaction : MonoBehaviour
{
    public enum NPCType
    {
        Guard,      // 村民A：不走动,对话时后退一步
        Fearful,    // 居民B：正常走,对话时冲向玩家
        Anxious,    // 居民C：正常走,对话时快步迎上来
        Relaxed     // 居民D：不走动,对话时也不动
    }

    [Header("NPC类型")]
    [SerializeField] private NPCType npcType = NPCType.Guard;

    private Animator animator;
    private bool wasTalking = false;
    private bool reactionPlayed = false;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Guard和Relaxed一开始就不走动
        if (npcType == NPCType.Guard
            || npcType == NPCType.Relaxed)
        {
            if (animator)
            {
                animator.applyRootMotion = false;
                animator.SetBool("isTalking", true);
                // 直接切到Idle,不走
            }
        }
    }

    void Update()
    {
        bool isTalking = DialogueRunner.Instance != null
            && DialogueRunner.Instance.IsDialogueOpen;

        if (isTalking && !wasTalking)
        {
            OnDialogueStart();
        }
        else if (!isTalking && wasTalking)
        {
            OnDialogueEnd();
        }

        // 对话中持续面向玩家(Relaxed除外)
        if (isTalking && npcType != NPCType.Relaxed)
        {
            FacePlayer();
        }

        wasTalking = isTalking;
    }

    void OnDialogueStart()
    {
        // 所有NPC停下来
        if (animator)
        {
            animator.applyRootMotion = false;
            animator.SetBool("isTalking", true);
        }

        // 根据类型做不同反应(只播一次)
        if (!reactionPlayed)
        {
            reactionPlayed = true;

            switch (npcType)
            {
                case NPCType.Guard:
                    StartCoroutine(StepBack());
                    break;
                case NPCType.Fearful:
                    StartCoroutine(RushToPlayer());
                    break;
                case NPCType.Anxious:
                    StartCoroutine(ApproachPlayer());
                    break;
                case NPCType.Relaxed:
                    // 什么都不做
                    break;
            }
        }
    }

    void OnDialogueEnd()
    {
        if (animator)
        {
            animator.SetBool("isTalking", false);
        }

        // Guard和Relaxed对话结束后也不走
        if (npcType == NPCType.Guard
            || npcType == NPCType.Relaxed)
        {
            return;
        }

        // Fearful和Anxious恢复走动
        if (animator)
        {
            animator.applyRootMotion = true;
        }
    }

    // ====== Guard：后退一小步 ======
    IEnumerator StepBack()
    {
        Vector3 backDir = GetAwayFromPlayer();
        Vector3 start = transform.position;
        Vector3 target = start + backDir * 1.5f;
        float elapsed = 0f;

        while (elapsed < 0.4f)
        {
            transform.position = Vector3.Lerp(
                start, target, elapsed / 0.4f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
    }

    // ====== Fearful：冲向玩家拦路 ======
    IEnumerator RushToPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) yield break;

        // 停在玩家前面1.5米
        Vector3 dir = GetAwayFromPlayer();
        Vector3 target = player.transform.position
            + dir * 1.5f;

        Vector3 start = transform.position;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(
                start, target, elapsed / duration);
            FacePlayer();
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
    }

    // ====== Anxious：快步走向玩家 ======
    IEnumerator ApproachPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) yield break;

        Vector3 dir = GetAwayFromPlayer();
        Vector3 target = player.transform.position
            + dir * 2f;

        float speed = 2f;

        while (Vector3.Distance(
            transform.position, target) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target,
                speed * Time.deltaTime);
            FacePlayer();
            yield return null;
        }
    }

    // ====== 工具方法 ======
    void FacePlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector3 dir = player.transform.position
            - transform.position;
        dir.y = 0;

        if (dir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                8f * Time.deltaTime);
        }
    }

    Vector3 GetAwayFromPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return -transform.forward;

        Vector3 dir = (transform.position
            - player.transform.position).normalized;
        dir.y = 0;
        return dir;
    }
}