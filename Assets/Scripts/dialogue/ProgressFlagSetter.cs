using UnityEngine;

/// <summary>
/// 在 DialogueEventRouter 的事件绑定里调用，用来在"某一步完成"时点亮一个进度 flag，
/// 从而解锁后面的触发器。
///
/// 用法：场景里放一个挂了本脚本的物体，在 DialogueEventRouter 的某个 Quest 事件（如
/// QuestGoToWell）的 UnityEvent 上添加这个物体 → ProgressFlagSetter.SetFlag，参数填要点亮的
/// flag 名（如 "well_unlocked"）。这样那段对话一结束，flag 就置位，门控就放行下一步。
/// </summary>
public class ProgressFlagSetter : MonoBehaviour
{
    /// <summary>UnityEvent 里直接调用：点亮一个 flag。</summary>
    public void SetFlag(string flag)
    {
        QuestProgress.Set(flag, true);
        Debug.Log($"[Progress] flag set: {flag}");
    }

    /// <summary>需要清除某个 flag 时用。</summary>
    public void ClearFlag(string flag)
    {
        QuestProgress.Set(flag, false);
    }
}
