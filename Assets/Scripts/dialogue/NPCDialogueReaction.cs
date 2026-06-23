using UnityEngine;

public class NPCDialogueReaction : MonoBehaviour
{
    private Animator animator;
    private bool wasTalking = false;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (animator)
        {
            animator.applyRootMotion = false;
        }
    }

    void Update()
    {
        bool isTalking = DialogueRunner.Instance != null
            && DialogueRunner.Instance.IsDialogueOpen;

        if (isTalking && !wasTalking)
        {
            if (animator)
                animator.SetBool("isTalking", true);
        }
        else if (!isTalking && wasTalking)
        {
            if (animator)
                animator.SetBool("isTalking", false);
        }

        if (isTalking)
        {
            FacePlayer();
        }

        wasTalking = isTalking;
    }

    void FacePlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0;

        if (dir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                8f * Time.deltaTime);
        }
    }
}