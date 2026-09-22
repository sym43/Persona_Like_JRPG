using System;

/// <summary>
/// 여러 스킬이 공유하는 전투 효과 원본. <br/>
/// 중복 그룹, 수치와 지속 규칙을 담음.
/// </summary>
public sealed class BattleEffectData
{
    //효과 ID
    public string Id { get; }
    //효과 종류
    public BattleEffectType Type { get; }
    //효과 처리 분류
    public BattleEffectCategory Category { get; }
    //함께 적용될 수 없는 효과를 묶는 ID
    public string ConflictGroupId { get; }
    //배율은 백분율, 치명타는 퍼센트포인트로 쓰는 수치
    public int Value { get; }
    //상태가 유지될 최소 기본 행동 수
    public int MinTurns { get; }
    //상태가 유지될 최대 기본 행동 수. 0이면 소모형 효과
    public int MaxTurns { get; }

    //전투 효과 원본을 만듦
    public BattleEffectData(string id, BattleEffectType type, string conflictGroupId,
        int value, int minTurns, int maxTurns)
    {
        BattleDataChecks.CheckText(id);
        BattleDataChecks.CheckText(conflictGroupId);
        if (!Enum.IsDefined(typeof(BattleEffectType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 전투 효과입니다.");
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "효과 수치는 0 이상이어야 합니다.");
        if (minTurns < 0 || maxTurns < minTurns)
            throw new ArgumentOutOfRangeException(nameof(minTurns), "효과 지속시간이 잘못됐습니다.");

        Category = GetCategory(type);
        if ((Category == BattleEffectCategory.Buff || Category == BattleEffectCategory.Debuff) && maxTurns == 0)
            throw new ArgumentException("능력치 효과에는 지속시간이 필요합니다.");
        if (Category == BattleEffectCategory.MentalState && (minTurns == 0 || maxTurns == 0))
            throw new ArgumentException("정신 상태에는 지속시간이 필요합니다.");

        Id = id;
        Type = type;
        ConflictGroupId = conflictGroupId;
        Value = value;
        MinTurns = minTurns;
        MaxTurns = maxTurns;
    }

    //효과 종류를 처리 분류로 바꿈
    private static BattleEffectCategory GetCategory(BattleEffectType type)
    {
        switch (type)
        {
            case BattleEffectType.AttackUp:
            case BattleEffectType.DefenseUp:
            case BattleEffectType.AccuracyEvasionUp:
            case BattleEffectType.CriticalUp:
                return BattleEffectCategory.Buff;
            case BattleEffectType.AttackDown:
            case BattleEffectType.DefenseDown:
            case BattleEffectType.AccuracyEvasionDown:
                return BattleEffectCategory.Debuff;
            case BattleEffectType.PhysicalCharge:
            case BattleEffectType.EmotionCharge:
                return BattleEffectCategory.Preparation;
            case BattleEffectType.PhysicalBarrier:
            case BattleEffectType.EmotionBarrier:
                return BattleEffectCategory.Barrier;
            case BattleEffectType.Intoxication:
            case BattleEffectType.Berserk:
            case BattleEffectType.Lethargy:
            case BattleEffectType.Panic:
            case BattleEffectType.Charm:
            case BattleEffectType.Thrill:
            case BattleEffectType.Intimidation:
                return BattleEffectCategory.MentalState;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 전투 효과입니다.");
        }
    }
}
