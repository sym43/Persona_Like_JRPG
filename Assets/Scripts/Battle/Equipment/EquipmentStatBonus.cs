using System;

/// <summary>
/// 장비에서 얻는 다섯 전투 능력치 증가량.
/// </summary>
public sealed class EquipmentStatBonus
{
    public int Strength { get; }
    public int Magic { get; }
    public int Endurance { get; }
    public int Agility { get; }
    public int Luck { get; }

    public EquipmentStatBonus(int strength, int magic, int endurance,
        int agility, int luck)
    {
        if (strength < 0 || magic < 0 || endurance < 0 || agility < 0 || luck < 0)
            throw new ArgumentOutOfRangeException(nameof(strength), "장비 능력치 증가는 0 이상이어야 합니다.");

        Strength = strength;
        Magic = magic;
        Endurance = endurance;
        Agility = agility;
        Luck = luck;
    }

    //기본 능력치에 장비 증가량을 더하고 99를 넘지 않게 함
    public BattleStats Apply(BattleStats stats)
    {
        if (stats == null) throw new ArgumentNullException(nameof(stats), "기본 능력치가 필요합니다.");
        return new BattleStats(
            Math.Min(99, stats.Strength + Strength),
            Math.Min(99, stats.Magic + Magic),
            Math.Min(99, stats.Endurance + Endurance),
            Math.Min(99, stats.Agility + Agility),
            Math.Min(99, stats.Luck + Luck));
    }
}
