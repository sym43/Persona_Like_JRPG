using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DungeonPlayerInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private InputActionMap playerActions;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction attackAction;

    public Vector2 Move => moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();
    public Vector2 Look => lookAction == null ? Vector2.zero : lookAction.ReadValue<Vector2>();
    public bool SprintHeld => sprintAction != null && sprintAction.IsPressed();
    public bool AttackPressed => attackAction != null && attackAction.WasPressedThisFrame();
    public bool IsPointerLook => lookAction?.activeControl?.device is Pointer;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("던전 입력 액션 에셋이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        playerActions = inputActions.FindActionMap("Player");
        moveAction = playerActions?.FindAction("Move");
        lookAction = playerActions?.FindAction("Look");
        sprintAction = playerActions?.FindAction("Sprint");
        attackAction = playerActions?.FindAction("Attack");

        if (playerActions == null || moveAction == null || lookAction == null || sprintAction == null || attackAction == null)
        {
            Debug.LogError("InputSystem_Actions의 Player 맵에 Move, Look, Sprint, Attack 액션이 필요합니다.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        playerActions?.Enable();
    }

    private void OnDisable()
    {
        playerActions?.Disable();
    }
}
