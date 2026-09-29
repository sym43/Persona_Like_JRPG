using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 정신 상태별 저항을 빠르게 찾는 표.
/// </summary>
public sealed class MentalResistanceTable
{
    private static readonly BattleEffectType[] MentalStateTypes =
    {
        BattleEffectType.Intoxication,
        BattleEffectType.Berserk,
        BattleEffectType.Lethargy,
        BattleEffectType.Panic,
        BattleEffectType.Charm,
        BattleEffectType.Thrill,
        BattleEffectType.Intimidation
    };

    private readonly IReadOnlyDictionary<BattleEffectType, MentalResistanceType> values;

    //상태별 저항을 빠짐없이 받음
    public MentalResistanceTable(
        IEnumerable<KeyValuePair<BattleEffectType, MentalResistanceType>> entries)
    {
        if (entries == null)
            throw new ArgumentNullException(nameof(entries), "상태 저항 목록이 필요합니다.");

        var copy = new Dictionary<BattleEffectType, MentalResistanceType>();
        foreach (var entry in entries)
        {
            if (!IsMentalState(entry.Key))
                throw new ArgumentException("정신 상태가 아닌 효과가 상태 저항 목록에 있습니다.", nameof(entries));
            if (!Enum.IsDefined(typeof(MentalResistanceType), entry.Value))
                throw new ArgumentException("알 수 없는 상태 저항입니다.", nameof(entries));
            if (!copy.TryAdd(entry.Key, entry.Value))
                throw new ArgumentException("중복된 상태 저항이 있습니다.", nameof(entries));
        }

        foreach (BattleEffectType type in MentalStateTypes)
        {
            if (!copy.ContainsKey(type))
                throw new ArgumentException($"상태 저항이 빠졌습니다: {type}", nameof(entries));
        }

        values = new ReadOnlyDictionary<BattleEffectType, MentalResistanceType>(copy);
    }

    //상태에 대한 저항 단계를 반환함
    public MentalResistanceType Get(BattleEffectType type)
    {
        if (!values.TryGetValue(type, out MentalResistanceType resistance))
            throw new ArgumentOutOfRangeException(nameof(type), "정신 상태가 아닌 효과입니다.");
        return resistance;
    }

    //저항 단계를 부여율 배수로 바꿈
    public double GetMultiplier(BattleEffectType type)
    {
        switch (Get(type))
        {
            case MentalResistanceType.Weak: return 1.25d;
            case MentalResistanceType.Normal: return 1d;
            case MentalResistanceType.Resist: return 0.5d;
            case MentalResistanceType.Immune: return 0d;
            default: throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 상태 저항입니다.");
        }
    }

    //정신 상태 효과인지 확인함
    public static bool IsMentalState(BattleEffectType type) =>
        type >= BattleEffectType.Intoxication && type <= BattleEffectType.Intimidation;
}
