using System;
using System.Collections.Generic;

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
    public string AnimaInstanceId { get; }
    //현재 아니마 데이터 ID
    public string AnimaDataId { get; }
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
    public BattleStats Stats { get; }
    //전투 시작 시 민첩
    public int StartAgility { get; }
    //현재 속성 저항 목록
    public ResistanceTable Resistances { get; }
    //현재 스킬 ID 목록
    public IReadOnlyList<string> SkillIds { get; }
    //현재 장비 ID 목록
    public IReadOnlyList<string> EquipmentIds { get; }
    //현재 적용된 효과 목록
    public IReadOnlyList<BattleEffectState> Effects { get; }

    private readonly List<BattleEffectState> effectStates = new List<BattleEffectState>();

    //전투원을 만듦
    public BattleUnit(string battleId, BattleUnitData unitData,
        string characterId, string animaInstanceId,
        string animaDataId, int turnTieOrder, int level,
        int maxHp, int maxSp, int hp, int sp, BattleStats stats,
        ResistanceTable resistances, IEnumerable<string> skillIds,
        IReadOnlyList<string> equipmentIds)
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
        Stats = stats;
        StartAgility = stats.Agility;
        Resistances = resistances;
        SkillIds = BattleDataChecks.CheckAndCopySkillIds(skillIds, isEnemy ? int.MaxValue : 8);
        EquipmentIds = BattleDataChecks.CheckAndCopyEquipmentIds(equipmentIds);
        Effects = effectStates.AsReadOnly();
        InitializeState(hp, sp);
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

    //효과를 추가함
    public void AddEffect(BattleEffectState effect)
    {
        #region 입력값 검사

        if (effect == null) throw new ArgumentNullException(nameof(effect), "전투 효과가 필요합니다.");

        #endregion

        effectStates.Add(effect);
    }

    //같은 ID의 효과를 모두 제거함
    public void RemoveEffects(string effectId)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(effectId);

        #endregion

        effectStates.RemoveAll(effect => string.Equals(effect.EffectId, effectId, StringComparison.Ordinal));
    }

    //모든 효과를 제거함
    public void ClearEffects()
    {
        effectStates.Clear();
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
