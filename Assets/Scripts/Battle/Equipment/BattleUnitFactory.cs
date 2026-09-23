using System;
using System.Collections.Generic;

/// <summary>
/// 전투원 원본, 아니마와 장비를 실제 전투원으로 조립함.
/// </summary>
public sealed class BattleUnitFactory
{
    private readonly BattleDataSet data;

    public BattleUnitFactory(BattleDataSet data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data), "전투 원본 데이터가 필요합니다.");
    }

    //플레이 가능한 전투원을 현재 아니마와 장비로 만듦
    public BattleUnit CreateAlly(string battleId, string unitDataId,
        string characterId, Anima anima, EquipmentSet equipment,
        int turnTieOrder, int level, int hp, int sp,
        MentalResistanceTable baseMentalResistance)
    {
        if (anima == null) throw new ArgumentNullException(nameof(anima), "현재 아니마가 필요합니다.");
        BattleUnitData unit = GetUnit(unitDataId);
        if (unit.Role == UnitRole.Enemy)
            throw new InvalidOperationException("적 원본으로 아군 전투원을 만들 수 없습니다.");

        return Create(battleId, unit, characterId, anima.InstanceId,
            anima.Data.Id, turnTieOrder, level, unit.BaseMaxHp, unit.BaseMaxSp,
            hp, sp, anima.Stats, anima.Data.Resistances,
            baseMentalResistance, anima.SkillIds, equipment, true);
    }

    //적 전투원을 현재 전투 수치와 장비로 만듦
    public BattleUnit CreateEnemy(string battleId, string unitDataId,
        EquipmentSet equipment, int turnTieOrder, int hp, int sp,
        BattleStats stats, ResistanceTable resistances,
        MentalResistanceTable baseMentalResistance,
        IEnumerable<string> skillIds, bool usesBasicAttack)
    {
        BattleUnitData unit = GetUnit(unitDataId);
        if (unit.Role != UnitRole.Enemy)
            throw new InvalidOperationException("아군 원본으로 적 전투원을 만들 수 없습니다.");

        return Create(battleId, unit, null, null, null, turnTieOrder,
            unit.BaseLevel, unit.BaseMaxHp, unit.BaseMaxSp, hp, sp,
            stats, resistances, baseMentalResistance, skillIds,
            equipment, usesBasicAttack);
    }

    //전투원의 한 부위 장비를 바꾸고 유효한 장착 상태인지 확인함
    public void Equip(PartyEquipment partyEquipment, string unitDataId,
        string equipmentId)
    {
        if (partyEquipment == null)
            throw new ArgumentNullException(nameof(partyEquipment), "파티 장착 상태가 필요합니다.");
        BattleUnitData unit = GetUnit(unitDataId);
        if (unit.Role == UnitRole.Enemy)
            throw new InvalidOperationException("적 장비는 전투 원본에서 구성해야 합니다.");
        if (!data.Equipments.TryGetValue(equipmentId, out EquipmentData equipment))
            throw new KeyNotFoundException($"없는 장비 ID입니다: {equipmentId}");

        EquipmentSet changed = partyEquipment.Get(unitDataId)
            .Change(equipment.Slot, equipment.Id);
        EquipmentRules.Check(unit, changed, data.Equipments,
            data.UnitEquipGroups, true);
        partyEquipment.Set(unitDataId, changed);
    }

    //무기를 제외한 한 부위 장비를 해제함
    public void Unequip(PartyEquipment partyEquipment, string unitDataId,
        EquipmentSlot slot)
    {
        if (slot == EquipmentSlot.Weapon)
            throw new InvalidOperationException("일반 공격을 사용하는 아군은 무기를 해제할 수 없습니다.");
        if (partyEquipment == null)
            throw new ArgumentNullException(nameof(partyEquipment), "파티 장착 상태가 필요합니다.");

        BattleUnitData unit = GetUnit(unitDataId);
        if (unit.Role == UnitRole.Enemy)
            throw new InvalidOperationException("적 장비는 전투 원본에서 구성해야 합니다.");
        EquipmentSet changed = partyEquipment.Get(unitDataId).Change(slot, null);
        EquipmentRules.Check(unit, changed, data.Equipments,
            data.UnitEquipGroups, true);
        partyEquipment.Set(unitDataId, changed);
    }

    //전투원의 기본 장착 상태를 가져옴
    public EquipmentSet GetDefaultEquipment(string unitDataId)
    {
        GetUnit(unitDataId);
        if (!data.DefaultEquipment.TryGetValue(unitDataId, out EquipmentSet equipment))
            throw new KeyNotFoundException($"전투원의 기본 장비가 없습니다: {unitDataId}");
        return equipment;
    }

    private BattleUnit Create(string battleId, BattleUnitData unit,
        string characterId, string animaInstanceId, string animaDataId,
        int turnTieOrder, int level, int maxHp, int maxSp, int hp, int sp,
        BattleStats stats, ResistanceTable resistances,
        MentalResistanceTable baseMentalResistance,
        IEnumerable<string> skillIds, EquipmentSet equipment,
        bool usesBasicAttack)
    {
        if (stats == null) throw new ArgumentNullException(nameof(stats), "전투 능력치가 필요합니다.");
        if (resistances == null) throw new ArgumentNullException(nameof(resistances), "속성 저항이 필요합니다.");
        if (baseMentalResistance == null)
            throw new ArgumentNullException(nameof(baseMentalResistance), "정신 상태 저항이 필요합니다.");

        EquipmentRules.Check(unit, equipment, data.Equipments,
            data.UnitEquipGroups, usesBasicAttack);
        EquipmentBuild build = BuildEquipment(equipment, baseMentalResistance);
        BasicAttackData basicAttack = usesBasicAttack
            ? new BasicAttackData(build.Weapon.AttackPower,
                build.Weapon.Accuracy, build.Weapon.DamageType.Value,
                build.BasicAttackEffects)
            : null;

        var result = new BattleUnit(battleId, unit, characterId,
            animaInstanceId, animaDataId, turnTieOrder, level,
            maxHp, maxSp, hp, sp, stats, resistances,
            build.MentalResistance, skillIds, equipment, basicAttack,
            build.Armor, build.ShoeEvasion, build.StatBonus,
            build.ResistanceChanges);

        foreach (string effectId in build.StartEffectIds)
            result.ApplyEffect(data.BattleEffects[effectId], result.BattleId);
        return result;
    }

    private EquipmentBuild BuildEquipment(EquipmentSet equipment,
        MentalResistanceTable baseMentalResistance)
    {
        EquipmentData weapon = null;
        int armor = 0;
        int shoeEvasion = 0;
        int strength = 0;
        int magic = 0;
        int endurance = 0;
        int agility = 0;
        int luck = 0;
        var resistanceChanges = new Dictionary<DamageType, ResistanceType>();
        var mentalChanges = new Dictionary<BattleEffectType, MentalResistanceType>();
        var attackEffects = new List<BattleEffectChance>();
        var startEffects = new List<string>();

        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            string equipmentId = equipment.GetId(slot);
            if (equipmentId == null) continue;
            EquipmentData item = data.Equipments[equipmentId];
            if (slot == EquipmentSlot.Weapon) weapon = item;
            if (slot == EquipmentSlot.Armor) armor = item.Defense;
            if (slot == EquipmentSlot.Shoes) shoeEvasion = item.Evasion;

            if (!data.EquipmentEffects.TryGetValue(equipmentId,
                    out IReadOnlyList<EquipmentEffectData> effects))
                continue;
            foreach (EquipmentEffectData effect in effects)
            {
                switch (effect.Type)
                {
                    case EquipmentEffectType.StatBonus:
                        AddStat(effect.StatType.Value, effect.Value,
                            ref strength, ref magic, ref endurance, ref agility, ref luck);
                        break;
                    case EquipmentEffectType.DamageResistance:
                        AddChange(resistanceChanges, effect.DamageType.Value,
                            effect.ResistanceType.Value, "속성 저항");
                        break;
                    case EquipmentEffectType.MentalResistance:
                        AddChange(mentalChanges, effect.MentalState.Value,
                            effect.MentalResistance.Value, "정신 상태 저항");
                        break;
                    case EquipmentEffectType.BasicAttackEffect:
                        attackEffects.Add(new BattleEffectChance(
                            effect.EffectId, effect.ApplyChance));
                        break;
                    case EquipmentEffectType.StartBattleEffect:
                        startEffects.Add(effect.EffectId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(effect), "알 수 없는 장비 효과입니다.");
                }
            }
        }

        var mentalEntries = new List<KeyValuePair<BattleEffectType, MentalResistanceType>>();
        for (BattleEffectType type = BattleEffectType.Intoxication;
             type <= BattleEffectType.Intimidation; type++)
        {
            MentalResistanceType value = mentalChanges.TryGetValue(type, out MentalResistanceType changed)
                ? changed
                : baseMentalResistance.Get(type);
            mentalEntries.Add(new KeyValuePair<BattleEffectType, MentalResistanceType>(type, value));
        }

        return new EquipmentBuild(weapon, armor, shoeEvasion,
            new EquipmentStatBonus(strength, magic, endurance, agility, luck),
            resistanceChanges, new MentalResistanceTable(mentalEntries),
            attackEffects, startEffects);
    }

    private BattleUnitData GetUnit(string unitDataId)
    {
        if (!data.BattleUnits.TryGetValue(unitDataId, out BattleUnitData unit))
            throw new KeyNotFoundException($"없는 전투원 ID입니다: {unitDataId}");
        return unit;
    }

    private static void AddStat(EquipmentStatType type, int value,
        ref int strength, ref int magic, ref int endurance,
        ref int agility, ref int luck)
    {
        switch (type)
        {
            case EquipmentStatType.Strength: strength += value; return;
            case EquipmentStatType.Magic: magic += value; return;
            case EquipmentStatType.Endurance: endurance += value; return;
            case EquipmentStatType.Agility: agility += value; return;
            case EquipmentStatType.Luck: luck += value; return;
            default: throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 장비 능력치입니다.");
        }
    }

    private static void AddChange<TKey, TValue>(IDictionary<TKey, TValue> values,
        TKey key, TValue value, string label)
    {
        if (values.TryGetValue(key, out TValue previous) &&
            !EqualityComparer<TValue>.Default.Equals(previous, value))
            throw new InvalidOperationException($"서로 다른 {label} 효과가 같은 대상에 겹쳤습니다: {key}");
        values[key] = value;
    }

    private sealed class EquipmentBuild
    {
        internal EquipmentData Weapon { get; }
        internal int Armor { get; }
        internal int ShoeEvasion { get; }
        internal EquipmentStatBonus StatBonus { get; }
        internal IReadOnlyDictionary<DamageType, ResistanceType> ResistanceChanges { get; }
        internal MentalResistanceTable MentalResistance { get; }
        internal IReadOnlyList<BattleEffectChance> BasicAttackEffects { get; }
        internal IReadOnlyList<string> StartEffectIds { get; }

        internal EquipmentBuild(EquipmentData weapon, int armor, int shoeEvasion,
            EquipmentStatBonus statBonus,
            IReadOnlyDictionary<DamageType, ResistanceType> resistanceChanges,
            MentalResistanceTable mentalResistance,
            IReadOnlyList<BattleEffectChance> basicAttackEffects,
            IReadOnlyList<string> startEffectIds)
        {
            Weapon = weapon;
            Armor = armor;
            ShoeEvasion = shoeEvasion;
            StatBonus = statBonus;
            ResistanceChanges = resistanceChanges;
            MentalResistance = mentalResistance;
            BasicAttackEffects = basicAttackEffects;
            StartEffectIds = startEffectIds;
        }
    }
}
