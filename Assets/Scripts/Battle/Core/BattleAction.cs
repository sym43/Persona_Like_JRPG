using System;

/// <summary>
/// 전투에서 선택할 수 있는 행동 종류.
/// </summary>
public enum BattleActionType
{
    BasicAttack = 0,
    Skill = 1,
    Guard = 2,
    AllOutAttack = 3,
    Skip = 4,
    Escape = 5,
    UseRandomItem = 6,
    DiscardMoney = 7
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
    //대상 ID. 전체 대상·자기 자신·방어일 때는 없음
    public string TargetId { get; }
    //사용할 스킬 ID. 스킬 행동이 아니면 없음
    public string SkillId { get; }
    //매혹으로 원래 대상 진영을 반대로 바꾸는지
    public bool ReverseTargetSide { get; }

    //선택한 행동을 만듦
    public BattleAction(string unitId, BattleActionType type,
        string targetId = null, string skillId = null,
        bool reverseTargetSide = false)
    {
        BattleDataChecks.CheckText(unitId);
        if (!Enum.IsDefined(typeof(BattleActionType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 전투 행동입니다.");

        if (type == BattleActionType.BasicAttack)
        {
            BattleDataChecks.CheckText(targetId);
            if (skillId != null)
                throw new ArgumentException("일반 공격에는 스킬 ID를 넣을 수 없습니다.", nameof(skillId));
        }
        else if (type == BattleActionType.Skill)
        {
            BattleDataChecks.CheckText(skillId);
        }
        else if (targetId != null || skillId != null)
        {
            throw new ArgumentException("이 행동에는 대상이나 스킬 ID가 필요하지 않습니다.");
        }
        if (reverseTargetSide && type != BattleActionType.BasicAttack &&
            type != BattleActionType.Skill)
            throw new ArgumentException("공격과 스킬만 대상 진영을 바꿀 수 있습니다.",
                nameof(reverseTargetSide));

        UnitId = unitId;
        Type = type;
        TargetId = targetId;
        SkillId = skillId;
        ReverseTargetSide = reverseTargetSide;
    }
}
