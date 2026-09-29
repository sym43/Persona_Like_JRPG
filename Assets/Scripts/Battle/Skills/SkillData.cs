using System;

/// <summary>
/// 스킬 원본 데이터. <br/>
/// 스킬의 사용 방식, 비용과 대상을 담고 실제 효과는 SkillEffectData로 연결함.
/// </summary>
public sealed class SkillData
{
    //스킬을 구분하는 ID
    public string Id { get; }
    //ui에 보여질 이름
    public string DisplayName { get; }
    //ui에 보여질 설명
    public string Description { get; }
    //액티브 또는 패시브
    public SkillUseType UseType { get; }
    //자원 종류
    public SkillCostType CostType { get; }
    //사용 비용
    public int Cost { get; }
    //스킬 대상
    public BattleTargetType TargetType { get; }

    //스킬 데이터를 만듦
    public SkillData(string id, string displayName, string description, SkillUseType useType,
        SkillCostType costType, int cost, BattleTargetType targetType)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(id);
        BattleDataChecks.CheckText(displayName);
        BattleDataChecks.CheckText(description);
        if (!Enum.IsDefined(typeof(SkillUseType), useType))
            throw new ArgumentOutOfRangeException(nameof(useType), "알 수 없는 스킬 사용 방식입니다.");
        if (!Enum.IsDefined(typeof(SkillCostType), costType))
            throw new ArgumentOutOfRangeException(nameof(costType), "알 수 없는 비용 종류입니다.");
        if (!Enum.IsDefined(typeof(BattleTargetType), targetType))
            throw new ArgumentOutOfRangeException(nameof(targetType), "알 수 없는 대상 종류입니다.");
        if (cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "스킬 비용은 0 이상이어야 합니다.");
        if (costType == SkillCostType.None && cost != 0)
            throw new ArgumentException("비용 없음 스킬은 비용이 0이어야 합니다.");
        if (useType == SkillUseType.Passive &&
            (costType != SkillCostType.None || cost != 0))
            throw new ArgumentException("패시브 스킬은 자원 비용을 가질 수 없습니다.");

        #endregion

        Id = id;
        DisplayName = displayName;
        Description = description;
        UseType = useType;
        CostType = costType;
        Cost = cost;
        TargetType = targetType;
    }
}
