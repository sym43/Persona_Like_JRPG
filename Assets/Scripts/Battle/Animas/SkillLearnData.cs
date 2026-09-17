using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 스킬을 몇레벨에 배우는지 데이터.
/// </summary>
public sealed class SkillLearnData
{
    //배울 스킬 ID
    public string SkillId { get; }
    //배우는 레벨
    public int Level { get; }

    //스킬 습득 데이터를 만듦
    public SkillLearnData(string skillId, int level)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(skillId);
        BattleDataChecks.CheckLevel(level);

        #endregion

        SkillId = skillId;
        Level = level;
    }
}
