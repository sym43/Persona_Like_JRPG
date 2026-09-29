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
    Item = 6,
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
    //사용할 아이템 ID. 아이템 행동이 아니면 없음
    public string ItemId { get; }
    //매혹으로 원래 대상 진영을 반대로 바꾸는지
    public bool ReverseTargetSide { get; }

    //선택한 행동을 만듦
    public BattleAction(string unitId, BattleActionType type,
        string targetId = null, string skillId = null,
        bool reverseTargetSide = false, string itemId = null)
    {
        BattleDataChecks.CheckText(unitId);
        if (!Enum.IsDefined(typeof(BattleActionType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 전투 행동입니다.");

        switch (type)
        {
            case BattleActionType.BasicAttack:
                BattleDataChecks.CheckText(targetId);
                CheckUnusedId(skillId, nameof(skillId), "일반 공격에는 스킬 ID를 넣을 수 없습니다.");
                CheckUnusedId(itemId, nameof(itemId), "일반 공격에는 아이템 ID를 넣을 수 없습니다.");
                break;
            case BattleActionType.Skill:
                BattleDataChecks.CheckText(skillId);
                CheckUnusedId(itemId, nameof(itemId), "스킬 행동에는 아이템 ID를 넣을 수 없습니다.");
                break;
            case BattleActionType.Item:
                BattleDataChecks.CheckText(itemId);
                CheckUnusedId(skillId, nameof(skillId), "아이템 행동에는 스킬 ID를 넣을 수 없습니다.");
                break;
            default:
                if (targetId != null || skillId != null || itemId != null)
                    throw new ArgumentException("이 행동에는 대상·스킬·아이템 ID가 필요하지 않습니다.");
                break;
        }

        if (reverseTargetSide && type != BattleActionType.BasicAttack &&
            type != BattleActionType.Skill)
            throw new ArgumentException("공격과 스킬만 대상 진영을 바꿀 수 있습니다.",
                nameof(reverseTargetSide));

        UnitId = unitId;
        Type = type;
        TargetId = targetId;
        SkillId = skillId;
        ItemId = itemId;
        ReverseTargetSide = reverseTargetSide;
    }

    //사용하지 않는 ID가 들어오지 않았는지 확인함
    private static void CheckUnusedId(string value, string parameterName, string message)
    {
        if (value != null)
            throw new ArgumentException(message, parameterName);
    }
}
