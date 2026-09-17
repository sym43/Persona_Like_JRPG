using System;

/// <summary>
/// 아니마 런타임 데이터와 저장 데이터를 서로 바꿈.
/// </summary>
public static class AnimaSaveConverter
{
    //아니마를 저장 데이터로 바꿈
    public static AnimaSaveData Capture(Anima anima)
    {
        #region 입력값 검사

        if (anima == null) throw new ArgumentNullException(nameof(anima), "아니마가 필요합니다.");

        #endregion

        var skills = new string[anima.SkillIds.Count];
        for (int i = 0; i < skills.Length; i++) skills[i] = anima.SkillIds[i];
        return new AnimaSaveData
        {
            instanceId = anima.InstanceId,
            dataId = anima.Data.Id,
            level = anima.Level,
            experience = anima.Experience,
            strength = anima.Stats.Strength,
            magic = anima.Stats.Magic,
            endurance = anima.Stats.Endurance,
            agility = anima.Stats.Agility,
            luck = anima.Stats.Luck,
            skillIds = skills
        };
    }

    //저장 데이터에서 아니마를 복원함
    public static Anima Restore(AnimaSaveData saveData, AnimaData animaData)
    {
        #region 저장 데이터 검사

        if (saveData == null) throw new ArgumentNullException(nameof(saveData), "저장 데이터가 필요합니다.");
        if (animaData == null) throw new ArgumentNullException(nameof(animaData), "아니마 원본 데이터가 필요합니다.");
        if (!string.Equals(saveData.dataId, animaData.Id, StringComparison.Ordinal))
            throw new ArgumentException("아니마 원본 ID가 저장 데이터의 ID와 일치하지 않습니다.");

        #endregion

        return new Anima(saveData.instanceId, animaData, saveData.level, saveData.experience,
            new BattleStats(saveData.strength, saveData.magic, saveData.endurance, saveData.agility,
                saveData.luck), saveData.skillIds);
    }
}
