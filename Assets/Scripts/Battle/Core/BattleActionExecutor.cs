using System;
using System.Collections.Generic;

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
    public BattleActionResult Execute(BattleAction action, BattleUnit actor,
        IReadOnlyList<BattleUnit> targets, SkillData skill = null,
        IReadOnlyList<SkillEffectData> effects = null)
    {
        if (action == null) throw new ArgumentNullException(nameof(action), "행동이 필요합니다.");
        if (actor == null) throw new ArgumentNullException(nameof(actor), "행동자가 필요합니다.");
        if (targets == null) throw new ArgumentNullException(nameof(targets), "대상 목록이 필요합니다.");
        if (!string.Equals(action.UnitId, actor.BattleId, StringComparison.Ordinal))
            throw new InvalidOperationException("현재 행동자와 명령의 전투원 ID가 다릅니다.");
        if (actor.IsDead || actor.HasLeftBattle)
            throw new InvalidOperationException("행동할 수 없는 전투원입니다.");

        switch (action.Type)
        {
            case BattleActionType.BasicAttack:
                return UseBasicAttack(actor, targets);
            case BattleActionType.Skill:
                return UseSkill(actor, targets, skill, effects);
            case BattleActionType.Guard:
                actor.StartGuard();
                return CreateResult(action.Type, actor.BattleId,
                    null, Array.Empty<BattleImpactResult>());
            case BattleActionType.AllOutAttack:
                throw new InvalidOperationException("총공격은 전투 세션의 총공격 흐름에서 실행해야 합니다.");
            default:
                throw new ArgumentOutOfRangeException(nameof(action), "지원하지 않는 전투 행동입니다.");
        }
    }

    //참여한 아군 수와 주인공 능력치로 총공격을 실행함
    public BattleActionResult ExecuteAllOutAttack(BattleUnit initiator,
        BattleUnit mainCharacter, IReadOnlyList<BattleUnit> targets,
        int participantCount)
    {
        if (initiator == null)
            throw new ArgumentNullException(nameof(initiator), "총공격을 시작한 전투원이 필요합니다.");
        if (mainCharacter == null)
            throw new ArgumentNullException(nameof(mainCharacter), "주인공이 필요합니다.");
        if (targets == null)
            throw new ArgumentNullException(nameof(targets), "총공격 대상 목록이 필요합니다.");
        if (initiator.IsEnemy || mainCharacter.IsEnemy)
            throw new InvalidOperationException("적은 총공격을 사용할 수 없습니다.");

        var impacts = new List<BattleImpactResult>();
        foreach (BattleUnit target in targets)
        {
            if (target.IsDead || target.HasLeftBattle)
                continue;

            double levelMultiplier = BattleAttackCalculator.GetLevelMultiplier(
                mainCharacter.Level, target.Level, bossBattle);
            int baseDamage = BattleAttackCalculator.CalculateAllOutAttackBaseDamage(
                mainCharacter, target, participantCount, levelMultiplier);
            int damage = BattleAttackCalculator.CalculateDamage(
                baseDamage, 1d, random.Next(95, 106));
            target.TakeDamage(damage);
            impacts.Add(CreateImpact(0, 1, target, target,
                true, false, null, null, damage, 0, false, false));
        }

        return CreateResult(BattleActionType.AllOutAttack,
            initiator.BattleId, null, impacts);
    }

    //일반 공격을 계산해 대상에게 적용함
    private BattleActionResult UseBasicAttack(BattleUnit actor,
        IReadOnlyList<BattleUnit> targets)
    {
        if (targets.Count != 1)
            throw new InvalidOperationException("일반 공격 대상은 한 명이어야 합니다.");
        BattleUnit target = targets[0];
        BasicAttackData attack = actor.BasicAttack;
        if (attack == null)
            throw new InvalidOperationException("이 전투원은 기본 공격을 사용할 수 없습니다.");

        var impacts = new List<BattleImpactResult>();
        ResolveAttack(actor, target, attack.DamageType, attack.Power,
            attack.Accuracy, 1, 3, true, 0, impacts);
        return CreateResult(BattleActionType.BasicAttack,
            actor.BattleId, null, impacts);
    }

    //스킬 비용을 지불하고 연결된 효과를 순서대로 적용함
    private BattleActionResult UseSkill(BattleUnit actor,
        IReadOnlyList<BattleUnit> targets, SkillData skill,
        IReadOnlyList<SkillEffectData> effects)
    {
        if (skill == null || effects == null || effects.Count == 0)
            throw new InvalidOperationException("실행할 스킬 효과가 없습니다.");
        PayCost(actor, skill);

        var impacts = new List<BattleImpactResult>();
        foreach (SkillEffectData effect in effects)
        {
            foreach (BattleUnit target in targets)
            {
                if (target.IsDead || target.HasLeftBattle)
                    continue;

                switch (effect.Type)
                {
                    case SkillEffectType.Damage:
                        ResolveAttack(actor, target, effect.DamageType.Value,
                            effect.Power, effect.Accuracy, effect.HitCount,
                            effect.CriticalRate, false, effect.Order, impacts);
                        break;
                    case SkillEffectType.Heal:
                        ResolveHealing(actor, target, effect, impacts);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(effect), "지원하지 않는 스킬 효과입니다.");
                }
            }
        }

        return CreateResult(BattleActionType.Skill,
            actor.BattleId, skill.Id, impacts);
    }

    //스킬의 HP 또는 SP 비용을 지불함
    private static void PayCost(BattleUnit actor, SkillData skill)
    {
        switch (skill.CostType)
        {
            case SkillCostType.None:
                return;
            case SkillCostType.Hp:
                int hpCost = BattleAttackCalculator.CalculateHpCost(actor, skill.Cost);
                if (!actor.TryUseHp(hpCost))
                    throw new InvalidOperationException("스킬을 사용할 HP가 부족합니다.");
                return;
            case SkillCostType.Sp:
                if (!actor.TryUseSp(skill.Cost))
                    throw new InvalidOperationException("스킬을 사용할 SP가 부족합니다.");
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(skill), "지원하지 않는 스킬 비용입니다.");
        }
    }

    //공격 효과의 명중, 상성, 반사와 각 타격 피해를 계산함
    private void ResolveAttack(BattleUnit actor, BattleUnit target,
        DamageType damageType, int power, int accuracy, int hitCount,
        int criticalRate, bool basicAttack, int effectOrder,
        ICollection<BattleImpactResult> impacts)
    {
        ResistanceType resistance = target.Resistances.Get(damageType);
        if (resistance == ResistanceType.Immune)
        {
            impacts.Add(CreateImpact(effectOrder, 1, target, target,
                true, false, resistance, resistance, 0, 0, false, false));
            return;
        }

        bool hit = accuracy == 100 || target.IsDown ||
            resistance == ResistanceType.Reflect || resistance == ResistanceType.Drain;
        if (!hit)
        {
            int baseChance = BattleAttackCalculator.CalculateBaseHitChance(
                actor, target, accuracy, target.ShoeEvasion);
            int chance = Math.Max(50, Math.Min(99, baseChance));
            hit = random.Next(100) < chance;
        }
        if (!hit)
        {
            impacts.Add(CreateImpact(effectOrder, 1, target, target,
                false, false, resistance, resistance, 0, 0, false, false));
            return;
        }

        //임시: 전투원별 치명타 면역 데이터가 추가되면 해당 값을 사용함.
        bool targetImmuneToCritical = false;
        bool critical = BattleAttackCalculator.CanCritical(target,
            damageType, criticalRate, true, resistance, false,
            targetImmuneToCritical) && random.Next(100) <
            BattleAttackCalculator.CalculateBaseCriticalChance(actor, target, criticalRate);

        bool reflected = resistance == ResistanceType.Reflect;
        bool drained = resistance == ResistanceType.Drain;
        ResistanceType appliedResistance = reflected
            ? actor.Resistances.Get(damageType) : resistance;
        BattleUnit receiver = reflected ? actor : target;

        if (reflected && (appliedResistance == ResistanceType.Immune ||
                          appliedResistance == ResistanceType.Reflect))
        {
            impacts.Add(CreateImpact(effectOrder, 1, target, actor,
                true, critical, resistance, appliedResistance,
                0, 0, false, false));
            return;
        }

        bool healing = drained || (reflected && appliedResistance == ResistanceType.Drain);
        bool effectiveCritical = critical && !healing &&
                                 appliedResistance != ResistanceType.Weak;
        double adjustedPower = basicAttack && !actor.IsEnemy ? power / 2d : power;
        double levelMultiplier = BattleAttackCalculator.GetLevelMultiplier(
            actor.Level, receiver.Level, bossBattle);
        int armor = actor.IsEnemy && receiver.IsEnemy ? 10 : receiver.Armor;
        int baseDamage = BattleAttackCalculator.CalculateBaseDamage(actor, receiver,
            damageType, adjustedPower, armor, levelMultiplier,
            healing ? 1d : BattleAttackCalculator.GetAffinityMultiplier(appliedResistance),
            basicAttack);
        bool guarded = !reflected && !drained && target.IsGuarding;
        double modifier = (effectiveCritical ? 1.5d : 1d) *
                          (guarded ? 0.4d : 1d);

        for (int hitNumber = 1; hitNumber <= hitCount; hitNumber++)
        {
            int damage = BattleAttackCalculator.CalculateDamage(
                baseDamage, modifier, random.Next(95, 106));
            int hpBefore = receiver.Hp;
            bool downed = false;

            if (healing)
            {
                receiver.RecoverHp(damage);
                impacts.Add(CreateImpact(effectOrder, hitNumber, target, receiver,
                    true, critical, resistance, appliedResistance, 0,
                    receiver.Hp - hpBefore, false, false));
                continue;
            }

            bool wasDown = receiver.IsDown;
            receiver.TakeDamage(damage);
            downed = !receiver.IsDead && !wasDown && !guarded &&
                     (appliedResistance == ResistanceType.Weak || effectiveCritical);
            if (downed)
                receiver.KnockDown();

            impacts.Add(CreateImpact(effectOrder, hitNumber, target, receiver,
                true, critical, resistance, appliedResistance, damage,
                0, downed, guarded));
            if (receiver.IsDead)
                break;
        }

        if (guarded && receiver == target && target.Hp < target.MaxHp)
            target.EndGuard();
    }

    //회복 효과를 대상에게 적용함
    private void ResolveHealing(BattleUnit actor, BattleUnit target,
        SkillEffectData effect, ICollection<BattleImpactResult> impacts)
    {
        int magicBonus = BattleAttackCalculator.GetHealingMagicBonus(actor.Stats.Magic);
        //임시: 패시브 스킬 적용 단계에서 Divine Grace 보유 여부를 연결함.
        bool divineGrace = false;
        int amount = BattleAttackCalculator.CalculateHealing(actor, effect.Power,
            magicBonus, divineGrace, random.Next(95, 106));
        int hpBefore = target.Hp;
        target.RecoverHp(amount);
        impacts.Add(CreateImpact(effect.Order, 1, target, target,
            true, false, null, null, 0, target.Hp - hpBefore, false, false));
    }

    //한 번의 적용 결과를 현재 대상 상태와 함께 만듦
    private static BattleImpactResult CreateImpact(int effectOrder, int hitNumber,
        BattleUnit target, BattleUnit receiver, bool hit, bool critical,
        ResistanceType? resistance, ResistanceType? appliedResistance,
        int damage, int healing, bool downed, bool guarded)
    {
        return new BattleImpactResult(effectOrder, hitNumber,
            target.BattleId, receiver.BattleId, hit, critical,
            resistance, appliedResistance, damage, healing,
            downed, guarded, receiver.Hp);
    }

    //행동 결과와 원모어를 받을 전투원을 함께 정리함
    private static BattleActionResult CreateResult(BattleActionType type,
        string unitId, string skillId, IReadOnlyList<BattleImpactResult> impacts)
    {
        return new BattleActionResult(type, unitId, skillId,
            FindOneMoreUnitId(unitId, impacts), impacts);
    }

    //약점 결과를 치명타보다 먼저 확인해 원모어 대상을 찾음
    private static string FindOneMoreUnitId(string unitId,
        IReadOnlyList<BattleImpactResult> impacts)
    {
        foreach (BattleImpactResult impact in impacts)
        {
            bool weakDownOrDefeat = impact.AppliedResistance == ResistanceType.Weak &&
                                    (impact.Downed || impact.HpAfter == 0);
            if (weakDownOrDefeat)
                return GetOneMoreUnitId(unitId, impact);
        }

        foreach (BattleImpactResult impact in impacts)
        {
            if (impact.Critical && impact.Downed)
                return GetOneMoreUnitId(unitId, impact);
        }

        return null;
    }

    //반사 공격이면 원래 대상, 그 외에는 공격자가 원모어를 받음
    private static string GetOneMoreUnitId(string unitId,
        BattleImpactResult impact)
    {
        return string.Equals(impact.AffectedUnitId, unitId,
            StringComparison.Ordinal)
            ? impact.TargetUnitId
            : unitId;
    }
}
