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
    private readonly IReadOnlyDictionary<string, BattleEffectData> battleEffects;

    public BattleActionExecutor(Random random, bool bossBattle,
        IReadOnlyDictionary<string, BattleEffectData> battleEffects)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random), "행동 난수 생성기가 필요합니다.");
        this.bossBattle = bossBattle;
        this.battleEffects = battleEffects ?? throw new ArgumentNullException(nameof(battleEffects), "전투 효과 목록이 필요합니다.");
    }

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
                return CreateResult(action.Type, actor.BattleId, null,
                    Array.Empty<BattleImpactResult>(), Array.Empty<BattleEffectResult>());
            case BattleActionType.Escape:
                actor.LeaveBattle();
                return CreateResult(action.Type, actor.BattleId, null,
                    Array.Empty<BattleImpactResult>(), Array.Empty<BattleEffectResult>());
            case BattleActionType.Skip:
                return CreateResult(action.Type, actor.BattleId, null,
                    Array.Empty<BattleImpactResult>(), Array.Empty<BattleEffectResult>());
            //임시: 소지품·소지금 시스템을 연결하기 전에는 도취 행동 종류만 결과에 남김.
            case BattleActionType.UseRandomItem:
            case BattleActionType.DiscardMoney:
                return CreateResult(action.Type, actor.BattleId, null,
                    Array.Empty<BattleImpactResult>(), Array.Empty<BattleEffectResult>());
            case BattleActionType.AllOutAttack:
                throw new InvalidOperationException("총공격은 전투 세션의 총공격 흐름에서 실행해야 합니다.");
            default:
                throw new ArgumentOutOfRangeException(nameof(action), "지원하지 않는 전투 행동입니다.");
        }
    }

    public BattleActionResult ExecuteAllOutAttack(BattleUnit initiator,
        BattleUnit mainCharacter, IReadOnlyList<BattleUnit> targets,
        int participantCount)
    {
        if (initiator == null) throw new ArgumentNullException(nameof(initiator), "총공격을 시작한 전투원이 필요합니다.");
        if (mainCharacter == null) throw new ArgumentNullException(nameof(mainCharacter), "주인공이 필요합니다.");
        if (targets == null) throw new ArgumentNullException(nameof(targets), "총공격 대상 목록이 필요합니다.");
        if (initiator.IsEnemy || mainCharacter.IsEnemy)
            throw new InvalidOperationException("적은 총공격을 사용할 수 없습니다.");

        var impacts = new List<BattleImpactResult>();
        foreach (BattleUnit target in targets)
        {
            if (target.IsDead || target.HasLeftBattle) continue;
            double levelMultiplier = BattleAttackCalculator.GetLevelMultiplier(
                mainCharacter.Level, target.Level, bossBattle);
            int baseDamage = BattleAttackCalculator.CalculateAllOutAttackBaseDamage(
                mainCharacter, target, participantCount, levelMultiplier);
            int damage = BattleAttackCalculator.CalculateDamage(baseDamage, 1d, random.Next(95, 106));
            target.TakeDamage(damage);
            impacts.Add(CreateImpact(0, 1, target, target,
                true, false, null, null, damage, 0, false, false));
        }

        return CreateResult(BattleActionType.AllOutAttack, initiator.BattleId,
            null, impacts, Array.Empty<BattleEffectResult>());
    }

    private BattleActionResult UseBasicAttack(BattleUnit actor,
        IReadOnlyList<BattleUnit> targets)
    {
        if (targets.Count != 1)
            throw new InvalidOperationException("일반 공격 대상은 한 명이어야 합니다.");
        BasicAttackData attack = actor.BasicAttack;
        if (attack == null)
            throw new InvalidOperationException("이 전투원은 기본 공격을 사용할 수 없습니다.");

        var impacts = new List<BattleImpactResult>();
        double charge = TakeChargeMultiplier(actor, attack.DamageType);
        ResolveAttack(actor, targets[0], attack.DamageType, attack.Power,
            attack.Accuracy, 1, 3, true, 0, charge, impacts);
        return CreateResult(BattleActionType.BasicAttack, actor.BattleId,
            null, impacts, Array.Empty<BattleEffectResult>());
    }

    private BattleActionResult UseSkill(BattleUnit actor,
        IReadOnlyList<BattleUnit> targets, SkillData skill,
        IReadOnlyList<SkillEffectData> effects)
    {
        if (skill == null || effects == null || effects.Count == 0)
            throw new InvalidOperationException("실행할 스킬 효과가 없습니다.");
        PayCost(actor, skill);

        var impacts = new List<BattleImpactResult>();
        var effectResults = new List<BattleEffectResult>();
        foreach (SkillEffectData effect in effects)
        {
            double charge = effect.Type == SkillEffectType.Damage
                ? TakeChargeMultiplier(actor, effect.DamageType.Value)
                : 1d;

            foreach (BattleUnit target in targets)
            {
                if (target.IsDead || target.HasLeftBattle) continue;

                switch (effect.Type)
                {
                    case SkillEffectType.Damage:
                        ResolveAttack(actor, target, effect.DamageType.Value,
                            effect.Power, effect.Accuracy, effect.HitCount,
                            effect.CriticalRate, false, effect.Order, charge, impacts);
                        break;
                    case SkillEffectType.Heal:
                        ResolveHealing(actor, target, effect, impacts);
                        break;
                    case SkillEffectType.ApplyEffect:
                        ApplyEffect(actor, target, effect, impacts, effectResults);
                        break;
                    case SkillEffectType.RemoveBuffs:
                        AddRemovalResults(target, target.RemoveStatBuffs(), effectResults);
                        break;
                    case SkillEffectType.RemoveDebuffs:
                        AddRemovalResults(target, target.RemoveStatDebuffs(), effectResults);
                        break;
                    case SkillEffectType.CureMentalStates:
                        AddRemovalResults(target, target.RemoveMentalStates(), effectResults);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(effect), "지원하지 않는 스킬 효과입니다.");
                }
            }
        }

        return CreateResult(BattleActionType.Skill, actor.BattleId,
            skill.Id, impacts, effectResults);
    }

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

    private void ApplyEffect(BattleUnit actor, BattleUnit target,
        SkillEffectData skillEffect, IReadOnlyList<BattleImpactResult> impacts,
        ICollection<BattleEffectResult> effectResults)
    {
        BattleEffectData data = battleEffects[skillEffect.EffectId];
        BattleUnit effectTarget = target;
        if (IsAttackLinkedState(data.Type))
        {
            BattleImpactResult impact = FindLatestImpact(
                target.BattleId, skillEffect.Order, impacts);
            bool blockedByAttack = impact == null || !impact.Hit || impact.Damage == 0 ||
                WasDownedByImpact(target.BattleId, impact.EffectOrder, impacts);
            if (blockedByAttack)
            {
                effectResults.Add(new BattleEffectResult(target.BattleId,
                    data.Id, BattleEffectResultType.Blocked));
                return;
            }
            if (string.Equals(impact.AffectedUnitId, actor.BattleId, StringComparison.Ordinal))
                effectTarget = actor;
        }

        if (data.Category == BattleEffectCategory.MentalState)
        {
            bool blocked = effectTarget.IsGuarding ||
                effectTarget.MentalResistance.Get(data.Type) == MentalResistanceType.Immune;
            if (!blocked)
            {
                int chance = BattleAttackCalculator.CalculateMentalChance(
                    actor, effectTarget, skillEffect.ApplyChance, data.Type);
                blocked = random.Next(100) >= chance;
            }
            if (blocked)
            {
                effectResults.Add(new BattleEffectResult(effectTarget.BattleId,
                    data.Id, BattleEffectResultType.Blocked));
                return;
            }
        }

        BattleEffectResultType change = effectTarget.ApplyEffect(data, actor.BattleId);
        effectResults.Add(new BattleEffectResult(effectTarget.BattleId, data.Id, change));
    }

    private static bool IsAttackLinkedState(BattleEffectType type) =>
        type == BattleEffectType.Thrill || type == BattleEffectType.Intimidation;

    private static BattleImpactResult FindLatestImpact(string targetId, int beforeOrder,
        IReadOnlyList<BattleImpactResult> impacts)
    {
        for (int i = impacts.Count - 1; i >= 0; i--)
        {
            if (impacts[i].EffectOrder < beforeOrder &&
                string.Equals(impacts[i].TargetUnitId, targetId, StringComparison.Ordinal))
                return impacts[i];
        }
        return null;
    }

    private static bool WasDownedByImpact(string targetId, int effectOrder,
        IReadOnlyList<BattleImpactResult> impacts)
    {
        foreach (BattleImpactResult impact in impacts)
        {
            if (impact.EffectOrder == effectOrder && impact.Downed &&
                string.Equals(impact.TargetUnitId, targetId, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private void ResolveAttack(BattleUnit actor, BattleUnit target,
        DamageType damageType, int power, int accuracy, int hitCount,
        int criticalRate, bool basicAttack, int effectOrder,
        double chargeMultiplier, ICollection<BattleImpactResult> impacts)
    {
        ResistanceType resistance = target.Resistances.Get(damageType);
        BattleEffectType barrierType = BattleAttackCalculator.IsPhysical(damageType)
            ? BattleEffectType.PhysicalBarrier
            : BattleEffectType.EmotionBarrier;
        if (target.ConsumeEffect(barrierType))
            resistance = ResistanceType.Reflect;

        if (resistance == ResistanceType.Immune)
        {
            impacts.Add(CreateImpact(effectOrder, 1, target, target,
                true, false, resistance, resistance, 0, 0, false, false));
            return;
        }

        bool cannotEvade = target.IsDown || target.HasEffect(BattleEffectType.Lethargy) ||
            target.HasEffect(BattleEffectType.Thrill) ||
            target.HasEffect(BattleEffectType.Intimidation);
        bool hit = accuracy == 100 || cannotEvade ||
            resistance == ResistanceType.Reflect || resistance == ResistanceType.Drain;
        if (!hit)
        {
            int chance = BattleAttackCalculator.CalculateBaseHitChance(
                actor, target, accuracy, target.ShoeEvasion);
            chance = (int)Math.Truncate(chance * GetAccuracyMultiplier(actor) *
                GetEvasionMultiplier(target));
            chance = Math.Max(50, Math.Min(99, chance));
            hit = random.Next(100) < chance;
        }
        if (!hit)
        {
            impacts.Add(CreateImpact(effectOrder, 1, target, target,
                false, false, resistance, resistance, 0, 0, false, false));
            return;
        }

        int adjustedCriticalRate = Math.Min(100, criticalRate +
            actor.GetEffectValue(BattleEffectType.CriticalUp) + GetCriticalTakenBonus(target));
        bool targetImmuneToCritical = false;
        bool critical = BattleAttackCalculator.CanCritical(target,
            damageType, adjustedCriticalRate, true, resistance, false,
            targetImmuneToCritical) && random.Next(100) <
            BattleAttackCalculator.CalculateBaseCriticalChance(actor, target, adjustedCriticalRate);
        if (!critical)
        {
            int extraCriticalRate = GetExtraCriticalRate(target);
            critical = extraCriticalRate > 0 &&
                BattleAttackCalculator.CanCritical(target, damageType,
                    extraCriticalRate, true, resistance, false, targetImmuneToCritical) &&
                random.Next(100) < extraCriticalRate;
        }

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
        bool effectiveCritical = critical && !healing && appliedResistance != ResistanceType.Weak;
        double adjustedPower = basicAttack && !actor.IsEnemy ? power / 2d : power;
        double levelMultiplier = BattleAttackCalculator.GetLevelMultiplier(
            actor.Level, receiver.Level, bossBattle);
        int armor = actor.IsEnemy && receiver.IsEnemy ? 10 : receiver.Armor;
        int baseDamage = BattleAttackCalculator.CalculateBaseDamage(actor, receiver,
            damageType, adjustedPower, armor, levelMultiplier,
            healing ? 1d : BattleAttackCalculator.GetAffinityMultiplier(appliedResistance),
            basicAttack);
        bool guarded = !reflected && !drained && target.IsGuarding;
        double modifier = chargeMultiplier * GetAttackMultiplier(actor) *
            GetDefenseMultiplier(receiver) * GetMentalDamageMultiplier(actor, receiver) *
            (effectiveCritical ? 1.5d : 1d) * (guarded ? 0.4d : 1d);

        for (int hitNumber = 1; hitNumber <= hitCount; hitNumber++)
        {
            int damage = BattleAttackCalculator.CalculateDamage(
                baseDamage, modifier, random.Next(95, 106));
            int hpBefore = receiver.Hp;

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
            bool downed = !receiver.IsDead && !wasDown && !guarded &&
                (appliedResistance == ResistanceType.Weak || effectiveCritical);
            if (downed) receiver.KnockDown();

            impacts.Add(CreateImpact(effectOrder, hitNumber, target, receiver,
                true, critical, resistance, appliedResistance, damage,
                0, downed, guarded));
            if (receiver.IsDead) break;
        }

        if (guarded && receiver == target && target.Hp < target.MaxHp)
            target.EndGuard();
    }

    private void ResolveHealing(BattleUnit actor, BattleUnit target,
        SkillEffectData effect, ICollection<BattleImpactResult> impacts)
    {
        int magicBonus = BattleAttackCalculator.GetHealingMagicBonus(actor.Stats.Magic);
        //임시: 패시브 스킬 연결 전에는 회복 강화 효과를 적용하지 않음.
        bool hasHealingBoost = false;
        int amount = BattleAttackCalculator.CalculateHealing(actor, effect.Power,
            magicBonus, hasHealingBoost, random.Next(95, 106));
        int hpBefore = target.Hp;
        target.RecoverHp(amount);
        impacts.Add(CreateImpact(effect.Order, 1, target, target,
            true, false, null, null, 0, target.Hp - hpBefore, false, false));
    }

    private static double TakeChargeMultiplier(BattleUnit actor, DamageType damageType)
    {
        BattleEffectType chargeType = BattleAttackCalculator.IsPhysical(damageType)
            ? BattleEffectType.PhysicalCharge
            : BattleEffectType.EmotionCharge;
        double multiplier = actor.GetEffectMultiplier(chargeType);
        actor.ConsumeEffect(chargeType);
        return multiplier;
    }

    private static double GetAttackMultiplier(BattleUnit unit)
    {
        double up = unit.GetEffectMultiplier(BattleEffectType.AttackUp);
        return up != 1d ? up : unit.GetEffectMultiplier(BattleEffectType.AttackDown);
    }

    private static double GetDefenseMultiplier(BattleUnit unit)
    {
        double up = unit.GetEffectMultiplier(BattleEffectType.DefenseUp);
        return up != 1d ? up : unit.GetEffectMultiplier(BattleEffectType.DefenseDown);
    }

    private static double GetAccuracyMultiplier(BattleUnit unit)
    {
        double up = unit.GetEffectMultiplier(BattleEffectType.AccuracyEvasionUp);
        double value = up != 1d ? up : unit.GetEffectMultiplier(BattleEffectType.AccuracyEvasionDown);
        if (unit.HasEffect(BattleEffectType.Berserk)) value *= 0.5d;
        return value;
    }

    private static double GetEvasionMultiplier(BattleUnit unit)
    {
        double up = unit.GetEffectMultiplier(BattleEffectType.AccuracyEvasionUp);
        if (up != 1d) return 0.7d;
        return unit.HasEffect(BattleEffectType.AccuracyEvasionDown) ? 1.3d : 1d;
    }

    private static double GetMentalDamageMultiplier(BattleUnit actor, BattleUnit receiver)
    {
        double value = 1d;
        if (actor.HasEffect(BattleEffectType.Berserk)) value *= 2d;
        if (actor.HasEffect(BattleEffectType.Lethargy)) value *= 0.5d;
        if (receiver.HasEffect(BattleEffectType.Berserk)) value *= 2d;
        return value;
    }

    private static int GetCriticalTakenBonus(BattleUnit target)
    {
        if (target.HasEffect(BattleEffectType.Lethargy)) return 60;
        if (target.HasEffect(BattleEffectType.Intoxication) ||
            target.HasEffect(BattleEffectType.Panic)) return 20;
        return 0;
    }

    private static int GetExtraCriticalRate(BattleUnit target)
    {
        if (target.HasEffect(BattleEffectType.Intimidation)) return 100;
        if (target.HasEffect(BattleEffectType.Thrill)) return 35;
        return 0;
    }

    private static void AddRemovalResults(BattleUnit target,
        IReadOnlyList<string> removedIds, ICollection<BattleEffectResult> effectResults)
    {
        foreach (string effectId in removedIds)
            effectResults.Add(new BattleEffectResult(target.BattleId,
                effectId, BattleEffectResultType.Removed));
    }

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

    private static BattleActionResult CreateResult(BattleActionType type,
        string unitId, string skillId, IReadOnlyList<BattleImpactResult> impacts,
        IReadOnlyList<BattleEffectResult> effectResults)
    {
        return new BattleActionResult(type, unitId, skillId,
            FindOneMoreUnitId(unitId, impacts), impacts, effectResults);
    }

    private static string FindOneMoreUnitId(string unitId,
        IReadOnlyList<BattleImpactResult> impacts)
    {
        foreach (BattleImpactResult impact in impacts)
        {
            bool weakDownOrDefeat = impact.AppliedResistance == ResistanceType.Weak &&
                                    (impact.Downed || impact.HpAfter == 0);
            if (weakDownOrDefeat) return GetOneMoreUnitId(unitId, impact);
        }

        foreach (BattleImpactResult impact in impacts)
        {
            if (impact.Critical && impact.Downed)
                return GetOneMoreUnitId(unitId, impact);
        }
        return null;
    }

    private static string GetOneMoreUnitId(string unitId,
        BattleImpactResult impact)
    {
        return string.Equals(impact.AffectedUnitId, unitId, StringComparison.Ordinal)
            ? impact.TargetUnitId
            : unitId;
    }
}
