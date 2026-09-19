using System;
using System.Collections.Generic;

/// <summary>
/// 전투 원본 CSV를 전부 읽고 서로의 ID 참조를 확인함.
/// </summary>
public sealed class BattleDataLoader
{
    private readonly SkillDataLoader skillLoader = new SkillDataLoader();
    private readonly SkillEffectDataLoader skillEffectLoader = new SkillEffectDataLoader();
    private readonly AnimaSkillDataLoader animaSkillLoader = new AnimaSkillDataLoader();
    private readonly AnimaDataLoader animaLoader = new AnimaDataLoader();
    private readonly BattleUnitDataLoader battleUnitLoader = new BattleUnitDataLoader();

    private BattleDataSet loadedData;

    //전투 CSV를 한 번 읽고 연결을 검사해서 반환함
    public BattleDataSet Load()
    {
        if (loadedData != null)
            return loadedData;

        var skills = skillLoader.Load();
        var skillEffects = skillEffectLoader.Load();
        var learnableSkills = animaSkillLoader.Load();
        var animas = animaLoader.Load(learnableSkills);
        var battleUnits = battleUnitLoader.Load();

        CheckReferences(animas, skills, skillEffects, learnableSkills, battleUnits);
        loadedData = new BattleDataSet(animas, skills, skillEffects, battleUnits);
        return loadedData;
    }

    //표 사이의 ID 참조를 검사함
    private static void CheckReferences(
        IReadOnlyDictionary<string, AnimaData> animas,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>> learnableSkills,
        IReadOnlyDictionary<string, BattleUnitData> battleUnits)
    {
        foreach (var pair in skillEffects)
        {
            if (!skills.TryGetValue(pair.Key, out SkillData skill))
                throw new FormatException($"skill_effects.csv에 없는 스킬 ID가 있습니다: {pair.Key}");
            if (skill.UseType != SkillUseType.Active)
                throw new FormatException($"패시브 스킬에는 실행 효과를 넣을 수 없습니다: {pair.Key}");
        }

        foreach (var skill in skills.Values)
        {
            if (skill.UseType == SkillUseType.Active && !skillEffects.ContainsKey(skill.Id))
                throw new FormatException($"액티브 스킬에 실행 효과가 없습니다: {skill.Id}");
        }

        foreach (var animaSkills in learnableSkills)
        {
            if (!animas.ContainsKey(animaSkills.Key))
                throw new FormatException($"anima_skills.csv에 없는 아니마 ID가 있습니다: {animaSkills.Key}");

            foreach (var learnableSkill in animaSkills.Value)
            {
                if (!skills.ContainsKey(learnableSkill.SkillId))
                    throw new FormatException($"anima_skills.csv에 없는 스킬 ID가 있습니다: {learnableSkill.SkillId}");
            }
        }

        foreach (var battleUnit in battleUnits.Values)
        {
            if (battleUnit.FixedAnimaId != null && !animas.ContainsKey(battleUnit.FixedAnimaId))
                throw new FormatException($"battle_units.csv에 없는 고정 아니마 ID가 있습니다: {battleUnit.FixedAnimaId}");
        }
    }
}
