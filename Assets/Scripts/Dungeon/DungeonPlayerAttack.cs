using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(DungeonPlayerInput), typeof(DungeonPlayerStateController))]
public sealed class DungeonPlayerAttack : MonoBehaviour
{
    [SerializeField, Min(0f)] private float attackDistance = 1.25f;
    [SerializeField, Min(0f)] private float attackRadius = 0.8f;
#if UNITY_EDITOR
    [SerializeField] private bool alwaysShowAttackGizmo = true;
    [SerializeField, Min(0f)] private float attackGizmoDuration = 0.2f;
#endif

    private readonly Collider[] hitBuffer = new Collider[8];
    private DungeonPlayerInput playerInput;
    private DungeonPlayerStateController stateController;
#if UNITY_EDITOR
    private float hideGizmoAt = float.NegativeInfinity;
#endif

    private void Awake()
    {
        playerInput = GetComponent<DungeonPlayerInput>();
        stateController = GetComponent<DungeonPlayerStateController>();
    }

    private void Update()
    {
        if (playerInput.AttackPressed)
        {
            Attack();
        }
    }

    public void Attack()
    {
        if (!stateController.TryEnter(DungeonPlayerState.Attacking))
        {
            return;
        }

        ApplyHit();
        EndAttack();
    }

    public void ApplyHit()
    {
        if (stateController.CurrentState != DungeonPlayerState.Attacking)
        {
            return;
        }

#if UNITY_EDITOR
        hideGizmoAt = Time.time + attackGizmoDuration;
#endif
        Vector3 center = GetAttackCenter();
        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            attackRadius,
            hitBuffer,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            EnemyHitReceiver enemy = hitBuffer[i].GetComponentInParent<EnemyHitReceiver>();
            if (enemy != null)
            {
                enemy.ReceiveHit();
            }
        }
    }

    public void EndAttack()
    {
        stateController.TryReturnToFree(DungeonPlayerState.Attacking);
    }

    private Vector3 GetAttackCenter()
    {
        return transform.position + Vector3.up + transform.forward * attackDistance;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!alwaysShowAttackGizmo && (!Application.isPlaying || Time.time > hideGizmoAt))
        {
            return;
        }

        Vector3 center = GetAttackCenter();
        UnityEngine.Rendering.CompareFunction previousZTest = Handles.zTest;

        Handles.color = Color.red;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Handles.DrawLine(transform.position + Vector3.up, center);
        Handles.DrawWireDisc(center, Vector3.right, attackRadius);
        Handles.DrawWireDisc(center, Vector3.up, attackRadius);
        Handles.DrawWireDisc(center, Vector3.forward, attackRadius);
        Handles.zTest = previousZTest;
    }
#endif
}
