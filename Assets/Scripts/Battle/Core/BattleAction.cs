using System;

/// <summary>
/// 전투에서 선택할 수 있는 행동 종류.
/// </summary>
public enum BattleActionType
{
    BasicAttack = 0,
    Guard = 1
}

/// <summary>
/// 플레이어 입력 또는 적 AI가 선택한 행동.
/// </summary>
public sealed class BattleAction
{
    //행동자 ID
    public string UnitId { get; }
    //행동 종류
    public BattleActionType Type { get; }
    //대상 ID. 방어일 때는 없음
    public string TargetId { get; }

    //선택한 행동을 만듦
    public BattleAction(string unitId, BattleActionType type, string targetId = null)
    {
        BattleDataChecks.CheckText(unitId);
        if (!Enum.IsDefined(typeof(BattleActionType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 전투 행동입니다.");
        if (type == BattleActionType.BasicAttack)
            BattleDataChecks.CheckText(targetId);
        else if (targetId != null)
            throw new ArgumentException("방어에는 대상이 필요하지 않습니다.", nameof(targetId));

        UnitId = unitId;
        Type = type;
        TargetId = targetId;
    }
}
