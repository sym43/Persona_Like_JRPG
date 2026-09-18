using System;

/// <summary>
/// 한 번의 전투 행동에 필요한 피해, 명중, 치명타, 비용, 회복량을 계산함.
/// 전투원의 상태 변경과 행동 진행은 맡지 않음.
/// </summary>
public static class BattleAttackCalculator
{
    private static readonly double[] LevelMultipliers =
    {
        0.5d, 0.51d, 0.53d, 0.59d, 0.66d, 0.75d,
        0.84d, 0.91d, 0.97d, 0.99d, 1d, 1d, 1d,
        1d, 1.01d, 1.03d, 1.09d, 1.16d, 1.25d,
        1.34d, 1.41d, 1.47d, 1.49d, 1.5d
    };

    //양쪽 레벨 차이로 원작의 피해 보정을 찾음
    public static double GetLevelMultiplier(int attackerLevel, int targetLevel, bool bossBattle)
    {
        BattleDataChecks.CheckLevel(attackerLevel);
        BattleDataChecks.CheckLevel(targetLevel);
        int index = Math.Max(0, Math.Min(LevelMultipliers.Length - 1,
            attackerLevel - targetLevel + 13));
        double multiplier = LevelMultipliers[index];
        return bossBattle ? Math.Max(1d, multiplier) : multiplier;
    }

    //공격자와 대상의 능력치로 기본 피해를 계산함
    public static int CalculateBaseDamage(BattleUnit attacker, BattleUnit target,
        DamageType damageType, double power, int armor, double levelMultiplier,
        double affinityMultiplier, bool basicAttack)
    {
        if (attacker == null) throw new ArgumentNullException(nameof(attacker), "공격자가 필요합니다.");
        if (target == null) throw new ArgumentNullException(nameof(target), "대상이 필요합니다.");
        if (!Enum.IsDefined(typeof(DamageType), damageType))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 피해 속성입니다.");
        if (double.IsNaN(power) || double.IsInfinity(power) || power < 0d)
            throw new ArgumentOutOfRangeException(nameof(power), "위력은 0 이상의 유한한 값이어야 합니다.");
        if (armor < 0) throw new ArgumentOutOfRangeException(nameof(armor), "방어구 방어력은 0 이상이어야 합니다.");
        CheckMultiplier(levelMultiplier, nameof(levelMultiplier));
        CheckMultiplier(affinityMultiplier, nameof(affinityMultiplier));

        bool physical = IsPhysical(damageType);
        int offense = basicAttack || physical ? attacker.Stats.Strength : attacker.Stats.Magic;
        int endurance = target.Stats.Endurance;
        if (!attacker.IsEnemy)
            return (int)Math.Truncate(Math.Sqrt(power * 15d * offense / endurance)
                * 2d * levelMultiplier * affinityMultiplier);

        return (int)Math.Truncate((Math.Sqrt(power * 6d * offense /
            (8d * endurance + armor)) * 9d * levelMultiplier
            - (basicAttack ? 0d : 10d)) * affinityMultiplier);
    }

    //일반 상성의 피해 배율을 구함. 무효·반사·흡수는 별도 처리해야 함
    public static double GetAffinityMultiplier(ResistanceType resistance)
    {
        switch (resistance)
        {
            case ResistanceType.Normal: return 1d;
            case ResistanceType.Weak: return 1.25d;
            case ResistanceType.Resist: return 0.5d;
            case ResistanceType.Immune:
            case ResistanceType.Reflect:
            case ResistanceType.Drain:
                throw new InvalidOperationException("무효·반사·흡수는 피해 배율로 계산할 수 없습니다.");
            default:
                throw new ArgumentOutOfRangeException(nameof(resistance), "알 수 없는 상성입니다.");
        }
    }

    //보정과 피해 범위 난수를 적용해 최종 피해를 구함
    public static int CalculateDamage(int baseDamage, double combinedModifier, int rangePercent)
    {
        CheckMultiplier(combinedModifier, nameof(combinedModifier));
        CheckRange(rangePercent);
        double adjusted = Math.Truncate(Math.Max(1d,
            Math.Min(99999d, baseDamage * combinedModifier)));
        return (int)Math.Truncate(Math.Max(1d, adjusted * rangePercent / 100d));
    }

    //양쪽 민첩과 기술 명중률로 기본 명중률을 계산함
    public static int CalculateBaseHitChance(BattleUnit attacker, BattleUnit target,
        int skillAccuracy, int defenderShoeEvasion)
    {
        if (attacker == null) throw new ArgumentNullException(nameof(attacker), "공격자가 필요합니다.");
        if (target == null) throw new ArgumentNullException(nameof(target), "대상이 필요합니다.");
        if (skillAccuracy < 0 || skillAccuracy > 100)
            throw new ArgumentOutOfRangeException(nameof(skillAccuracy), "기술 명중률은 0 이상 100 이하여야 합니다.");
        if (defenderShoeEvasion < 0)
            throw new ArgumentOutOfRangeException(nameof(defenderShoeEvasion), "신발 회피 수치는 0 이상이어야 합니다.");

        double chance = (attacker.Stats.Agility + 200d) /
            (target.Stats.Agility + 200d) * skillAccuracy;
        if (attacker.IsEnemy)
            chance *= (attacker.Stats.Agility + 200d) /
                (defenderShoeEvasion / 2d + 200d);
        return (int)Math.Floor(chance);
    }

