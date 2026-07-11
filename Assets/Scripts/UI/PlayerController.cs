using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float playerSpeed = 30.0f;

    [Header("初始重生点")]
    [Tooltip("游戏开始时的安全重生位置")]
    [SerializeField] private Transform initialRespawnPoint;

    [SerializeField] private float gravityMultiplier = 10f;

    private PlayerBoundaryState boundaryState;
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
        boundaryState = GetComponent<PlayerBoundaryState>();

        if (initialRespawnPoint != null)
        {
            currentRespawnPosition = initialRespawnPoint.position;
            currentRespawnRotation = initialRespawnPoint.rotation;
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
            cameraTransform = Camera.main.transform;
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

        Vector2 movement = inputManager.GetLeftStick();
        Vector3 move = new Vector3(movement.x, 0f, movement.y);

        if (cameraTransform != null)
        {
            move =
                cameraTransform.forward * move.z +
                cameraTransform.right * move.x;
        }

        move.y = 0f;
        move = Vector3.ClampMagnitude(move, 1f);

        float speedMultiplier =
            boundaryState != null
                ? boundaryState.SpeedMultiplier
                : 1f;

        Vector3 inputVelocity =
            move * playerSpeed * speedMultiplier;

        Vector3 pushVelocity =
            boundaryState != null
                ? boundaryState.UpdateAndGetPushVelocity(Time.deltaTime)
                : Vector3.zero;

        Vector3 horizontalVelocity =
            inputVelocity + pushVelocity;

        horizontalVelocity.y = 0f;

        horizontalVelocity =
            Vector3.ClampMagnitude(
                horizontalVelocity,
                playerSpeed
            );

        Vector3 finalMove =
            horizontalVelocity +
            Vector3.up * playerVelocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }

    public bool TrySetRespawnPoint(
        Transform newRespawnPoint,
        int checkpointOrder)
    {
        if (newRespawnPoint == null)
        {
            Debug.LogWarning("检查点没有设置 Respawn Point。");
            return false;
        }

        if (checkpointOrder <= currentCheckpointOrder)
            return false;

        currentCheckpointOrder = checkpointOrder;
        currentRespawnPosition = newRespawnPoint.position;
        currentRespawnRotation = newRespawnPoint.rotation;

        Debug.Log(
            $"已激活检查点 {checkpointOrder}，重生位置：{currentRespawnPosition}"
        );

        return true;
    }

    public void Respawn()
    {
        if (isRespawning || controller == null)
            return;

        isRespawning = true;
        controller.enabled = false;

        transform.SetPositionAndRotation(
            currentRespawnPosition,
            currentRespawnRotation
        );

        playerVelocity = Vector3.zero;

        if (boundaryState != null)
            boundaryState.ResetBoundaryState();

        controller.enabled = true;
        isRespawning = false;
    }

    public void TeleportTo(Transform target)
    {
        if (target == null || controller == null)
            return;

        controller.enabled = false;

        transform.SetPositionAndRotation(
            target.position,
            target.rotation
        );

        playerVelocity = Vector3.zero;

        if (boundaryState != null)
            boundaryState.ResetBoundaryState();

        controller.enabled = true;
    }
}