/// <summary>
/// 전투 효과 적용 결과 종류.
/// </summary>
public enum BattleEffectResultType
{
    Applied = 0,
    Refreshed = 1,
    Canceled = 2,
    Removed = 3,
    Blocked = 4
}

/// <summary>
/// 연출과 로그에 전달할 전투 효과 처리 결과.
/// </summary>
public sealed class BattleEffectResult
{
    //효과 대상 전투원 ID
    public string TargetUnitId { get; }
    //처리한 전투 효과 ID
    public string EffectId { get; }
    //적용·갱신·상쇄·제거·차단 결과
    public BattleEffectResultType ResultType { get; }

    public BattleEffectResult(string targetUnitId, string effectId,
        BattleEffectResultType resultType)
    {
        BattleDataChecks.CheckText(targetUnitId);
        BattleDataChecks.CheckText(effectId);
        TargetUnitId = targetUnitId;
        EffectId = effectId;
        ResultType = resultType;
    }
}
