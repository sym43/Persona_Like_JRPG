using System;

/// <summary>
/// 장비에 붙는 추가 효과 한 줄. <br/>
/// 같은 장비의 여러 효과는 순서대로 읽음.
/// </summary>
public sealed class EquipmentEffectData
{
    public string EquipmentId { get; }
    public int Order { get; }
    public EquipmentEffectType Type { get; }
    public EquipmentStatType? StatType { get; }
    public int Value { get; }
    public DamageType? DamageType { get; }
    public ResistanceType? ResistanceType { get; }
    public BattleEffectType? MentalState { get; }
    public MentalResistanceType? MentalResistance { get; }
    public string EffectId { get; }
    public int ApplyChance { get; }

    //장비 효과를 만듦
    public EquipmentEffectData(string equipmentId, int order,
        EquipmentEffectType type, EquipmentStatType? statType, int value,
        DamageType? damageType, ResistanceType? resistanceType,
        BattleEffectType? mentalState, MentalResistanceType? mentalResistance,
        string effectId, int applyChance)
    {
        BattleDataChecks.CheckText(equipmentId);
        if (order < 0) throw new ArgumentOutOfRangeException(nameof(order), "효과 순서는 0 이상이어야 합니다.");
        if (!Enum.IsDefined(typeof(EquipmentEffectType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 장비 효과입니다.");
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "효과 수치는 0 이상이어야 합니다.");
        if (applyChance < 0 || applyChance > 100)
            throw new ArgumentOutOfRangeException(nameof(applyChance), "적용 확률은 0~100이어야 합니다.");

        switch (type)
        {
            case EquipmentEffectType.StatBonus:
                if (!statType.HasValue || value == 0)
                    throw new ArgumentException("능력치 증가에는 능력치 종류와 1 이상의 수치가 필요합니다.");
                break;
            case EquipmentEffectType.DamageResistance:
                if (!damageType.HasValue || !resistanceType.HasValue)
                    throw new ArgumentException("속성 저항 효과에는 속성과 저항 종류가 필요합니다.");
                break;
            case EquipmentEffectType.MentalResistance:
                if (!mentalState.HasValue || !MentalResistanceTable.IsMentalState(mentalState.Value) ||
                    !mentalResistance.HasValue)
                    throw new ArgumentException("정신 상태 저항 효과의 값이 잘못됐습니다.");
                break;
            case EquipmentEffectType.BasicAttackEffect:
                BattleDataChecks.CheckText(effectId);
                break;
            case EquipmentEffectType.StartBattleEffect:
                BattleDataChecks.CheckText(effectId);
                if (applyChance != 100)
                    throw new ArgumentException("전투 시작 효과의 적용 확률은 100이어야 합니다.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 장비 효과입니다.");
        }

        EquipmentId = equipmentId;
        Order = order;
        Type = type;
        StatType = statType;
        Value = value;
        DamageType = damageType;
        ResistanceType = resistanceType;
        MentalState = mentalState;
        MentalResistance = mentalResistance;
        EffectId = effectId;
        ApplyChance = applyChance;
    }
}
