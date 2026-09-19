using System;

/// <summary>
/// 스킬 하나에 연결된 실행 효과. <br/>
/// 같은 스킬에 여러 개를 넣으면 Order 순서대로 실행됨.
/// </summary>
public sealed class SkillEffectData
{
    //효과를 가진 스킬 ID
    public string SkillId { get; }
    //스킬 안에서 실행될 순서
    public int Order { get; }
    //효과 종류
    public SkillEffectType Type { get; }
    //피해 속성. 피해 효과가 아니면 없음
    public DamageType? DamageType { get; }
    //피해 또는 회복 위력
    public int Power { get; }
    //명중률
    public int Accuracy { get; }
    //타격 수
    public int HitCount { get; }
    //기본 치명타율
    public int CriticalRate { get; }

    //스킬 효과 데이터를 만듦
    public SkillEffectData(string skillId, int order, SkillEffectType type,
        DamageType? damageType, int power, int accuracy, int hitCount, int criticalRate)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(skillId);
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), "효과 순서는 0 이상이어야 합니다.");
        if (!Enum.IsDefined(typeof(SkillEffectType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 스킬 효과입니다.");
        if (damageType.HasValue && !Enum.IsDefined(typeof(DamageType), damageType.Value))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 피해 속성입니다.");
        if (power < 0)
            throw new ArgumentOutOfRangeException(nameof(power), "효과 위력은 0 이상이어야 합니다.");
        if (accuracy < 0 || accuracy > 100)
            throw new ArgumentOutOfRangeException(nameof(accuracy), "명중률은 0 이상 100 이하여야 합니다.");
        if (hitCount < 1)
            throw new ArgumentOutOfRangeException(nameof(hitCount), "타격 수는 1 이상이어야 합니다.");
        if (criticalRate < 0 || criticalRate > 100)
            throw new ArgumentOutOfRangeException(nameof(criticalRate), "치명타율은 0 이상 100 이하여야 합니다.");
        if (type == SkillEffectType.Damage && !damageType.HasValue)
            throw new ArgumentException("피해 효과에는 피해 속성이 필요합니다.", nameof(damageType));
        if (type == SkillEffectType.Heal && damageType.HasValue)
            throw new ArgumentException("회복 효과에는 피해 속성을 넣을 수 없습니다.", nameof(damageType));
        if (type == SkillEffectType.Heal && criticalRate != 0)
            throw new ArgumentException("회복 효과의 치명타율은 0이어야 합니다.", nameof(criticalRate));
        if (type == SkillEffectType.Heal && (accuracy != 100 || hitCount != 1))
            throw new ArgumentException("회복 효과는 명중률 100, 타격 수 1이어야 합니다.");

        #endregion

        SkillId = skillId;
        Order = order;
        Type = type;
        DamageType = damageType;
        Power = power;
        Accuracy = accuracy;
        HitCount = hitCount;
        CriticalRate = criticalRate;
    }
}
