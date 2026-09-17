using System;
using System.Collections.Generic;

/// <summary>
/// 전투원에게 적용된 효과의 상태 데이터. <br/>
/// 효과 ID, 턴 카운터, 효과를 건 전투원 ID를 담음.
/// </summary>
public sealed class BattleEffectState
{
    //적용된 효과 ID
    public string EffectId { get; }
    //효과 턴 카운터
    public int TurnCounter { get; }
    //효과를 건 전투원 ID
    public string SourceUnitId { get; }

    //효과 상태를 만듦
    public BattleEffectState(string effectId, int turnCounter, string sourceUnitId)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(effectId);
        if (turnCounter < 0) throw new ArgumentOutOfRangeException(nameof(turnCounter), "효과 턴 카운터는 0 이상이어야 합니다.");
        if (sourceUnitId != null) BattleDataChecks.CheckText(sourceUnitId);

        #endregion

        EffectId = effectId;
        TurnCounter = turnCounter;
        SourceUnitId = sourceUnitId;
    }
}
