using System;
using System.Collections.Generic;

/// <summary>
/// 현재 전투 상태.
/// </summary>
public enum BattleState
{
    Ongoing = 0,
    Victory = 1,
    Defeat = 2
}

/// <summary>
/// 전투 한 판의 행동자, 실행 대기와 결과를 관리함.
/// </summary>
public sealed class BattleSession
{
    private readonly BattleTurnOrder turnOrder;
    private readonly Dictionary<string, BattleUnit> units;
    private readonly BattleUnit mainCharacter;
    private readonly BattleActionExecutor actionExecutor;

    private int orderIndex = -1;
    private BattleUnit currentUnit;
    private bool awaitingPresentation;

    //현재 전투 순환 번호
    public int Round { get; private set; } = 1;
    //현재 행동 중인 전투원
    public BattleUnit CurrentUnit => currentUnit;
    //행동 결과의 연출이 끝나기를 기다리는지
    public bool AwaitingPresentation => awaitingPresentation;
    //현재 전투 상태
    public BattleState State { get; private set; } = BattleState.Ongoing;

    //턴 순서와 전투원을 연결함
    public BattleSession(BattleTurnOrder turnOrder, IEnumerable<BattleUnit> units,
        Random random, bool bossBattle = false)
    {
        #region 입력값 검사

        if (turnOrder == null) throw new ArgumentNullException(nameof(turnOrder), "턴 순서가 필요합니다.");
        if (units == null) throw new ArgumentNullException(nameof(units), "전투원 목록이 필요합니다.");
        if (random == null) throw new ArgumentNullException(nameof(random), "행동 난수 생성기가 필요합니다.");

        #endregion

        this.turnOrder = turnOrder;
        this.units = CopyUnits(units, out mainCharacter);
        actionExecutor = new BattleActionExecutor(random, bossBattle);
        CheckOrderMembers();
        UpdateOutcome();
    }

