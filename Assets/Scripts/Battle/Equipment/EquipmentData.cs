using System;

/// <summary>
/// 장비 하나의 변하지 않는 원본 데이터. <br/>
/// 장착 부위와 그룹, 무기·방어구·신발의 기본 수치를 담음.
/// </summary>
public sealed class EquipmentData
{
    //장비 ID
    public string Id { get; }
    //화면에 보여줄 이름
    public string DisplayName { get; }
    //장착 부위
    public EquipmentSlot Slot { get; }
    //장착 가능한 전투원을 묶는 그룹 ID
    public string EquipGroupId { get; }
    //무기 공격력
    public int AttackPower { get; }
    //무기 명중률
    public int Accuracy { get; }
    //무기 공격 속성
    public DamageType? DamageType { get; }
    //방어구 방어력
    public int Defense { get; }
    //신발 회피 수치
    public int Evasion { get; }

    //장비 원본을 만듦
    public EquipmentData(string id, string displayName, EquipmentSlot slot,
        string equipGroupId, int attackPower, int accuracy,
        DamageType? damageType, int defense, int evasion)
    {
        BattleDataChecks.CheckText(id);
        BattleDataChecks.CheckText(displayName);
        BattleDataChecks.CheckText(equipGroupId);
        if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
            throw new ArgumentOutOfRangeException(nameof(slot), "알 수 없는 장비 부위입니다.");
        if (attackPower < 0 || defense < 0 || evasion < 0)
            throw new ArgumentOutOfRangeException(nameof(attackPower), "장비 수치는 0 이상이어야 합니다.");
        if (accuracy < 0 || accuracy > 100)
            throw new ArgumentOutOfRangeException(nameof(accuracy), "무기 명중률은 0~100이어야 합니다.");

        bool weapon = slot == EquipmentSlot.Weapon;
        if (weapon && !damageType.HasValue)
            throw new ArgumentException("무기에는 공격 속성이 필요합니다.", nameof(damageType));
        if (!weapon && (attackPower != 0 || accuracy != 0 || damageType.HasValue))
            throw new ArgumentException("무기가 아닌 장비에는 공격 수치를 넣을 수 없습니다.");
        if (slot != EquipmentSlot.Armor && defense != 0)
            throw new ArgumentException("방어력은 방어구에만 넣을 수 있습니다.", nameof(defense));
        if (slot != EquipmentSlot.Shoes && evasion != 0)
            throw new ArgumentException("회피는 신발에만 넣을 수 있습니다.", nameof(evasion));

        Id = id;
        DisplayName = displayName;
        Slot = slot;
        EquipGroupId = equipGroupId;
        AttackPower = attackPower;
        Accuracy = accuracy;
        DamageType = damageType;
        Defense = defense;
        Evasion = evasion;
    }
}
