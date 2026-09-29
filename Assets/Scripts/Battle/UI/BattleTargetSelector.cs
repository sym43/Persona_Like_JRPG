using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 행동의 대상 종류에 맞는 전투원을 선택함.
/// 키 입력과 전투 행동 실행은 담당하지 않음.
/// </summary>
public sealed class BattleTargetSelector : MonoBehaviour
{
    //생성된 전투원 오브젝트를 찾을 스포너
    [SerializeField] private BattleActorSpawner actorSpawner;

    private readonly List<BattleUnitActor> candidates = new();
    private readonly List<BattleUnitActor> selectedActors = new();
    private BattleUnitActor lastEnemyTarget;
    private int selectedIndex;

    //현재 대상 종류
    public BattleTargetType TargetType { get; private set; }
    //현재 선택된 전투원 오브젝트. 광역 대상이면 여러 개가 들어감
    public IReadOnlyList<BattleUnitActor> SelectedActors => selectedActors;
    //단일 대상일 때 현재 선택된 전투원
    public BattleUnit CurrentTarget => selectedActors.Count == 1
        ? selectedActors[0].Unit
        : null;
    //마지막으로 선택한 적. 다른 대상 선택 중에도 유지됨
    public BattleUnit SelectedEnemy => lastEnemyTarget?.Unit;

    //선택 대상이 바뀌었을 때 알림
    public event Action<IReadOnlyList<BattleUnitActor>> SelectionChanged;

    //대상 종류와 현재 전투 상황에 맞는 대상을 설정함
    public void SetTargets(BattleTargetType targetType, BattleUnit actor,
        IReadOnlyList<BattleUnit> opponents, IReadOnlyList<BattleUnit> allies,
        IReadOnlyList<BattleUnit> deadAllies)
    {
        if (actorSpawner == null)
            throw new InvalidOperationException("대상 선택기에 전투원 스포너가 연결되지 않았습니다.");
        if (actor == null)
            throw new ArgumentNullException(nameof(actor), "현재 행동자가 필요합니다.");

        BattleUnitActor previousTarget = targetType == BattleTargetType.OneEnemy
            ? lastEnemyTarget
            : selectedActors.Count == 1 ? selectedActors[0] : null;
        TargetType = targetType;
        candidates.Clear();
        selectedActors.Clear();

        switch (targetType)
        {
            case BattleTargetType.OneEnemy:
            case BattleTargetType.AllEnemies:
                AddCandidates(opponents);
                break;
            case BattleTargetType.OneAlly:
            case BattleTargetType.AllAllies:
                AddCandidates(allies);
                break;
            case BattleTargetType.Self:
                AddCandidate(actor);
                break;
            case BattleTargetType.OneDeadAlly:
                AddCandidates(deadAllies);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(targetType),
                    "지원하지 않는 대상 종류입니다.");
        }

        SortBySpawnOrder();

        if (candidates.Count == 0)
            throw new InvalidOperationException("선택할 수 있는 전투원이 없습니다.");

        if (IsAllTargets(targetType))
        {
            selectedActors.AddRange(candidates);
        }
        else
        {
            selectedIndex = FindIndex(previousTarget);
            selectedActors.Add(candidates[selectedIndex]);
            RememberEnemyTarget();
        }

        SelectionChanged?.Invoke(selectedActors);
    }

    //단일 대상을 이전 또는 다음 전투원으로 바꿈
    public void Move(int direction)
    {
        if (direction == 0 || IsAllTargets(TargetType) ||
            candidates.Count <= 1)
            return;

        selectedIndex = Wrap(selectedIndex + direction, candidates.Count);
        selectedActors.Clear();
        selectedActors.Add(candidates[selectedIndex]);
        RememberEnemyTarget();
        SelectionChanged?.Invoke(selectedActors);
    }

    //단일 대상 행동에 전달할 ID를 구함. 광역·자신 대상이면 비워둠
    public string GetTargetId()
    {
        if (TargetType == BattleTargetType.AllEnemies ||
            TargetType == BattleTargetType.AllAllies ||
            TargetType == BattleTargetType.Self)
            return null;
        if (CurrentTarget == null)
            throw new InvalidOperationException("선택된 단일 대상이 없습니다.");
        return CurrentTarget.BattleId;
    }

    //현재 대상 선택을 비움
    public void Clear()
    {
        candidates.Clear();
        selectedActors.Clear();
        selectedIndex = 0;
        SelectionChanged?.Invoke(selectedActors);
    }

    private void AddCandidates(IReadOnlyList<BattleUnit> units)
    {
        if (units == null) return;
        foreach (BattleUnit unit in units)
            AddCandidate(unit);
    }

    private void AddCandidate(BattleUnit unit)
    {
        BattleUnitActor actor = actorSpawner.GetActor(unit.BattleId);
        if (actor == null)
            throw new InvalidOperationException(
                $"전투원 오브젝트를 찾을 수 없습니다: {unit.BattleId}");
        candidates.Add(actor);
    }

    private int FindIndex(BattleUnitActor target)
    {
        if (target == null) return 0;
        for (int index = 0; index < candidates.Count; index++)
        {
            if (candidates[index] == target)
                return index;
        }
        return 0;
    }

    private void RememberEnemyTarget()
    {
        if (TargetType == BattleTargetType.OneEnemy && selectedActors.Count == 1)
            lastEnemyTarget = selectedActors[0];
    }

    //씬의 스폰 지점에 배치된 순서대로 전투원을 정렬함
    private void SortBySpawnOrder()
    {
        if (candidates.Count <= 1)
            return;

        candidates.Sort((left, right) =>
            actorSpawner.GetSpawnOrder(left.Unit.BattleId).CompareTo(
                actorSpawner.GetSpawnOrder(right.Unit.BattleId)));
    }

    private static bool IsAllTargets(BattleTargetType targetType)
    {
        return targetType == BattleTargetType.AllEnemies ||
               targetType == BattleTargetType.AllAllies;
    }

    private static int Wrap(int value, int count)
    {
        return (value % count + count) % count;
    }
}
