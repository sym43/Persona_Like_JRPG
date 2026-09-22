using System;
using System.Collections.Generic;

/// <summary>
/// 전투 원본 CSV를 전부 읽고 서로의 ID 참조를 확인함.
/// </summary>
public sealed class BattleDataLoader
{
    private readonly SkillDataLoader skillLoader = new SkillDataLoader();
    private readonly SkillEffectDataLoader skillEffectLoader = new SkillEffectDataLoader();
    private readonly ItemDataLoader itemLoader = new ItemDataLoader();
    private readonly ItemEffectDataLoader itemEffectLoader = new ItemEffectDataLoader();
    private readonly BattleEffectDataLoader battleEffectLoader = new BattleEffectDataLoader();
    private readonly AnimaSkillDataLoader animaSkillLoader = new AnimaSkillDataLoader();
    private readonly AnimaDataLoader animaLoader = new AnimaDataLoader();
    private readonly BattleUnitDataLoader battleUnitLoader = new BattleUnitDataLoader();

    private BattleDataSet loadedData;

    public BattleDataSet Load()
    {
        if (loadedData != null) return loadedData;

        var skills = skillLoader.Load();
        var skillEffects = skillEffectLoader.Load();
        var items = itemLoader.Load();
        var itemEffects = itemEffectLoader.Load();
        var battleEffects = battleEffectLoader.Load();
        var learnableSkills = animaSkillLoader.Load();
        var animas = animaLoader.Load(learnableSkills);
        var battleUnits = battleUnitLoader.Load();

        CheckReferences(animas, skills, skillEffects, items, itemEffects,
            battleEffects, learnableSkills, battleUnits);
        loadedData = new BattleDataSet(animas, skills, skillEffects, items,
            itemEffects, battleEffects, battleUnits);
        return loadedData;
    }

    private static void CheckReferences(
        IReadOnlyDictionary<string, AnimaData> animas,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, ItemData> items,
        IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> itemEffects,
        IReadOnlyDictionary<string, BattleEffectData> battleEffects,
        IReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>> learnableSkills,
        IReadOnlyDictionary<string, BattleUnitData> battleUnits)
    {
        foreach (var pair in skillEffects)
        {
            if (!skills.TryGetValue(pair.Key, out SkillData skill))
                throw new FormatException($"skill_effects.csv에 없는 스킬 ID가 있습니다: {pair.Key}");
            if (skill.UseType != SkillUseType.Active)
                throw new FormatException($"패시브 스킬에는 실행 효과를 넣을 수 없습니다: {pair.Key}");

            bool hasAnalyze = false;
            foreach (SkillEffectData effect in pair.Value)
            {
                if (effect.Type == SkillEffectType.ApplyEffect &&
                    !battleEffects.ContainsKey(effect.EffectId))
                    throw new FormatException($"skill_effects.csv에 없는 전투 효과 ID가 있습니다: {effect.EffectId}");
                if (effect.Type == SkillEffectType.Analyze)
                {
                    hasAnalyze = true;
                    if (effect.ApplyChance != 100)
                        throw new FormatException($"분석 효과의 적용 확률은 100이어야 합니다: {skill.Id}");
                }
            }

            if (hasAnalyze && (skill.TargetType != BattleTargetType.OneEnemy ||
                               pair.Value.Count != 1))
                throw new FormatException($"분석 스킬은 적 1체를 대상으로 분석 효과 하나만 가져야 합니다: {skill.Id}");
        }

        foreach (var skill in skills.Values)
        {
            if (skill.UseType == SkillUseType.Active && !skillEffects.ContainsKey(skill.Id))
                throw new FormatException($"액티브 스킬에 실행 효과가 없습니다: {skill.Id}");
        }

        foreach (var pair in itemEffects)
        {
            if (!items.TryGetValue(pair.Key, out ItemData item))
                throw new FormatException($"item_effects.csv에 없는 아이템 ID가 있습니다: {pair.Key}");

            bool hasRevive = false;
            foreach (ItemEffectData effect in pair.Value)
            {
                if (effect.Type == ItemEffectType.ApplyEffect &&
                    !battleEffects.ContainsKey(effect.EffectId))
                    throw new FormatException($"item_effects.csv에 없는 전투 효과 ID가 있습니다: {effect.EffectId}");
                if (effect.Type == ItemEffectType.Revive) hasRevive = true;
            }

            if (item.TargetType == BattleTargetType.OneDeadAlly && !hasRevive)
                throw new FormatException($"전투 불능 아군 대상 아이템에는 부활 효과가 필요합니다: {item.Id}");
            if (hasRevive && item.TargetType != BattleTargetType.OneDeadAlly)
                throw new FormatException($"부활 아이템의 대상은 전투 불능 아군 1명이어야 합니다: {item.Id}");
        }

        foreach (var item in items.Values)
        {
            if (!itemEffects.TryGetValue(item.Id, out IReadOnlyList<ItemEffectData> effects) ||
                effects.Count == 0)
                throw new FormatException($"아이템에 실행 효과가 없습니다: {item.Id}");
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
