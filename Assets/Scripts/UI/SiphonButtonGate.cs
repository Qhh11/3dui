using UnityEngine;
using UnityEngine.UI;
 
/// <summary>
/// 按剧情进度解锁 siphon 按钮：未点亮 requiredFlag 时隐藏且不可点，点亮后显示可用。
/// 挂在 siphon 按钮上即可。
///
/// 重要：这里用 CanvasGroup 来隐藏（alpha=0 + 不接收点击），而不是 SetActive。
/// 因为如果用 SetActive 把按钮自己关掉，本脚本的 Update 也会停，之后 flag 点亮就再也收不到了。
///
/// 解锁：在 DialogueEventRouter 给 unlock_siphon 事件绑 ProgressFlagSetter.SetFlag("siphon_unlocked")，
/// 这里的 requiredFlag 填同样的 "siphon_unlocked"。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SiphonButtonGate : MonoBehaviour
{
    [Header("门控")]
    [Tooltip("必须已点亮这个 flag，按钮才显示可用（空=一直可用）。要和 SetFlag 的名字完全一致。")]
    public string requiredFlag = "siphon_unlocked";
 
    [Header("目标（留空自动取本物体上的）")]
    [Tooltip("用来隐藏/显示的 CanvasGroup；留空自动取/自动加到本物体")]
    public CanvasGroup canvasGroup;
    [Tooltip("可选：额外控制这个 Button 的 interactable")]
    public Button button;
 
    private bool applied;
    private bool lastUnlocked;
 
    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (button == null) button = GetComponent<Button>();
    }
 
    void OnEnable()
    {
        applied = false;   // 重新启用时强制应用一次当前状态
    }
 
    void Update()
    {
        bool unlocked = QuestProgress.Has(requiredFlag);
        if (!applied || unlocked != lastUnlocked)
            Apply(unlocked);
    }
 
    private void Apply(bool unlocked)
    {
        applied = true;
        lastUnlocked = unlocked;
 
        if (canvasGroup != null)
        {
            canvasGroup.alpha = unlocked ? 1f : 0f;   // 隐藏但物体仍 active，Update 继续跑
            canvasGroup.interactable = unlocked;
            canvasGroup.blocksRaycasts = unlocked;
        }
 
        if (button != null) button.interactable = unlocked;
    }
}
 