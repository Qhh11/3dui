using System.Collections.Generic;

/// <summary>
/// 任务进度的统一读写入口（自带存储，不依赖任何其它脚本，保证能编译）。
/// 门控触发器读它，ProgressFlagSetter 写它。运行期有效（重开场景/重启会清空）。
/// </summary>
public static class QuestProgress
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    /// <summary>flag 是否已点亮。空字符串视为"无要求"，永远放行。</summary>
    public static bool Has(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return true;
        return flags.Contains(flag);
    }

    /// <summary>多个 flag 必须全部点亮。</summary>
    public static bool HasAll(params string[] keys)
    {
        if (keys == null) return true;
        foreach (var k in keys)
            if (!Has(k)) return false;
        return true;
    }

    /// <summary>点亮 / 熄灭一个 flag。</summary>
    public static void Set(string flag, bool value = true)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;
        if (value) flags.Add(flag);
        else flags.Remove(flag);
    }

    public static void Clear() => flags.Clear();
}
