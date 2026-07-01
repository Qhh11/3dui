using System.Collections.Generic;
using UnityEngine;

/// <summary>视角模式，供高亮按视角过滤。</summary>
public enum ViewMode { FirstPerson, ThirdPerson }

/// <summary>
/// 检测进入范围内的物体并点亮亮框，离开范围则熄灭。
/// 支持多组：每组一个 Layer + 颜色 + 在哪个视角显示。
/// 检测距离按视角分别设置(第一人称 / 第三人称)。
/// 需要门控时填 requiredFlag：未点亮该 flag 时熄灭全部、不检测。
/// </summary>
public class DraggableHighlighter : MonoBehaviour
{
    public enum GroupVisibility { Both, FirstPersonOnly, ThirdPersonOnly }

    [System.Serializable]
    public class HighlightGroup
    {
        [Tooltip("仅作辨认用，如 Draggable / Siphonable")]
        public string name = "Group";
        [Tooltip("这一组对应的 Layer")]
        public LayerMask mask;
        [Tooltip("这一组的框颜色")]
        public Color color = Color.yellow;
        [Tooltip("这一组在哪个视角显示")]
        public GroupVisibility visibleIn = GroupVisibility.Both;
    }

    [Tooltip("以谁为中心算距离；留空用自身(一般用相机)")]
    public Transform origin;
    [Tooltip("当前视角，由切换视角的脚本设置(见 SetView)")]
    public ViewMode currentView = ViewMode.FirstPerson;

    [Header("门控")]
    [Tooltip("必须已点亮这个 flag 才显示高亮（空=无要求，一直显示）。与 TouchDragger 填同一个即可联动。")]
    public string requiredFlag = "";

    [Tooltip("每个 Layer 配一组：层、颜色、显示视角")]
    public HighlightGroup[] groups;

    [Header("显示距离(按视角)")]
    [Tooltip("第一人称的点亮距离")]
    public float firstPersonRange = 500f;
    [Tooltip("第三人称的点亮距离")]
    public float thirdPersonRange = 10000f;

    [Tooltip("检测间隔(秒)，省点性能")]
    public float refreshInterval = 0.1f;
    [Tooltip("给没有 DraggableHighlight 的物体自动补上")]
    public bool autoAddHighlight = true;

    private readonly HashSet<DraggableHighlight> current = new();
    private readonly HashSet<DraggableHighlight> found = new();
    private readonly Collider[] buffer = new Collider[64];
    private float timer;

    void Start()
    {
        if (origin == null) origin = transform;
    }

    void Update()
    {
        // 门控：flag 没点亮 → 熄灭全部、不做检测
        if (!QuestProgress.Has(requiredFlag))
        {
            if (current.Count > 0) ClearAllHighlights();
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = refreshInterval;
        Refresh();
    }

    /// <summary>切换视角时调用，立刻刷新一次高亮。</summary>
    public void SetView(ViewMode v)
    {
        currentView = v;
        timer = 0f;
    }

    private float CurrentRange()
    {
        return currentView == ViewMode.FirstPerson ? firstPersonRange : thirdPersonRange;
    }

    private int CombinedMask()
    {
        int m = 0;
        if (groups != null) foreach (var g in groups) m |= g.mask.value;
        return m;
    }

    private HighlightGroup GetGroup(int layer)
    {
        if (groups != null)
            foreach (var g in groups)
                if ((g.mask.value & (1 << layer)) != 0) return g;
        return null;
    }

    private bool VisibleNow(GroupVisibility v)
    {
        switch (v)
        {
            case GroupVisibility.FirstPersonOnly: return currentView == ViewMode.FirstPerson;
            case GroupVisibility.ThirdPersonOnly: return currentView == ViewMode.ThirdPerson;
            default: return true; // Both
        }
    }

    private void Refresh()
    {
        found.Clear();
        int n = Physics.OverlapSphereNonAlloc(origin.position, CurrentRange(), buffer, CombinedMask());
        for (int i = 0; i < n; i++)
        {
            Collider col = buffer[i];
            HighlightGroup g = GetGroup(col.gameObject.layer);
            if (g == null || !VisibleNow(g.visibleIn)) continue; // 当前视角不显示这组 → 跳过(会被熄灭)

            var h = col.GetComponentInParent<DraggableHighlight>();
            if (h == null && autoAddHighlight)
                h = col.gameObject.AddComponent<DraggableHighlight>();
            if (h == null) continue;

            h.SetColor(g.color);
            found.Add(h);
            if (!current.Contains(h)) h.SetHighlighted(true);
        }

        foreach (var h in current)
            if (!found.Contains(h)) h.SetHighlighted(false);

        current.Clear();
        foreach (var h in found) current.Add(h);
    }

    private void ClearAllHighlights()
    {
        foreach (var h in current) if (h != null) h.SetHighlighted(false);
        current.Clear();
    }

    void OnDisable() // 禁用时熄灭所有
    {
        ClearAllHighlights();
    }
}
