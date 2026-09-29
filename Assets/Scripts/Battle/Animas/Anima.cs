using System;
using System.Collections.Generic;

/// <summary>
/// 아니마 런타임 데이터.
/// </summary>
public sealed class Anima
{
    //아니마 개체를 구분하는 ID
    public string InstanceId { get; }
    //아니마 기본 데이터
    public AnimaData Data { get; }
    //현재 레벨
    public int Level { get; private set; }
    //현재 경험치
    public long Experience { get; private set; }
    //현재 능력치
    public BattleStats Stats { get; private set; }
    //현재 배운 스킬 ID 목록
    public IReadOnlyList<string> SkillIds { get; private set; }

    //아니마를 만듦
    public Anima(string instanceId, AnimaData animaData, int level,
        long experience, BattleStats stats, IEnumerable<string> skillIds)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(instanceId);
        if (animaData == null) throw new ArgumentNullException(nameof(animaData), "아니마 원본 데이터가 필요합니다.");
        var skills = BattleDataChecks.CheckAndCopySkillIds(skillIds, 8);

        #endregion

        InstanceId = instanceId;
        Data = animaData;
        SetGrowth(level, experience, stats);
        SkillIds = skills;
    }

    //성장 수치를 설정함
    public void SetGrowth(int level, long experience, BattleStats stats)
    {
        #region 입력값 검사

        BattleDataChecks.CheckLevel(level);
        if (level < Data.BaseLevel)
            throw new ArgumentOutOfRangeException(nameof(level), "레벨은 아니마의 기본 레벨 이상이어야 합니다.");
        if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience), "경험치는 0 이상이어야 합니다.");
        if (stats == null) throw new ArgumentNullException(nameof(stats), "전투 능력치가 필요합니다.");

        #endregion

        Level = level;
        Experience = experience;
        Stats = stats;
    }

    //현재 스킬 목록을 설정함
    public void SetSkillIds(IEnumerable<string> skillIds)
    {
        var copy = BattleDataChecks.CheckAndCopySkillIds(skillIds, 8);
        SkillIds = copy;
    }
}
