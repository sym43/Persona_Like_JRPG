/// <summary>
/// 스킬이 순서대로 실행할 효과 종류. <br/>
/// 피해·회복·전투 효과 적용·효과 해제·적 분석을 구분함.
/// </summary>
public enum SkillEffectType
{
    Damage = 0,
    Heal = 1,
    ApplyEffect = 2,
    RemoveBuffs = 3,
    RemoveDebuffs = 4,
    CureMentalStates = 5,
    Analyze = 6
}
