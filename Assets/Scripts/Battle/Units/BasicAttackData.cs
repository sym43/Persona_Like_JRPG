using System;

/// <summary>
/// 전투원의 일반 공격 수치. <br/>
/// 아군의 장착 무기 또는 적의 고유 공격에서 공격력·명중률·속성을 받음.
/// </summary>
public sealed class BasicAttackData
{
    //기본 공격력
    public int Power { get; }
    //기본 명중률
    public int Accuracy { get; }
    //기본 공격 속성
    public DamageType DamageType { get; }

    //일반 공격 수치를 만듦
    public BasicAttackData(int power, int accuracy, DamageType damageType)
    {
        if (power < 0)
            throw new ArgumentOutOfRangeException(nameof(power), "일반 공격력은 0 이상이어야 합니다.");
        if (accuracy < 0 || accuracy > 100)
            throw new ArgumentOutOfRangeException(nameof(accuracy), "일반 공격 명중률은 0~100이어야 합니다.");
        if (!Enum.IsDefined(typeof(DamageType), damageType))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 일반 공격 속성입니다.");

        Power = power;
        Accuracy = accuracy;
        DamageType = damageType;
    }
}
