using UnityEngine;

/// <summary>
/// 单根木梁。抓取/拖动由 TouchDragger 负责；本脚本通过 IDragNotify 在被拾起/松手时
/// 通知 WoodBeamPuzzle "我正被拖 / 我松手了"，由 puzzle 每帧检查是否进入 slot 自动吸附。
///
/// 要求：
///   - DraggableBeam + Collider 放在【同一个根物体】上。
///   - 木梁所在 Layer 要包含在 TouchDragger.draggableMask 里。
/// </summary>
[RequireComponent(typeof(Collider))]
public class DraggableBeam : MonoBehaviour, IDragNotify
{
    [Tooltip("可选高亮/描边物体，玩法激活时显示，放好后隐藏")]
    [SerializeField] private GameObject highlight;

    [Tooltip("有 Rigidbody 时保持 kinematic，木梁不会掉")]
    [SerializeField] private bool forceKinematic = true;

    [Tooltip("放好后禁用木梁碰撞体，防止被二次抓起")]
    [SerializeField] private bool disableColliderWhenPlaced = true;

    public bool IsPlaced { get; private set; }

    private WoodBeamPuzzle puzzle;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody body;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        body = GetComponent<Rigidbody>();
        if (body != null && forceKinematic)
        {
            body.isKinematic = true;
        }

        SetGrabbable(false);
    }

    /// <summary>WoodBeamPuzzle 在 Start() 里注册自己。</summary>
    public void Bind(WoodBeamPuzzle owner)
    {
        puzzle = owner;
    }

    /// <summary>玩法露出时开启高亮。</summary>
    public void SetGrabbable(bool grabbable)
    {
        if (IsPlaced)
        {
            grabbable = false;
        }

        if (highlight != null)
        {
            highlight.SetActive(grabbable);
        }
    }

    // ----- IDragNotify (TouchDragger 调用) -----

    public void OnDragPickedUp()
    {
        if (puzzle != null)
        {
            puzzle.OnBeamGrabbed(this);   // 告诉 puzzle 我开始被拖了
        }
    }

    public void OnDragReleased()
    {
        if (puzzle != null)
        {
            puzzle.OnBeamReleased(this);  // 玩家松手(没吸附到)，木梁留在原地
        }
    }

    // ----- 由 puzzle 驱动 -----

    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        if (body != null)
        {
            body.isKinematic = true;
        }

        IsPlaced = true;

        if (highlight != null)
        {
            highlight.SetActive(false);
        }

        if (disableColliderWhenPlaced)
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
        }
    }

    public void ReturnToStart()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}
