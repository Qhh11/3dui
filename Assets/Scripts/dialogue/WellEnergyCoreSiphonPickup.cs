using UnityEngine;

// 挂在"老井底部能量块"这个物体上,和 SiphonableObject 是同一个 GameObject
// 写法参照 TowerEnergyCoreSiphonPickup,保持项目内一致的判断方式:
// IsHolding(obj) + localPosition.magnitude 判断"是否已经稳定停在手上"
public class WellEnergyCoreSiphonPickup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SiphonController siphonController;
    [SerializeField] private SiphonableObject siphonableObject;

    [Header("Collect Condition")]
    [SerializeField] private float collectLocalDistance = 0.6f;
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Progression Gate")]
    [Tooltip("必须已点亮这个 flag 才允许被吸取拾取（空=无前置）。")]
    [SerializeField] private string requiredFlag = "";
    [Tooltip("若这个 flag 已点亮则不再允许拾取（可选）。")]
    [SerializeField] private string blockedByFlag = "";

    private bool hasCollected;

    private void Awake()
    {
        if (siphonableObject == null)
        {
            siphonableObject = GetComponent<SiphonableObject>();
        }

        if (siphonController == null)
        {
            siphonController = FindObjectOfType<SiphonController>();
        }
    }

    private void Update()
    {
        if (triggerOnlyOnce && hasCollected)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(requiredFlag) && !QuestProgress.Has(requiredFlag))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(blockedByFlag) && QuestProgress.Has(blockedByFlag))
        {
            return;
        }

        if (siphonController == null || siphonableObject == null)
        {
            return;
        }

        if (!siphonController.IsHolding(siphonableObject))
        {
            return;
        }

        if (transform.localPosition.magnitude > collectLocalDistance)
        {
            return;
        }

        Collect();
    }

    private void Collect()
    {
        if (triggerOnlyOnce && hasCollected)
        {
            return;
        }

        hasCollected = true;

        Debug.Log("Well energy unit collected by siphon.");

        // 手动触发它自己的 On Placed 事件,
        // 复用 Inspector 里已经配好的 DelayedDialogueTrigger,
        // 不需要玩家真的把它放到某个地方
        siphonableObject.onPlaced?.Invoke();

        siphonController.ConsumeHeldObject(siphonableObject, true);
    }
}

// =============================================================
// Inspector 接线步骤(老井底能量块 Prefab)
// =============================================================
// 1. 挂上这个脚本
// 2. Siphonable Object 不填会自动 GetComponent 找同物体上的;
//    Siphon Controller 不填会自动 FindObjectOfType 找场景里的
// 3. Required Flag / Blocked By Flag 这两个先留空即可(井这里不需要前置条件),
//    以后如果想加"必须先怎样才能吸取"的限制,直接填对应 flag 字符串
// 4. On Picked Up / On Placed 里原有的配置完全不用动
//
// 注意: 上一轮我们讨论过 DelayedDialogueTrigger 如果挂在能量块自身,
// 消耗时的 SetActive(false) 可能打断它的延迟协程。这版代码把
// onPlaced.Invoke() 放在 ConsumeHeldObject 之前调用,只要 delay
// 不是特别长、且 DelayedDialogueTrigger 挂在能量块自身上,
// 协程会在 SetActive(false) 生效前的这一帧内正常启动;
// 但只有当 DelayedDialogueTrigger 是独立常驻物体时才能保证
// delay 时间内不被打断。建议还是按上一条回复确认一下它具体挂在哪。