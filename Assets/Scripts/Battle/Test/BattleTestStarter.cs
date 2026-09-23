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

    //임시: 주인공의 서로 다른 전투 행동을 차례대로 확인함.
    private int playerActionCount;
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
        Debug.Log("4대4 전투 테스트를 시작했습니다.");
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

    //임시: 플레이어 입력 UI 대신 현재 전투원이 사용할 행동을 자동으로 선택함.
    private void ChoosePlayerAction(BattleUnit unit)
    {
        BattleAction action = unit.Data.Role == UnitRole.MainCharacter
            ? ChooseMainCharacterAction(unit)
            : ChooseCompanionAction(unit);
        battleController.SubmitPlayerAction(action);
    }

    //임시: 주인공의 광역기·교체·아이템·일반 공격·스킬을 차례로 확인함.
    private BattleAction ChooseMainCharacterAction(BattleUnit unit)
    {
        playerActionCount++;

        if (playerActionCount == 1 && TryGetSkill("sample_joy_all", out SkillData areaSkill))
            return CreateSkillAction(unit, areaSkill, null);

        if (playerActionCount == 2 && battleController.CanChangeAnima &&
            unit.AnimaInstanceId == FirstAnimaInstanceId)
        {
            battleController.ChangeAnima(SecondAnimaInstanceId);
        }

        if (playerActionCount == 3 && battleController.GetItemCount(ItemId) > 0)
        {
            return new BattleAction(unit.BattleId, BattleActionType.Item,
                GetFirstOpponentId(), itemId: ItemId);
        }

        if (playerActionCount == 4 && unit.BasicAttack != null)
        {
            return new BattleAction(unit.BattleId, BattleActionType.BasicAttack,
                GetFirstOpponentId());
        }

        return ChooseSkillOrBasicAttack(unit, "sample_fire", "test_enemy_anger");
    }

    //임시: 동료별 테스트 속성으로 약점을 우선 공격함.
    private BattleAction ChooseCompanionAction(BattleUnit unit)
    {
        switch (unit.BattleId)
        {
            case "test_ally_love":
                return ChooseSkillOrBasicAttack(unit, "sample_love", "test_enemy_love");
            case "test_ally_anger":
                return ChooseSkillOrBasicAttack(unit, "sample_fire", "test_enemy_anger");
            case "test_ally_fear":
                return ChooseSkillOrBasicAttack(unit, "sample_fear", "test_enemy_fear");
            default:
                return ChooseSkillOrBasicAttack(unit, null, null);
        }
    }

    //임시: 우선 스킬을 쓸 수 없으면 다른 스킬이나 일반 공격을 고름.
    private BattleAction ChooseSkillOrBasicAttack(BattleUnit unit,
        string preferredSkillId, string preferredTargetId)
    {
        if (TryGetSkill(preferredSkillId, out SkillData preferred))
            return CreateSkillAction(unit, preferred, preferredTargetId);

        IReadOnlyList<SkillData> skills = testBattle.GetUsableSkills();
        foreach (SkillData skill in skills)
        {
            if (skill.TargetType == BattleTargetType.OneDeadAlly)
                continue;
            return CreateSkillAction(unit, skill, null);
        }

        if (unit.BasicAttack != null)
        {
            return new BattleAction(unit.BattleId, BattleActionType.BasicAttack,
                GetFirstOpponentId());
        }

        return new BattleAction(unit.BattleId, BattleActionType.Guard);
    }

    //현재 사용할 수 있는 스킬에서 ID가 같은 스킬을 찾음.
    private bool TryGetSkill(string skillId, out SkillData found)
    {
        foreach (SkillData skill in testBattle.GetUsableSkills())
        {
            if (string.Equals(skill.Id, skillId, StringComparison.Ordinal))
            {
                found = skill;
                return true;
            }
        }

        found = null;
        return false;
    }

    //스킬 대상 방식에 맞춰 테스트 행동을 만듦.
    private BattleAction CreateSkillAction(BattleUnit unit,
        SkillData skill, string preferredTargetId)
    {
        string targetId = null;
        switch (skill.TargetType)
        {
            case BattleTargetType.OneEnemy:
                targetId = GetLivingTargetId(testBattle.GetOpponents(), preferredTargetId);
                break;
            case BattleTargetType.OneAlly:
                targetId = GetLowestHpAllyId();
                break;
            case BattleTargetType.OneDeadAlly:
                throw new InvalidOperationException("임시 자동 전투는 부활 대상을 선택하지 않습니다.");
        }

        return new BattleAction(unit.BattleId, BattleActionType.Skill,
            targetId, skill.Id);
    }

    //살아 있는 우선 대상이나 첫 대상을 구함.
    private static string GetLivingTargetId(IReadOnlyList<BattleUnit> units,
        string preferredId)
    {
        foreach (BattleUnit unit in units)
        {
            if (string.Equals(unit.BattleId, preferredId, StringComparison.Ordinal))
                return unit.BattleId;
        }

        if (units.Count == 0)
            throw new InvalidOperationException("행동할 수 있는 대상이 없습니다.");
        return units[0].BattleId;
    }

    //현재 행동자의 첫 번째 살아 있는 상대 ID를 구함.
    private string GetFirstOpponentId()
    {
        return GetLivingTargetId(testBattle.GetOpponents(), null);
    }

    //현재 행동자 편에서 HP 비율이 가장 낮은 전투원 ID를 구함.
    private string GetLowestHpAllyId()
    {
        IReadOnlyList<BattleUnit> allies = testBattle.GetAllies();
        BattleUnit selected = allies[0];
        foreach (BattleUnit ally in allies)
        {
            if ((long)ally.Hp * selected.MaxHp < (long)selected.Hp * ally.MaxHp)
                selected = ally;
        }
        return selected.BattleId;
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
    private static BattleSession CreateTestBattle()
    {
        BattleDataSet data = new BattleDataLoader().Load();
        var factory = new BattleUnitFactory(data);
        Anima firstAnima = CreateAnima(data, FirstAnimaInstanceId, FirstAnimaDataId);
        Anima secondAnima = CreateAnima(data, SecondAnimaInstanceId, SecondAnimaDataId);
        var units = new List<BattleUnit>();

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
