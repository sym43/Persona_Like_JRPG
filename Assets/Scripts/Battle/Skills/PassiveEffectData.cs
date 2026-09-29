using System;

/// <summary>
/// 패시브 스킬 하나에 연결된 자동 효과.
/// 수치는 배율 백분율, 회복률 또는 확률로 사용함.
/// </summary>
public sealed class PassiveEffectData
{
    public string SkillId { get; }
    public int Order { get; }
    public PassiveEffectType Type { get; }
    public DamageType? DamageType { get; }
    public int Value { get; }
    public ResistanceType? ResistanceType { get; }
    public string EffectId { get; }

    public PassiveEffectData(string skillId, int order, PassiveEffectType type,
        DamageType? damageType, int value,
        ResistanceType? resistanceType, string effectId)
    {
        BattleDataChecks.CheckText(skillId);
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), "패시브 효과 순서는 0 이상이어야 합니다.");
        if (!Enum.IsDefined(typeof(PassiveEffectType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 패시브 효과입니다.");
        if (damageType.HasValue && !Enum.IsDefined(typeof(DamageType), damageType.Value))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 피해 속성입니다.");
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "패시브 효과 수치는 0 이상이어야 합니다.");

        bool resistance = type == PassiveEffectType.Resistance;
        bool startEffect = type == PassiveEffectType.StartBattleEffect;
        bool ailment = type == PassiveEffectType.AilmentMultiplier;
        bool damageFilter = type == PassiveEffectType.DamageMultiplier ||
                            type == PassiveEffectType.EvasionMultiplier;
        if (resistance && (!resistanceType.HasValue || !damageType.HasValue))
            throw new ArgumentException("속성 상성 패시브에는 피해 속성과 상성이 모두 필요합니다.");
        if (!resistance && resistanceType.HasValue)
            throw new ArgumentException("속성 상성 패시브가 아니면 상성을 넣을 수 없습니다.");
        if (startEffect) BattleDataChecks.CheckText(effectId);
        else if (!ailment && effectId != null)
            throw new ArgumentException("전투 시작·상태이상 강화 외 패시브에는 효과 ID를 넣을 수 없습니다.");
        if (!resistance && !damageFilter && damageType.HasValue)
            throw new ArgumentException("피해 강화·회피·속성 상성 외 패시브에는 피해 속성을 넣을 수 없습니다.");
        if ((type == PassiveEffectType.CounterChance ||
             type == PassiveEffectType.TurnHpRecovery ||
             type == PassiveEffectType.TurnSpRecovery) && value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "확률·회복률은 100 이하여야 합니다.");
        bool noValue = resistance || startEffect || type == PassiveEffectType.Endure ||
                       type == PassiveEffectType.EnduringSoul;
        if (noValue && value != 0)
            throw new ArgumentException("이 패시브 효과는 수치를 사용하지 않습니다.");
        if (!noValue && type != PassiveEffectType.CounterChance && value == 0)
            throw new ArgumentException("배율·회복 패시브 수치는 0보다 커야 합니다.");

        SkillId = skillId;
        Order = order;
        Type = type;
        DamageType = damageType;
        Value = value;
        ResistanceType = resistanceType;
        EffectId = effectId;
    }
}
