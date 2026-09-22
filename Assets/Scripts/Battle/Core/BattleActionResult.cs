using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 전투 행동 안에서 한 대상에게 적용된 한 번의 결과.
/// </summary>
public sealed class BattleImpactResult
{
    public int EffectOrder { get; }
    public int HitNumber { get; }
    public string TargetUnitId { get; }
    public string AffectedUnitId { get; }
    public bool Hit { get; }
    public bool Critical { get; }
    public ResistanceType? Resistance { get; }
    public ResistanceType? AppliedResistance { get; }
    public int Damage { get; }
    public int Healing { get; }
    public bool Downed { get; }
    public bool Guarded { get; }
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
/// 화면 연출은 피해와 효과 변경을 다시 계산하지 않고 표시함.
/// </summary>
public sealed class BattleActionResult
{
    public BattleActionType Type { get; }
    public string UnitId { get; }
    public string SkillId { get; }
    public string OneMoreUnitId { get; }
    public IReadOnlyList<BattleImpactResult> Impacts { get; }
    public IReadOnlyList<BattleEffectResult> EffectResults { get; }

    internal BattleActionResult(BattleActionType type, string unitId,
        string skillId, string oneMoreUnitId,
        IEnumerable<BattleImpactResult> impacts,
        IEnumerable<BattleEffectResult> effectResults = null)
    {
        if (impacts == null)
            throw new ArgumentNullException(nameof(impacts), "행동 결과 목록이 필요합니다.");
        if (oneMoreUnitId != null) BattleDataChecks.CheckText(oneMoreUnitId);

        Type = type;
        UnitId = unitId;
        SkillId = skillId;
        OneMoreUnitId = oneMoreUnitId;
        Impacts = new ReadOnlyCollection<BattleImpactResult>(new List<BattleImpactResult>(impacts));
        EffectResults = new ReadOnlyCollection<BattleEffectResult>(
            new List<BattleEffectResult>(effectResults ?? Array.Empty<BattleEffectResult>()));
    }
}
