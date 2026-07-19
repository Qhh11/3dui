using UnityEngine;

/// <summary>
/// 一个 Objective 对应的 Hint 目标。
/// </summary>
[DisallowMultipleComponent]
public class ObjectiveHintTarget : MonoBehaviour
{
    [Header("Camera Focus")]
    [Tooltip(
        "建议放在目标视觉中心。留空时使用 Renderer 合并 Bounds 中心。"
    )]
    [SerializeField]
    private Transform focusPoint;

    [Header("Outlines")]
    [Tooltip(
        "可包含多个目标。留空时自动获取子物体中的 ObjectiveHintOutline。"
    )]
    [SerializeField]
    private ObjectiveHintOutline[] outlineTargets;

    [Header("Location-only Target")]
    [Tooltip(
        "关闭：至少一个 Outline Renderer 可用时才能点击 Hint。" +
        "开启：即使没有 Outline，也允许只对准 Focus Point。" +
        "只用于水井位置、入口、路径点等纯位置 Objective。"
    )]
    [SerializeField]
    private bool allowFocusWithoutUsableOutline = false;

    public bool HasUsableOutline
    {
        get
        {
            EnsureOutlineTargets();

            if (outlineTargets == null)
                return false;

            foreach (ObjectiveHintOutline outline
                     in outlineTargets)
            {
                if (outline != null &&
                    outline.IsUsable)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsAvailable
    {
        get
        {
            EnsureOutlineTargets();

            if (!isActiveAndEnabled ||
                !gameObject.activeInHierarchy)
            {
                return false;
            }

            if (HasUsableOutline)
                return true;

            if (!allowFocusWithoutUsableOutline)
                return false;

            if (focusPoint == null)
                return true;

            return focusPoint.gameObject
                .activeInHierarchy;
        }
    }

    private void Awake()
    {
        EnsureOutlineTargets();
    }

    /// <summary>
    /// 配置运行时生成的公共 Hint Target。
    /// Inspector 中已经配置的 Target 不需要调用。
    /// </summary>
    public void Configure(
        Transform configuredFocusPoint,
        ObjectiveHintOutline[] configuredOutlines,
        bool allowFocusWithoutOutline = false)
    {
        focusPoint = configuredFocusPoint;
        outlineTargets = configuredOutlines;
        allowFocusWithoutUsableOutline =
            allowFocusWithoutOutline;
    }

    public ObjectiveHintOutline[]
        GetOutlineTargetsSnapshot()
    {
        EnsureOutlineTargets();

        if (outlineTargets == null)
            return new ObjectiveHintOutline[0];

        return (ObjectiveHintOutline[])
            outlineTargets.Clone();
    }

    public bool TryGetFocusPosition(
        out Vector3 focusPosition)
    {
        if (!IsAvailable)
        {
            focusPosition = default;
            return false;
        }

        if (focusPoint != null &&
            focusPoint.gameObject.activeInHierarchy)
        {
            focusPosition = focusPoint.position;
            return true;
        }

        bool hasBounds = false;
        Bounds combinedBounds = default;

        if (outlineTargets != null)
        {
            foreach (ObjectiveHintOutline outline
                     in outlineTargets)
            {
                if (outline == null ||
                    !outline.TryGetWorldBounds(
                        out Bounds bounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(
                        bounds
                    );
                }
            }
        }

        if (hasBounds)
        {
            focusPosition =
                combinedBounds.center;

            return true;
        }

        if (allowFocusWithoutUsableOutline)
        {
            focusPosition = transform.position;
            return true;
        }

        focusPosition = default;
        return false;
    }

    public void SetHintActive(bool active)
    {
        EnsureOutlineTargets();

        if (outlineTargets == null)
            return;

        foreach (ObjectiveHintOutline outline
                 in outlineTargets)
        {
            if (outline != null)
                outline.SetHintActive(active);
        }
    }

    private void EnsureOutlineTargets()
    {
        if (outlineTargets == null ||
            outlineTargets.Length == 0)
        {
            outlineTargets =
                GetComponentsInChildren
                    <ObjectiveHintOutline>(true);
        }
    }
}
