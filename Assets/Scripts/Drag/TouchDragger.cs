using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 第一人称里点击并拖动场景中的可拖拽物体。
/// 需要门控时填 requiredFlag：未点亮该 flag 时不可拾取/拖动（会松开当前物体）。
/// </summary>
public class TouchDragger : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("发射线用的相机；留空自动取 Camera.main")]
    public Camera cam;
    [Tooltip("哪些 Layer 算可拖拽物体(用于点选)")]
    public LayerMask draggableMask = ~0;

    [Header("门控")]
    [Tooltip("必须已点亮这个 flag 才允许拾取/拖拽（空=无要求，一直可拖）。与 DraggableHighlighter 填同一个即可联动。")]
    public string requiredFlag = "";

    [Header("碰撞")]
    [Tooltip("被拖物体不会与这些层的【凸】碰撞体重叠。木梁互相挡就把木梁的层(Draggable)勾进来。" +
             "不要勾玩家/地面。")]
    public LayerMask obstacleMask = 0;
    [Tooltip("穿透解算迭代次数，越多越不易插穿；过多会轻微抖")]
    [Range(1, 8)] public int solverIterations = 4;
    [Tooltip("重叠检测的额外余量")]
    public float skinWidth = 0.01f;

    [Header("拖拽")]
    [Tooltip("拾取射线的最大距离")]
    public float pickRange = 50f;
    [Tooltip("跟手平滑，越大越跟手")]
    public float followSmooth = 15f;
    [Tooltip("离相机的最近距离(防止贴脸或跑到身后)")]
    public float minDistance = 0.3f;

    private Transform dragging;
    private Rigidbody draggingRb;
    private IDragNotify draggingNotify;
    private bool rbWasKinematic;
    private float grabDistance;   // 球面半径 = 拾取时物体到相机的距离
    private Vector3 targetPos;

    private Collider[] draggingColliders;                 // 被拖物体自己的碰撞体(解算时跳过)
    private static readonly Collider[] overlapBuffer = new Collider[32];

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;

        // 门控：flag 没点亮 → 不可拖，松开当前物体
        if (!QuestProgress.Has(requiredFlag))
        {
            ForceRelease();
            return;
        }

        if (GameplayInputLockService.IsBlocked)
        {
            ForceRelease();
            return;
        }

        GetPointer(out bool down, out bool held, out bool up, out Vector2 sp, out int finger);

        if (down && !PointerOverUI(finger)) TryPick(sp);
        else if (held && dragging != null)  UpdateDrag(sp);
        else if (up)                         Release();

        if (dragging != null)
        {
            // 先自由跟手
            dragging.position = Vector3.Lerp(dragging.position, targetPos, Time.deltaTime * followSmooth);
            // 再把它从障碍里推出来(木梁之间、凸墙体)
            ResolveOverlaps();
        }
    }

    private void TryPick(Vector2 sp)
    {
        Ray ray = cam.ScreenPointToRay(sp);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickRange, draggableMask)) return;

        // 抓根物体(带 DraggableBeam 的那个)，而不是子碰撞体
        IDragNotify notify = hit.transform.GetComponentInParent<IDragNotify>();
        dragging = notify is Component c ? c.transform : hit.transform;

        draggingRb = dragging.GetComponent<Rigidbody>();
        if (draggingRb != null)
        {
            rbWasKinematic = draggingRb.isKinematic;
            draggingRb.isKinematic = true;   // 拖拽时关物理，手动移动
        }

        draggingColliders = dragging.GetComponentsInChildren<Collider>();

        draggingNotify = notify;
        draggingNotify?.OnDragPickedUp();

        grabDistance = Vector3.Distance(cam.transform.position, dragging.position);
        targetPos = dragging.position;
    }

    private void UpdateDrag(Vector2 sp)
    {
        Ray ray = cam.ScreenPointToRay(sp);
        Vector3 origin = cam.transform.position;
        Vector3 dir = ray.direction;

        // 自由目标点：球面上，离相机 grabDistance；不再做前向障碍射线
        float dist = Mathf.Max(grabDistance, minDistance);
        targetPos = origin + dir * dist;
    }

    /// <summary>把被拖物体从 obstacleMask 里的碰撞体中推出来(穿透解算)。</summary>
    private void ResolveOverlaps()
    {
        if (draggingColliders == null || obstacleMask.value == 0) return;

        for (int iter = 0; iter < solverIterations; iter++)
        {
            bool movedThisIter = false;

            foreach (Collider self in draggingColliders)
            {
                if (self == null || !self.enabled || self.isTrigger) continue;

                Bounds b = self.bounds;
                int count = Physics.OverlapBoxNonAlloc(
                    b.center,
                    b.extents + Vector3.one * skinWidth,
                    overlapBuffer,
                    Quaternion.identity,
                    obstacleMask.value,
                    QueryTriggerInteraction.Ignore);

                for (int i = 0; i < count; i++)
                {
                    Collider other = overlapBuffer[i];
                    if (other == null || IsOwnCollider(other)) continue;

                    if (Physics.ComputePenetration(
                            self, self.transform.position, self.transform.rotation,
                            other, other.transform.position, other.transform.rotation,
                            out Vector3 pushDir, out float pushDist))
                    {
                        if (pushDist > 0f)
                        {
                            dragging.position += pushDir * pushDist;
                            movedThisIter = true;
                        }
                    }
                }
            }

            if (!movedThisIter) break;
        }
    }

    private bool IsOwnCollider(Collider c)
    {
        for (int i = 0; i < draggingColliders.Length; i++)
        {
            if (draggingColliders[i] == c) return true;
        }
        return false;
    }

    private void Release()
    {
        if (dragging == null) return;
        if (draggingRb != null) draggingRb.isKinematic = rbWasKinematic;

        IDragNotify notify = draggingNotify;
        ClearDragState();
        notify?.OnDragReleased();
    }

    /// <summary>
    /// 外部强制松手(自动吸附时调用)。不触发 IDragNotify.OnDragReleased，避免和吸附逻辑重复。
    /// </summary>
    public void ForceRelease()
    {
        if (dragging == null) return;
        if (draggingRb != null) draggingRb.isKinematic = rbWasKinematic;
        ClearDragState();
    }

    private void ClearDragState()
    {
        dragging = null;
        draggingRb = null;
        draggingNotify = null;
        draggingColliders = null;
    }

    private void OnDisable()
    {
        ForceRelease();
    }

    private void GetPointer(out bool down, out bool held, out bool up, out Vector2 pos, out int finger)
    {
        down = held = up = false; pos = Vector2.zero; finger = -1;
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            pos = t.position; finger = t.fingerId;
            down = t.phase == TouchPhase.Began;
            held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            up   = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
        }
        else
        {
            pos = Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up   = Input.GetMouseButtonUp(0);
        }
    }

    private bool PointerOverUI(int finger)
    {
        if (EventSystem.current == null) return false;
        return finger >= 0 ? EventSystem.current.IsPointerOverGameObject(finger)
                           : EventSystem.current.IsPointerOverGameObject();
    }
}
