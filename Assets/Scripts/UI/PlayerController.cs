using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float playerSpeed = 30.0f;

    [Header("初始重生点")]
    [Tooltip("游戏开始时的安全重生位置")]
    [SerializeField] private Transform initialRespawnPoint;
    
    [SerializeField]
    private float gravityMultiplier = 10f;

    private CharacterController controller;
    private InputManager inputManager;
    private Transform cameraTransform;

    private Vector3 playerVelocity;

    private Vector3 currentRespawnPosition;
    private Quaternion currentRespawnRotation;

    private int currentCheckpointOrder;
    private bool isRespawning;

    public bool IsRespawning => isRespawning;
    public int CurrentCheckpointOrder => currentCheckpointOrder;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (initialRespawnPoint != null)
        {
            currentRespawnPosition =
                initialRespawnPoint.position;

            currentRespawnRotation =
                initialRespawnPoint.rotation;
        }
        else
        {
            currentRespawnPosition = transform.position;
            currentRespawnRotation = transform.rotation;
        }

        currentCheckpointOrder = 0;
    }

    private void Start()
    {
        inputManager = InputManager.Instance;

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (isRespawning ||
            controller == null ||
            !controller.enabled ||
            inputManager == null)
        {
            return;
        }

        // 重力
        if (controller.isGrounded)
        {
            playerVelocity.y = -0.5f;
        }
        else
        {
            playerVelocity.y +=
                Physics.gravity.y *
                gravityMultiplier *
                Time.deltaTime;
        }

        // 移动输入
        Vector2 movement =
            inputManager.GetLeftStick();

        Vector3 move =
            new Vector3(movement.x, 0f, movement.y);

        if (cameraTransform != null)
        {
            move =
                cameraTransform.forward * move.z +
                cameraTransform.right * move.x;
        }

        move.y = 0f;
        move = Vector3.ClampMagnitude(move, 1f);

        Vector3 finalMove =
            move * playerSpeed +
            Vector3.up * playerVelocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }

    /// <summary>
    /// 只有编号更大的检查点才能更新重生位置。
    /// </summary>
    public bool TrySetRespawnPoint(
        Transform newRespawnPoint,
        int checkpointOrder)
    {
        if (newRespawnPoint == null)
        {
            Debug.LogWarning(
                "检查点没有设置 Respawn Point。"
            );

            return false;
        }

        if (checkpointOrder <= currentCheckpointOrder)
        {
            return false;
        }

        currentCheckpointOrder = checkpointOrder;

        currentRespawnPosition =
            newRespawnPoint.position;

        currentRespawnRotation =
            newRespawnPoint.rotation;

        Debug.Log(
            $"已激活检查点 {checkpointOrder}，" +
            $"重生位置：{currentRespawnPosition}"
        );

        return true;
    }

    /// <summary>
    /// 回到最后一个合法检查点。
    /// </summary>
    public void Respawn()
    {
        if (isRespawning || controller == null)
        {
            return;
        }

        isRespawning = true;

        controller.enabled = false;

        transform.SetPositionAndRotation(
            currentRespawnPosition,
            currentRespawnRotation
        );

        playerVelocity = Vector3.zero;

        controller.enabled = true;
        isRespawning = false;
    }
}