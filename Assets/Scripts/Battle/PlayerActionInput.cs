using System;
using UnityEngine;

/// <summary>
/// 플레이어가 고른 행동을 전투 컨트롤러에 전달함.
/// 버튼 배치와 외형은 담당하지 않음.
/// </summary>
public sealed class PlayerActionInput : MonoBehaviour
{
    [SerializeField] private BattleController battleController;

    //선택한 대상에게 일반 공격을 요청함
    public void ChooseBasicAttack(string targetId)
    {
        BattleUnit unit = GetCurrentUnit();
        battleController.SubmitPlayerAction(
            new BattleAction(unit.BattleId, BattleActionType.BasicAttack, targetId));
    }

    //선택한 스킬 사용을 요청함
    public void ChooseSkill(string skillId, string targetId = null)
    {
        BattleUnit unit = GetCurrentUnit();
        battleController.SubmitPlayerAction(
            new BattleAction(unit.BattleId, BattleActionType.Skill, targetId, skillId));
    }

    //선택한 아이템 사용을 요청함
    public void ChooseItem(string itemId, string targetId = null)
    {
        BattleUnit unit = GetCurrentUnit();
        battleController.SubmitPlayerAction(new BattleAction(
            unit.BattleId, BattleActionType.Item, targetId, itemId: itemId));
    }

    //현재 전투원을 방어 상태로 만듦
    public void ChooseGuard()
    {
        BattleUnit unit = GetCurrentUnit();
        battleController.SubmitPlayerAction(
            new BattleAction(unit.BattleId, BattleActionType.Guard));
    }

    //주인공이 사용할 아니마를 선택함
    public void ChooseAnima(string instanceId)
    {
        GetCurrentUnit();
        battleController.ChangeAnima(instanceId);
    }

    //현재 플레이어 차례의 전투원을 확인함
    private BattleUnit GetCurrentUnit()
    {
        if (battleController == null || battleController.CurrentUnit == null ||
            battleController.CurrentUnit.IsEnemy)
            throw new InvalidOperationException("플레이어 행동을 선택할 수 없습니다.");
        return battleController.CurrentUnit;
    }
}
