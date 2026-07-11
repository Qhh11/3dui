using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class BoundaryDangerTrigger : MonoBehaviour
{
    [Header("Slow")]
    [Range(0.05f, 1f)]
    [SerializeField] private float speedMultiplier = 0.5f;

    [Header("Push Back")]
    [SerializeField] private bool pushBack = true;

    [Tooltip("本地空间安全方向。默认 (0,0,1) 表示沿这个 trigger 的本地 Z 轴往安全方向推。")]
    [SerializeField] private Vector3 localSafeDirection = Vector3.forward;

    [Tooltip("回推加速度。数值越大，越快达到 Max Push Speed。")]
    [SerializeField] private float pushAcceleration = 600f;

    [Tooltip("回推最大速度。最终水平速度会在 PlayerController 中被限制到 Player Speed 内。")]
    [SerializeField] private float maxPushSpeed = 220f;

    [Tooltip("多个 trigger 重叠时，优先选择 Push Priority 更高的回推。")]
    [SerializeField] private int pushPriority = 0;

    [Header("Warning")]
    [SerializeField] private bool showWarning = true;
    [SerializeField] private string warningText = "Careful, you're near the edge.";
    [SerializeField] private int warningPriority = 0;

    public float SpeedMultiplier => speedMultiplier;
    public bool PushBack => pushBack;
    public float PushAcceleration => Mathf.Max(0f, pushAcceleration);
    public float MaxPushSpeed => Mathf.Max(0f, maxPushSpeed);
    public int PushPriority => pushPriority;
    public bool ShowWarning => showWarning;
    public string WarningText => warningText;
    public int WarningPriority => warningPriority;

    public Vector3 SafeDirection
    {
        get
        {
            Vector3 direction =
                transform.TransformDirection(localSafeDirection);

            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            return direction.normalized;
        }
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        speedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        pushAcceleration = Mathf.Max(0f, pushAcceleration);
        maxPushSpeed = Mathf.Max(0f, maxPushSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerBoundaryState state =
            other.GetComponentInParent<PlayerBoundaryState>();

        if (state != null)
            state.EnterDanger(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerBoundaryState state =
            other.GetComponentInParent<PlayerBoundaryState>();

        if (state != null)
            state.ExitDanger(this);
    }
}