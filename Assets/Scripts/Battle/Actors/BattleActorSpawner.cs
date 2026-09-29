using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투원 ID에 맞는 프리팹을 불러와 아군과 적 위치에 배치함.
/// </summary>
public sealed class BattleActorSpawner : MonoBehaviour
{
    private const string AllyPrefabPath = "BattleActors/Allies/";
    private const string EnemyPrefabPath = "BattleActors/Enemies/";

    //아군이 배치될 위치
    [SerializeField] private Transform[] allySpawnPoints;
    //적이 배치될 위치
    [SerializeField] private Transform[] enemySpawnPoints;
    //적 위치와 방향을 함께 돌리는 진형 부모
    [SerializeField] private Transform enemyFormation;

    private readonly Dictionary<string, GameObject> allyPrefabs =
        new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> enemyPrefabs =
        new Dictionary<string, GameObject>();
    private readonly List<BattleUnitActor> actors =
        new List<BattleUnitActor>();
    private Quaternion enemyFormationStartRotation;
    private bool savedEnemyFormationRotation;

    //현재 씬에 생성된 전투원 오브젝트
    public IReadOnlyList<BattleUnitActor> Actors => actors;

    //씬에 설정한 적 진형의 기본 방향을 저장함
    private void Awake()
    {
        SaveEnemyFormationRotation();
    }

    //전투 개체 ID가 같은 전투원 오브젝트를 찾음
    public BattleUnitActor GetActor(string battleId)
    {
        foreach (BattleUnitActor actor in actors)
        {
            if (actor != null && actor.Unit != null &&
                actor.Unit.BattleId == battleId)
                return actor;
        }
        return null;
    }

    //현재 진영에서 전투원이 배치된 스폰 순서를 구함
    public int GetSpawnOrder(string battleId)
    {
        BattleUnitActor target = GetActor(battleId);
        if (target == null)
            throw new InvalidOperationException(
                $"배치 순서를 찾을 수 없는 전투원입니다: {battleId}");

        int order = 0;
        foreach (BattleUnitActor actor in actors)
        {
            if (actor.Unit.IsEnemy != target.Unit.IsEnemy)
                continue;
            if (actor == target)
                return order;
            order++;
        }
        throw new InvalidOperationException(
            $"배치 순서를 찾을 수 없는 전투원입니다: {battleId}");
    }

    //사용할 아군 프리팹을 미리 불러둠
    public void CacheAllies(IEnumerable<string> unitIds)
    {
        Cache(unitIds, AllyPrefabPath, allyPrefabs);
    }

    //사용할 적 프리팹을 미리 불러둠
    public void CacheEnemies(IEnumerable<string> unitIds)
    {
        Cache(unitIds, EnemyPrefabPath, enemyPrefabs);
    }

    //전투원들을 각 진영의 스폰 지점에 배치함
    public void Spawn(IReadOnlyList<BattleUnit> units)
    {
        if (units == null)
            throw new ArgumentNullException(nameof(units), "배치할 전투원 목록이 필요합니다.");

        ResetEnemyFormation();
        ClearActors();
        int allyIndex = 0;
        int enemyIndex = 0;

        foreach (BattleUnit unit in units)
        {
            Transform[] spawnPoints = unit.IsEnemy
                ? enemySpawnPoints
                : allySpawnPoints;
            int spawnIndex = unit.IsEnemy ? enemyIndex++ : allyIndex++;

            if (spawnPoints == null || spawnIndex >= spawnPoints.Length ||
                spawnPoints[spawnIndex] == null)
                throw new InvalidOperationException(
                    $"{(unit.IsEnemy ? "적" : "아군")} 스폰 지점이 부족합니다.");

            GameObject prefab = GetPrefab(unit);
            Transform point = spawnPoints[spawnIndex];
            Transform actorParent = unit.IsEnemy
                ? enemyFormation
                : transform;
            if (actorParent == null)
                throw new InvalidOperationException("적 진형 부모가 연결되지 않았습니다.");
            GameObject instance = Instantiate(prefab, point.position,
                point.rotation, actorParent);
            BattleUnitActor actor = instance.GetComponent<BattleUnitActor>();
            if (actor == null)
                throw new InvalidOperationException(
                    $"전투원 프리팹에 BattleUnitActor가 없습니다: {unit.Data.Id}");

            actor.Bind(unit);
            actors.Add(actor);
        }
    }

    //현재 생성된 전투원 오브젝트를 정리함
    private void ClearActors()
    {
        foreach (BattleUnitActor actor in actors)
        {
            if (actor != null)
                Destroy(actor.gameObject);
        }
        actors.Clear();
    }

    //적 진형의 기본 방향을 처음 한 번 저장함
    private void SaveEnemyFormationRotation()
    {
        if (savedEnemyFormationRotation || enemyFormation == null)
            return;

        enemyFormationStartRotation = enemyFormation.localRotation;
        savedEnemyFormationRotation = true;
    }

    //이전 전투에서 회전한 적 진형을 기본 방향으로 되돌림
    private void ResetEnemyFormation()
    {
        if (enemyFormation == null)
            throw new InvalidOperationException("적 진형 부모가 연결되지 않았습니다.");

        SaveEnemyFormationRotation();
        enemyFormation.localRotation = enemyFormationStartRotation;
    }

    //지정한 전투원 프리팹들을 리소스에서 불러둠
    private static void Cache(IEnumerable<string> unitIds, string path,
        IDictionary<string, GameObject> cache)
    {
        if (unitIds == null)
            throw new ArgumentNullException(nameof(unitIds),
                "캐싱할 전투원 ID 목록이 필요합니다.");

        foreach (string unitId in unitIds)
        {
            if (cache.ContainsKey(unitId)) continue;

            GameObject prefab = Resources.Load<GameObject>(path + unitId);
            if (prefab == null)
                throw new InvalidOperationException(
                    $"전투원 프리팹을 찾을 수 없습니다: {path}{unitId}");
            cache.Add(unitId, prefab);
        }
    }

    //전투원 진영과 데이터 ID에 맞는 프리팹을 가져옴
    private GameObject GetPrefab(BattleUnit unit)
    {
        IDictionary<string, GameObject> cache = unit.IsEnemy
            ? enemyPrefabs
            : allyPrefabs;
        if (!cache.TryGetValue(unit.Data.Id, out GameObject prefab))
            throw new InvalidOperationException(
                $"캐싱되지 않은 전투원 프리팹입니다: {unit.Data.Id}");
        return prefab;
    }
}
