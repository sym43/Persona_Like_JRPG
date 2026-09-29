using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 현재 전투 상태.
/// </summary>
public enum BattleState
{
    Ongoing = 0,
    Victory = 1,
    Defeat = 2
}

/// <summary>
/// 전투 한 판의 행동자, 실행 대기와 결과를 관리함.
/// </summary>
public sealed class BattleSession
{
    private readonly BattleTurnOrder turnOrder;
    private readonly Dictionary<string, BattleUnit> units;
    private readonly IReadOnlyDictionary<string, SkillData> skills;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects;
    private readonly IReadOnlyDictionary<string, BattleEffectData> battleEffects;
    private readonly IReadOnlyDictionary<string, ItemData> items;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> itemEffects;
    private readonly ItemInventory itemInventory;
    private readonly EnemyKnowledge enemyKnowledge;
    private readonly BattleUnit mainCharacter;
    private readonly IReadOnlyList<Anima> animas;
    private readonly BattleActionExecutor actionExecutor;
    private readonly Random random;

    private int orderIndex = -1;
    private BattleUnit currentUnit;
    private bool actionExecuted;
    private bool shiftUsed;
    private string pendingOneMoreUnitId;
    private bool allOutAttackAvailable;
    private BattleAction mentalAction;
    private bool animaChangeUsed;
    private bool animaChangePending;

    //현재 전투 순환 번호
    public int Round { get; private set; } = 1;
    //현재 행동 중인 전투원
    public BattleUnit CurrentUnit => currentUnit;
    //현재 행동이 적용되고 차례 종료를 기다리는지
    public bool ActionExecuted => actionExecuted;
    //현재 행동이 원모어로 받은 추가 행동인지
    public bool IsOneMoreTurn { get; private set; }
    //현재 행동 결과에서 총공격을 선택할 수 있는지
    public bool CanStartAllOutAttack => allOutAttackAvailable;
    //현재 전투 상태
    public BattleState State { get; private set; } = BattleState.Ongoing;
    //현재 주인공이 아니마를 교체할 수 있는지
    public bool CanChangeAnima => currentUnit == mainCharacter && !actionExecuted &&
        mentalAction == null && !animaChangeUsed && animas.Count > 1;

    //정신 상태로 이미 정해진 행동을 구함
    public bool TryGetMentalAction(out BattleAction action)
    {
        action = mentalAction;
        return action != null;
    }

    //스킬 없이 기본 행동만 가능한 전투를 만듦
    public BattleSession(BattleTurnOrder turnOrder, IEnumerable<BattleUnit> units,
        Random random, EnemyKnowledge enemyKnowledge, bool bossBattle = false)
        : this(turnOrder, units,
            new ReadOnlyDictionary<string, SkillData>(new Dictionary<string, SkillData>()),
            new ReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>>(
                new Dictionary<string, IReadOnlyList<SkillEffectData>>()),
            new ReadOnlyDictionary<string, BattleEffectData>(
                new Dictionary<string, BattleEffectData>()),
            Array.Empty<Anima>(), random, enemyKnowledge, bossBattle)
    {
    }

    //턴 순서, 전투원과 스킬 데이터를 연결함
    public BattleSession(BattleTurnOrder turnOrder, IEnumerable<BattleUnit> units,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, BattleEffectData> battleEffects,
        Random random, EnemyKnowledge enemyKnowledge,
        bool bossBattle = false)
        : this(turnOrder, units, skills, skillEffects, battleEffects,
            Array.Empty<Anima>(), random, enemyKnowledge, bossBattle)
    {
    }

    //턴 순서, 전투원, 스킬과 주인공의 지참 아니마를 연결함
    public BattleSession(BattleTurnOrder turnOrder, IEnumerable<BattleUnit> units,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, BattleEffectData> battleEffects,
        IEnumerable<Anima> animas, Random random,
        EnemyKnowledge enemyKnowledge, bool bossBattle = false)
        : this(turnOrder, units, skills, skillEffects, battleEffects,
            new ReadOnlyDictionary<string, ItemData>(
                new Dictionary<string, ItemData>()),
            new ReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>>(
                new Dictionary<string, IReadOnlyList<ItemEffectData>>()),
            new ItemInventory(), animas, random,
            enemyKnowledge, bossBattle)
    {
    }

    //전투에 사용할 모든 원본 데이터와 런타임 상태를 연결함
    public BattleSession(BattleTurnOrder turnOrder, IEnumerable<BattleUnit> units,
        IReadOnlyDictionary<string, SkillData> skills,
        IReadOnlyDictionary<string, IReadOnlyList<SkillEffectData>> skillEffects,
        IReadOnlyDictionary<string, BattleEffectData> battleEffects,
        IReadOnlyDictionary<string, ItemData> items,
        IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> itemEffects,
        ItemInventory itemInventory, IEnumerable<Anima> animas,
        Random random, EnemyKnowledge enemyKnowledge,
        bool bossBattle = false)
    {
        #region 입력값 검사

        if (turnOrder == null) throw new ArgumentNullException(nameof(turnOrder), "턴 순서가 필요합니다.");
        if (units == null) throw new ArgumentNullException(nameof(units), "전투원 목록이 필요합니다.");
        if (skills == null) throw new ArgumentNullException(nameof(skills), "스킬 목록이 필요합니다.");
        if (skillEffects == null) throw new ArgumentNullException(nameof(skillEffects), "스킬 효과 목록이 필요합니다.");
        if (battleEffects == null) throw new ArgumentNullException(nameof(battleEffects), "전투 효과 목록이 필요합니다.");
        if (items == null) throw new ArgumentNullException(nameof(items), "아이템 목록이 필요합니다.");
        if (itemEffects == null) throw new ArgumentNullException(nameof(itemEffects), "아이템 효과 목록이 필요합니다.");
        if (itemInventory == null) throw new ArgumentNullException(nameof(itemInventory), "아이템 보유 수량이 필요합니다.");
        if (animas == null) throw new ArgumentNullException(nameof(animas), "지참 아니마 목록이 필요합니다.");
        if (random == null) throw new ArgumentNullException(nameof(random), "행동 난수 생성기가 필요합니다.");
        if (enemyKnowledge == null) throw new ArgumentNullException(nameof(enemyKnowledge), "적 상성 기록이 필요합니다.");

        #endregion

        this.turnOrder = turnOrder;
        this.units = CopyUnits(units, out mainCharacter);
        this.skills = skills;
        this.skillEffects = skillEffects;
        this.battleEffects = battleEffects;
        this.items = items;
        this.itemEffects = itemEffects;
        this.itemInventory = itemInventory;
        this.enemyKnowledge = enemyKnowledge;
        this.animas = CopyAnimas(animas);
        this.random = random;
        actionExecutor = new BattleActionExecutor(random, bossBattle,
            battleEffects, this.enemyKnowledge);
        CheckOrderMembers();
        CheckAnimas();
        UpdateOutcome();
    }

