using System;

/// <summary>
/// 플레이 가능한 전투원 한 명의 장착 장비 ID를 저장함.
/// </summary>
[Serializable]
public sealed class EquipmentSaveData
{
    public string unitDataId;
    public string weaponId;
    public string armorId;
    public string shoesId;
    public string accessoryId;
}
