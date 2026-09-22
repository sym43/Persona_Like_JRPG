using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 전투에 사용할 원본 데이터를 한 번에 묶은 목록.
/// </summary>
public sealed class BattleDataSet
{
    public IReadOnlyDictionary<string, AnimaData> Animas { get; }
    public IReadOnlyDictionary<string, SkillData> Skills { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> SkillEffects { get; }
    public IReadOnlyDictionary<string, BattleEffectData> BattleEffects { get; }
    public IReadOnlyDictionary<string, BattleUnitData> BattleUnits { get; }

    internal BattleDataSet(
        IReadOnlyDictionary<string, AnimaData> animas,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, BattleEffectData> battleEffects,
        IReadOnlyDictionary<string, BattleUnitData> battleUnits)
    {
        if (animas == null) throw new ArgumentNullException(nameof(animas), "아니마 목록이 필요합니다.");
        if (skills == null) throw new ArgumentNullException(nameof(skills), "스킬 목록이 필요합니다.");
        if (skillEffects == null) throw new ArgumentNullException(nameof(skillEffects), "스킬 효과 목록이 필요합니다.");
        if (battleEffects == null) throw new ArgumentNullException(nameof(battleEffects), "전투 효과 목록이 필요합니다.");
        if (battleUnits == null) throw new ArgumentNullException(nameof(battleUnits), "전투원 목록이 필요합니다.");

        Animas = Copy(animas);
        Skills = Copy(skills);
        SkillEffects = CopyLists(skillEffects);
        BattleEffects = Copy(battleEffects);
        BattleUnits = Copy(battleUnits);
    }

    private static IReadOnlyDictionary<string, T> Copy<T>(IReadOnlyDictionary<string, T> source)
    {
        var copy = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var pair in source) copy.Add(pair.Key, pair.Value);
        return new ReadOnlyDictionary<string, T>(copy);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<T>> CopyLists<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>> source)
    {
        var copy = new Dictionary<string, IReadOnlyList<T>>(StringComparer.Ordinal);
        foreach (var pair in source)
            copy.Add(pair.Key, new ReadOnlyCollection<T>(new List<T>(pair.Value)));
        return new ReadOnlyDictionary<string, IReadOnlyList<T>>(copy);
    }
}
