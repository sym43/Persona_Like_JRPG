using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private DungeonPlayerInput playerInput;
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Min(0.1f)] private float distance = 5f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Min(0f)] private float gamepadSensitivity = 120f;
    [SerializeField] private float minimumPitch = -30f;
    [SerializeField] private float maximumPitch = 70f;
    [SerializeField, Min(0f)] private float collisionRadius = 0.2f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.1f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private readonly RaycastHit[] collisionHits = new RaycastHit[8];
    private float yaw;
    private float pitch;

    private void Awake()
    {
        if (target == null || playerInput == null)
        {
            Debug.LogError("3인칭 카메라의 추적 대상과 입력 컴포넌트를 연결해야 합니다.", this);
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizeAngle(angles.x);
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        Vector2 look = playerInput.Look;
        float sensitivity = playerInput.IsPointerLook
            ? mouseSensitivity
            : gamepadSensitivity * Time.unscaledDeltaTime;

        yaw += look.x * sensitivity;
        pitch = Mathf.Clamp(pitch - look.y * sensitivity, minimumPitch, maximumPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + pivotOffset;
        Vector3 backward = rotation * Vector3.back;
        float cameraDistance = ResolveCameraDistance(pivot, backward);

        transform.SetPositionAndRotation(pivot + backward * cameraDistance, rotation);
    }

    private float ResolveCameraDistance(Vector3 pivot, Vector3 backward)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            pivot,
            collisionRadius,
            backward,
            collisionHits,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float resolvedDistance = distance;

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = collisionHits[i].collider.transform;
            if (hitTransform == target || hitTransform.IsChildOf(target))
                continue;

            resolvedDistance = Mathf.Min(
                resolvedDistance,
                Mathf.Max(0f, collisionHits[i].distance - collisionPadding));
        }

        return resolvedDistance;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
