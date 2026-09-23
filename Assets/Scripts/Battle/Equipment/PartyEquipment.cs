using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 플레이 가능한 전투원별 현재 장착 상태. <br/>
/// 장비 변경과 저장에서 같은 상태를 사용함.
/// </summary>
public sealed class PartyEquipment
{
    private readonly Dictionary<string, EquipmentSet> sets;
    public IReadOnlyDictionary<string, EquipmentSet> Sets { get; }

    public PartyEquipment(IEnumerable<KeyValuePair<string, EquipmentSet>> entries = null)
    {
        sets = new Dictionary<string, EquipmentSet>(StringComparer.Ordinal);
        if (entries != null)
        {
            foreach (KeyValuePair<string, EquipmentSet> entry in entries)
                Set(entry.Key, entry.Value);
        }
        Sets = new ReadOnlyDictionary<string, EquipmentSet>(sets);
    }

    //전투원의 현재 장착 상태를 가져옴
    public EquipmentSet Get(string unitDataId)
    {
        BattleDataChecks.CheckText(unitDataId);
        if (!sets.TryGetValue(unitDataId, out EquipmentSet equipment))
            throw new KeyNotFoundException($"전투원의 장착 상태가 없습니다: {unitDataId}");
        return equipment;
    }

    //전투원의 장착 상태를 지정함
    internal void Set(string unitDataId, EquipmentSet equipment)
    {
        BattleDataChecks.CheckText(unitDataId);
        if (equipment == null)
            throw new ArgumentNullException(nameof(equipment), "장착 상태가 필요합니다.");
        sets[unitDataId] = equipment;
    }
}
