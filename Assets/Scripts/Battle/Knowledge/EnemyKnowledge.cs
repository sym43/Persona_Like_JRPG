using System;
using System.Collections.Generic;

/// <summary>
/// 적 원본별로 전투 중 밝혀진 속성 상성을 기억함. <br/>
/// 실제 상성값은 원본 전투 데이터에서 읽고 공개 여부만 관리함.
/// </summary>
public sealed class EnemyKnowledge
{
    private readonly Dictionary<string, HashSet<DamageType>> revealedTypes =
        new Dictionary<string, HashSet<DamageType>>(StringComparer.Ordinal);

    internal IEnumerable<string> EnemyDataIds => revealedTypes.Keys;

    //적의 한 속성 상성을 공개함
    public bool Reveal(string enemyDataId, DamageType damageType)
    {
        Check(enemyDataId, damageType);
        if (!revealedTypes.TryGetValue(enemyDataId,
                out HashSet<DamageType> types))
        {
            types = new HashSet<DamageType>();
            revealedTypes.Add(enemyDataId, types);
        }
        return types.Add(damageType);
    }

    //적의 모든 속성 상성을 공개함
    public void RevealAll(string enemyDataId)
    {
        BattleDataChecks.CheckText(enemyDataId);
        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            Reveal(enemyDataId, damageType);
    }

    //적의 해당 속성 상성이 공개됐는지 확인함
    public bool IsRevealed(string enemyDataId, DamageType damageType)
    {
        Check(enemyDataId, damageType);
        return revealedTypes.TryGetValue(enemyDataId,
                   out HashSet<DamageType> types) &&
               types.Contains(damageType);
    }

    //적에게 공개된 속성 목록을 구함
    public IReadOnlyList<DamageType> GetRevealedTypes(string enemyDataId)
    {
        BattleDataChecks.CheckText(enemyDataId);
        if (!revealedTypes.TryGetValue(enemyDataId,
                out HashSet<DamageType> types))
            return Array.Empty<DamageType>();

        var result = new List<DamageType>(types);
        result.Sort((left, right) => left.CompareTo(right));
        return result.AsReadOnly();
    }

    //입력값을 확인함
    private static void Check(string enemyDataId, DamageType damageType)
    {
        BattleDataChecks.CheckText(enemyDataId);
        if (!Enum.IsDefined(typeof(DamageType), damageType))
            throw new ArgumentOutOfRangeException(nameof(damageType),
                "알 수 없는 피해 속성입니다.");
    }
}
