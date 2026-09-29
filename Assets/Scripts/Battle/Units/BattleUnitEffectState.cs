using System;

/// <summary>
/// 전투원에게 적용된 효과의 현재 상태. <br/>
/// 원본 효과, 경과 턴과 효과를 적용한 전투원 ID를 담음.
/// </summary>
public sealed class BattleUnitEffectState
{
    //적용된 전투 효과 원본
    public BattleEffectData Effect { get; }
    //적용된 효과 ID
    public string EffectId => Effect.Id;
    //효과 적용 후 지난 기본 행동 수
    public int PassedTurns { get; private set; }
    //강제 만료까지 남은 최대 행동 수. 정신 상태는 이보다 먼저 자연 회복될 수 있음
    public int RemainingTurns => Math.Max(0, Effect.MaxTurns - PassedTurns);
    //효과를 적용한 전투원 ID
    public string AppliedByUnitId { get; }

    //효과 상태를 만듦
    public BattleUnitEffectState(BattleEffectData effect, string appliedByUnitId)
    {
        Effect = effect ?? throw new ArgumentNullException(nameof(effect), "전투 효과 원본이 필요합니다.");
        if (appliedByUnitId != null) BattleDataChecks.CheckText(appliedByUnitId);
        AppliedByUnitId = appliedByUnitId;
    }

    //같은 효과를 다시 받아 경과 턴을 초기화함
    public void Refresh()
    {
        PassedTurns = 0;
    }

    //시간제 효과의 경과 턴을 늘리고 만료 여부를 반환함
    public bool ReduceDuration()
    {
        if (Effect.MaxTurns == 0) return false;
        PassedTurns++;
        return PassedTurns >= Effect.MaxTurns;
    }

    //정신 상태에서 한 턴이 지났음을 기록함
    public int PassMentalTurn()
    {
        PassedTurns++;
        return PassedTurns;
    }
}
