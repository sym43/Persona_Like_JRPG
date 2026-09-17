using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 힘, 마력, 내구, 민첩, 운을 묶은 능력치 데이터.
/// </summary>
public sealed class BattleStats
{
    //힘 수치
    public int Strength { get; }
    //마력 수치
    public int Magic { get; }
    //내구 수치
    public int Endurance { get; }
    //민첩 수치
    public int Agility { get; }
    //운 수치
    public int Luck { get; }

    //전투 능력치를 만듦
    public BattleStats(int strength, int magic, int endurance, int agility, int luck)
    {
        #region 입력값 검사

        if (strength < 1 || strength > 99 || magic < 1 || magic > 99 ||
            endurance < 1 || endurance > 99 || agility < 1 || agility > 99 ||
            luck < 1 || luck > 99)
            throw new ArgumentOutOfRangeException(nameof(strength),
                "모든 전투 능력치는 1 이상 99 이하여야 합니다.");

        #endregion

        Strength = strength;
        Magic = magic;
        Endurance = endurance;
        Agility = agility;
        Luck = luck;
    }
}
