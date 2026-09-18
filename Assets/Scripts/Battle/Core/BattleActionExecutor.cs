using System;

/// <summary>
/// 선택된 전투 행동을 계산하고 전투원 상태에 적용함.
/// 턴 순서와 화면 연출은 관리하지 않음.
/// </summary>
internal sealed class BattleActionExecutor
{
    private readonly Random random;
    private readonly bool bossBattle;

    //행동 실행에 사용할 난수와 보스전 여부를 받음
    public BattleActionExecutor(Random random, bool bossBattle)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random), "행동 난수 생성기가 필요합니다.");
        this.bossBattle = bossBattle;
    }

    //선택된 행동을 한 번 실행함
    public BattleActionResult Execute(BattleAction action, BattleUnit actor, BattleUnit target)
    {
        if (action == null) throw new ArgumentNullException(nameof(action), "행동이 필요합니다.");
        if (actor == null) throw new ArgumentNullException(nameof(actor), "행동자가 필요합니다.");
        if (!string.Equals(action.UnitId, actor.BattleId, StringComparison.Ordinal))
            throw new InvalidOperationException("현재 행동자와 명령의 전투원 ID가 다릅니다.");
        if (actor.IsDead || actor.HasLeftBattle)
            throw new InvalidOperationException("행동할 수 없는 전투원입니다.");

        switch (action.Type)
        {
            case BattleActionType.BasicAttack:
                return UseBasicAttack(actor, target);
            case BattleActionType.Guard:
                actor.StartGuard();
                return new BattleActionResult(action.Type, actor.BattleId, null,
                    false, false, null, 0, false, false, actor.Hp);
            default:
                throw new ArgumentOutOfRangeException(nameof(action), "지원하지 않는 전투 행동입니다.");
        }
    }

    //일반 공격을 계산해 대상에게 적용함
    private BattleActionResult UseBasicAttack(BattleUnit actor, BattleUnit target)
    {
        if (target == null || target.IsDead || target.HasLeftBattle ||
            actor.IsEnemy == target.IsEnemy)
            throw new InvalidOperationException("공격할 수 없는 대상입니다.");

        BasicAttackData attack = actor.BasicAttack;
        if (attack == null)
            throw new InvalidOperationException("이 전투원은 기본 공격을 사용할 수 없습니다.");
        ResistanceType resistance = target.Resistances.Get(attack.DamageType);
        if (resistance == ResistanceType.Immune)
            return new BattleActionResult(BattleActionType.BasicAttack, actor.BattleId,
                target.BattleId, true, false, resistance, 0, false,
                target.IsGuarding, target.Hp);

        bool hit = attack.Accuracy == 100 || target.IsDown ||
            resistance == ResistanceType.Reflect || resistance == ResistanceType.Drain;
        if (!hit)
        {
            int baseChance = BattleAttackCalculator.CalculateBaseHitChance(
                actor, target, attack.Accuracy, target.ShoeEvasion);
            int chance = Math.Max(50, Math.Min(99, baseChance));
            hit = random.Next(100) < chance;
        }
        if (!hit)
            return new BattleActionResult(BattleActionType.BasicAttack, actor.BattleId,
                target.BattleId, false, false, resistance, 0, false,
                false, target.Hp);

        bool critical = BattleAttackCalculator.CanCritical(target,
            attack.DamageType, 3, true, resistance, false, false) &&
            random.Next(100) < BattleAttackCalculator.CalculateBaseCriticalChance(
                actor, target, 3);

        bool reflected = resistance == ResistanceType.Reflect;
        bool drained = resistance == ResistanceType.Drain;
        ResistanceType appliedResistance = reflected
            ? actor.Resistances.Get(attack.DamageType) : resistance;
        if (reflected && (appliedResistance == ResistanceType.Immune ||
                          appliedResistance == ResistanceType.Reflect))
            return new BattleActionResult(BattleActionType.BasicAttack, actor.BattleId,
                target.BattleId, true, false, resistance, 0, false,
                false, actor.Hp, actor.BattleId,
                appliedResistance: appliedResistance);

        bool healing = drained || (reflected && appliedResistance == ResistanceType.Drain);
        BattleUnit receiver = reflected ? actor : target;
        bool effectiveCritical = critical && !healing &&
                                 appliedResistance != ResistanceType.Weak;
        double power = actor.IsEnemy ? attack.Power : attack.Power / 2d;
        double levelMultiplier = BattleAttackCalculator.GetLevelMultiplier(
            actor.Level, receiver.Level, bossBattle);
        int armor = actor.IsEnemy && receiver.IsEnemy ? 10 : receiver.Armor;
        int baseDamage = BattleAttackCalculator.CalculateBaseDamage(actor, receiver,
            attack.DamageType, power, armor, levelMultiplier,
            healing ? 1d : BattleAttackCalculator.GetAffinityMultiplier(appliedResistance), true);
        bool guarded = !reflected && !drained && target.IsGuarding;
        double modifier = (effectiveCritical ? 1.5d : 1d) * (guarded ? 0.4d : 1d);
        int damage = BattleAttackCalculator.CalculateDamage(
            baseDamage, modifier, random.Next(95, 106));

        if (healing)
        {
            int hpBefore = receiver.Hp;
            receiver.RecoverHp(damage);
            return new BattleActionResult(BattleActionType.BasicAttack, actor.BattleId,
                target.BattleId, true, false, resistance, 0, false,
                guarded, receiver.Hp, receiver.BattleId,
                receiver.Hp - hpBefore, appliedResistance: appliedResistance);
        }

        if (reflected)
        {
            bool wasDownActor = actor.IsDown;
            actor.TakeDamage(damage);
            bool downedActor = !actor.IsDead && !wasDownActor &&
                (appliedResistance == ResistanceType.Weak || effectiveCritical);
            if (downedActor)
                actor.KnockDown();
            return new BattleActionResult(BattleActionType.BasicAttack, actor.BattleId,
                target.BattleId, true, critical, resistance, damage, false,
                guarded, actor.Hp, actor.BattleId, downedActor: downedActor,
                appliedResistance: appliedResistance);
        }

        bool wasDown = target.IsDown;
        target.TakeDamage(damage);
        bool downedTarget = !target.IsDead && !wasDown && !guarded &&
            (resistance == ResistanceType.Weak || effectiveCritical);
        if (downedTarget)
            target.KnockDown();
        if (guarded)
            target.EndGuard();

        return new BattleActionResult(BattleActionType.BasicAttack, actor.BattleId,
            target.BattleId, true, critical, resistance, damage, downedTarget,
            guarded, target.Hp);
    }
}
