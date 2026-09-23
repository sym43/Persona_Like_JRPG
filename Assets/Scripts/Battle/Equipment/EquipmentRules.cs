using System;
using System.Collections.Generic;

/// <summary>
/// 전투원이 장비를 해당 부위에 장착할 수 있는지 검사함.
/// </summary>
internal static class EquipmentRules
{
    internal static void Check(BattleUnitData unit, EquipmentSet equipment,
        IReadOnlyDictionary<string, EquipmentData> equipmentData,
        IReadOnlyDictionary<string, IReadOnlyList<string>> unitEquipGroups,
        bool requireWeapon)
    {
        if (unit == null) throw new ArgumentNullException(nameof(unit), "전투원 원본이 필요합니다.");
        if (equipment == null) throw new ArgumentNullException(nameof(equipment), "장착 상태가 필요합니다.");
        if (equipmentData == null) throw new ArgumentNullException(nameof(equipmentData), "장비 원본이 필요합니다.");
        if (unitEquipGroups == null) throw new ArgumentNullException(nameof(unitEquipGroups), "장착 그룹 목록이 필요합니다.");
        if (requireWeapon && equipment.WeaponId == null)
            throw new InvalidOperationException($"일반 공격을 사용하는 전투원에게 무기가 없습니다: {unit.Id}");

        unitEquipGroups.TryGetValue(unit.Id, out IReadOnlyList<string> allowedGroups);
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            string id = equipment.GetId(slot);
            if (id == null) continue;
            if (!equipmentData.TryGetValue(id, out EquipmentData data))
                throw new InvalidOperationException($"없는 장비 ID입니다: {id}");
            if (data.Slot != slot)
                throw new InvalidOperationException($"장비 부위가 맞지 않습니다: {unit.Id} / {id}");
            if (allowedGroups == null || !Contains(allowedGroups, data.EquipGroupId))
                throw new InvalidOperationException($"장착할 수 없는 장비입니다: {unit.Id} / {id}");
        }
    }

    private static bool Contains(IReadOnlyList<string> values, string target)
    {
        foreach (string value in values)
        {
            if (string.Equals(value, target, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
