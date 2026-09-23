using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 전투 중인 아군 또는 적 개체. <br/>
/// 현재 HP, SP와 다운, 방어 등 전투 상태를 관리함.
/// </summary>
public sealed class BattleUnit
{
    //전투에서 개체를 구분하는 ID
    public string BattleId { get; }
    //전투원 기본 데이터
    public BattleUnitData Data { get; }
    //캐릭터 ID
    public string CharacterId { get; }
    //현재 아니마 개체 ID
    public string AnimaInstanceId { get; private set; }
    //현재 아니마 데이터 ID
    public string AnimaDataId { get; private set; }
    //속도가 같을 때 사용할 순서
    public int TurnTieOrder { get; }
    //전투 레벨
    public int Level { get; }
    //최대 HP
    public int MaxHp { get; }
    //최대 SP
    public int MaxSp { get; }
    //현재 HP
    public int Hp { get; private set; }
    //현재 SP
    public int Sp { get; private set; }
    //적 여부
    public bool IsEnemy => Data.Role == UnitRole.Enemy;
    //사망 여부
    public bool IsDead => Hp == 0;
    //다운 여부
    public bool IsDown { get; private set; }
    //방어 여부
    public bool IsGuarding { get; private set; }
    //전투 이탈 여부
    public bool HasLeftBattle { get; private set; }
    //전투 능력치
    public BattleStats Stats { get; private set; }
    //전투 시작 시 민첩
    public int StartAgility { get; }
    //현재 속성 저항 목록
    public ResistanceTable Resistances { get; private set; }
    //현재 정신 상태 저항 목록
    public MentalResistanceTable MentalResistance { get; }
    //현재 스킬 ID 목록
    public IReadOnlyList<string> SkillIds { get; private set; }
    //현재 장착한 무기, 방어구, 신발, 액세서리
    public EquipmentSet Equipment { get; }
    //장착 무기 또는 적 데이터에서 확정한 일반 공격 수치. 기본 공격이 없는 적은 비어 있음.
    public BasicAttackData BasicAttack { get; }
    //방어구 방어력
    public int Armor { get; }
    //신발 회피 수치
    public int ShoeEvasion { get; }
    //현재 적용된 효과 목록
    public IReadOnlyList<BattleUnitEffectState> Effects { get; }

    private readonly List<BattleUnitEffectState> effectStates = new List<BattleUnitEffectState>();
    private readonly EquipmentStatBonus equipmentStatBonus;
    private readonly IReadOnlyDictionary<DamageType, ResistanceType> equipmentResistanceChanges;

    //전투원을 만듦
    public BattleUnit(string battleId, BattleUnitData unitData,
        string characterId, string animaInstanceId,
        string animaDataId, int turnTieOrder, int level,
        int maxHp, int maxSp, int hp, int sp, BattleStats stats,
        ResistanceTable resistances, MentalResistanceTable mentalResistance,
        IEnumerable<string> skillIds,
        EquipmentSet equipment, BasicAttackData basicAttack,
        int armor, int shoeEvasion,
        EquipmentStatBonus statBonus = null,
        IReadOnlyDictionary<DamageType, ResistanceType> resistanceChanges = null)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(battleId);
        if (unitData == null) throw new ArgumentNullException(nameof(unitData), "전투원 원본 데이터가 필요합니다.");
        BattleDataChecks.CheckLevel(level);
        if (turnTieOrder < 0) throw new ArgumentOutOfRangeException(nameof(turnTieOrder), "속도 동률 순번은 0 이상이어야 합니다.");
        if (maxHp < 1) throw new ArgumentOutOfRangeException(nameof(maxHp), "최대 HP는 1 이상이어야 합니다.");
        if (maxSp < 0) throw new ArgumentOutOfRangeException(nameof(maxSp), "최대 SP는 0 이상이어야 합니다.");
        if (stats == null) throw new ArgumentNullException(nameof(stats), "전투 능력치가 필요합니다.");
        if (resistances == null) throw new ArgumentNullException(nameof(resistances), "저항 표가 필요합니다.");
        if (mentalResistance == null) throw new ArgumentNullException(nameof(mentalResistance), "상태 저항 표가 필요합니다.");
        if (equipment == null) throw new ArgumentNullException(nameof(equipment), "장착 상태가 필요합니다.");
        if (basicAttack == null && unitData.Role != UnitRole.Enemy)
            throw new ArgumentNullException(nameof(basicAttack), "아군의 일반 공격 수치가 필요합니다.");
        if (armor < 0) throw new ArgumentOutOfRangeException(nameof(armor), "방어구 방어력은 0 이상이어야 합니다.");
        if (shoeEvasion < 0) throw new ArgumentOutOfRangeException(nameof(shoeEvasion), "신발 회피는 0 이상이어야 합니다.");
        bool isEnemy = unitData.Role == UnitRole.Enemy;
        if (!isEnemy)
        {
            BattleDataChecks.CheckText(characterId);
            BattleDataChecks.CheckText(animaInstanceId);
            BattleDataChecks.CheckText(animaDataId);
        }
        if (unitData.Role == UnitRole.Companion &&
            !string.Equals(animaDataId, unitData.FixedAnimaId,
                StringComparison.Ordinal))
            throw new ArgumentException("동료에게 지정된 고정 아니마와 일치하지 않습니다.");