    //양쪽 운과 기술 치명타율로 기본 치명타율을 계산함
    public static int CalculateBaseCriticalChance(BattleUnit attacker, BattleUnit target,
        int skillCriticalRate)
    {
        if (attacker == null) throw new ArgumentNullException(nameof(attacker), "공격자가 필요합니다.");
        if (target == null) throw new ArgumentNullException(nameof(target), "대상이 필요합니다.");
        CheckCriticalRate(skillCriticalRate);
        if (skillCriticalRate == 100) return 100;

        double enemyMultiplier = attacker.IsEnemy ? 0.8d : 1d;
        return (int)Math.Truncate((attacker.Stats.Luck + 50d) /
            (target.Stats.Luck + 50d) * skillCriticalRate * enemyMultiplier);
    }

    //이번 공격에서 치명타 판정을 할 수 있는지 확인함
    public static bool CanCritical(BattleUnit target, DamageType damageType, int skillCriticalRate,
        bool hit, ResistanceType resistance, bool friendlyFire, bool targetImmuneToCritical)
    {
        if (target == null) throw new ArgumentNullException(nameof(target), "대상이 필요합니다.");
        if (!Enum.IsDefined(typeof(DamageType), damageType))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 피해 속성입니다.");
        if (!Enum.IsDefined(typeof(ResistanceType), resistance))
            throw new ArgumentOutOfRangeException(nameof(resistance), "알 수 없는 상성입니다.");
        CheckCriticalRate(skillCriticalRate);

        return IsPhysical(damageType) && skillCriticalRate > 0 && hit &&
            resistance != ResistanceType.Weak &&
            resistance != ResistanceType.Immune &&
            resistance != ResistanceType.Drain &&
            !friendlyFire && !targetImmuneToCritical && !target.IsGuarding;
    }

    //최대 HP 비율로 사용하는 기술의 HP 비용을 계산함
    public static int CalculateHpCost(BattleUnit user, int costPercent)
    {
        if (user == null) throw new ArgumentNullException(nameof(user), "기술 사용자가 필요합니다.");
        if (costPercent < 0 || costPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(costPercent), "HP 비용 비율은 0 이상 100 이하여야 합니다.");
        return Math.Max(1, (int)Math.Floor(user.MaxHp * (double)costPercent / 100d));
    }

    //비용 지불 후 HP가 1 이상 남는지 확인함
    public static bool CanPayHp(BattleUnit user, int hpCost)
    {
        if (user == null) throw new ArgumentNullException(nameof(user), "기술 사용자가 필요합니다.");
        if (hpCost < 0) throw new ArgumentOutOfRangeException(nameof(hpCost), "HP 비용은 0 이상이어야 합니다.");
        return user.Hp > hpCost;
    }

    //비용만큼 SP가 있는지 확인함
    public static bool CanPaySp(BattleUnit user, int spCost)
    {
        if (user == null) throw new ArgumentNullException(nameof(user), "기술 사용자가 필요합니다.");
        if (spCost < 0) throw new ArgumentOutOfRangeException(nameof(spCost), "SP 비용은 0 이상이어야 합니다.");
        return user.Sp >= spCost;
    }

    //기술 회복력과 마력 보너스로 HP 회복량을 계산함
    public static int CalculateHealing(BattleUnit healer, int healingPower,
        int magicBonus, bool divineGrace, int rangePercent)
    {
        if (healer == null) throw new ArgumentNullException(nameof(healer), "회복 기술 사용자가 필요합니다.");
        if (healingPower < 0) throw new ArgumentOutOfRangeException(nameof(healingPower), "회복력은 0 이상이어야 합니다.");
        if (magicBonus < 0) throw new ArgumentOutOfRangeException(nameof(magicBonus), "마력 보너스는 0 이상이어야 합니다.");
        CheckRange(rangePercent);

        double teamMultiplier = healer.IsEnemy ? 0.6d : 1d;
        double healed = Math.Truncate((healingPower + (double)magicBonus) * teamMultiplier);
        if (divineGrace) healed = Math.Truncate(healed * 1.5d);
        healed = Math.Max(1d, Math.Min(99999d, healed));
        return (int)Math.Truncate(Math.Max(1d, healed * rangePercent / 100d));
    }

    private static bool IsPhysical(DamageType type) =>
        type == DamageType.Slash || type == DamageType.Strike || type == DamageType.Pierce;

    private static void CheckMultiplier(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            throw new ArgumentOutOfRangeException(name, "계산 배율은 0보다 큰 유한한 값이어야 합니다.");
    }

    private static void CheckRange(int rangePercent)
    {
        if (rangePercent < 95 || rangePercent > 105)
            throw new ArgumentOutOfRangeException(nameof(rangePercent), "피해·회복 범위는 95 이상 105 이하여야 합니다.");
    }

    private static void CheckCriticalRate(int skillCriticalRate)
    {
        if (skillCriticalRate < 0 || skillCriticalRate > 100)
            throw new ArgumentOutOfRangeException(nameof(skillCriticalRate), "기술 치명타율은 0 이상 100 이하여야 합니다.");
    }
}
