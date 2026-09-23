using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 임시 전투 데이터로 전투 시작부터 승패까지 확인함.
/// 실제 전투 진입 코드가 생기면 제거함.
/// </summary>
public sealed class BattleTestStarter : MonoBehaviour
{
    //전투를 진행할 컨트롤러
    [SerializeField] private BattleController battleController;

    private const string PlayerId = "test_player";
    private const string EnemyId = "test_enemy";
    private const string FirstAnimaDataId = "test_anima_joy";
    private const string SecondAnimaDataId = "test_anima_anger";
    private const string SkillId = "sample_fire";
    private const string ItemId = "sample_joy_gem";
    private const string FirstAnimaInstanceId = "test_anima_1";
    private const string SecondAnimaInstanceId = "test_anima_2";

    //임시: 아이템 행동을 한 번만 확인함.
    private bool itemUsed;
    //임시: 장착 무기의 일반 공격을 한 번만 확인함.
    private bool basicAttackUsed;
    //임시: Awake에서 CSV 데이터로 구성한 전투.
    private BattleSession testBattle;

    //임시: 씬에 들어오면 CSV 데이터로 테스트 전투를 구성함.
    private void Awake()
    {
        if (battleController == null)
        {
            Debug.LogError("전투 테스트에 BattleController가 연결되지 않았습니다.");
            return;
        }

        battleController.PlayerTurnStarted += ChoosePlayerAction;
        battleController.ActionResolved += LogActionResult;
        battleController.AnimaChanged += FinishAnimaChange;
        battleController.BattleEnded += LogBattleEnd;
        testBattle = CreateTestBattle();
    }

    //임시: 다른 컴포넌트의 이벤트 연결이 끝난 뒤 테스트 전투를 시작함.
    private void Start()
    {
        if (testBattle == null)
            return;

        battleController.StartBattle(testBattle);
        Debug.Log("전투 테스트를 시작했습니다.");
    }

    //오브젝트가 사라질 때 테스트 이벤트 연결을 끊음
    private void OnDestroy()
    {
        if (battleController == null)
            return;

        battleController.PlayerTurnStarted -= ChoosePlayerAction;
        battleController.ActionResolved -= LogActionResult;
        battleController.AnimaChanged -= FinishAnimaChange;
        battleController.BattleEnded -= LogBattleEnd;
    }

    //임시: 플레이어 입력 UI 대신 공격 스킬을 자동으로 선택함.
    private void ChoosePlayerAction(BattleUnit unit)
    {
        if (battleController.CanChangeAnima &&
            unit.AnimaInstanceId == FirstAnimaInstanceId)
            battleController.ChangeAnima(SecondAnimaInstanceId);

        if (!itemUsed)
        {
            itemUsed = true;
            battleController.SubmitPlayerAction(new BattleAction(
                unit.BattleId, BattleActionType.Item, EnemyId, itemId: ItemId));
            return;
        }

        if (!basicAttackUsed)
        {
            basicAttackUsed = true;
            battleController.SubmitPlayerAction(new BattleAction(
                unit.BattleId, BattleActionType.BasicAttack, EnemyId));
            return;
        }

        battleController.SubmitPlayerAction(new BattleAction(
            unit.BattleId, BattleActionType.Skill, EnemyId, SkillId));
    }

    //한 행동의 계산 결과를 콘솔에 표시함
    private static void LogActionResult(BattleActionResult result)
    {
        foreach (BattleImpactResult impact in result.Impacts)
        {
            Debug.Log($"행동 결과: {result.UnitId} → {impact.AffectedUnitId}, " +
                      $"피해 {impact.Damage}, HP 회복 {impact.Healing}, " +
                      $"SP 회복 {impact.SpRecovery}, 부활 {impact.Revived}, " +
                      $"남은 HP {impact.HpAfter}, 남은 SP {impact.SpAfter}");
        }

        if (result.ItemId != null)
        {
            Debug.Log($"아이템 사용: {result.ItemId}, 남은 수량 {result.ItemCountAfter}");
        }
    }

    //임시: 아니마 교체 연출 대신 로그를 남기고 선택을 다시 허용함.
    private void FinishAnimaChange(BattleUnit unit, string previousAnimaId)
    {
        Debug.Log($"아니마 교체: {previousAnimaId} → {unit.AnimaInstanceId}");
        battleController.FinishAnimaChange();
    }

