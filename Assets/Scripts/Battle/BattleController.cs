using System;
using UnityEngine;

/// <summary>
/// 전투 컨트롤러가 현재 진행 중인 단계.
/// </summary>
public enum BattlePhase
{
    None = 0,
    ChoosingAction = 1,
    PlayingAction = 2,
    ShowingResult = 3,
    Ended = 4
}

/// <summary>
/// Unity 입력·적 선택·연출 완료 신호를 전투 세션과 연결함.
/// 피해나 턴 순서는 직접 계산하지 않음.
/// </summary>
public sealed class BattleController : MonoBehaviour
{
    private BattleSession session;
    private EnemyActionSelector enemySelector;
    private BattleAction pendingAction;

    //플레이어가 행동을 선택해야 하는 차례
    public event Action<BattleUnit> PlayerTurnStarted;
    //선택한 행동의 연출을 시작할 때 전달함
    public event Action<BattleAction> ActionStarted;
    //연출에 전달할 확정 행동 결과
    public event Action<BattleActionResult> ActionResolved;
    //전투가 끝났을 때의 상태
    public event Action<BattleState> BattleEnded;

    //현재 행동자
    public BattleUnit CurrentUnit => session?.CurrentUnit;
    //현재 전투 진행 단계
    public BattlePhase Phase { get; private set; } = BattlePhase.None;

    //전투를 시작함
    public void StartBattle(BattleSession battleSession)
    {
        if (battleSession == null)
            throw new ArgumentNullException(nameof(battleSession), "전투 세션이 필요합니다.");
        if (session != null)
            throw new InvalidOperationException("이미 진행 중인 전투가 있습니다.");

        session = battleSession;
        enemySelector = new EnemyActionSelector(new System.Random());
        Phase = BattlePhase.None;
        StartNextTurn();
    }

    //플레이어가 선택한 행동을 실행함
    public void SubmitPlayerAction(BattleAction action)
    {
        if (session == null || Phase != BattlePhase.ChoosingAction ||
            session.CurrentUnit == null || session.CurrentUnit.IsEnemy)
            throw new InvalidOperationException("플레이어가 행동할 차례가 아닙니다.");
        RunAction(action);
    }

    //애니메이션·카메라 연출이 끝났을 때 호출함
    public void FinishAction()
    {
        if (session == null || Phase != BattlePhase.ShowingResult)
            throw new InvalidOperationException("마무리할 행동 결과가 없습니다.");
        if (pendingAction != null)
            throw new InvalidOperationException("타격 판정을 먼저 완료해야 합니다.");
        BattleState state = session.FinishAction();
        if (state == BattleState.Ongoing)
            StartNextTurn();
        else
            EndBattle(state);
    }

    //공격이 닿거나 방어가 시작되는 연출 시점에 행동을 적용함
    public void ExecuteAction()
    {
        if (session == null || Phase != BattlePhase.PlayingAction || pendingAction == null)
            throw new InvalidOperationException("적용할 행동이 없습니다.");

        BattleActionResult result = session.Execute(pendingAction);
        pendingAction = null;
        Phase = BattlePhase.ShowingResult;
        if (ActionResolved == null)
            FinishAction();
        else
            ActionResolved.Invoke(result);
    }

    //다음 차례의 선택 주체를 결정함
    private void StartNextTurn()
    {
        if (!session.TryStartNextTurn(out BattleUnit unit))
        {
            EndBattle(session.State);
            return;
        }

        Phase = BattlePhase.ChoosingAction;
        if (!unit.IsEnemy)
        {
            PlayerTurnStarted?.Invoke(unit);
            return;
        }

        RunAction(enemySelector.ChooseAction(session));
    }

    //행동을 예약하고 연출 쪽에 시작을 알림
    private void RunAction(BattleAction action)
    {
        if (Phase != BattlePhase.ChoosingAction || pendingAction != null ||
            session.ActionExecuted)
            throw new InvalidOperationException("현재 행동을 먼저 끝내야 합니다.");
        pendingAction = action ?? throw new ArgumentNullException(nameof(action), "전투 행동이 필요합니다.");
        Phase = BattlePhase.PlayingAction;
        if (ActionStarted == null)
            ExecuteAction();
        else
            ActionStarted.Invoke(action);
    }

    //전투 상태를 알리고 현재 전투 참조를 정리함
    private void EndBattle(BattleState state)
    {
        Phase = BattlePhase.Ended;
        BattleEnded?.Invoke(state);
        session = null;
        enemySelector = null;
        pendingAction = null;
    }
}
