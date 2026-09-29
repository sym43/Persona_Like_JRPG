using System;

/// <summary>
/// 전투원 한 명이 현재 장착한 장비 ID. <br/>
/// 무기, 방어구, 신발, 액세서리 네 칸을 고정해서 관리함.
/// </summary>
public sealed class EquipmentSet
{
    public string WeaponId { get; }
    public string ArmorId { get; }
    public string ShoesId { get; }
    public string AccessoryId { get; }

    //장착 상태를 만듦. 비어 있는 슬롯은 null을 사용함
    public EquipmentSet(string weaponId, string armorId,
        string shoesId, string accessoryId)
    {
        CheckId(weaponId);
        CheckId(armorId);
        CheckId(shoesId);
        CheckId(accessoryId);

        WeaponId = weaponId;
        ArmorId = armorId;
        ShoesId = shoesId;
        AccessoryId = accessoryId;
    }

    //지정한 부위의 장비 ID를 가져옴
    public string GetId(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return WeaponId;
            case EquipmentSlot.Armor: return ArmorId;
            case EquipmentSlot.Shoes: return ShoesId;
            case EquipmentSlot.Accessory: return AccessoryId;
            default: throw new ArgumentOutOfRangeException(nameof(slot), "알 수 없는 장비 부위입니다.");
        }
    }

    //한 부위만 바꾼 새 장착 상태를 반환함
    public EquipmentSet Change(EquipmentSlot slot, string equipmentId)
    {
        CheckId(equipmentId);
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                return new EquipmentSet(equipmentId, ArmorId, ShoesId, AccessoryId);
            case EquipmentSlot.Armor:
                return new EquipmentSet(WeaponId, equipmentId, ShoesId, AccessoryId);
            case EquipmentSlot.Shoes:
                return new EquipmentSet(WeaponId, ArmorId, equipmentId, AccessoryId);
            case EquipmentSlot.Accessory:
                return new EquipmentSet(WeaponId, ArmorId, ShoesId, equipmentId);
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), "알 수 없는 장비 부위입니다.");
        }
    }

    private static void CheckId(string id)
    {
        if (id != null) BattleDataChecks.CheckText(id);
    }
}