    //주인공이 전투에 지참한 아니마를 구함
    public IReadOnlyList<Anima> GetBattleAnimas()
    {
        return animas;
    }

    //주인공의 현재 아니마를 바꿈. 같은 아니마면 변경하지 않음
    public bool ChangeAnima(string instanceId)
    {
        BattleDataChecks.CheckText(instanceId);
        if (currentUnit == mainCharacter && !actionExecuted &&
            mentalAction == null &&
            string.Equals(mainCharacter.AnimaInstanceId, instanceId,
                StringComparison.Ordinal))
            return false;
        if (!CanChangeAnima)
            throw new InvalidOperationException("현재 아니마를 교체할 수 없습니다.");

        foreach (Anima anima in animas)
        {
            if (!string.Equals(anima.InstanceId, instanceId, StringComparison.Ordinal))
                continue;

            mainCharacter.ChangeAnima(anima);
            animaChangePending = true;
            return true;
        }

        throw new InvalidOperationException("전투에 지참하지 않은 아니마입니다.");
    }

    //현재 차례의 행동을 실행하고 연출용 결과를 반환함
    public BattleActionResult Execute(BattleAction action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action), "전투 행동이 필요합니다.");
        if (currentUnit == null || State != BattleState.Ongoing)
            throw new InvalidOperationException("현재 실행할 수 있는 차례가 아닙니다.");
        if (actionExecuted)
            throw new InvalidOperationException("현재 행동은 이미 실행됐습니다.");
        if (!string.Equals(action.UnitId, currentUnit.BattleId, StringComparison.Ordinal))
            throw new InvalidOperationException("현재 행동자와 명령의 전투원 ID가 다릅니다.");
        if (action.Type == BattleActionType.AllOutAttack)
            throw new InvalidOperationException("총공격은 총공격 선택 흐름에서 실행해야 합니다.");
        if (mentalAction != null && !IsSameAction(action, mentalAction))
            throw new InvalidOperationException("정신 상태로 정해진 행동만 실행할 수 있습니다.");
        if (mentalAction == null && action.ReverseTargetSide)
            throw new InvalidOperationException("대상 진영 변경은 매혹 행동에만 사용할 수 있습니다.");

        SkillData skill = null;
        IReadOnlyList<SkillEffectData> selectedSkillEffects = null;
        ItemData item = null;
        IReadOnlyList<ItemEffectData> selectedItemEffects = null;
        int? itemCountAfter = null;

        if (action.Type == BattleActionType.Skill)
        {
            skill = GetOwnedActiveSkill(currentUnit, action.SkillId);
            selectedSkillEffects = skillEffects[skill.Id];
            CheckCost(currentUnit, skill);
        }
        else if (action.Type == BattleActionType.Item)
        {
            item = GetUsableItem(currentUnit, action.ItemId);
            selectedItemEffects = itemEffects[item.Id];
        }

        IReadOnlyList<BattleUnit> targets = ResolveTargets(
            action, currentUnit, skill, item);
        if (item != null)
        {
            if (!itemInventory.RemoveOne(item.Id))
                throw new InvalidOperationException("아이템 보유 수량이 부족합니다.");
            itemCountAfter = itemInventory.GetCount(item.Id);
        }

        BattleActionResult result = actionExecutor.Execute(action, currentUnit,
            targets, skill, selectedSkillEffects, item, selectedItemEffects,
            itemCountAfter);
        if (currentUnit == mainCharacter)
        {
            animaChangeUsed |= animaChangePending;
            animaChangePending = false;
        }
        pendingOneMoreUnitId = result.OneMoreUnitId;
        allOutAttackAvailable = CanOfferAllOutAttack(result);
        actionExecuted = true;
        mentalAction = null;
        return result;
    }

    //원모어를 넘길 수 있는 살아 있는 아군을 구함
    public IReadOnlyList<BattleUnit> GetShiftTargets()
    {
        if (currentUnit == null || !IsOneMoreTurn || actionExecuted ||
            shiftUsed || currentUnit.IsEnemy || currentUnit.HasMentalState)
            return Array.Empty<BattleUnit>();

        var result = new List<BattleUnit>();
        foreach (BattleTurnEntry entry in turnOrder.CurrentOrder)
        {
            BattleUnit unit = units[entry.UnitId];
            if (!unit.IsEnemy && !unit.IsDead && !unit.HasLeftBattle &&
                !unit.HasMentalState &&
                !string.Equals(unit.BattleId, currentUnit.BattleId,
                    StringComparison.Ordinal))
                result.Add(unit);
        }
        return result.AsReadOnly();
    }

    //현재 원모어 행동을 선택한 아군에게 넘김
    public BattleUnit ShiftOneMoreTo(string unitId)
    {
        BattleDataChecks.CheckText(unitId);
        foreach (BattleUnit unit in GetShiftTargets())
        {
            if (!string.Equals(unit.BattleId, unitId, StringComparison.Ordinal))
                continue;

            currentUnit = unit;
            shiftUsed = true;
            if (unit == mainCharacter)
            {
                animaChangeUsed = false;
                animaChangePending = false;
            }
            mentalAction = ChooseMentalAction(unit);
            return unit;
        }

        throw new InvalidOperationException("현재 시프트할 수 없는 전투원입니다.");
    }

    //총공격에 참여할 수 있는 살아 있는 아군을 구함
    public IReadOnlyList<BattleUnit> GetAllOutAttackParticipants()
    {
        if (currentUnit == null || currentUnit.IsEnemy)
            return Array.Empty<BattleUnit>();

        var result = new List<BattleUnit>();
        foreach (BattleTurnEntry entry in turnOrder.CurrentOrder)
        {
            BattleUnit unit = units[entry.UnitId];
            if (!unit.IsEnemy && !unit.IsDead && !unit.IsDown &&
                !unit.HasLeftBattle && !unit.HasMentalState)
                result.Add(unit);
        }
        return result.AsReadOnly();
    }

    //현재 총공격 선택을 거절하고 원모어를 유지함
    public void DeclineAllOutAttack()
    {
        if (!allOutAttackAvailable)
            throw new InvalidOperationException("현재 선택할 수 있는 총공격이 없습니다.");
        allOutAttackAvailable = false;
    }

    //현재 행동자가 시작한 총공격을 실행함
    public BattleActionResult ExecuteAllOutAttack()
    {
        if (!allOutAttackAvailable || currentUnit == null || currentUnit.IsEnemy)
            throw new InvalidOperationException("현재 총공격을 실행할 수 없습니다.");

        IReadOnlyList<BattleUnit> participants = GetAllOutAttackParticipants();
        IReadOnlyList<BattleUnit> targets = GetUnitsBySide(true);
        allOutAttackAvailable = false;
        pendingOneMoreUnitId = null;
        return actionExecutor.ExecuteAllOutAttack(currentUnit, mainCharacter,
            targets, participants.Count);
    }

    //실행한 행동을 마무리하고 현재 차례를 끝냄
    public BattleState FinishAction()
    {
        if (!actionExecuted)
            throw new InvalidOperationException("마무리할 행동이 없습니다.");
        if (allOutAttackAvailable)
            throw new InvalidOperationException("총공격 실행 여부를 먼저 선택해야 합니다.");
        return CompleteTurn();
    }

    //현재 행동자의 살아 있는 상대를 구함
    public IReadOnlyList<BattleUnit> GetOpponents()
    {
        return GetUnitsBySide(true);
    }

    //현재 행동자와 같은 편의 살아 있는 전투원을 구함
    public IReadOnlyList<BattleUnit> GetAllies()
    {
        return GetUnitsBySide(false);
    }

    //현재 행동자와 같은 편의 전투 불능 전투원을 구함
    public IReadOnlyList<BattleUnit> GetDeadAllies()
    {
        return GetUnitsBySide(false, deadOnly: true);
    }

    //현재 행동자가 자원을 낼 수 있는 액티브 스킬을 구함
    public IReadOnlyList<SkillData> GetUsableSkills()
    {
        if (currentUnit == null)
            throw new InvalidOperationException("현재 행동자가 없습니다.");

        var result = new List<SkillData>();
        foreach (string skillId in currentUnit.SkillIds)
        {
            if (!skills.TryGetValue(skillId, out SkillData skill) ||
                skill.UseType != SkillUseType.Active ||
                !skillEffects.ContainsKey(skillId))
                continue;
            if (CanPayCost(currentUnit, skill) && CanUseSkill(currentUnit, skill))
                result.Add(skill);
        }
        return result.AsReadOnly();
    }

    //전투원이 가진 액티브·패시브 스킬 원본을 보유 순서대로 구함
    public IReadOnlyList<SkillData> GetUnitSkills(BattleUnit unit)
    {
        if (unit == null || !units.TryGetValue(unit.BattleId,
                out BattleUnit registered) || registered != unit)
            throw new InvalidOperationException("현재 전투에 없는 전투원입니다.");

        var result = new List<SkillData>();
        foreach (string skillId in unit.SkillIds)
        {
            if (skills.TryGetValue(skillId, out SkillData skill))
                result.Add(skill);
        }
        return result.AsReadOnly();
    }

    //전투에 지참한 아니마의 스킬 원본을 보유 순서대로 구함
    public IReadOnlyList<SkillData> GetAnimaSkills(Anima anima)
    {
        bool registered = false;
        foreach (Anima battleAnima in animas)
        {
            if (!ReferenceEquals(battleAnima, anima)) continue;
            registered = true;
            break;
        }
        if (!registered)
            throw new InvalidOperationException("전투에 지참하지 않은 아니마입니다.");

        var result = new List<SkillData>();
        foreach (string skillId in anima.SkillIds)
        {
            if (skills.TryGetValue(skillId, out SkillData skill))
                result.Add(skill);
        }
        return result.AsReadOnly();
    }

    //스킬에 연결된 실행 효과를 구함
    internal IReadOnlyList<SkillEffectData> GetSkillEffects(string skillId)
    {
        BattleDataChecks.CheckText(skillId);
        if (!skillEffects.TryGetValue(skillId,
                out IReadOnlyList<SkillEffectData> effects))
            throw new InvalidOperationException("스킬에 실행 효과가 없습니다.");
        return effects;
    }

    //스킬의 첫 피해 효과 속성을 구함. 회복·보조 스킬은 속성이 없음.
    public DamageType? GetSkillDamageType(string skillId)
    {
        foreach (SkillEffectData effect in GetSkillEffects(skillId))
        {
            if (effect.Type == SkillEffectType.Damage)
                return effect.DamageType;
        }

        return null;
    }

    //아이템의 첫 피해 효과 속성을 구함. 회복·보조 아이템은 속성이 없음.
    public DamageType? GetItemDamageType(string itemId)
    {
        BattleDataChecks.CheckText(itemId);
        if (!itemEffects.TryGetValue(itemId,
                out IReadOnlyList<ItemEffectData> effects))
            throw new InvalidOperationException("아이템에 실행 효과가 없습니다.");

        foreach (ItemEffectData effect in effects)
        {
            if (effect.Type == ItemEffectType.Damage)
                return effect.DamageType;
        }

        return null;
    }

    //ID에 해당하는 전투 효과 원본을 구함
    internal BattleEffectData GetBattleEffect(string effectId)
    {
        BattleDataChecks.CheckText(effectId);
        if (!battleEffects.TryGetValue(effectId, out BattleEffectData effect))
            throw new InvalidOperationException("전투 효과 원본이 없습니다.");
        return effect;
    }

    //현재 행동자가 전투에서 사용할 수 있는 보유 아이템을 구함
    public IReadOnlyList<ItemData> GetUsableItems()
    {
        if (currentUnit == null)
            throw new InvalidOperationException("현재 행동자가 없습니다.");
        if (currentUnit.IsEnemy)
            return Array.Empty<ItemData>();

        var result = new List<ItemData>();
        foreach (ItemData item in items.Values)
        {
            if (CanUseInBattle(item) && itemEffects.ContainsKey(item.Id) &&
                itemInventory.CanUse(item.Id) && HasItemTarget(item, currentUnit))
                result.Add(item);
        }
        return result.AsReadOnly();
    }

    //현재 가진 아이템 수량을 구함
    public int GetItemCount(string itemId)
    {
        return itemInventory.GetCount(itemId);
    }

    //현재 전투에서 적의 공개된 상성을 구함. 미공개면 null을 반환함
    public ResistanceType? GetKnownResistance(string unitId, DamageType damageType)
    {
        BattleDataChecks.CheckText(unitId);
        if (!units.TryGetValue(unitId, out BattleUnit unit) || !unit.IsEnemy)
            throw new InvalidOperationException("상성을 확인할 수 없는 대상입니다.");
        if (!enemyKnowledge.IsRevealed(unit.Data.Id, damageType))
            return null;
        return unit.GetResistance(damageType);
    }

    //스킬 공격 속성에 해당하는 현재 적의 공개된 상성을 구함
    public ResistanceType? GetKnownSkillResistance(string unitId, string skillId)
    {
        DamageType? damageType = GetSkillDamageType(skillId);
        return damageType.HasValue
            ? GetKnownResistance(unitId, damageType.Value)
            : null;
    }

    //다음 행동자를 찾음. 죽거나 이탈한 전투원은 차례를 건너뜀.
    public bool TryStartNextTurn(out BattleUnit unit)
    {
        #region 상태값 검사

        if (currentUnit != null)
            throw new InvalidOperationException("현재 행동을 먼저 끝내야 합니다.");

        #endregion

        unit = null;
        UpdateOutcome();
        if (State != BattleState.Ongoing)
            return false;

        if (TryStartOneMore(out unit))
            return true;

        IsOneMoreTurn = false;

        int checkedEntries = 0;
        while (checkedEntries < turnOrder.CurrentOrder.Count)
        {
            MoveToNextEntry();
            BattleTurnEntry entry = turnOrder.CurrentOrder[orderIndex];
            BattleUnit nextUnitInOrder = units[entry.UnitId];
            checkedEntries++;

            if (nextUnitInOrder.IsDead || nextUnitInOrder.HasLeftBattle)
                continue;

            //기본 행동 시작에 방어와 정신 상태 경과를 처리함.
            nextUnitInOrder.EndGuard();
            nextUnitInOrder.ApplyTurnStartPassives();
            nextUnitInOrder.AdvanceMentalStates(random);
            if (!nextUnitInOrder.HasEffect(BattleEffectType.Thrill) &&
                !nextUnitInOrder.HasEffect(BattleEffectType.Intimidation))
                nextUnitInOrder.RecoverFromDown();
            currentUnit = nextUnitInOrder;
            if (nextUnitInOrder == mainCharacter)
            {
                animaChangeUsed = false;
                animaChangePending = false;
            }
            mentalAction = ChooseMentalAction(nextUnitInOrder);
            unit = nextUnitInOrder;
            return true;
        }

        UpdateOutcome();
        return false;
    }

    //대기 중인 원모어가 있으면 기존 턴 순서를 움직이지 않고 추가 행동을 시작함
    private bool TryStartOneMore(out BattleUnit unit)
    {
        unit = null;
        if (pendingOneMoreUnitId == null)
            return false;

        string unitId = pendingOneMoreUnitId;
        pendingOneMoreUnitId = null;
        BattleUnit oneMoreUnit = units[unitId];
        if (oneMoreUnit.IsDead || oneMoreUnit.IsDown || oneMoreUnit.HasLeftBattle)
            return false;

        currentUnit = oneMoreUnit;
        IsOneMoreTurn = true;
        shiftUsed = false;
        mentalAction = ChooseMentalAction(oneMoreUnit);
        unit = oneMoreUnit;
        return true;
    }

    //행동 종류와 대상 방식에 맞는 실제 대상을 구함
    private IReadOnlyList<BattleUnit> ResolveTargets(
        BattleAction action, BattleUnit actor, SkillData skill, ItemData item)
    {
        if (action.Type == BattleActionType.Guard ||
            action.Type == BattleActionType.Skip ||
            action.Type == BattleActionType.Escape ||
            action.Type == BattleActionType.DiscardMoney)
            return Array.Empty<BattleUnit>();
        if (action.Type == BattleActionType.BasicAttack)
            return GetLivingTarget(action.TargetId, actor, !action.ReverseTargetSide);

        BattleTargetType targetType = skill != null
            ? skill.TargetType
            : item?.TargetType ?? throw new InvalidOperationException("행동 대상 정보가 없습니다.");
        return ResolveTargets(action, actor, targetType);
    }

    //대상 종류에 맞는 실제 전투원을 구함
    private IReadOnlyList<BattleUnit> ResolveTargets(
        BattleAction action, BattleUnit actor, BattleTargetType targetType)
    {
        switch (targetType)
        {
            case BattleTargetType.OneEnemy:
                return GetLivingTarget(action.TargetId, actor, !action.ReverseTargetSide);
            case BattleTargetType.AllEnemies:
                CheckNoTargetId(action);
                return GetUnitsBySide(!action.ReverseTargetSide);
            case BattleTargetType.OneAlly:
                return GetLivingTarget(action.TargetId, actor, action.ReverseTargetSide);
            case BattleTargetType.AllAllies:
                CheckNoTargetId(action);
                return GetUnitsBySide(action.ReverseTargetSide);
            case BattleTargetType.Self:
                if (action.ReverseTargetSide)
                    throw new InvalidOperationException("자기 대상 행동은 대상 진영을 바꿀 수 없습니다.");
                CheckNoTargetId(action);
                return new[] { actor };
            case BattleTargetType.OneDeadAlly:
                if (action.ReverseTargetSide)
                    throw new InvalidOperationException("전투 불능 대상 행동은 대상 진영을 바꿀 수 없습니다.");
                return GetDeadTarget(action.TargetId, actor);
            default:
                throw new ArgumentOutOfRangeException(nameof(targetType), "지원하지 않는 대상 종류입니다.");
        }
    }

    //ID로 살아 있는 한 명의 적 또는 아군을 구함
    private IReadOnlyList<BattleUnit> GetLivingTarget(
        string targetId, BattleUnit actor, bool targetOpponent)
    {
        return GetTarget(targetId, actor, targetOpponent, deadOnly: false);
    }

    //ID로 전투 불능 상태인 아군 한 명을 구함
    private IReadOnlyList<BattleUnit> GetDeadTarget(string targetId, BattleUnit actor)
    {
        return GetTarget(targetId, actor, targetOpponent: false, deadOnly: true);
    }

    //ID와 생존 조건에 맞는 대상 한 명을 구함
    private IReadOnlyList<BattleUnit> GetTarget(string targetId,
        BattleUnit actor, bool targetOpponent, bool deadOnly)
    {
        BattleDataChecks.CheckText(targetId);
        if (!units.TryGetValue(targetId, out BattleUnit target) ||
            target.HasLeftBattle || target.IsDead != deadOnly)
            throw new InvalidOperationException("선택할 수 없는 대상입니다.");

        bool isOpponent = target.IsEnemy != actor.IsEnemy;
        if (isOpponent != targetOpponent)
            throw new InvalidOperationException("선택할 수 없는 대상입니다.");
        return new[] { target };
    }

    //전체 대상이나 자기 대상 행동에 불필요한 대상 ID가 없는지 확인함
    private static void CheckNoTargetId(BattleAction action)
    {
        if (action.TargetId != null)
            throw new InvalidOperationException("이 행동은 대상 ID를 직접 선택하지 않습니다.");
    }

    //현재 행동자 기준으로 같은 편 또는 상대편의 살아 있는 전투원을 구함
    private IReadOnlyList<BattleUnit> GetUnitsBySide(bool opponents)
    {
        return GetUnitsBySide(opponents, deadOnly: false);
    }

    //현재 행동자 기준으로 같은 편 또는 상대편을 생존 조건에 맞춰 구함
    private IReadOnlyList<BattleUnit> GetUnitsBySide(bool opponents, bool deadOnly)
    {
        if (currentUnit == null)
            throw new InvalidOperationException("현재 행동자가 없습니다.");

        var result = new List<BattleUnit>();
        foreach (BattleTurnEntry entry in turnOrder.CurrentOrder)
        {
            BattleUnit unit = units[entry.UnitId];
            bool otherSide = unit.IsEnemy != currentUnit.IsEnemy;
            if (otherSide == opponents && unit.IsDead == deadOnly && !unit.HasLeftBattle)
                result.Add(unit);
        }
        return result.AsReadOnly();
    }

    //현재 전투원이 가진 실행 가능한 스킬을 확인함
    private SkillData GetOwnedActiveSkill(BattleUnit actor, string skillId)
    {
        bool owned = false;
        foreach (string ownedSkillId in actor.SkillIds)
        {
            if (!string.Equals(ownedSkillId, skillId, StringComparison.Ordinal))
                continue;
            owned = true;
            break;
        }

        if (!owned || !skills.TryGetValue(skillId, out SkillData skill))
            throw new InvalidOperationException("현재 전투원이 가진 스킬이 아닙니다.");
        if (skill.UseType != SkillUseType.Active)
            throw new InvalidOperationException("패시브 스킬은 행동으로 사용할 수 없습니다.");
        if (!skillEffects.ContainsKey(skill.Id))
            throw new InvalidOperationException("스킬에 실행 효과가 없습니다.");
        if (!CanUseSkill(actor, skill))
            throw new InvalidOperationException("현재 분석 스킬을 사용할 수 없습니다.");
        return skill;
    }

    //분석 스킬은 첫 행동 순환이 끝난 뒤 동료만 사용할 수 있음
    private bool CanUseSkill(BattleUnit actor, SkillData skill)
    {
        if (!HasAnalyzeEffect(skill)) return true;
        return actor.Data.Role == UnitRole.Companion && Round > 1;
    }

    //적 분석 효과가 든 스킬인지 확인함
    private bool HasAnalyzeEffect(SkillData skill)
    {
        foreach (SkillEffectData effect in skillEffects[skill.Id])
        {
            if (effect.Type == SkillEffectType.Analyze) return true;
        }
        return false;
    }

    //스킬 비용을 지불할 수 있는지 확인함
    private static bool CanPayCost(BattleUnit actor, SkillData skill)
    {
        switch (skill.CostType)
        {
            case SkillCostType.None:
                return true;
            case SkillCostType.Hp:
                return BattleAttackCalculator.CanPayHp(actor, actor.GetSkillCost(skill));
            case SkillCostType.Sp:
                return BattleAttackCalculator.CanPaySp(actor, actor.GetSkillCost(skill));
            default:
                throw new ArgumentOutOfRangeException(nameof(skill), "지원하지 않는 스킬 비용입니다.");
        }
    }

    //스킬 비용이 부족하면 실행 전에 막음
    private static void CheckCost(BattleUnit actor, SkillData skill)
    {
        if (!CanPayCost(actor, skill))
            throw new InvalidOperationException("스킬 사용에 필요한 자원이 부족합니다.");
    }

    //전투에서 사용할 수 있는 보유 아이템을 확인함
    private ItemData GetUsableItem(BattleUnit actor, string itemId)
    {
        if (actor.IsEnemy)
            throw new InvalidOperationException("적은 파티 아이템을 사용할 수 없습니다.");
        if (!items.TryGetValue(itemId, out ItemData item) ||
            !CanUseInBattle(item) || !itemEffects.TryGetValue(item.Id,
                out IReadOnlyList<ItemEffectData> effects) || effects.Count == 0)
            throw new InvalidOperationException("전투에서 사용할 수 없는 아이템입니다.");
        if (!itemInventory.CanUse(item.Id))
            throw new InvalidOperationException("아이템 보유 수량이 부족합니다.");
        return item;
    }

    //아이템을 전투 중에 사용할 수 있는지 확인함
    private static bool CanUseInBattle(ItemData item)
    {
        return item.UseType == ItemUseType.BattleOnly ||
               item.UseType == ItemUseType.Both;
    }

    //아이템이 현재 고를 수 있는 대상을 가지는지 확인함
    private bool HasItemTarget(ItemData item, BattleUnit actor)
    {
        switch (item.TargetType)
        {
            case BattleTargetType.OneEnemy:
            case BattleTargetType.AllEnemies:
                return GetUnitsBySide(true).Count > 0;
            case BattleTargetType.OneAlly:
            case BattleTargetType.AllAllies:
            case BattleTargetType.Self:
                return !actor.IsDead && !actor.HasLeftBattle;
            case BattleTargetType.OneDeadAlly:
                return GetUnitsBySide(false, deadOnly: true).Count > 0;
            default:
                return false;
        }
    }

    //현재 정신 상태가 강제하는 행동을 한 번만 정함
    private BattleAction ChooseMentalAction(BattleUnit actor)
    {
        if (actor.HasEffect(BattleEffectType.Thrill) ||
            actor.HasEffect(BattleEffectType.Intimidation))
            return new BattleAction(actor.BattleId, BattleActionType.Skip);
        if (actor.HasEffect(BattleEffectType.Berserk))
            return ChooseBerserkAction(actor);
        if (actor.HasEffect(BattleEffectType.Intoxication))
            return ChooseIntoxicationAction(actor);
        if (actor.HasEffect(BattleEffectType.Panic))
            return ChoosePanicAction(actor);
        if (actor.HasEffect(BattleEffectType.Charm))
            return ChooseCharmAction(actor);
        return null;
    }

    //폭주는 무작위 상대에게 일반 공격함
    private BattleAction ChooseBerserkAction(BattleUnit actor)
    {
        IReadOnlyList<BattleUnit> targets = GetUnitsBySide(true);
        if (actor.BasicAttack == null || targets.Count == 0)
            return new BattleAction(actor.BattleId, BattleActionType.Skip);
        BattleUnit target = targets[random.Next(targets.Count)];
        return new BattleAction(actor.BattleId, BattleActionType.BasicAttack,
            target.BattleId);
    }

    //도취는 세 행동을 같은 확률로 선택함
    private BattleAction ChooseIntoxicationAction(BattleUnit actor)
    {
        switch (random.Next(3))
        {
            case 0:
                return ChooseRandomItemAction(actor);
            case 1:
                return new BattleAction(actor.BattleId, BattleActionType.DiscardMoney);
            default:
                return new BattleAction(actor.BattleId, BattleActionType.Skip);
        }
    }

    //도취로 사용할 수 있는 아이템과 대상을 무작위로 고름
    private BattleAction ChooseRandomItemAction(BattleUnit actor)
    {
        if (actor.IsEnemy)
            return new BattleAction(actor.BattleId, BattleActionType.Skip);

        var actions = new List<BattleAction>();
        foreach (ItemData item in GetUsableItems())
            AddItemActions(actor, item, actions);
        return actions.Count == 0
            ? new BattleAction(actor.BattleId, BattleActionType.Skip)
            : actions[random.Next(actions.Count)];
    }

    //아이템으로 고를 수 있는 대상별 행동을 추가함
    private void AddItemActions(BattleUnit actor, ItemData item,
        ICollection<BattleAction> actions)
    {
        if (item.TargetType == BattleTargetType.OneEnemy ||
            item.TargetType == BattleTargetType.OneAlly ||
            item.TargetType == BattleTargetType.OneDeadAlly)
        {
            bool opponents = item.TargetType == BattleTargetType.OneEnemy;
            bool deadOnly = item.TargetType == BattleTargetType.OneDeadAlly;
            foreach (BattleUnit target in GetUnitsBySide(opponents, deadOnly))
                actions.Add(new BattleAction(actor.BattleId,
                    BattleActionType.Item, target.BattleId, itemId: item.Id));
            return;
        }

        actions.Add(new BattleAction(actor.BattleId,
            BattleActionType.Item, itemId: item.Id));
    }

    //공황은 10% 확률로 도주하고 주인공은 도주하지 않음
    private BattleAction ChoosePanicAction(BattleUnit actor)
    {
        bool canEscape = actor.Data.Role != UnitRole.MainCharacter;
        BattleActionType actionType = canEscape && random.Next(100) < 10
            ? BattleActionType.Escape
            : BattleActionType.Skip;
        return new BattleAction(actor.BattleId, actionType);
    }

    //매혹은 가능한 아군 공격·적 회복·적 강화 중 행동 종류를 같은 확률로 고름
    private BattleAction ChooseCharmAction(BattleUnit actor)
    {
        var actionGroups = new List<List<BattleAction>>();
        List<BattleAction> attacks = GetCharmAttackActions(actor);
        List<BattleAction> heals = GetCharmSkillActions(actor, healing: true);
        List<BattleAction> buffs = GetCharmSkillActions(actor, healing: false);
        if (attacks.Count > 0) actionGroups.Add(attacks);
        if (heals.Count > 0) actionGroups.Add(heals);
        if (buffs.Count > 0) actionGroups.Add(buffs);
        if (actionGroups.Count == 0)
            return new BattleAction(actor.BattleId, BattleActionType.Skip);

        List<BattleAction> choices = actionGroups[random.Next(actionGroups.Count)];
        return choices[random.Next(choices.Count)];
    }

    //매혹된 전투원이 자기편에 사용할 일반 공격을 만듦
    private List<BattleAction> GetCharmAttackActions(BattleUnit actor)
    {
        var result = new List<BattleAction>();
        if (actor.BasicAttack == null) return result;

        IReadOnlyList<BattleUnit> allies = GetUnitsBySide(false);
        foreach (BattleUnit target in allies)
        {
            if (target == actor && allies.Count > 1) continue;
            result.Add(new BattleAction(actor.BattleId,
                BattleActionType.BasicAttack, target.BattleId,
                reverseTargetSide: true));
        }
        return result;
    }

    //매혹된 전투원이 상대편에 사용할 회복 또는 강화 스킬을 만듦
    private List<BattleAction> GetCharmSkillActions(BattleUnit actor, bool healing)
    {
        var result = new List<BattleAction>();
        IReadOnlyList<BattleUnit> opponents = GetUnitsBySide(true);
        foreach (SkillData skill in GetUsableSkills())
        {
            if (HasOnlyHealEffects(skill) != healing ||
                (!healing && !HasOnlyBuffEffects(skill)))
                continue;

            if (skill.TargetType == BattleTargetType.OneAlly)
            {
                foreach (BattleUnit target in opponents)
                    result.Add(new BattleAction(actor.BattleId,
                        BattleActionType.Skill, target.BattleId, skill.Id, true));
            }
            else if (skill.TargetType == BattleTargetType.AllAllies &&
                     opponents.Count > 0)
            {
                result.Add(new BattleAction(actor.BattleId,
                    BattleActionType.Skill, skillId: skill.Id,
                    reverseTargetSide: true));
            }
        }
        return result;
    }

    //회복만 실행하는 스킬인지 확인함
    private bool HasOnlyHealEffects(SkillData skill)
    {
        IReadOnlyList<SkillEffectData> effects = skillEffects[skill.Id];
        bool found = false;
        foreach (SkillEffectData effect in effects)
        {
            if (effect.Type != SkillEffectType.Heal) return false;
            found = true;
        }
        return found;
    }

    //능력치 강화만 적용하는 스킬인지 확인함
    private bool HasOnlyBuffEffects(SkillData skill)
    {
        IReadOnlyList<SkillEffectData> effects = skillEffects[skill.Id];
        bool found = false;
        foreach (SkillEffectData effect in effects)
        {
            if (effect.Type != SkillEffectType.ApplyEffect ||
                !battleEffects.TryGetValue(effect.EffectId, out BattleEffectData data) ||
                data.Category != BattleEffectCategory.Buff)
                return false;
            found = true;
        }
        return found;
    }

    //정해진 강제 행동과 제출된 행동이 같은지 확인함
    private static bool IsSameAction(BattleAction left, BattleAction right)
    {
        return left.Type == right.Type &&
               string.Equals(left.UnitId, right.UnitId, StringComparison.Ordinal) &&
               string.Equals(left.TargetId, right.TargetId, StringComparison.Ordinal) &&
               string.Equals(left.SkillId, right.SkillId, StringComparison.Ordinal) &&
               string.Equals(left.ItemId, right.ItemId, StringComparison.Ordinal) &&
               left.ReverseTargetSide == right.ReverseTargetSide;
    }

    //현재 행동을 끝내고 다음 차례를 받을 수 있게 함.
    private BattleState CompleteTurn()
    {
        #region 상태값 검사

        if (currentUnit == null)
            throw new InvalidOperationException("끝낼 행동이 없습니다.");

        #endregion

        if (!IsOneMoreTurn)
            currentUnit.AdvanceTimedEffects();

        currentUnit = null;
        actionExecuted = false;
        shiftUsed = false;
        IsOneMoreTurn = false;
        allOutAttackAvailable = false;
        mentalAction = null;
        UpdateOutcome();
        return State;
    }

    //이번 아군 행동으로 원모어를 얻고 살아 있는 모든 적이 다운됐는지 확인함
    private bool CanOfferAllOutAttack(BattleActionResult result)
    {
        if (currentUnit.IsEnemy ||
            !string.Equals(result.OneMoreUnitId, currentUnit.BattleId,
                StringComparison.Ordinal))
            return false;
        if (mainCharacter.IsDead || mainCharacter.IsDown ||
            mainCharacter.HasLeftBattle || mainCharacter.HasMentalState)
            return false;

        int participantCount = GetAllOutAttackParticipants().Count;
        if (participantCount < 2)
            return false;

        bool foundEnemy = false;
        foreach (BattleUnit unit in units.Values)
        {
            if (!unit.IsEnemy || unit.IsDead || unit.HasLeftBattle)
                continue;

            foundEnemy = true;
            if (!unit.IsDown)
                return false;
        }
        return foundEnemy;
    }

    //전투 종료 후 모든 전투원의 전투 전용 상태를 정리함
    public void ClearBattleState()
    {
        foreach (BattleUnit unit in units.Values)
            unit.ClearBattleState();
        pendingOneMoreUnitId = null;
        allOutAttackAvailable = false;
        mentalAction = null;
        animaChangeUsed = false;
        animaChangePending = false;
    }

    //지참 아니마를 입력 순서대로 복사하고 중복 ID를 막음
    private static IReadOnlyList<Anima> CopyAnimas(IEnumerable<Anima> source)
    {
        var result = new List<Anima>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Anima anima in source)
        {
            if (anima == null)
                throw new ArgumentException("지참 아니마 목록에 빈 항목이 있습니다.", nameof(source));
            if (!ids.Add(anima.InstanceId))
                throw new ArgumentException($"중복된 아니마 개체 ID가 있습니다: {anima.InstanceId}", nameof(source));
            result.Add(anima);
        }
        return result.AsReadOnly();
    }

    //지참 목록을 사용하면 현재 아니마가 목록에 포함됐는지 확인함
    private void CheckAnimas()
    {
        if (animas.Count == 0) return;
        foreach (Anima anima in animas)
        {
            if (string.Equals(anima.InstanceId, mainCharacter.AnimaInstanceId,
                    StringComparison.Ordinal))
                return;
        }
        throw new ArgumentException("주인공의 현재 아니마가 지참 목록에 없습니다.");
    }

    //전투 결과를 다시 계산함.
    private void UpdateOutcome()
    {
        if (mainCharacter.IsDead)
        {
            State = BattleState.Defeat;
            pendingOneMoreUnitId = null;
            allOutAttackAvailable = false;
            return;
        }

        foreach (BattleUnit unit in units.Values)
        {
            if (unit.IsEnemy && !unit.IsDead && !unit.HasLeftBattle)
            {
                State = BattleState.Ongoing;
                return;
            }
        }

        State = BattleState.Victory;
        pendingOneMoreUnitId = null;
        allOutAttackAvailable = false;
    }

    //순환 안에서 다음 순서 위치로 이동함.
    private void MoveToNextEntry()
    {
        orderIndex++;
        if (orderIndex < turnOrder.CurrentOrder.Count)
            return;

        turnOrder.CompleteRound();
        Round++;
        orderIndex = 0;
    }

    //전투원 목록을 복사하고 주인공을 찾음.
    private static Dictionary<string, BattleUnit> CopyUnits(
        IEnumerable<BattleUnit> source, out BattleUnit mainCharacter)
    {
        var copy = new Dictionary<string, BattleUnit>(StringComparer.Ordinal);
        mainCharacter = null;

        foreach (BattleUnit unit in source)
        {
            if (unit == null)
                throw new ArgumentException("전투원 목록에 빈 값이 있습니다.", nameof(source));
            if (!copy.TryAdd(unit.BattleId, unit))
                throw new ArgumentException("중복된 전투원 ID가 있습니다.", nameof(source));

            if (unit.Data.Role == UnitRole.MainCharacter)
            {
                if (mainCharacter != null)
                    throw new ArgumentException("주인공은 한 명만 참가할 수 있습니다.", nameof(source));
                mainCharacter = unit;
            }
        }

        if (copy.Count == 0)
            throw new ArgumentException("전투원이 필요합니다.", nameof(source));
        if (mainCharacter == null)
            throw new ArgumentException("주인공이 필요합니다.", nameof(source));

        return copy;
    }

    //턴 순서와 전투원 목록의 ID가 같은지 확인함.
    private void CheckOrderMembers()
    {
        var orderIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (BattleTurnEntry entry in turnOrder.CurrentOrder)
        {
            if (!orderIds.Add(entry.UnitId))
                throw new ArgumentException("턴 순서에 중복된 전투원 ID가 있습니다.", nameof(turnOrder));
            if (!units.ContainsKey(entry.UnitId))
                throw new ArgumentException("턴 순서에 없는 전투원이 있습니다.", nameof(turnOrder));
        }

        if (orderIds.Count != units.Count)
            throw new ArgumentException("전투원 목록과 턴 순서의 대상이 다릅니다.", nameof(turnOrder));
    }
}
