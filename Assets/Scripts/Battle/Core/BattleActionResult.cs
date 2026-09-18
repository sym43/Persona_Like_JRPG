/// <summary>
/// 전투 행동으로 확정된 결과. <br/>
/// 화면 연출은 이 값을 표시하고 피해를 다시 계산하지 않음.
/// </summary>
public sealed class BattleActionResult
{
    //행동 종류
    public BattleActionType Type { get; }
    //행동자 ID
    public string UnitId { get; }
    //대상 ID. 방어일 때는 없음
    public string TargetId { get; }
    //명중 여부
    public bool Hit { get; }
    //반사 전 공격에 실린 치명타 여부. 실제 피격자가 약점이면 피해에는 치명타 배율을 적용하지 않음
    public bool Critical { get; }
    //처음 공격한 대상의 속성 상성
    public ResistanceType? Resistance { get; }
    //실제로 피해나 회복을 받은 전투원의 속성 상성
    public ResistanceType? AppliedResistance { get; }
    //실제로 HP가 변한 전투원 ID. 방어와 반사는 행동자일 수 있음
    public string AffectedUnitId { get; }
    //화면에 표시할 피해량
    public int Damage { get; }
    //화면에 표시할 HP 회복량
    public int Healing { get; }
    //이번 행동으로 대상이 다운됐는지
    public bool DownedTarget { get; }
    //반사로 행동자가 다운됐는지
    public bool DownedActor { get; }
    //대상이 방어 중이었는지
    public bool Guarded { get; }
    //적용 후 AffectedUnitId의 HP
    public int HpAfter { get; }

    internal BattleActionResult(BattleActionType type, string unitId, string targetId,
        bool hit, bool critical, ResistanceType? resistance, int damage,
        bool downedTarget, bool guarded, int hpAfter,
        string affectedUnitId = null, int healing = 0, bool downedActor = false,
        ResistanceType? appliedResistance = null)
    {
        Type = type;
        UnitId = unitId;
        TargetId = targetId;
        Hit = hit;
        Critical = critical;
        Resistance = resistance;
        AppliedResistance = appliedResistance ?? resistance;
        AffectedUnitId = affectedUnitId ?? targetId ?? unitId;
        Damage = damage;
        Healing = healing;
        DownedTarget = downedTarget;
        DownedActor = downedActor;
        Guarded = guarded;
        HpAfter = hpAfter;
    }
}
