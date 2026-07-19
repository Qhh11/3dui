using UnityEngine;

/// <summary>
/// Objective Hint 的临时描边兼容层。
///
/// 优先级：
/// UfoSelected > Hint > UfoIdle > Default
///
/// HintEligible 为 false 时：
/// - 不参与 ObjectiveHintTarget 可用性检测
/// - 不参与聚焦 Bounds 计算
/// - 如果当前正在 Hint，立即结束并恢复基础状态
/// </summary>
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public class ObjectiveHintOutline : MonoBehaviour
{
    [Header("Rendering Layer Masks")]
    [SerializeField]
    private uint defaultMask = 1u << 0;

    [SerializeField]
    private uint selectedMask = 1u << 1;

    [SerializeField]
    private uint hintMask = 1u << 3;

    [Header("Renderers")]
    [Tooltip(
        "建议显式填写。留空时会获取当前物体和子物体中的所有 Renderer。"
    )]
    [SerializeField]
    private Renderer[] targetRenderers;

    [Header("Dynamic Availability")]
    [Tooltip(
        "关闭后，此 Outline 不再参与 Hint。" +
        "适用于已完成、已归位或不再需要提示的动态目标。"
    )]
    [SerializeField]
    private bool hintEligible = true;

    private uint[] latestBaseMasks;
    private bool hintActive;

    public bool HintActive => hintActive;
    public bool HintEligible => hintEligible;

    /// <summary>
    /// 为运行时生成的公共 Hint Target 显式指定 Renderer。
    /// 只应传入真正需要描边的 MeshRenderer/SkinnedMeshRenderer。
    /// </summary>
    public void ConfigureRenderers(
        Renderer[] renderers)
    {
        if (hintActive)
        {
            hintActive = false;
            RestoreBaseMasks();
        }

        targetRenderers = renderers;
        latestBaseMasks = null;

        EnsureStateArray();
        CaptureCurrentMasks();
    }

    public bool IsUsable
    {
        get
        {
            if (!CanParticipateInHint())
                return false;

            EnsureRenderers();

            if (targetRenderers == null)
                return false;

            foreach (Renderer targetRenderer
                     in targetRenderers)
            {
                if (targetRenderer != null &&
                    targetRenderer.enabled &&
                    targetRenderer.gameObject
                        .activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Awake()
    {
        EnsureRenderers();
        CaptureCurrentMasks();
    }

    private void OnEnable()
    {
        EnsureRenderers();

        if (!hintActive)
            CaptureCurrentMasks();
    }

    private void LateUpdate()
    {
        if (!hintActive ||
            !CanParticipateInHint())
        {
            return;
        }

        EnsureRenderers();
        EnsureStateArray();

        for (int i = 0;
             i < targetRenderers.Length;
             i++)
        {
            Renderer targetRenderer =
                targetRenderers[i];

            if (targetRenderer == null)
                continue;

            uint currentMask =
                targetRenderer.renderingLayerMask;

            // Selected 永远最高优先级。
            if (HasMask(
                    currentMask,
                    selectedMask))
            {
                continue;
            }

            // 现有系统可能每帧写入 Default 或 Idle。
            // 如果当前不是 Hint，记录最新基础状态。
            if (!HasMask(
                    currentMask,
                    hintMask))
            {
                latestBaseMasks[i] =
                    currentMask == 0u
                        ? defaultMask
                        : currentMask;
            }

            targetRenderer.renderingLayerMask =
                hintMask;
        }
    }

    /// <summary>
    /// 动态设置该 Outline 是否仍属于 Hint 目标。
    ///
    /// 木梁归位后传入 false：
    /// - 当前 Hint 立即结束
    /// - 恢复原本 Outline 状态
    /// - 不再参与 Target Bounds
    /// </summary>
    public void SetHintEligible(bool eligible)
    {
        if (hintEligible == eligible)
            return;

        hintEligible = eligible;

        if (!hintEligible)
        {
            if (hintActive)
            {
                hintActive = false;
                RestoreBaseMasks();
            }

            return;
        }

        CaptureCurrentMasks();
    }

    public void SetHintActive(bool active)
    {
        EnsureRenderers();
        EnsureStateArray();

        if (active &&
            !CanParticipateInHint())
        {
            return;
        }

        if (hintActive == active)
            return;

        hintActive = active;

        if (hintActive)
        {
            CaptureCurrentMasks();
            ApplyHintImmediately();
        }
        else
        {
            RestoreBaseMasks();
        }
    }

    public bool TryGetWorldBounds(
        out Bounds worldBounds)
    {
        if (!CanParticipateInHint())
        {
            worldBounds = default;
            return false;
        }

        EnsureRenderers();

        bool hasBounds = false;
        worldBounds = default;

        if (targetRenderers == null)
            return false;

        foreach (Renderer targetRenderer
                 in targetRenderers)
        {
            if (targetRenderer == null ||
                !targetRenderer.enabled ||
                !targetRenderer.gameObject
                    .activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                worldBounds =
                    targetRenderer.bounds;

                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(
                    targetRenderer.bounds
                );
            }
        }

        return hasBounds;
    }

    private bool CanParticipateInHint()
    {
        return
            hintEligible &&
            isActiveAndEnabled &&
            gameObject.activeInHierarchy;
    }

    private void ApplyHintImmediately()
    {
        if (targetRenderers == null)
            return;

        foreach (Renderer targetRenderer
                 in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            uint currentMask =
                targetRenderer.renderingLayerMask;

            if (HasMask(
                    currentMask,
                    selectedMask))
            {
                continue;
            }

            targetRenderer.renderingLayerMask =
                hintMask;
        }
    }

    private void RestoreBaseMasks()
    {
        EnsureStateArray();

        if (targetRenderers == null)
            return;

        for (int i = 0;
             i < targetRenderers.Length;
             i++)
        {
            Renderer targetRenderer =
                targetRenderers[i];

            if (targetRenderer == null)
                continue;

            // 当前正处于 Selected 时不覆盖。
            if (HasMask(
                    targetRenderer
                        .renderingLayerMask,
                    selectedMask))
            {
                continue;
            }

            uint restoreMask =
                latestBaseMasks[i];

            if (restoreMask == 0u ||
                HasMask(
                    restoreMask,
                    hintMask) ||
                HasMask(
                    restoreMask,
                    selectedMask))
            {
                restoreMask = defaultMask;
            }

            targetRenderer.renderingLayerMask =
                restoreMask;
        }
    }

    private void CaptureCurrentMasks()
    {
        EnsureStateArray();

        if (targetRenderers == null)
            return;

        for (int i = 0;
             i < targetRenderers.Length;
             i++)
        {
            Renderer targetRenderer =
                targetRenderers[i];

            if (targetRenderer == null)
            {
                latestBaseMasks[i] =
                    defaultMask;

                continue;
            }

            uint currentMask =
                targetRenderer.renderingLayerMask;

            if (currentMask == 0u ||
                HasMask(
                    currentMask,
                    hintMask) ||
                HasMask(
                    currentMask,
                    selectedMask))
            {
                latestBaseMasks[i] =
                    defaultMask;
            }
            else
            {
                latestBaseMasks[i] =
                    currentMask;
            }
        }
    }

    private void EnsureRenderers()
    {
        if (targetRenderers == null ||
            targetRenderers.Length == 0)
        {
            targetRenderers =
                GetComponentsInChildren
                    <Renderer>(true);
        }

        EnsureStateArray();
    }

    private void EnsureStateArray()
    {
        int count =
            targetRenderers != null
                ? targetRenderers.Length
                : 0;

        if (latestBaseMasks == null ||
            latestBaseMasks.Length != count)
        {
            latestBaseMasks =
                new uint[count];

            for (int i = 0;
                 i < count;
                 i++)
            {
                latestBaseMasks[i] =
                    defaultMask;
            }
        }
    }

    private static bool HasMask(
        uint value,
        uint mask)
    {
        return (value & mask) != 0u;
    }

    private void OnDisable()
    {
        if (!hintActive)
            return;

        hintActive = false;
        RestoreBaseMasks();
    }

    private void OnDestroy()
    {
        hintActive = false;
    }
}
