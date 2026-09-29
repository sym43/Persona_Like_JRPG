using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 현재 행동 아군을 기준으로 적의 방향과 전투 카메라 초점을 맞춤.
/// </summary>
public sealed class BattleFocusView : MonoBehaviour
{
    [Header("연결")]
    //현재 행동 아군을 알려주는 전투 컨트롤러
    [SerializeField] private BattleController battleController;
    //시프트 대상 변경을 알려주는 전투 메뉴
    [SerializeField] private BattleMenuUI battleMenu;
    //씬에 배치된 전투원 오브젝트를 찾는 스포너
    [SerializeField] private BattleActorSpawner actorSpawner;
    //현재 행동 아군을 비출 전투 카메라
    [SerializeField] private Camera battleCamera;
    //적들이 모여 있는 전투 중앙
    [SerializeField] private Transform battleCenter;
    //적 위치와 방향을 함께 돌리는 진형 부모
    [SerializeField] private Transform enemyFormation;

    [Header("기본 행동 카메라")]
    //행동자 뒤쪽으로 떨어질 거리
    [SerializeField] private float cameraBackDistance = 4f;
    //행동자 오른쪽으로 떨어질 거리
    [SerializeField] private float cameraRightDistance = 3f;
    //카메라 높이
    [SerializeField] private float cameraHeight = 5.5f;
    //카메라가 바라볼 높이
    [SerializeField] private float lookHeight = 1f;

    [Header("아군 대상 카메라")]
    //선택한 아군을 비출 앞쪽 거리
    [FormerlySerializedAs("shiftFrontDistance")]
    [SerializeField] private float allyTargetFrontDistance = 3f;
    //선택한 아군을 비출 오른쪽 거리
    [FormerlySerializedAs("shiftRightDistance")]
    [SerializeField] private float allyTargetRightDistance = 1.5f;
    //선택한 아군을 비출 카메라 높이
    [FormerlySerializedAs("shiftCameraHeight")]
    [SerializeField] private float allyTargetCameraHeight = 2.5f;
    //선택한 아군 카메라가 바라볼 높이
    [FormerlySerializedAs("shiftLookHeight")]
    [SerializeField] private float allyTargetLookHeight;

    //현재 행동 아군이 바뀌는 이벤트를 연결함
    private void OnEnable()
    {
        if (battleController == null || battleMenu == null ||
            actorSpawner == null ||
            battleCamera == null || battleCenter == null ||
            enemyFormation == null)
        {
            Debug.LogError("전투 초점 화면 연결이 빠졌습니다.");
            enabled = false;
            return;
        }

        battleController.PlayerTurnStarted += ShowFocus;
        battleController.OneMoreStarted += ShowFocus;
        battleMenu.ShiftTargetChanged += ShowAllyTarget;
        battleMenu.ShiftSelectionClosed += RestoreTurnFocus;
        battleMenu.AllyTargetChanged += ShowAllyTarget;
        battleMenu.AllyTargetSelectionClosed += RestoreTurnFocus;
    }

    //현재 행동 아군 이벤트 연결을 끊음
    private void OnDisable()
    {
        if (battleController == null)
            return;

        battleController.PlayerTurnStarted -= ShowFocus;
        battleController.OneMoreStarted -= ShowFocus;
        if (battleMenu != null)
        {
            battleMenu.ShiftTargetChanged -= ShowAllyTarget;
            battleMenu.ShiftSelectionClosed -= RestoreTurnFocus;
            battleMenu.AllyTargetChanged -= ShowAllyTarget;
            battleMenu.AllyTargetSelectionClosed -= RestoreTurnFocus;
        }
    }

    //적들이 행동 아군을 바라보게 하고 카메라 초점을 맞춤
    private void ShowFocus(BattleUnit unit)
    {
        if (unit == null || unit.IsEnemy)
            return;

        BattleUnitActor currentActor = actorSpawner.GetActor(unit.BattleId);
        if (currentActor == null)
            return;

        FaceEnemyFormationTo(currentActor.transform.position);
        MoveCamera(currentActor.transform.position);
    }

    //적 진형 전체가 지정한 위치를 바라보게 함
    private void FaceEnemyFormationTo(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - enemyFormation.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > Mathf.Epsilon)
            enemyFormation.rotation = Quaternion.LookRotation(
                direction, Vector3.up);
    }

    //행동 아군이 앞에 보이도록 카메라를 뒤쪽·오른쪽·위쪽에 둠
    private void MoveCamera(Vector3 actorPosition)
    {
        Vector3 forward = battleCenter.position - actorPosition;
        forward.y = 0f;
        if (forward.sqrMagnitude <= Mathf.Epsilon)
            return;

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        battleCamera.transform.position = actorPosition -
            forward * cameraBackDistance +
            right * cameraRightDistance +
            Vector3.up * cameraHeight;

        Vector3 lookPosition = Vector3.Lerp(actorPosition,
            battleCenter.position, 0.6f) + Vector3.up * lookHeight;
        battleCamera.transform.rotation = Quaternion.LookRotation(
            lookPosition - battleCamera.transform.position, Vector3.up);
    }

    //선택한 아군의 앞쪽에서 캐릭터를 바라봄
    private void ShowAllyTarget(BattleUnitActor actor)
    {
        if (actor == null)
            return;

        Vector3 actorPosition = actor.transform.position;
        Vector3 forward = battleCenter.position - actorPosition;
        forward.y = 0f;
        if (forward.sqrMagnitude <= Mathf.Epsilon)
            return;

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        battleCamera.transform.position = actorPosition +
            forward * allyTargetFrontDistance +
            right * allyTargetRightDistance +
            Vector3.up * allyTargetCameraHeight;
        Vector3 lookPosition = actor.TargetPosition +
            Vector3.up * allyTargetLookHeight;
        battleCamera.transform.rotation = Quaternion.LookRotation(
            lookPosition - battleCamera.transform.position, Vector3.up);
    }

    //시프트 선택을 닫고 현재 행동자 카메라로 돌아감
    private void RestoreTurnFocus()
    {
        ShowFocus(battleController.CurrentUnit);
    }

}
