using System;

/// <summary>
/// 스킬 하나에 연결된 실행 효과. <br/>
/// 같은 스킬에 여러 개를 넣으면 Order 순서대로 실행됨.
/// </summary>
public sealed class SkillEffectData
{
    public string SkillId { get; }
    public int Order { get; }
    public SkillEffectType Type { get; }
    public DamageType? DamageType { get; }
    public int Power { get; }
    public int Accuracy { get; }
    public int HitCount { get; }
    public int CriticalRate { get; }
    public string EffectId { get; }
    public int ApplyChance { get; }

    //스킬 효과 데이터를 만듦
    public SkillEffectData(string skillId, int order, SkillEffectType type,
        DamageType? damageType, int power, int accuracy, int hitCount,
        int criticalRate, string effectId = null, int applyChance = 100)
    {
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
        if (applyChance < 0 || applyChance > 100)
            throw new ArgumentOutOfRangeException(nameof(applyChance), "효과 부여율은 0 이상 100 이하여야 합니다.");

        bool damage = type == SkillEffectType.Damage;
        bool heal = type == SkillEffectType.Heal;
        bool apply = type == SkillEffectType.ApplyEffect;
        if (damage && !damageType.HasValue)
            throw new ArgumentException("피해 효과에는 피해 속성이 필요합니다.", nameof(damageType));
        if (!damage && damageType.HasValue)
            throw new ArgumentException("피해 효과가 아니면 피해 속성을 넣을 수 없습니다.", nameof(damageType));
        if (heal && criticalRate != 0)
            throw new ArgumentException("회복 효과의 치명타율은 0이어야 합니다.", nameof(criticalRate));
        if (!damage && !heal && power != 0)
            throw new ArgumentException("피해·회복 외 효과의 위력은 0이어야 합니다.", nameof(power));
        if (!damage && !heal && (accuracy != 100 || hitCount != 1 || criticalRate != 0))
            throw new ArgumentException("전투 효과는 명중률 100, 타격 수 1, 치명타율 0이어야 합니다.");
        if (apply) BattleDataChecks.CheckText(effectId);
        else if (effectId != null)
            throw new ArgumentException("효과 적용이 아니면 효과 ID를 넣을 수 없습니다.", nameof(effectId));

        SkillId = skillId;
        Order = order;
        Type = type;
        DamageType = damageType;
        Power = power;
        Accuracy = accuracy;
        HitCount = hitCount;
        CriticalRate = criticalRate;
        EffectId = effectId;
        ApplyChance = applyChance;
    }
}
