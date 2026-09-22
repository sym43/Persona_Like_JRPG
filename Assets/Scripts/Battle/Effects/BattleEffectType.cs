/// <summary>
/// 전투원에게 적용할 수 있는 효과 종류. <br/>
/// 능력치 강화·약화, 공격 준비, 방어막을 포함함. <br/>
/// 정신 상태는 도취·폭주·무기력·공황·매혹·전율·겁박으로 구분함.
/// </summary>
public enum BattleEffectType
{
    AttackUp = 0,
    AttackDown = 1,
    DefenseUp = 2,
    DefenseDown = 3,
    AccuracyEvasionUp = 4,
    AccuracyEvasionDown = 5,
    CriticalUp = 6,
    PhysicalCharge = 7,
    EmotionCharge = 8,
    PhysicalBarrier = 9,
    EmotionBarrier = 10,
    Intoxication = 11,
    Berserk = 12,
    Lethargy = 13,
    Panic = 14,
    Charm = 15,
    Thrill = 16,
    Intimidation = 17
}

/// <summary>
/// 전투 효과가 어떤 규칙으로 처리되는지 구분함. <br/>
/// 강화, 약화, 공격 준비, 방어막, 정신 상태로 나뉨.
/// </summary>
public enum BattleEffectCategory
{
    Buff = 0,
    Debuff = 1,
    Preparation = 2,
    Barrier = 3,
    MentalState = 4
}
