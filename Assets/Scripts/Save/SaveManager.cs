using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 게임 저장·로드의 외부 진입점. <br/>
/// 파일 처리, 저장 데이터 검사, 원본 연결과 런타임 복원을 내부에서 순서대로 호출함.
/// </summary>
public sealed class SaveManager
{
    private readonly JsonSaveStore saveStore;
    private readonly BattleDataLoader battleDataLoader;

    //기본 저장 폴더와 전투 데이터 로더를 사용함
    public SaveManager()
        : this(new JsonSaveStore(), new BattleDataLoader())
    {
    }

    //테스트나 별도 저장 폴더를 사용함
    public SaveManager(string folderPath)
        : this(new JsonSaveStore(folderPath), new BattleDataLoader())
    {
    }

    //저장소와 원본 데이터 로더를 주입함
    internal SaveManager(JsonSaveStore saveStore, BattleDataLoader battleDataLoader)
    {
        this.saveStore = saveStore ?? throw new ArgumentNullException(nameof(saveStore), "저장소가 필요합니다.");
        this.battleDataLoader = battleDataLoader ?? throw new ArgumentNullException(nameof(battleDataLoader), "전투 데이터 로더가 필요합니다.");
    }

    //보유·장착 상태와 적 상성 기록을 저장함
    public void Save(string slotId, string contentVersion, string saveLocation,
        IEnumerable<Anima> ownedAnimas, ItemInventory itemInventory,
        PartyEquipment partyEquipment, EnemyKnowledge enemyKnowledge)
    {
        var saveData = GameSaveData.Capture(contentVersion, saveLocation,
            ownedAnimas, itemInventory, partyEquipment, enemyKnowledge);
        saveStore.Save(slotId, saveData);
    }

    //저장 데이터를 읽어 반환함
    public GameSaveData Load(string slotId)
    {
        return saveStore.Load(slotId);
    }

    //저장 파일을 읽고 보유 아니마를 런타임 상태로 복원함
    public IReadOnlyList<Anima> LoadOwnedAnimas(string slotId)
    {
        var saveData = Load(slotId);
        var battleData = battleDataLoader.Load();
        var restored = new List<Anima>(saveData.ownedAnimas.Length);

        foreach (var savedAnima in saveData.ownedAnimas)
        {
            if (!battleData.Animas.TryGetValue(savedAnima.dataId, out var animaData))
                throw new FormatException($"세이브에 없는 아니마 원본 ID가 있습니다: {savedAnima.dataId}");

            foreach (var skillId in savedAnima.skillIds)
            {
                if (!battleData.Skills.ContainsKey(skillId))
                    throw new FormatException($"세이브에 없는 스킬 ID가 있습니다: {skillId}");
            }

            restored.Add(AnimaSaveConverter.Restore(savedAnima, animaData));
        }

        return new ReadOnlyCollection<Anima>(restored);
    }

    //저장 파일을 읽고 아이템 수량을 복원함
    public ItemInventory LoadItemInventory(string slotId)
    {
        var saveData = Load(slotId);
        var battleData = battleDataLoader.Load();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var savedItem in saveData.items)
        {
            if (!battleData.Items.ContainsKey(savedItem.itemId))
                throw new FormatException($"세이브에 없는 아이템 ID가 있습니다: {savedItem.itemId}");

            counts.Add(savedItem.itemId, savedItem.count);
        }

        return new ItemInventory(counts);
    }

    //저장 파일을 읽고 적 상성 기록을 복원함
    public EnemyKnowledge LoadEnemyKnowledge(string slotId)
    {
        var saveData = Load(slotId);
        var battleData = battleDataLoader.Load();
        var result = new EnemyKnowledge();

        foreach (EnemyKnowledgeSaveData savedKnowledge in saveData.enemyKnowledge)
        {
            if (!battleData.BattleUnits.TryGetValue(savedKnowledge.enemyDataId,
                    out BattleUnitData unitData) ||
                unitData.Role != UnitRole.Enemy)
                throw new FormatException($"세이브에 없는 적 원본 ID가 있습니다: {savedKnowledge.enemyDataId}");

            foreach (DamageType damageType in savedKnowledge.revealedDamageTypes)
                result.Reveal(savedKnowledge.enemyDataId, damageType);
        }
        return result;
    }

    //저장 파일을 읽고 플레이 가능한 전투원의 장착 상태를 복원함
    public PartyEquipment LoadPartyEquipment(string slotId)
    {
        GameSaveData saveData = Load(slotId);
        BattleDataSet battleData = battleDataLoader.Load();
        var result = new PartyEquipment();

        foreach (EquipmentSaveData saved in saveData.equipment)
        {
            if (!battleData.BattleUnits.TryGetValue(saved.unitDataId,
                    out BattleUnitData unit) || unit.Role == UnitRole.Enemy)
                throw new FormatException($"세이브에 없는 플레이 가능 전투원 ID가 있습니다: {saved.unitDataId}");

            var equipment = new EquipmentSet(saved.weaponId, saved.armorId,
                saved.shoesId, saved.accessoryId);
            try
            {
                EquipmentRules.Check(unit, equipment, battleData.Equipments,
                    battleData.UnitEquipGroups, true);
            }
            catch (InvalidOperationException exception)
            {
                throw new FormatException($"세이브의 장착 상태가 잘못됐습니다: {exception.Message}", exception);
            }
            result.Set(saved.unitDataId, equipment);
        }
        return result;
    }

    //슬롯 파일이 있는지 확인함
    public bool Exists(string slotId)
    {
        return saveStore.Exists(slotId);
    }
}
