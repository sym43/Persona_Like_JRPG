using System;
using System.Collections.Generic;
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
    ChoosingAllOutAttack = 4,
    ChangingAnima = 5,
    Ended = 6
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
    private BattleActionResult lastActionResult;

    //플레이어가 행동을 선택해야 하는 차례
    public event Action<BattleUnit> PlayerTurnStarted;
    //원모어로 추가 행동을 시작한 전투원
    public event Action<BattleUnit> OneMoreStarted;
    //선택한 행동의 연출을 시작할 때 전달함
    public event Action<BattleAction> ActionStarted;
    //연출에 전달할 확정 행동 결과
    public event Action<BattleActionResult> ActionResolved;
    //총공격을 선택할 수 있을 때 참여 가능한 아군과 함께 알림
    public event Action<IReadOnlyList<BattleUnit>> AllOutAttackAvailable;
    //주인공 아니마가 바뀌었을 때 이전 아니마 ID와 함께 알림
    public event Action<BattleUnit, string> AnimaChanged;
    //전투가 끝났을 때의 상태
    public event Action<BattleState> BattleEnded;

    //현재 행동자
    public BattleUnit CurrentUnit => session?.CurrentUnit;
    //현재 전투 진행 단계
    public BattlePhase Phase { get; private set; } = BattlePhase.None;
    //현재 주인공이 아니마를 교체할 수 있는지
    public bool CanChangeAnima => session?.CanChangeAnima ?? false;

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

    //현재 원모어를 선택한 아군에게 넘김
    public void SubmitShift(string unitId)
    {
        if (session == null || Phase != BattlePhase.ChoosingAction ||
            !session.IsOneMoreTurn || session.CurrentUnit == null ||
            session.CurrentUnit.IsEnemy)
            throw new InvalidOperationException("현재 시프트할 수 있는 차례가 아닙니다.");

        BattleUnit unit = session.ShiftOneMoreTo(unitId);
        OneMoreStarted?.Invoke(unit);
        if (session.TryGetMentalAction(out BattleAction mentalAction))
            RunAction(mentalAction);
        else
            PlayerTurnStarted?.Invoke(unit);
    }

    //현재 시프트할 수 있는 아군을 구함
    public IReadOnlyList<BattleUnit> GetShiftTargets()
    {
        return session?.GetShiftTargets() ?? Array.Empty<BattleUnit>();
    }

    //현재 행동자가 사용할 수 있는 보유 아이템을 구함
    public IReadOnlyList<ItemData> GetUsableItems()
    {
        return session?.GetUsableItems() ?? Array.Empty<ItemData>();
    }

    //현재 행동자가 사용할 수 있는 스킬을 구함
    public IReadOnlyList<SkillData> GetUsableSkills()
    {
        return session?.GetUsableSkills() ?? Array.Empty<SkillData>();
    }

    //현재 행동자의 살아 있는 상대를 구함
    public IReadOnlyList<BattleUnit> GetOpponents()
    {
        return session?.GetOpponents() ?? Array.Empty<BattleUnit>();
    }

    //현재 행동자와 같은 편의 살아 있는 전투원을 구함
    public IReadOnlyList<BattleUnit> GetAllies()
    {
        return session?.GetAllies() ?? Array.Empty<BattleUnit>();
    }

    //현재 가진 아이템 수량을 구함
    public int GetItemCount(string itemId)
    {
        return session?.GetItemCount(itemId) ?? 0;
    }

    //현재 적에게 공개된 상성을 구함. 미공개면 null을 반환함
    public ResistanceType? GetKnownResistance(string unitId, DamageType damageType)
    {
        return session?.GetKnownResistance(unitId, damageType);
    }

    //주인공이 전투에 지참한 아니마를 구함
    public IReadOnlyList<Anima> GetBattleAnimas()
    {
        return session?.GetBattleAnimas() ?? Array.Empty<Anima>();
    }

    //주인공의 아니마를 바꾸고 교체 연출이 끝날 때까지 입력을 기다림
    public void ChangeAnima(string instanceId)
    {
        if (session == null || Phase != BattlePhase.ChoosingAction ||
            session.CurrentUnit == null || session.CurrentUnit.IsEnemy)
            throw new InvalidOperationException("현재 아니마를 교체할 수 없습니다.");

        BattleUnit unit = session.CurrentUnit;
        string previousAnimaId = unit.AnimaInstanceId;
        session.ChangeAnima(instanceId);
        Phase = BattlePhase.ChangingAnima;
        if (AnimaChanged == null)
            FinishAnimaChange();
        else
            AnimaChanged.Invoke(unit, previousAnimaId);
    }

    //아니마 교체 연출을 마치고 행동 선택을 다시 허용함
    public void FinishAnimaChange()
    {
        if (session == null || Phase != BattlePhase.ChangingAnima)
            throw new InvalidOperationException("마무리할 아니마 교체가 없습니다.");
        Phase = BattlePhase.ChoosingAction;
    }

    //제안된 총공격을 실행하거나 거절함
    public void SubmitAllOutAttack(bool useAllOutAttack)
    {
        if (session == null || Phase != BattlePhase.ChoosingAllOutAttack ||
            !session.CanStartAllOutAttack)
            throw new InvalidOperationException("현재 총공격을 선택할 수 없습니다.");

        if (!useAllOutAttack)
        {
            session.DeclineAllOutAttack();
            CompleteAction();
            return;
        }

        pendingAction = new BattleAction(
            session.CurrentUnit.BattleId, BattleActionType.AllOutAttack);
        Phase = BattlePhase.PlayingAction;
        if (ActionStarted == null)
            ExecuteAction();
        else
            ActionStarted.Invoke(pendingAction);
    }

    //애니메이션·카메라 연출이 끝났을 때 호출함
    public void FinishAction()
    {
        if (session == null || Phase != BattlePhase.ShowingResult)
            throw new InvalidOperationException("마무리할 행동 결과가 없습니다.");
        if (pendingAction != null)
            throw new InvalidOperationException("타격 판정을 먼저 완료해야 합니다.");
        if (lastActionResult == null)
            throw new InvalidOperationException("마무리할 행동 결과가 없습니다.");

        if (lastActionResult.Type != BattleActionType.AllOutAttack &&
            session.CanStartAllOutAttack)
        {
            Phase = BattlePhase.ChoosingAllOutAttack;
            IReadOnlyList<BattleUnit> participants =
                session.GetAllOutAttackParticipants();
            if (AllOutAttackAvailable == null)
                SubmitAllOutAttack(false);
            else
                AllOutAttackAvailable.Invoke(participants);
            return;
        }

        CompleteAction();
    }

    //공격이 닿거나 방어가 시작되는 연출 시점에 행동을 적용함
    public void ExecuteAction()
    {
        if (session == null || Phase != BattlePhase.PlayingAction || pendingAction == null)
            throw new InvalidOperationException("적용할 행동이 없습니다.");

        BattleAction action = pendingAction;
        BattleActionResult result = action.Type == BattleActionType.AllOutAttack
            ? session.ExecuteAllOutAttack()
            : session.Execute(action);
        pendingAction = null;
        lastActionResult = result;
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
        if (session.IsOneMoreTurn)
            OneMoreStarted?.Invoke(unit);

        if (session.TryGetMentalAction(out BattleAction mentalAction))
        {
            RunAction(mentalAction);
            return;
        }

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

    //현재 행동과 선택 가능한 후속 처리를 끝내고 다음 차례로 이동함
    private void CompleteAction()
    {
        BattleState state = session.FinishAction();
        lastActionResult = null;
        if (state == BattleState.Ongoing)
            StartNextTurn();
        else
            EndBattle(state);
    }

    //전투 상태를 알리고 현재 전투 참조를 정리함
    private void EndBattle(BattleState state)
    {
        Phase = BattlePhase.Ended;
        session.ClearBattleState();
        BattleEnded?.Invoke(state);
        session = null;
        enemySelector = null;
        pendingAction = null;
        lastActionResult = null;
    }
}
