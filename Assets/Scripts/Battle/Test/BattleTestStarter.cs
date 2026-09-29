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
    //임시 전투원 오브젝트를 배치할 스포너
    [SerializeField] private BattleActorSpawner battleActorSpawner;

    private const string PlayerId = "test_player";
    private const string FirstAnimaDataId = "test_anima_joy";
    private const string SecondAnimaDataId = "test_anima_anger";
    private const string ItemId = "sample_joy_gem";
    private const string FirstAnimaInstanceId = "test_anima_1";
    private const string SecondAnimaInstanceId = "test_anima_2";

    private static readonly string[] AllyIds =
    {
        PlayerId,
        "test_ally_love",
        "test_ally_anger",
        "test_ally_fear"
    };

    private static readonly string[] EnemyIds =
    {
        "test_enemy",
        "test_enemy_love",
        "test_enemy_anger",
        "test_enemy_fear"
    };

    //임시: Awake에서 CSV 데이터로 구성한 전투.
    private BattleSession testBattle;

    //임시: 씬에 들어오면 CSV 데이터로 테스트 전투를 구성함.
    private void Awake()
    {
        if (battleController == null || battleActorSpawner == null)
        {
            Debug.LogError("전투 테스트에 컨트롤러 또는 전투원 스포너가 연결되지 않았습니다.");
            return;
        }

        battleController.ActionResolved += LogActionResult;
        battleController.AnimaChanged += FinishAnimaChange;
        battleController.BattleEnded += LogBattleEnd;
        testBattle = CreateTestBattle(out List<BattleUnit> units);
        battleActorSpawner.CacheAllies(AllyIds);
        battleActorSpawner.CacheEnemies(EnemyIds);
        battleActorSpawner.Spawn(units);
    }

    //임시: 다른 컴포넌트의 이벤트 연결이 끝난 뒤 테스트 전투를 시작함.
    private void Start()
    {
        if (testBattle == null)
            return;

        battleController.StartBattle(testBattle);
        Debug.Log("4대4 전투 테스트를 시작했습니다.");
    }

    //오브젝트가 사라질 때 테스트 이벤트 연결을 끊음
    private void OnDestroy()
    {
        if (battleController == null)
            return;

        battleController.ActionResolved -= LogActionResult;
        battleController.AnimaChanged -= FinishAnimaChange;
        battleController.BattleEnded -= LogBattleEnd;
    }

    //한 행동의 계산 결과를 콘솔에 표시함.
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
            Debug.Log($"아이템 사용: {result.ItemId}, 남은 수량 {result.ItemCountAfter}");
    }

    //임시: 아니마 교체 연출 대신 로그를 남기고 선택을 다시 허용함.
    private void FinishAnimaChange(BattleUnit unit, string previousAnimaId)
    {
        Debug.Log($"아니마 교체: {previousAnimaId} → {unit.AnimaInstanceId}");
        battleController.FinishAnimaChange();
    }

    //전투 종료 결과를 콘솔에 표시함.
    private static void LogBattleEnd(BattleState state)
    {
        Debug.Log($"전투 테스트 종료: {state}");
    }

    //임시: CSV 원본과 기본 장비를 조립해 4대4 테스트 전투를 만듦.
    private static BattleSession CreateTestBattle(out List<BattleUnit> units)
    {
        BattleDataSet data = new BattleDataLoader().Load();
        var factory = new BattleUnitFactory(data);
        Anima firstAnima = CreateAnima(data, FirstAnimaInstanceId, FirstAnimaDataId);
        Anima secondAnima = CreateAnima(data, SecondAnimaInstanceId, SecondAnimaDataId);
        units = new List<BattleUnit>();

        for (int index = 0; index < AllyIds.Length; index++)
        {
            string unitId = AllyIds[index];
            Anima anima = index == 0
                ? firstAnima
                : CreateAnima(data, unitId + "_anima_instance",
                    data.BattleUnits[unitId].FixedAnimaId);
            units.Add(CreateAlly(data, factory, unitId, anima, index));
        }

        for (int index = 0; index < EnemyIds.Length; index++)
            units.Add(CreateEnemy(data, factory, EnemyIds[index], AllyIds.Length + index));

        var entries = new List<BattleTurnEntry>();
        foreach (BattleUnit unit in units)
        {
            entries.Add(new BattleTurnEntry(unit.BattleId,
                unit.IsEnemy ? BattleSide.Enemy : BattleSide.Ally,
                unit.StartAgility, unit.TurnTieOrder));
        }

        var inventory = new ItemInventory(new Dictionary<string, int>
        {
            { ItemId, 1 },
            { "sample_medicine", 2 }
        });
        return new BattleSession(
            new BattleTurnOrder(entries, BattleEncounterType.Normal,
                new System.Random(1)),
            units, data.Skills, data.SkillEffects, data.BattleEffects,
            data.Items, data.ItemEffects, inventory,
            new[] { firstAnima, secondAnima }, new System.Random(2),
            new EnemyKnowledge());
    }

    //임시: 테스트용 아군을 CSV 원본과 기본 장비로 만듦.
    private static BattleUnit CreateAlly(BattleDataSet data,
        BattleUnitFactory factory, string unitId, Anima anima, int turnTieOrder)
    {
        BattleUnitData unit = data.BattleUnits[unitId];
        return factory.CreateAlly(unitId, unitId, unitId + "_character",
            anima, factory.GetDefaultEquipment(unitId), turnTieOrder,
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
        BattleUnitFactory factory, string unitId, int turnTieOrder)
    {
        BattleUnitData unit = data.BattleUnits[unitId];
        Anima enemyAnima = CreateAnima(data, unitId + "_anima_instance",
            unit.FixedAnimaId);
        return factory.CreateEnemy(unitId, unitId,
            factory.GetDefaultEquipment(unitId), turnTieOrder,
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
