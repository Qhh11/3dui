using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 能量魔方 HUD 栏：固定 N 个空位，每吸取一个魔方点亮下一格。
/// 计数用静态字段保存，跨场景有效、进入 Play 时清零。
///
/// 点亮方式二选一：
///   代码里    EnergyCubeBar.Collect();
///   UnityEvent 里绑本组件的 AddOne()
/// </summary>
public class EnergyCubeBar : MonoBehaviour
{
    public static EnergyCubeBar Instance { get; private set; }

    private static int collected;   // 跨场景保留

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { collected = 0; }

    [Header("空位的'填充'图（每格一个，按顺序；初始会被隐藏）")]
    [SerializeField] private Image[] slotFills;

    [Header("点亮动画")]
    [SerializeField] private bool animatePop = true;
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popTime = 0.18f;

    void Awake() { Instance = this; }
    void OnEnable() { Refresh(false); }   // 场景里出现时按当前计数显示

    /// <summary>吸到一个魔方：点亮下一格。</summary>
    public static void Collect()
    {
        int max = Instance != null ? Instance.SlotCount : int.MaxValue;
        if (collected >= max) return;
        collected++;
        if (Instance != null) Instance.Refresh(true);
    }

    /// <summary>供 UnityEvent 绑定（等同 Collect）。</summary>
    public void AddOne() => Collect();

    /// <summary>直接设定已点亮数量。</summary>
    public static void SetCount(int n)
    {
        collected = Mathf.Max(0, n);
        if (Instance != null) Instance.Refresh(false);
    }

    public static int Count => collected;
    public bool IsFull => slotFills != null && collected >= slotFills.Length;

    private int SlotCount => slotFills != null ? slotFills.Length : 0;

    private void Refresh(bool animateLast)
    {
        if (slotFills == null) return;
        collected = Mathf.Clamp(collected, 0, slotFills.Length);

        for (int i = 0; i < slotFills.Length; i++)
        {
            if (slotFills[i] == null) continue;
            slotFills[i].enabled = i < collected;          // 点亮=显示填充图
            slotFills[i].transform.localScale = Vector3.one;
        }

        if (animateLast && animatePop && collected > 0 && slotFills[collected - 1] != null)
        {
            StopAllCoroutines();
            StartCoroutine(Pop(slotFills[collected - 1].transform));
        }
    }

    private IEnumerator Pop(Transform t)
    {
        float e = 0f;
        while (e < popTime)
        {
            e += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(popScale, 1f, e / popTime);
            t.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}