        #endregion

        BattleId = battleId;
        Data = unitData;
        CharacterId = characterId;
        AnimaInstanceId = animaInstanceId;
        AnimaDataId = animaDataId;
        TurnTieOrder = turnTieOrder;
        Level = level;
        MaxHp = maxHp;
        MaxSp = maxSp;
        equipmentStatBonus = statBonus ?? new EquipmentStatBonus(0, 0, 0, 0, 0);
        equipmentResistanceChanges = CopyResistanceChanges(resistanceChanges);
        Stats = equipmentStatBonus.Apply(stats);
        StartAgility = Stats.Agility;
        Resistances = ApplyResistanceChanges(resistances, equipmentResistanceChanges);
        MentalResistance = mentalResistance;
        SkillIds = BattleDataChecks.CheckAndCopySkillIds(skillIds, isEnemy ? int.MaxValue : 8);
        Equipment = equipment;
        BasicAttack = basicAttack;
        Armor = armor;
        ShoeEvasion = shoeEvasion;
        Effects = effectStates.AsReadOnly();
        InitializeState(hp, sp);
    }

    //주인공이 사용하는 아니마의 능력치·상성·스킬을 바꿈
    internal void ChangeAnima(Anima anima)
    {
        if (Data.Role != UnitRole.MainCharacter)
            throw new InvalidOperationException("주인공만 아니마를 교체할 수 있습니다.");
        if (anima == null)
            throw new ArgumentNullException(nameof(anima), "교체할 아니마가 필요합니다.");

        AnimaInstanceId = anima.InstanceId;
        AnimaDataId = anima.Data.Id;
        Stats = equipmentStatBonus.Apply(anima.Stats);
        Resistances = ApplyResistanceChanges(anima.Data.Resistances,
            equipmentResistanceChanges);
        SkillIds = BattleDataChecks.CheckAndCopySkillIds(anima.SkillIds, 8);
    }

    //계산된 피해를 현재 HP에 적용함
    public void TakeDamage(int damage)
    {
        #region 입력값 검사

        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage), "피해량은 0 이상이어야 합니다.");

        #endregion

        Hp = (int)Math.Max(0, (long)Hp - damage);
        if (Hp == 0)
        {
            IsDown = false;
            IsGuarding = false;
        }
    }

    //현재 HP를 회복함
    public void RecoverHp(int amount)
    {
        #region 입력값 검사

        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "회복량은 0 이상이어야 합니다.");

        #endregion

        Hp = (int)Math.Min(MaxHp, (long)Hp + amount);
    }

    //전투 불능 상태에서 지정한 HP로 부활함
    public void Revive(int hp)
    {
        if (!IsDead)
            throw new InvalidOperationException("전투 불능 상태가 아닙니다.");
        if (hp <= 0)
            throw new ArgumentOutOfRangeException(nameof(hp), "부활 HP는 1 이상이어야 합니다.");

        Hp = Math.Min(MaxHp, hp);
        IsDown = false;
        IsGuarding = false;
    }

    //SP를 사용함. 부족하면 false를 반환함
    public bool TryUseSp(int amount)
    {
        #region 입력값 검사

        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "SP 사용량은 0 이상이어야 합니다.");

        #endregion

        if (Sp < amount) return false;
        Sp -= amount;
        return true;
    }

    //HP를 사용함. 사용 후 1 미만이 되면 false를 반환함
    public bool TryUseHp(int amount)
    {
        #region 입력값 검사

        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "HP 사용량은 0 이상이어야 합니다.");

        #endregion

        if (Hp <= amount) return false;
        Hp -= amount;
        return true;
    }

    //현재 SP를 회복함
    public void RecoverSp(int amount)
    {
        #region 입력값 검사

        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "SP 회복량은 0 이상이어야 합니다.");

        #endregion

        Sp = (int)Math.Min(MaxSp, (long)Sp + amount);
    }

    //방어를 시작함
    public void StartGuard()
    {
        #region 상태값 검사

        if (IsDead) throw new InvalidOperationException("사망한 전투원은 방어할 수 없습니다.");
        if (IsDown) throw new InvalidOperationException("다운된 전투원은 방어할 수 없습니다.");
        if (HasLeftBattle) throw new InvalidOperationException("전투를 이탈한 전투원은 방어할 수 없습니다.");

        #endregion

        IsGuarding = true;
    }

    //방어를 끝냄
    public void EndGuard()
    {
        IsGuarding = false;
    }

    //다운 상태가 됨
    public void KnockDown()
    {
        #region 상태값 검사

        if (IsDead) throw new InvalidOperationException("사망한 전투원은 다운될 수 없습니다.");
        if (HasLeftBattle) throw new InvalidOperationException("전투를 이탈한 전투원은 다운될 수 없습니다.");

        #endregion

        IsDown = true;
        IsGuarding = false;
    }

    //다운 상태를 해제함
    public void RecoverFromDown()
    {
        IsDown = false;
    }

    //전투에서 이탈함
    public void LeaveBattle()
    {
        HasLeftBattle = true;
        IsGuarding = false;
    }

    //효과를 적용하고 신규 적용·갱신·상쇄·차단 결과를 반환함
    public BattleEffectResultType ApplyEffect(BattleEffectData data, string appliedByUnitId)
    {
        if (data == null) throw new ArgumentNullException(nameof(data), "전투 효과가 필요합니다.");

        foreach (BattleUnitEffectState state in effectStates)
        {
            if (!string.Equals(state.Effect.ConflictGroupId, data.ConflictGroupId, StringComparison.Ordinal))
                continue;
            if (state.Effect.Type == data.Type)
            {
                state.Refresh();
                return BattleEffectResultType.Refreshed;
            }
            if (data.Category == BattleEffectCategory.MentalState)
                return BattleEffectResultType.Blocked;
        }

        BattleEffectType? opposite = GetOppositeType(data.Type);
        if (opposite.HasValue)
        {
            int removed = effectStates.RemoveAll(state => state.Effect.Type == opposite.Value);
            if (removed > 0)
                return BattleEffectResultType.Canceled;
        }

        effectStates.Add(new BattleUnitEffectState(data, appliedByUnitId));
        return BattleEffectResultType.Applied;
    }

    //해당 종류 효과가 있는지 확인함
    public bool HasEffect(BattleEffectType type)
    {
        return effectStates.Exists(state => state.Effect.Type == type);
    }

    //해당 종류 효과 하나를 소비함
    public bool ConsumeEffect(BattleEffectType type)
    {
        int index = effectStates.FindIndex(state => state.Effect.Type == type);
        if (index < 0) return false;
        effectStates.RemoveAt(index);
        return true;
    }

    //해당 종류 효과의 첫 번째 수치를 배율로 반환함
    public double GetEffectMultiplier(BattleEffectType type)
    {
        BattleUnitEffectState state = effectStates.Find(item => item.Effect.Type == type);
        return state == null ? 1d : state.Effect.Value / 100d;
    }

    //같은 종류 효과 수치를 모두 더함
    public int GetEffectValue(BattleEffectType type)
    {
        int value = 0;
        foreach (BattleUnitEffectState state in effectStates)
        {
            if (state.Effect.Type == type)
                value += state.Effect.Value;
        }
        return value;
    }

    //능력치 강화 효과를 제거하고 제거된 ID를 반환함
    public IReadOnlyList<string> RemoveStatBuffs()
    {
        return RemoveEffects(state => state.Effect.Category == BattleEffectCategory.Buff);
    }

    //능력치 약화 효과를 제거하고 제거된 ID를 반환함
    public IReadOnlyList<string> RemoveStatDebuffs()
    {
        return RemoveEffects(state => state.Effect.Category == BattleEffectCategory.Debuff);
    }

    //정신 상태를 제거하고 제거된 ID를 반환함
    public IReadOnlyList<string> RemoveMentalStates()
    {
        return RemoveEffects(state => state.Effect.Category == BattleEffectCategory.MentalState);
    }

    //기본 행동이 끝날 때 시간제 효과의 남은 턴을 줄임
    public void AdvanceTimedEffects()
    {
        for (int i = effectStates.Count - 1; i >= 0; i--)
        {
            BattleUnitEffectState state = effectStates[i];
            if (state.Effect.Category != BattleEffectCategory.MentalState && state.ReduceDuration())
                effectStates.RemoveAt(i);
        }
    }

    //기본 행동 시작에 정신 상태의 자연 회복을 판정함
    public void AdvanceMentalStates(Random random)
    {
        if (random == null) throw new ArgumentNullException(nameof(random), "상태 회복 난수가 필요합니다.");

        for (int i = effectStates.Count - 1; i >= 0; i--)
        {
            BattleUnitEffectState state = effectStates[i];
            if (state.Effect.Category != BattleEffectCategory.MentalState) continue;

            int turns = state.PassMentalTurn();
            if (turns >= state.Effect.MaxTurns)
            {
                effectStates.RemoveAt(i);
                continue;
            }
            if (turns < state.Effect.MinTurns)
                continue;

            if (state.Effect.Type == BattleEffectType.Thrill ||
                state.Effect.Type == BattleEffectType.Intimidation)
                continue;

            int chance = BattleAttackCalculator.CalculateMentalRecoveryChance(this, state.Effect.Type);
            if (random.Next(100) < chance)
                effectStates.RemoveAt(i);
        }
    }

    //전투 종료 후 HP·SP를 제외한 전투 전용 상태를 정리함
    public void ClearBattleState()
    {
        effectStates.Clear();
        IsDown = false;
        IsGuarding = false;
        HasLeftBattle = false;
    }

    //조건에 맞는 효과를 제거함
    private IReadOnlyList<string> RemoveEffects(Predicate<BattleUnitEffectState> match)
    {
        var removedIds = new List<string>();
        for (int i = effectStates.Count - 1; i >= 0; i--)
        {
            if (!match(effectStates[i])) continue;
            removedIds.Add(effectStates[i].EffectId);
            effectStates.RemoveAt(i);
        }
        removedIds.Reverse();
        return removedIds.AsReadOnly();
    }

    //서로 상쇄되는 능력치 효과를 찾음
    private static BattleEffectType? GetOppositeType(BattleEffectType type)
    {
        switch (type)
        {
            case BattleEffectType.AttackUp: return BattleEffectType.AttackDown;
            case BattleEffectType.AttackDown: return BattleEffectType.AttackUp;
            case BattleEffectType.DefenseUp: return BattleEffectType.DefenseDown;
            case BattleEffectType.DefenseDown: return BattleEffectType.DefenseUp;
            case BattleEffectType.AccuracyEvasionUp: return BattleEffectType.AccuracyEvasionDown;
            case BattleEffectType.AccuracyEvasionDown: return BattleEffectType.AccuracyEvasionUp;
            default: return null;
        }
    }

    //장비의 속성 저항 변경값을 검사하고 복사함
    private static IReadOnlyDictionary<DamageType, ResistanceType> CopyResistanceChanges(
        IReadOnlyDictionary<DamageType, ResistanceType> source)
    {
        var copy = new Dictionary<DamageType, ResistanceType>();
        if (source != null)
        {
            foreach (KeyValuePair<DamageType, ResistanceType> entry in source)
            {
                if (!Enum.IsDefined(typeof(DamageType), entry.Key) ||
                    !Enum.IsDefined(typeof(ResistanceType), entry.Value))
                    throw new ArgumentException("장비 속성 저항 변경값이 잘못됐습니다.", nameof(source));
                copy.Add(entry.Key, entry.Value);
            }
        }
        return new ReadOnlyDictionary<DamageType, ResistanceType>(copy);
    }

    //아니마 상성에 장비의 속성 저항 변경을 적용함
    private static ResistanceTable ApplyResistanceChanges(ResistanceTable baseTable,
        IReadOnlyDictionary<DamageType, ResistanceType> changes)
    {
        var entries = new List<KeyValuePair<DamageType, ResistanceType>>();
        foreach (KeyValuePair<DamageType, ResistanceType> entry in baseTable.Entries)
        {
            ResistanceType value = changes.TryGetValue(entry.Key,
                out ResistanceType changed) ? changed : entry.Value;
            entries.Add(new KeyValuePair<DamageType, ResistanceType>(entry.Key, value));
        }
        return new ResistanceTable(entries);
    }

    //전투 시작 상태를 초기화함
    private void InitializeState(int hp, int sp)
    {
        #region 상태값 검사

        if (hp < 0 || hp > MaxHp) throw new ArgumentOutOfRangeException(nameof(hp), "현재 HP는 0 이상 최대 HP 이하여야 합니다.");
        if (sp < 0 || sp > MaxSp) throw new ArgumentOutOfRangeException(nameof(sp), "현재 SP는 0 이상 최대 SP 이하여야 합니다.");

        #endregion

        Hp = hp;
        Sp = sp;
        IsDown = false;
        IsGuarding = false;
        HasLeftBattle = false;
    }
}
