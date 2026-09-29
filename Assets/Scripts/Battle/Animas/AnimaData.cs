using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 아니마 데이터. <br/>
/// csv를 기준으로 구성함.
/// </summary>
public sealed class AnimaData
{
    //아니마를 구분하는 ID
    public string Id { get; }
    //ui에 보여질 이름
    public string DisplayName { get; }
    //기본 레벨
    public int BaseLevel { get; }
    //기본 능력치
    public BattleStats BaseStats { get; }
    //속성 저항 목록
    public ResistanceTable Resistances { get; }
    //배울 수 있는 스킬
    public IReadOnlyList<SkillLearnData> LearnableSkills { get; }

    //아니마 데이터를 만듦
    public AnimaData(string id, string displayName,
        int baseLevel, BattleStats baseStats, ResistanceTable resistances,
        IEnumerable<SkillLearnData> learnableSkills)
    {
        #region 기본값 검사

        BattleDataChecks.CheckText(id);
        BattleDataChecks.CheckText(displayName);
        BattleDataChecks.CheckLevel(baseLevel);
        if (baseStats == null) throw new ArgumentNullException(nameof(baseStats), "기본 능력치가 필요합니다.");
        if (resistances == null) throw new ArgumentNullException(nameof(resistances), "저항 표가 필요합니다.");
        if (learnableSkills == null) throw new ArgumentNullException(nameof(learnableSkills), "습득 스킬 목록이 필요합니다.");

        #endregion

        #region 스킬 목록 검사

        var skills = new List<SkillLearnData>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var skill in learnableSkills)
        {
            if (skill == null || !ids.Add(skill.SkillId))
                throw new ArgumentException("습득 스킬 목록에 비어 있거나 중복된 항목이 있습니다.", nameof(learnableSkills));
            skills.Add(skill);
        }

        #endregion

        Id = id;
        DisplayName = displayName;
        BaseLevel = baseLevel;
        BaseStats = baseStats;
        Resistances = resistances;
        LearnableSkills = skills.AsReadOnly();
    }
}
