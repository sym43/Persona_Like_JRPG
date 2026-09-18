using System;
using System.Collections.Generic;

/// <summary>
/// 현재 적의 다음 행동을 고름. <br/>
/// 스킬 행동이 연결되기 전까지는 기본 공격만 선택함.
/// </summary>
public sealed class EnemyActionSelector
{
    private readonly Random random;

    //행동 선택에 사용할 난수를 받음
    public EnemyActionSelector(Random random)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random), "행동 선택 난수가 필요합니다.");
    }

    //살아 있는 아군 중 하나를 골라 일반 공격함
    public BattleAction ChooseAction(BattleSession session)
    {
        if (session == null || session.CurrentUnit == null || !session.CurrentUnit.IsEnemy)
            throw new InvalidOperationException("이 적의 행동을 선택할 수 없습니다.");
        if (session.CurrentUnit.BasicAttack == null)
            throw new InvalidOperationException("현재 적에게 사용할 수 있는 기본 공격이 없습니다.");

        IReadOnlyList<BattleUnit> targets = session.GetOpponents();
        if (targets.Count == 0)
            throw new InvalidOperationException("공격할 아군이 없습니다.");
        BattleUnit target = targets[random.Next(targets.Count)];
        return new BattleAction(session.CurrentUnit.BattleId,
            BattleActionType.BasicAttack, target.BattleId);
    }
}
