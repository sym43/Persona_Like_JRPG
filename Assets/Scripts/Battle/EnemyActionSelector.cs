using System;
using System.Collections.Generic;

/// <summary>
/// 적이 가진 스킬과 현재 전투 상황을 보고 행동과 대상을 고름.
/// </summary>
public sealed class EnemyActionSelector
{
    private const int AttackBaseScore = 5;
    private const int ExtraTargetPercent = 40;

    private sealed class ActionChoice
    {
        public BattleAction Action { get; }
        public int Weight { get; }

        public ActionChoice(BattleAction action, int weight)
        {
            Action = action;
            Weight = Math.Max(1, weight);
        }
    }

    private readonly Random random;

    //행동 선택에 사용할 난수를 받음
    public EnemyActionSelector(Random random)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random), "행동 선택 난수가 필요합니다.");
    }

    //현재 적이 사용할 행동과 대상을 고름
    public BattleAction ChooseAction(BattleSession session)
    {
        if (session == null || session.CurrentUnit == null || !session.CurrentUnit.IsEnemy)
            throw new InvalidOperationException("이 적의 행동을 선택할 수 없습니다.");

        BattleUnit actor = session.CurrentUnit;
        IReadOnlyList<BattleUnit> opponents = session.GetOpponents();
        IReadOnlyList<BattleUnit> allies = session.GetAllies();
        var choices = new List<ActionChoice>();

        if (actor.BasicAttack != null)
        {
            foreach (BattleUnit target in opponents)
            {
                var action = new BattleAction(actor.BattleId,
                    BattleActionType.BasicAttack, target.BattleId);
                int score = GetAttackScore(target,
                    actor.BasicAttack.DamageType, actor.BasicAttack.Power,
                    actor.BasicAttack.Accuracy, 1);
                foreach (BattleEffectChance effect in actor.BasicAttack.Effects)
                {
                    score += GetBattleEffectScore(
                        session.GetBattleEffect(effect.EffectId), target,
                        effect.ApplyChance);
                }
                choices.Add(new ActionChoice(action, score));
            }
        }

        foreach (SkillData skill in session.GetUsableSkills())
            AddSkillChoices(session, actor, skill, opponents, allies, choices);

        if (choices.Count == 0)
            throw new InvalidOperationException("현재 적에게 사용할 수 있는 행동이 없습니다.");

        int totalWeight = 0;
        foreach (ActionChoice choice in choices)
            totalWeight += choice.Weight;

        int selected = random.Next(totalWeight);
        foreach (ActionChoice choice in choices)
        {
            selected -= choice.Weight;
            if (selected < 0)
                return choice.Action;
        }

        return choices[choices.Count - 1].Action;
    }

    //스킬 대상 방식에 맞는 선택지를 추가함
    private static void AddSkillChoices(BattleSession session,
        BattleUnit actor, SkillData skill,
        IReadOnlyList<BattleUnit> opponents, IReadOnlyList<BattleUnit> allies,
        ICollection<ActionChoice> choices)
    {
        switch (skill.TargetType)
        {
            case BattleTargetType.OneEnemy:
                foreach (BattleUnit target in opponents)
                    AddSkillChoice(session, actor, skill,
                        new[] { target }, target.BattleId, choices);
                return;
            case BattleTargetType.AllEnemies:
                if (opponents.Count > 0)
                    AddSkillChoice(session, actor, skill,
                        opponents, null, choices);
                return;
            case BattleTargetType.OneAlly:
                foreach (BattleUnit target in allies)
                    AddSkillChoice(session, actor, skill,
                        new[] { target }, target.BattleId, choices);
                return;
            case BattleTargetType.AllAllies:
                AddSkillChoice(session, actor, skill, allies, null, choices);
                return;
            case BattleTargetType.Self:
                AddSkillChoice(session, actor, skill,
                    new[] { actor }, null, choices);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(skill), "지원하지 않는 스킬 대상입니다.");
        }
    }

    //스킬과 대상의 현재 가치를 계산해 선택지에 추가함
    private static void AddSkillChoice(BattleSession session, BattleUnit actor,
        SkillData skill, IReadOnlyList<BattleUnit> targets, string targetId,
        ICollection<ActionChoice> choices)
    {
        int score = 20;
        foreach (SkillEffectData effect in session.GetSkillEffects(skill.Id))
        {
            if (effect.Type == SkillEffectType.Damage && targets.Count > 1)
            {
                score += GetAreaAttackScore(effect, targets);
                continue;
            }

            foreach (BattleUnit target in targets)
                score += GetEffectScore(session, effect, target);
        }

        if (skill.CostType == SkillCostType.Sp)
            score -= skill.Cost / 2;

        var action = new BattleAction(actor.BattleId,
            BattleActionType.Skill, targetId, skill.Id);
        choices.Add(new ActionChoice(action, score));
    }

    //광역 공격은 가장 유리한 대상만 전부 반영하고 나머지 이득은 줄여서 합침
    private static int GetAreaAttackScore(SkillEffectData effect,
        IReadOnlyList<BattleUnit> targets)
    {
        int total = 0;
        int best = 0;
        foreach (BattleUnit target in targets)
        {
            int score = GetAttackScore(target, effect.DamageType.Value,
                effect.Power, effect.Accuracy, effect.HitCount);
            if (score <= 0)
            {
                total += score;
                continue;
            }

            if (score > best)
            {
                total += best * ExtraTargetPercent / 100;
                best = score;
                continue;
            }

            total += score * ExtraTargetPercent / 100;
        }
        return total + best;
    }

    //스킬 효과 하나가 대상에게 얼마나 유용한지 계산함
    private static int GetEffectScore(BattleSession session,
        SkillEffectData effect, BattleUnit target)
    {
        switch (effect.Type)
        {
            case SkillEffectType.Damage:
                return GetAttackScore(target, effect.DamageType.Value,
                    effect.Power, effect.Accuracy, effect.HitCount);
            case SkillEffectType.Heal:
                return GetHealScore(target);
            case SkillEffectType.ApplyEffect:
                return GetBattleEffectScore(session.GetBattleEffect(effect.EffectId),
                    target, effect.ApplyChance);
            case SkillEffectType.RemoveBuffs:
                return CountEffects(target, BattleEffectCategory.Buff) * 70 - 40;
            case SkillEffectType.RemoveDebuffs:
                return CountEffects(target, BattleEffectCategory.Debuff) * 70 - 40;
            case SkillEffectType.CureMentalStates:
                return CountEffects(target, BattleEffectCategory.MentalState) * 80 - 40;
            case SkillEffectType.Analyze:
                return -100;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect),
                    "지원하지 않는 스킬 효과입니다.");
        }
    }

    //공격의 위력과 대상 상성으로 공격 가치를 계산함
    private static int GetAttackScore(BattleUnit target, DamageType damageType,
        int power, int accuracy, int hitCount)
    {
        int score = AttackBaseScore + Math.Min(power, 200) / 2 +
            Math.Min(hitCount, 3) * 10;
        switch (target.Resistances.Get(damageType))
        {
            case ResistanceType.Weak:
                score += 100;
                break;
            case ResistanceType.Resist:
                score -= 40;
                break;
            case ResistanceType.Immune:
            case ResistanceType.Reflect:
            case ResistanceType.Drain:
                score -= 200;
                break;
        }

        if ((long)target.Hp * 4 <= target.MaxHp)
            score += 30;
        if (target.IsDown)
            score -= 20;
        return score * accuracy / 100;
    }

    //잃은 HP 비율로 회복 가치를 계산함
    private static int GetHealScore(BattleUnit target)
    {
        int missingHp = target.MaxHp - target.Hp;
        if (missingHp <= 0)
            return -100;
        return 40 + (int)((long)missingHp * 160 / target.MaxHp);
    }

    //강화·약화·정신 상태 등의 적용 가치를 계산함
    private static int GetBattleEffectScore(BattleEffectData effect,
        BattleUnit target, int applyChance)
    {
        foreach (BattleUnitEffectState state in target.Effects)
        {
            if (state.Effect.Type != effect.Type)
                continue;
            return state.RemainingTurns <= 1 && effect.MaxTurns > 0 ? 30 : -50;
        }

        int score = 70;
        if (effect.Category == BattleEffectCategory.MentalState)
        {
            double resistance = target.MentalResistance.GetMultiplier(effect.Type);
            if (resistance == 0d)
                return -100;
            score = (int)(score * resistance);
        }
        return score * applyChance / 100;
    }

    //대상에게 적용된 특정 분류의 효과 수를 셈
    private static int CountEffects(BattleUnit target,
        BattleEffectCategory category)
    {
        int count = 0;
        foreach (BattleUnitEffectState state in target.Effects)
        {
            if (state.Effect.Category == category)
                count++;
        }
        return count;
    }
}