    //전투 종료 결과를 콘솔에 표시함
    private static void LogBattleEnd(BattleState state)
    {
        Debug.Log($"전투 테스트 종료: {state}");
    }

    //임시: CSV 원본과 기본 장비를 조립해 테스트 전투를 만듦.
    private static BattleSession CreateTestBattle()
    {
        BattleDataSet data = new BattleDataLoader().Load();
        var factory = new BattleUnitFactory(data);
        Anima firstAnima = CreateAnima(data, FirstAnimaInstanceId,
            FirstAnimaDataId);
        Anima secondAnima = CreateAnima(data, SecondAnimaInstanceId,
            SecondAnimaDataId);
        BattleUnit player = CreatePlayer(data, factory, firstAnima);
        BattleUnit enemy = CreateEnemy(data, factory);
        var units = new[] { player, enemy };
        var order = new BattleTurnOrder(new[]
        {
            new BattleTurnEntry(player.BattleId, BattleSide.Ally,
                player.StartAgility, player.TurnTieOrder),
            new BattleTurnEntry(enemy.BattleId, BattleSide.Enemy,
                enemy.StartAgility, enemy.TurnTieOrder)
        }, BattleEncounterType.Normal, new System.Random(1));

        var inventory = new ItemInventory(new Dictionary<string, int>
        {
            { ItemId, 1 },
            { "sample_medicine", 2 }
        });
        return new BattleSession(order, units, data.Skills, data.SkillEffects,
            data.BattleEffects, data.Items, data.ItemEffects, inventory,
            new[] { firstAnima, secondAnima }, new System.Random(2),
            new EnemyKnowledge());
    }

    //임시: 테스트용 주인공을 CSV 원본과 기본 장비로 만듦.
    private static BattleUnit CreatePlayer(BattleDataSet data,
        BattleUnitFactory factory, Anima anima)
    {
        BattleUnitData unit = data.BattleUnits[PlayerId];
        return factory.CreateAlly(PlayerId, PlayerId, "test_character",
            anima, factory.GetDefaultEquipment(PlayerId), 0,
            unit.BaseLevel, unit.BaseMaxHp, unit.BaseMaxSp,
            CreateNormalMentalResistance());
    }

    //임시: 기본 레벨에 배운 스킬로 테스트용 아니마를 만듦.
    private static Anima CreateAnima(BattleDataSet data,
        string instanceId, string dataId)
    {
        AnimaData animaData = data.Animas[dataId];
        var skillIds = new List<string>();
        foreach (SkillLearnData skill in animaData.LearnableSkills)
        {
            if (skill.Level <= animaData.BaseLevel)
                skillIds.Add(skill.SkillId);
        }
        return new Anima(instanceId, animaData, animaData.BaseLevel,
            0, animaData.BaseStats, skillIds);
    }

    //임시: 적의 고정 아니마와 기본 장비로 테스트용 적을 만듦.
    private static BattleUnit CreateEnemy(BattleDataSet data,
        BattleUnitFactory factory)
    {
        BattleUnitData unit = data.BattleUnits[EnemyId];
        Anima enemyAnima = CreateAnima(data, "test_enemy_anima_instance",
            unit.FixedAnimaId);
        return factory.CreateEnemy(EnemyId, EnemyId,
            factory.GetDefaultEquipment(EnemyId), 0,
            unit.BaseMaxHp, unit.BaseMaxSp, enemyAnima.Stats,
            enemyAnima.Data.Resistances, CreateNormalMentalResistance(),
            enemyAnima.SkillIds, true);
    }

    //임시: 모든 정신 상태를 보통 저항으로 채움.
    private static MentalResistanceTable CreateNormalMentalResistance()
    {
        var entries = new List<KeyValuePair<BattleEffectType, MentalResistanceType>>();
        for (BattleEffectType type = BattleEffectType.Intoxication;
             type <= BattleEffectType.Intimidation; type++)
        {
            entries.Add(new KeyValuePair<BattleEffectType, MentalResistanceType>(
                type, MentalResistanceType.Normal));
        }
        return new MentalResistanceTable(entries);
    }

}
