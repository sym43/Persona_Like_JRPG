using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 중인 아군을 각 슬롯에 연결하고 현재 차례와 수치를 갱신함.
/// </summary>
public sealed class BattleAllyListView : MonoBehaviour
{
    //전투 진행 알림을 받을 컨트롤러
    [SerializeField] private BattleController battleController;
    //현재 생성된 아군을 찾을 스포너
    [SerializeField] private BattleActorSpawner actorSpawner;
    //아군 정보를 표시할 슬롯들
    [SerializeField] private BattleAllySlotView[] slots;

    private readonly List<BattleUnit> allies = new();

    private void Awake()
    {
        if (battleController == null || actorSpawner == null ||
            slots == null || slots.Length == 0)
            throw new InvalidOperationException("아군 목록 UI 연결이 빠졌습니다.");
    }

    private void OnEnable()
    {
        battleController.PlayerTurnStarted += ShowPlayerTurn;
        battleController.ActionStarted += HideTurn;
        battleController.ActionResolved += RefreshAfterAction;
        battleController.BattleEnded += Clear;
    }

    private void Start()
    {
        ShowAllies(battleController.CurrentUnit);
    }

    private void OnDisable()
    {
        battleController.PlayerTurnStarted -= ShowPlayerTurn;
        battleController.ActionStarted -= HideTurn;
        battleController.ActionResolved -= RefreshAfterAction;
        battleController.BattleEnded -= Clear;
    }

    private void ShowPlayerTurn(BattleUnit currentUnit)
    {
        ShowAllies(currentUnit);
    }

    private void HideTurn(BattleAction _)
    {
        foreach (BattleAllySlotView slot in slots)
            slot.ShowTurn(false);
    }

    private void RefreshAfterAction(BattleActionResult _)
    {
        foreach (BattleAllySlotView slot in slots)
            slot.Refresh();
    }

    private void ShowAllies(BattleUnit currentUnit)
    {
        allies.Clear();
        foreach (BattleUnitActor actor in actorSpawner.Actors)
        {
            if (actor != null && actor.Unit != null && !actor.Unit.IsEnemy)
                allies.Add(actor.Unit);
        }

        allies.Sort((left, right) =>
            actorSpawner.GetSpawnOrder(left.BattleId).CompareTo(
                actorSpawner.GetSpawnOrder(right.BattleId)));

        for (int index = 0; index < slots.Length; index++)
        {
            if (index >= allies.Count)
            {
                slots[index].Clear();
                continue;
            }

            BattleUnit unit = allies[index];
            slots[index].Bind(unit);
            slots[index].ShowTurn(unit == currentUnit);
        }
    }

    private void Clear(BattleState _)
    {
        foreach (BattleAllySlotView slot in slots)
            slot.Clear();
        allies.Clear();
    }
}
