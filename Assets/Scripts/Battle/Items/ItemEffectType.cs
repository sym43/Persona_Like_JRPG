/// <summary>
/// 아이템을 사용했을 때 실행할 효과.<br/>
/// Damage(피해), RecoverHp(HP 회복), RecoverSp(SP 회복), Revive(부활),<br/>
/// ApplyEffect(전투 효과 부여), RemoveBuffs(강화 해제), RemoveDebuffs(약화 해제),
/// CureMentalStates(정신 상태 회복).
/// </summary>
public enum ItemEffectType
{
    Damage = 0,
    RecoverHp = 1,
    RecoverSp = 2,
    Revive = 3,
    ApplyEffect = 4,
    RemoveBuffs = 5,
    RemoveDebuffs = 6,
    CureMentalStates = 7
}
