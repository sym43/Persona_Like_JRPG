using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(DungeonPlayerInput), typeof(DungeonPlayerStateController))]
public sealed class DungeonPlayerMotor : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float walkSpeed = 5f;
    [SerializeField, Min(0f)] private float sprintSpeed = 8f;
    [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.08f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVerticalSpeed = -2f;

    private CharacterController characterController;
    private DungeonPlayerInput playerInput;
    private DungeonPlayerStateController stateController;
    private float verticalSpeed;
    private float targetFacingAngle;
    private float rotationVelocity;
    private bool hasFacingDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<DungeonPlayerInput>();
        stateController = GetComponent<DungeonPlayerStateController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("플레이어 이동 기준 카메라가 연결되지 않았습니다.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Vector2 moveInput = stateController.CanMove ? playerInput.Move : Vector2.zero;
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 moveDirection = Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            targetFacingAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            hasFacingDirection = true;
        }

        if (hasFacingDirection)
        {
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetFacingAngle,
                ref rotationVelocity,
                rotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        if (characterController.isGrounded && verticalSpeed < 0f)
            verticalSpeed = groundedVerticalSpeed;

        verticalSpeed += gravity * Time.deltaTime;

        float speed = playerInput.SprintHeld ? sprintSpeed : walkSpeed;
        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalSpeed;
        characterController.Move(velocity * Time.deltaTime);
    }
}
