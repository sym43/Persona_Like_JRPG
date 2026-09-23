using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 진행 상태의 저장용 데이터. <br/>
/// CSV 원본을 복사하지 않고, 플레이 중 변한 값만 담음.
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    //저장 구조 버전
    public int saveVersion;
    //저장할 때 사용한 원본 데이터 버전
    public string contentVersion;
    //저장 위치 ID
    public string saveLocation;
    //보유 아니마 상태 목록
    public AnimaSaveData[] ownedAnimas;
    //보유 아이템 수량 목록
    public ItemSaveData[] items;
    //플레이 가능한 전투원별 장착 장비
    public EquipmentSaveData[] equipment;
    //적 원본별 공개된 속성 목록
    public EnemyKnowledgeSaveData[] enemyKnowledge;

    //현재 보유·장착 상태와 적 상성 기록을 저장 데이터로 묶음
    public static GameSaveData Capture(string contentVersion, string saveLocation,
        IEnumerable<Anima> ownedAnimas, ItemInventory itemInventory,
        PartyEquipment partyEquipment, EnemyKnowledge enemyKnowledge)
    {
        #region 입력값 검사

        if (string.IsNullOrWhiteSpace(contentVersion))
            throw new ArgumentException("원본 데이터 버전은 비워 둘 수 없습니다.", nameof(contentVersion));
        if (string.IsNullOrWhiteSpace(saveLocation))
            throw new ArgumentException("저장 위치는 비워 둘 수 없습니다.", nameof(saveLocation));
        if (ownedAnimas == null)
            throw new ArgumentNullException(nameof(ownedAnimas), "보유 아니마 목록이 필요합니다.");
        if (itemInventory == null)
            throw new ArgumentNullException(nameof(itemInventory), "아이템 목록이 필요합니다.");
        if (partyEquipment == null)
            throw new ArgumentNullException(nameof(partyEquipment), "파티 장착 상태가 필요합니다.");
        if (enemyKnowledge == null)
            throw new ArgumentNullException(nameof(enemyKnowledge), "적 상성 기록이 필요합니다.");

        #endregion

        var saves = new List<AnimaSaveData>();
        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anima in ownedAnimas)
        {
            if (anima == null)
                throw new ArgumentException("보유 아니마 목록에 비어 있는 항목이 있습니다.", nameof(ownedAnimas));

            var save = AnimaSaveConverter.Capture(anima);
            if (!instanceIds.Add(save.instanceId))
                throw new ArgumentException("보유 아니마 개체 ID가 중복됩니다.", nameof(ownedAnimas));
            saves.Add(save);
        }

        var itemSaves = new List<ItemSaveData>(itemInventory.Counts.Count);
        foreach (var item in itemInventory.Counts)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
                throw new ArgumentException("아이템 ID는 비워 둘 수 없습니다.", nameof(itemInventory));
            if (item.Value < 1)
                throw new ArgumentException($"아이템 수량이 잘못됐습니다: {item.Key}", nameof(itemInventory));

            itemSaves.Add(new ItemSaveData
            {
                itemId = item.Key,
                count = item.Value
            });
        }
        itemSaves.Sort((left, right) => StringComparer.Ordinal.Compare(left.itemId, right.itemId));

        var equipmentSaves = new List<EquipmentSaveData>(partyEquipment.Sets.Count);
        foreach (KeyValuePair<string, EquipmentSet> pair in partyEquipment.Sets)
        {
            equipmentSaves.Add(new EquipmentSaveData
            {
                unitDataId = pair.Key,
                weaponId = pair.Value.WeaponId,
                armorId = pair.Value.ArmorId,
                shoesId = pair.Value.ShoesId,
                accessoryId = pair.Value.AccessoryId
            });
        }
        equipmentSaves.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.unitDataId, right.unitDataId));

        var knowledgeSaves = new List<EnemyKnowledgeSaveData>();
        foreach (string enemyDataId in enemyKnowledge.EnemyDataIds)
        {
            IReadOnlyList<DamageType> revealedTypes =
                enemyKnowledge.GetRevealedTypes(enemyDataId);
            if (revealedTypes.Count == 0) continue;

            var values = new DamageType[revealedTypes.Count];
            for (int i = 0; i < values.Length; i++) values[i] = revealedTypes[i];
            knowledgeSaves.Add(new EnemyKnowledgeSaveData
            {
                enemyDataId = enemyDataId,
                revealedDamageTypes = values
            });
        }
        knowledgeSaves.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.enemyDataId, right.enemyDataId));

        return new GameSaveData
        {
            saveVersion = JsonSaveStore.CurrentSaveVersion,
            contentVersion = contentVersion,
            saveLocation = saveLocation,
            ownedAnimas = saves.ToArray(),
            items = itemSaves.ToArray(),
            equipment = equipmentSaves.ToArray(),
            enemyKnowledge = knowledgeSaves.ToArray()
        };
    }
}
