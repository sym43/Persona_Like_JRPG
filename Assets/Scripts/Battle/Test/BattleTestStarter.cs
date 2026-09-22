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
    private const string SkillId = "test_slash";
    private const string ItemId = "test_fire_item";
    private const string FirstAnimaInstanceId = "test_anima_1";
    private const string SecondAnimaInstanceId = "test_anima_2";

    //임시: 아이템 행동을 한 번만 확인함.
    private bool itemUsed;

    //임시: 플레이 모드에서 테스트 전투를 자동으로 시작함.
    private void Start()
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
        battleController.StartBattle(CreateTestBattle());
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

    //임시: CSV와 저장 데이터 연결 전 사용할 전투를 만듦.
    private static BattleSession CreateTestBattle()
    {
        Anima firstAnima = CreateAnima(FirstAnimaInstanceId,
            "test_anima_data_1", new BattleStats(20, 15, 15, 10, 10));
        Anima secondAnima = CreateAnima(SecondAnimaInstanceId,
            "test_anima_data_2", new BattleStats(20, 20, 12, 18, 12));
        BattleUnit player = CreatePlayer(firstAnima);
        BattleUnit enemy = CreateEnemy();
        var units = new[] { player, enemy };
        var order = new BattleTurnOrder(new[]
        {
            new BattleTurnEntry(player.BattleId, BattleSide.Ally,
                player.StartAgility, player.TurnTieOrder),
            new BattleTurnEntry(enemy.BattleId, BattleSide.Enemy,
                enemy.StartAgility, enemy.TurnTieOrder)
        }, BattleEncounterType.Ambushed, new System.Random(1));

        var skill = new SkillData(SkillId, "테스트 참격",
            SkillUseType.Active, SkillCostType.Sp, 3, BattleTargetType.OneEnemy);
        var skills = new Dictionary<string, SkillData>
        {
            { skill.Id, skill }
        };
        var effects = new Dictionary<string, IReadOnlyList<SkillEffectData>>
        {
            {
                skill.Id,
                new[]
                {
                    new SkillEffectData(skill.Id, 0, SkillEffectType.Damage,
                        DamageType.Slash, 30, 100, 1, 0)
                }
            }
        };

        var battleEffects = new Dictionary<string, BattleEffectData>();
        var item = new ItemData(ItemId, "테스트 화염 아이템",
            ItemUseType.BattleOnly, BattleTargetType.OneEnemy);
        var items = new Dictionary<string, ItemData>
        {
            { item.Id, item }
        };
        var itemEffects = new Dictionary<string, IReadOnlyList<ItemEffectData>>
        {
            {
                item.Id,
                new[]
                {
                    new ItemEffectData(item.Id, 0, ItemEffectType.Damage,
                        DamageType.Anger, 10, ItemAmountType.Fixed, 100)
                }
            }
        };
        var inventory = new ItemInventory(new Dictionary<string, int>
        {
            { item.Id, 1 }
        });
        return new BattleSession(order, units, skills, effects, battleEffects,
            items, itemEffects, inventory, new[] { firstAnima, secondAnima },
            new System.Random(2), new EnemyKnowledge());
    }

    //임시: 테스트용 주인공을 만듦.
    private static BattleUnit CreatePlayer(Anima anima)
    {
        var data = new BattleUnitData(PlayerId, "테스트 주인공",
            UnitRole.MainCharacter, 10, 120, 30, null);
        return new BattleUnit(PlayerId, data, "test_character",
            anima.InstanceId, anima.Data.Id, 0, 10,
            120, 30, 120, 30, anima.Stats,
            anima.Data.Resistances, CreateNormalMentalResistance(),
            anima.SkillIds, EmptyEquipment(),
            new BasicAttackData(30, 100, DamageType.Slash), 10, 0);
    }

    //임시: 테스트용 아니마를 만듦.
    private static Anima CreateAnima(string instanceId, string dataId,
        BattleStats stats)
    {
        var data = new AnimaData(dataId, dataId, 1, stats,
            CreateNormalResistances(), Array.Empty<SkillLearnData>());
        return new Anima(instanceId, data, 1, 0, stats, new[] { SkillId });
    }

    //임시: 테스트용 적을 만듦.
    private static BattleUnit CreateEnemy()
    {
        var data = new BattleUnitData(EnemyId, "테스트 적",
            UnitRole.Enemy, 10, 50, 0, null);
        return new BattleUnit(EnemyId, data, null, null, null,
            0, 10, 50, 0, 50, 0,
            new BattleStats(8, 8, 10, 20, 8), CreateEnemyResistances(),
            CreateNormalMentalResistance(), Array.Empty<string>(), EmptyEquipment(),
            new BasicAttackData(10, 100, DamageType.Strike), 5, 0);
    }

    //임시: 모든 속성을 보통 상성으로 채움.
    private static ResistanceTable CreateNormalResistances()
    {
        var entries = new List<KeyValuePair<DamageType, ResistanceType>>();
        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            entries.Add(new KeyValuePair<DamageType, ResistanceType>(
                damageType, ResistanceType.Normal));
        }

        return new ResistanceTable(entries);
    }

    //임시: 화염 아이템의 약점·원모어 판정을 확인할 적 상성을 만듦.
    private static ResistanceTable CreateEnemyResistances()
    {
        var entries = new List<KeyValuePair<DamageType, ResistanceType>>();
        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            ResistanceType resistance = damageType == DamageType.Anger
                ? ResistanceType.Weak
                : ResistanceType.Normal;
            entries.Add(new KeyValuePair<DamageType, ResistanceType>(
                damageType, resistance));
        }
        return new ResistanceTable(entries);
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

    //임시: 장비 데이터 연결 전 사용할 빈 장비 슬롯을 만듦.
    private static IReadOnlyList<string> EmptyEquipment()
    {
        return new string[4];
    }
}
