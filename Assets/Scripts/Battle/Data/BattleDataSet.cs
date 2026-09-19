using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 전투에 사용할 원본 데이터를 한 번에 묶은 목록.
/// </summary>
public sealed class BattleDataSet
{
    //아니마 원본 목록
    public IReadOnlyDictionary<string, AnimaData> Animas { get; }
    //스킬 원본 목록
    public IReadOnlyDictionary<string, SkillData> Skills { get; }
    //스킬별 실행 효과 목록
    public IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> SkillEffects { get; }
    //전투원 원본 목록
    public IReadOnlyDictionary<string, BattleUnitData> BattleUnits { get; }

    //검증이 끝난 전투 데이터를 묶음
    internal BattleDataSet(
        IReadOnlyDictionary<string, AnimaData> animas,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, BattleUnitData> battleUnits)
    {
        if (animas == null) throw new ArgumentNullException(nameof(animas), "아니마 목록이 필요합니다.");
        if (skills == null) throw new ArgumentNullException(nameof(skills), "스킬 목록이 필요합니다.");
        if (skillEffects == null) throw new ArgumentNullException(nameof(skillEffects), "스킬 효과 목록이 필요합니다.");
        if (battleUnits == null) throw new ArgumentNullException(nameof(battleUnits), "전투원 목록이 필요합니다.");

        Animas = Copy(animas);
        Skills = Copy(skills);
        SkillEffects = CopyLists(skillEffects);
        BattleUnits = Copy(battleUnits);
    }

    //목록을 복사해서 외부 변경을 막음
    private static IReadOnlyDictionary<string, T> Copy<T>(IReadOnlyDictionary<string, T> source)
    {
        var copy = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var pair in source)
            copy.Add(pair.Key, pair.Value);

        return new ReadOnlyDictionary<string, T>(copy);
    }

    //목록 안의 효과 목록까지 복사해서 외부 변경을 막음
    private static IReadOnlyDictionary<string, IReadOnlyList<T>> CopyLists<T>(
        IReadOnlyDictionary<string, IReadOnlyList<T>> source)
    {
        var copy = new Dictionary<string, IReadOnlyList<T>>(StringComparer.Ordinal);
        foreach (var pair in source)
            copy.Add(pair.Key, new ReadOnlyCollection<T>(new List<T>(pair.Value)));

        return new ReadOnlyDictionary<string, IReadOnlyList<T>>(copy);
    }
}
