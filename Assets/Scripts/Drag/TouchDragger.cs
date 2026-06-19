using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 第一人称里点击并拖动场景中的可拖拽物体。
/// 模型：被拖物体始终位于「以相机为球心、拾取时距离为半径」的球面上，沿视线/手指方向。
///   - 每帧从当前相机位置重算 → 视角转动、人物移动都会跟着更新。
///   - 用 SphereCast 检测障碍：若该方向上有其它 collider，物体停在它前面，不穿插。
///
/// 用法：可拖拽物体放到单独 Layer(如 Draggable)、加 Collider(要掉落再加 Rigidbody)；
/// 脚本挂 Main Camera 或玩家上，draggableMask 选该层，obstacleMask 选会挡路的环境层；
/// 只在第一人称视角启用。需要场景里有 EventSystem，点 UI 时不会误触发。
/// </summary>
public class TouchDragger : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("发射线用的相机；留空自动取 Camera.main")]
    public Camera cam;
    [Tooltip("哪些 Layer 算可拖拽物体")]
    public LayerMask draggableMask = ~0;
    [Tooltip("哪些 Layer 算障碍物(被拖物体会自动排除，不会挡自己)")]
    public LayerMask obstacleMask = ~0;

    [Header("拖拽")]
    [Tooltip("拾取射线的最大距离")]
    public float pickRange = 50f;
    [Tooltip("跟手平滑，越大越跟手")]
    public float followSmooth = 15f;
    [Tooltip("和障碍物之间留的间隙")]
    public float collisionPadding = 0.05f;
    [Tooltip("离相机的最近距离(防止贴脸或跑到身后)")]
    public float minDistance = 0.3f;

    private Transform dragging;
    private Rigidbody draggingRb;
    private bool rbWasKinematic;
    private float grabDistance;   // 球面半径 = 拾取时物体到相机的距离
    private float holdRadius;     // 物体近似半径，给 SphereCast 防穿插
    private Vector3 targetPos;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;

        GetPointer(out bool down, out bool held, out bool up, out Vector2 sp, out int finger);

        if (down && !PointerOverUI(finger)) TryPick(sp);
        else if (held && dragging != null)  UpdateDrag(sp);
        else if (up)                         Release();

        if (dragging != null)
            dragging.position = Vector3.Lerp(dragging.position, targetPos, Time.deltaTime * followSmooth);
    }

    private void TryPick(Vector2 sp)
    {
        Ray ray = cam.ScreenPointToRay(sp);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickRange, draggableMask)) return;

        dragging = hit.transform;
        draggingRb = dragging.GetComponent<Rigidbody>();
        if (draggingRb != null)
        {
            rbWasKinematic = draggingRb.isKinematic;
            draggingRb.isKinematic = true;   // 拖拽时关物理
        }

        // 球面半径 = 拾取瞬间物体到相机的距离
        grabDistance = Vector3.Distance(cam.transform.position, dragging.position);

        // 物体近似半径(取碰撞盒最大半边长)，SphereCast 防穿插用
        Collider col = dragging.GetComponentInChildren<Collider>();
        holdRadius = col != null
            ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.y, col.bounds.extents.z)
            : 0.2f;

        targetPos = dragging.position;
    }

    private void UpdateDrag(Vector2 sp)
    {
        Ray ray = cam.ScreenPointToRay(sp);
        Vector3 origin = cam.transform.position;   // 球心 = 当前相机位置(人物移动会带着它走)
        Vector3 dir = ray.direction;               // 视线/手指方向

        // 障碍层 = obstacleMask 去掉可拖拽层，避免物体挡到自己
        int mask = obstacleMask.value & ~draggableMask.value;

        float dist = grabDistance;
        // 把物体当成半径 holdRadius 的球往视线方向投：碰到障碍就停在它前面
        if (Physics.SphereCast(origin, holdRadius, dir, out RaycastHit obstacle,
                               grabDistance, mask, QueryTriggerInteraction.Ignore))
        {
            dist = obstacle.distance - collisionPadding;
        }
        dist = Mathf.Max(dist, minDistance);

        // 球面上的目标点
        targetPos = origin + dir * dist;
    }

    private void Release()
    {
        if (dragging == null) return;
        if (draggingRb != null) draggingRb.isKinematic = rbWasKinematic; // 恢复物理
        dragging = null;
        draggingRb = null;
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
