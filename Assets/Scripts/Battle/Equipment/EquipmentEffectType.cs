/// <summary>
/// 장비에 붙는 추가 효과 종류. <br/>
/// 능력치, 속성·정신 상태 저항, 일반 공격 부가 효과, 전투 시작 효과로 구분함.
/// </summary>
public enum EquipmentEffectType
{
    StatBonus = 0,
    DamageResistance = 1,
    MentalResistance = 2,
    BasicAttackEffect = 3,
    StartBattleEffect = 4
}
