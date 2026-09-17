using System;

/// <summary>
/// 스킬 원본 데이터. <br/>
/// 스킬의 사용 방식, 속성, 비용, 대상, 위력 정보를 담음.
/// </summary>
public sealed class SkillData
{
    //스킬을 구분하는 ID
    public string Id { get; }
    //ui에 보여질 이름
    public string DisplayName { get; }
    //액티브 또는 패시브
    public SkillUseType UseType { get; }
    //피해 속성. 피해가 없으면 없음
    public DamageType? DamageType { get; }
    //자원 종류
    public SkillCostType CostType { get; }
    //사용 비용
    public int Cost { get; }
    //스킬 대상
    public SkillTargetType TargetType { get; }
    //위력
    public int Power { get; }
    //명중률
    public int Accuracy { get; }
    //타격 수
    public int HitCount { get; }

    //스킬 데이터를 만듦
    public SkillData(string id, string displayName, SkillUseType useType,
        DamageType? damageType, SkillCostType costType, int cost,
        SkillTargetType targetType, int power, int accuracy, int hitCount)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(id);
        BattleDataChecks.CheckText(displayName);
        if (!Enum.IsDefined(typeof(SkillUseType), useType))
            throw new ArgumentOutOfRangeException(nameof(useType), "알 수 없는 스킬 사용 방식입니다.");
        if (damageType.HasValue &&
            !Enum.IsDefined(typeof(DamageType), damageType.Value))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 피해 속성입니다.");
        if (!Enum.IsDefined(typeof(SkillCostType), costType))
            throw new ArgumentOutOfRangeException(nameof(costType), "알 수 없는 비용 종류입니다.");
        if (!Enum.IsDefined(typeof(SkillTargetType), targetType))
            throw new ArgumentOutOfRangeException(nameof(targetType), "알 수 없는 대상 종류입니다.");
        if (cost < 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "스킬 비용은 0 이상이어야 합니다.");
        if (costType == SkillCostType.None && cost != 0)
            throw new ArgumentException("비용 없음 스킬은 비용이 0이어야 합니다.");
        if (power < 0)
            throw new ArgumentOutOfRangeException(nameof(power), "스킬 위력은 0 이상이어야 합니다.");
        if (accuracy < 0 || accuracy > 100)
            throw new ArgumentOutOfRangeException(nameof(accuracy), "명중률은 0 이상 100 이하여야 합니다.");
        if (hitCount < 1)
            throw new ArgumentOutOfRangeException(nameof(hitCount), "타격 수는 1 이상이어야 합니다.");
        if (useType == SkillUseType.Passive &&
            (costType != SkillCostType.None || cost != 0))
            throw new ArgumentException("패시브 스킬은 자원 비용을 가질 수 없습니다.");

        #endregion

        Id = id;
        DisplayName = displayName;
        UseType = useType;
        DamageType = damageType;
        CostType = costType;
        Cost = cost;
        TargetType = targetType;
        Power = power;
        Accuracy = accuracy;
        HitCount = hitCount;
    }
}
