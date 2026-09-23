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
    public IReadOnlyDictionary<string, ItemData> Items { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> ItemEffects { get; }
    public IReadOnlyDictionary<string, BattleEffectData> BattleEffects { get; }
    public IReadOnlyDictionary<string, BattleUnitData> BattleUnits { get; }
    public IReadOnlyDictionary<string, EquipmentData> Equipments { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<EquipmentEffectData>> EquipmentEffects { get; }
    public IReadOnlyDictionary<string, EquipmentSet> DefaultEquipment { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> UnitEquipGroups { get; }

    internal BattleDataSet(
        IReadOnlyDictionary<string, AnimaData> animas,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, ItemData> items,
        IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> itemEffects,
        IReadOnlyDictionary<string, BattleEffectData> battleEffects,
        IReadOnlyDictionary<string, BattleUnitData> battleUnits,
        IReadOnlyDictionary<string, EquipmentData> equipments,
        IReadOnlyDictionary<string, IReadOnlyList<EquipmentEffectData>> equipmentEffects,
        IReadOnlyDictionary<string, EquipmentSet> defaultEquipment,
        IReadOnlyDictionary<string, IReadOnlyList<string>> unitEquipGroups)
    {
        if (animas == null) throw new ArgumentNullException(nameof(animas), "아니마 목록이 필요합니다.");
        if (skills == null) throw new ArgumentNullException(nameof(skills), "스킬 목록이 필요합니다.");
        if (skillEffects == null) throw new ArgumentNullException(nameof(skillEffects), "스킬 효과 목록이 필요합니다.");
        if (items == null) throw new ArgumentNullException(nameof(items), "아이템 목록이 필요합니다.");
        if (itemEffects == null) throw new ArgumentNullException(nameof(itemEffects), "아이템 효과 목록이 필요합니다.");
        if (battleEffects == null) throw new ArgumentNullException(nameof(battleEffects), "전투 효과 목록이 필요합니다.");
        if (battleUnits == null) throw new ArgumentNullException(nameof(battleUnits), "전투원 목록이 필요합니다.");
        if (equipments == null) throw new ArgumentNullException(nameof(equipments), "장비 목록이 필요합니다.");
        if (equipmentEffects == null) throw new ArgumentNullException(nameof(equipmentEffects), "장비 효과 목록이 필요합니다.");
        if (defaultEquipment == null) throw new ArgumentNullException(nameof(defaultEquipment), "기본 장비 목록이 필요합니다.");
        if (unitEquipGroups == null) throw new ArgumentNullException(nameof(unitEquipGroups), "장착 그룹 목록이 필요합니다.");

        Animas = Copy(animas);
        Skills = Copy(skills);
        SkillEffects = CopyLists(skillEffects);
        Items = Copy(items);
        ItemEffects = CopyLists(itemEffects);
        BattleEffects = Copy(battleEffects);
        BattleUnits = Copy(battleUnits);
        Equipments = Copy(equipments);
        EquipmentEffects = CopyLists(equipmentEffects);
        DefaultEquipment = Copy(defaultEquipment);
        UnitEquipGroups = CopyLists(unitEquipGroups);
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
