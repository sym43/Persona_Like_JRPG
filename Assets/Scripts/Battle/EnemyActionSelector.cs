using System;
using System.Collections.Generic;

/// <summary>
/// 현재 적이 사용할 수 있는 행동과 대상을 고름.
/// </summary>
public sealed class EnemyActionSelector
{
    private readonly Random random;

    //행동 선택에 사용할 난수를 받음
    public EnemyActionSelector(Random random)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random), "행동 선택 난수가 필요합니다.");
    }

    //임시: 적 AI 조건표가 추가되기 전까지 사용 가능한 행동 중 하나를 무작위로 고름.
    //현재 적이 사용할 행동과 대상을 고름
    public BattleAction ChooseAction(BattleSession session)
    {
        if (session == null || session.CurrentUnit == null || !session.CurrentUnit.IsEnemy)
            throw new InvalidOperationException("이 적의 행동을 선택할 수 없습니다.");

        BattleUnit actor = session.CurrentUnit;
        IReadOnlyList<BattleUnit> opponents = session.GetOpponents();
        IReadOnlyList<BattleUnit> allies = session.GetAllies();
        var choices = new List<BattleAction>();

        if (actor.BasicAttack != null)
        {
            foreach (BattleUnit target in opponents)
                choices.Add(new BattleAction(actor.BattleId,
                    BattleActionType.BasicAttack, target.BattleId));
        }

        foreach (SkillData skill in session.GetUsableSkills())
            AddSkillChoices(actor, skill, opponents, allies, choices);

        if (choices.Count == 0)
            throw new InvalidOperationException("현재 적에게 사용할 수 있는 행동이 없습니다.");
        return choices[random.Next(choices.Count)];
    }

    //스킬 대상 방식에 맞는 선택지를 추가함
    private static void AddSkillChoices(BattleUnit actor, SkillData skill,
        IReadOnlyList<BattleUnit> opponents, IReadOnlyList<BattleUnit> allies,
        ICollection<BattleAction> choices)
    {
        switch (skill.TargetType)
        {
            case SkillTargetType.OneEnemy:
                foreach (BattleUnit target in opponents)
                    choices.Add(new BattleAction(actor.BattleId,
                        BattleActionType.Skill, target.BattleId, skill.Id));
                return;
            case SkillTargetType.AllEnemies:
                if (opponents.Count > 0)
                    choices.Add(new BattleAction(actor.BattleId,
                        BattleActionType.Skill, skillId: skill.Id));
                return;
            case SkillTargetType.OneAlly:
                foreach (BattleUnit target in allies)
                    choices.Add(new BattleAction(actor.BattleId,
                        BattleActionType.Skill, target.BattleId, skill.Id));
                return;
            case SkillTargetType.AllAllies:
            case SkillTargetType.Self:
                choices.Add(new BattleAction(actor.BattleId,
                    BattleActionType.Skill, skillId: skill.Id));
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(skill), "지원하지 않는 스킬 대상입니다.");
        }
    }
}
