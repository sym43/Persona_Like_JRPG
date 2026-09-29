using System;
using UnityEngine;

/// <summary>
/// 씬의 전투원 오브젝트와 실제 전투원을 연결함.
/// </summary>
public sealed class BattleUnitActor : MonoBehaviour
{
    //대상 표시 UI가 따라갈 위치. 비어 있으면 오브젝트 위치를 사용함
    [SerializeField] private Transform targetPoint;

    //이 오브젝트가 보여주는 전투원
    public BattleUnit Unit { get; private set; }
    //대상 표시 UI가 따라갈 월드 위치
    public Vector3 TargetPosition => targetPoint != null
        ? targetPoint.position
        : transform.position;

    //이 오브젝트에 실제 전투원을 연결함
    public void Bind(BattleUnit unit)
    {
        Unit = unit ?? throw new ArgumentNullException(nameof(unit),
            "연결할 전투원이 필요합니다.");
        name = unit.BattleId;
    }
}
