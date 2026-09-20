using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 전투 행동 안에서 한 대상에게 적용된 한 번의 결과.
/// </summary>
public sealed class BattleImpactResult
{
    //스킬 효과 실행 순서. 일반 공격은 0
    public int EffectOrder { get; }
    //같은 효과 안의 타격 순서
    public int HitNumber { get; }
    //원래 공격이나 회복의 대상 ID
    public string TargetUnitId { get; }
    //반사까지 계산한 실제 적용 대상 ID
    public string AffectedUnitId { get; }
    //명중 여부
    public bool Hit { get; }
    //반사 전 공격에 실린 치명타 여부
    public bool Critical { get; }
    //원래 대상의 속성 상성
    public ResistanceType? Resistance { get; }
    //실제 적용 대상의 속성 상성
    public ResistanceType? AppliedResistance { get; }
    //화면에 표시할 피해량
    public int Damage { get; }
    //화면에 표시할 HP 회복량
    public int Healing { get; }
    //이번 결과로 실제 적용 대상이 다운됐는지
    public bool Downed { get; }
    //원래 대상이 방어 중이었는지
    public bool Guarded { get; }
    //적용 후 실제 대상의 HP
    public int HpAfter { get; }

    internal BattleImpactResult(int effectOrder, int hitNumber,
        string targetUnitId, string affectedUnitId, bool hit, bool critical,
        ResistanceType? resistance, ResistanceType? appliedResistance,
        int damage, int healing, bool downed, bool guarded, int hpAfter)
    {
        EffectOrder = effectOrder;
        HitNumber = hitNumber;
        TargetUnitId = targetUnitId;
        AffectedUnitId = affectedUnitId;
        Hit = hit;
        Critical = critical;
        Resistance = resistance;
        AppliedResistance = appliedResistance;
        Damage = damage;
        Healing = healing;
        Downed = downed;
        Guarded = guarded;
        HpAfter = hpAfter;
    }
}

/// <summary>
/// 전투 행동으로 확정된 결과. <br/>
/// 화면 연출은 순서대로 Impacts를 표시하고 피해를 다시 계산하지 않음.
/// </summary>
public sealed class BattleActionResult
{
    //행동 종류
    public BattleActionType Type { get; }
    //행동자 ID
    public string UnitId { get; }
    //사용한 스킬 ID. 스킬 행동이 아니면 없음
    public string SkillId { get; }
    //이번 행동으로 원모어를 받아 추가 행동할 전투원 ID. 없으면 비어 있음
    public string OneMoreUnitId { get; }
    //효과와 다중 타격을 실행한 순서대로 담은 결과
    public IReadOnlyList<BattleImpactResult> Impacts { get; }

    internal BattleActionResult(BattleActionType type, string unitId,
        string skillId, string oneMoreUnitId,
        IEnumerable<BattleImpactResult> impacts)
    {
        if (impacts == null)
            throw new ArgumentNullException(nameof(impacts), "행동 결과 목록이 필요합니다.");
        if (oneMoreUnitId != null)
            BattleDataChecks.CheckText(oneMoreUnitId);

        Type = type;
        UnitId = unitId;
        SkillId = skillId;
        OneMoreUnitId = oneMoreUnitId;
        Impacts = new ReadOnlyCollection<BattleImpactResult>(
            new List<BattleImpactResult>(impacts));
    }
}