    //현재 차례의 행동을 실행하고 연출용 결과를 반환함
    public BattleActionResult Execute(BattleAction action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action), "전투 행동이 필요합니다.");
        if (currentUnit == null || State != BattleState.Ongoing)
            throw new InvalidOperationException("현재 실행할 수 있는 차례가 아닙니다.");
        if (awaitingPresentation)
            throw new InvalidOperationException("현재 행동의 연출이 끝나기를 기다리고 있습니다.");
        if (!string.Equals(action.UnitId, currentUnit.BattleId, StringComparison.Ordinal))
            throw new InvalidOperationException("현재 행동자와 명령의 전투원 ID가 다릅니다.");

        BattleUnit target = null;
        if (action.TargetId != null && !units.TryGetValue(action.TargetId, out target))
            throw new InvalidOperationException("전투에 없는 대상입니다.");

        BattleActionResult result = actionExecutor.Execute(action, currentUnit, target);
        awaitingPresentation = true;
        return result;
    }

    //연출이 끝난 행동을 마무리함
    public BattleState FinishAction()
    {
        if (!awaitingPresentation)
            throw new InvalidOperationException("마무리할 행동 연출이 없습니다.");
        return CompleteTurn();
    }

    //현재 행동자의 살아 있는 상대를 구함
    public IReadOnlyList<BattleUnit> GetOpponents()
    {
        if (currentUnit == null)
            throw new InvalidOperationException("현재 행동자가 없습니다.");
        var result = new List<BattleUnit>();
        foreach (var unit in units.Values)
        {
            if (unit.IsEnemy != currentUnit.IsEnemy && !unit.IsDead && !unit.HasLeftBattle)
                result.Add(unit);
        }
        return result.AsReadOnly();
    }

    //다음 행동자를 찾음. 죽거나 이탈한 전투원은 차례를 건너뜀.
    public bool TryStartNextTurn(out BattleUnit unit)
    {
        #region 상태값 검사

        if (currentUnit != null)
            throw new InvalidOperationException("현재 행동을 먼저 끝내야 합니다.");

        #endregion

        unit = null;
        UpdateOutcome();
        if (State != BattleState.Ongoing)
            return false;

        int checkedEntries = 0;
        while (checkedEntries < turnOrder.CurrentOrder.Count)
        {
            MoveToNextEntry();
            BattleTurnEntry entry = turnOrder.CurrentOrder[orderIndex];
            BattleUnit nextUnitInOrder = units[entry.UnitId];
            checkedEntries++;

            if (nextUnitInOrder.IsDead || nextUnitInOrder.HasLeftBattle)
                continue;

            //자기 차례가 다시 시작되면 이전 방어를 끝냄.
            nextUnitInOrder.EndGuard();
            nextUnitInOrder.RecoverFromDown();
            currentUnit = nextUnitInOrder;
            unit = nextUnitInOrder;
            return true;
        }

        UpdateOutcome();
        return false;
    }

    //현재 행동을 끝내고 다음 차례를 받을 수 있게 함.
    private BattleState CompleteTurn()
    {
        #region 상태값 검사

        if (currentUnit == null)
            throw new InvalidOperationException("끝낼 행동이 없습니다.");

        #endregion

        currentUnit = null;
        awaitingPresentation = false;
        UpdateOutcome();
        return State;
    }

    //전투 결과를 다시 계산함.
    private void UpdateOutcome()
    {
        if (mainCharacter.IsDead)
        {
            State = BattleState.Defeat;
            return;
        }

        foreach (var unit in units.Values)
        {
            if (unit.IsEnemy && !unit.IsDead && !unit.HasLeftBattle)
            {
                State = BattleState.Ongoing;
                return;
            }
        }

        State = BattleState.Victory;
    }

    //순환 안에서 다음 순서 위치로 이동함.
    private void MoveToNextEntry()
    {
        orderIndex++;
        if (orderIndex < turnOrder.CurrentOrder.Count)
            return;

        turnOrder.CompleteRound();
        Round++;
        orderIndex = 0;
    }

    //전투원 목록을 복사하고 주인공을 찾음.
    private static Dictionary<string, BattleUnit> CopyUnits(
        IEnumerable<BattleUnit> source, out BattleUnit mainCharacter)
    {
        var copy = new Dictionary<string, BattleUnit>(StringComparer.Ordinal);
        mainCharacter = null;

        foreach (var unit in source)
        {
            if (unit == null)
                throw new ArgumentException("전투원 목록에 빈 값이 있습니다.", nameof(source));
            if (!copy.TryAdd(unit.BattleId, unit))
                throw new ArgumentException("중복된 전투원 ID가 있습니다.", nameof(source));

            if (unit.Data.Role == UnitRole.MainCharacter)
            {
                if (mainCharacter != null)
                    throw new ArgumentException("주인공은 한 명만 참가할 수 있습니다.", nameof(source));
                mainCharacter = unit;
            }
        }

        if (copy.Count == 0)
            throw new ArgumentException("전투원이 필요합니다.", nameof(source));
        if (mainCharacter == null)
            throw new ArgumentException("주인공이 필요합니다.", nameof(source));

        return copy;
    }

    //턴 순서와 전투원 목록의 ID가 같은지 확인함.
    private void CheckOrderMembers()
    {
        var orderIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in turnOrder.CurrentOrder)
        {
            if (!orderIds.Add(entry.UnitId))
                throw new ArgumentException("턴 순서에 중복된 전투원 ID가 있습니다.", nameof(turnOrder));
            if (!units.ContainsKey(entry.UnitId))
                throw new ArgumentException("턴 순서에 없는 전투원이 있습니다.", nameof(turnOrder));
        }

        if (orderIds.Count != units.Count)
            throw new ArgumentException("전투원 목록과 턴 순서의 대상이 다릅니다.", nameof(turnOrder));
    }
}
