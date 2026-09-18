using System;
using System.Collections.Generic;

/// <summary>
/// 전투에서 아군과 적을 구분하는 진영.
/// </summary>
public enum BattleSide
{
    Ally = 0,
    Enemy = 1
}

/// <summary>
/// 전투 시작 방식.
/// </summary>
public enum BattleEncounterType
{
    Normal = 0,
    Advantage = 1,
    Ambushed = 2
}

/// <summary>
/// 턴 순서 계산에 필요한 전투원 정보.
/// </summary>
public sealed class BattleTurnEntry
{
    //전투원을 구분하는 ID
    public string UnitId { get; }
    //아군 또는 적 진영
    public BattleSide Side { get; }
    //전투 진입 시 민첩
    public int StartAgility { get; }
    //민첩이 같을 때 사용할 배치 순서
    public int TieOrder { get; }

    //턴 순서에 사용할 정보를 만듦
    public BattleTurnEntry(string unitId, BattleSide side, int startAgility, int tieOrder)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(unitId);
        if (!Enum.IsDefined(typeof(BattleSide), side))
            throw new ArgumentOutOfRangeException(nameof(side), "알 수 없는 전투 진영입니다.");
        if (startAgility < 1 || startAgility > 99)
            throw new ArgumentOutOfRangeException(nameof(startAgility), "전투 시작 민첩은 1 이상 99 이하여야 합니다.");
        if (tieOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(tieOrder), "동률 순서는 0 이상이어야 합니다.");

        #endregion

        UnitId = unitId;
        Side = side;
        StartAgility = startAgility;
        TieOrder = tieOrder;
    }
}

/// <summary>
/// 전투 시작 방식과 민첩을 기준으로 턴 순서를 계산함.
/// </summary>
public sealed class BattleTurnOrder
{
    //행은 아군과 적의 민첩 차이, 열은 연속으로 선택된 아군 수임.
    private static readonly int[,] AllyChance =
    {
        { 100, 80, 40, 27, 20 },
        { 100, 60, 30, 20, 15 },
        { 95, 40, 20, 14, 10 },
        { 90, 20, 10, 7, 5 },
        { 85, 0, 0, 0, 0 },
        { 80, 0, 0, 0, 0 },
        { 75, 0, 0, 0, 0 },
        { 70, 0, 0, 0, 0 },
        { 60, 0, 0, 0, 0 },
        { 50, 0, 0, 0, 0 }
    };

    private readonly List<BattleTurnEntry> allies = new List<BattleTurnEntry>();
    private readonly List<BattleTurnEntry> enemies = new List<BattleTurnEntry>();
    private readonly Random random;
    private readonly BattleEncounterType encounterType;
    private IReadOnlyList<BattleTurnEntry> normalOrder;

    //현재 순환에서 사용할 턴 순서
    public IReadOnlyList<BattleTurnEntry> CurrentOrder { get; private set; }

    //전투 시작 순서를 계산함
    public BattleTurnOrder(IEnumerable<BattleTurnEntry> entries,
        BattleEncounterType encounterType, Random random)
    {
        #region 입력값 검사

        if (entries == null) throw new ArgumentNullException(nameof(entries), "턴 순서 대상이 필요합니다.");
        if (random == null) throw new ArgumentNullException(nameof(random), "턴 순서 난수 생성기가 필요합니다.");
        if (!Enum.IsDefined(typeof(BattleEncounterType), encounterType))
            throw new ArgumentOutOfRangeException(nameof(encounterType), "알 수 없는 전투 시작 방식입니다.");

        #endregion

        this.random = random;
        this.encounterType = encounterType;

        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        var allyTieOrders = new HashSet<int>();
        var enemyTieOrders = new HashSet<int>();

        foreach (var entry in entries)
        {
            if (entry == null)
                throw new ArgumentException("턴 순서 대상에 빈 전투원이 있습니다.", nameof(entries));
            if (!usedIds.Add(entry.UnitId))
                throw new ArgumentException("중복된 전투원 ID가 있습니다.", nameof(entries));

            HashSet<int> tieOrders = entry.Side == BattleSide.Ally
                ? allyTieOrders
                : enemyTieOrders;
            if (!tieOrders.Add(entry.TieOrder))
                throw new ArgumentException("같은 진영에 중복된 동률 순서가 있습니다.", nameof(entries));

            if (entry.Side == BattleSide.Ally)
                allies.Add(entry);
            else
                enemies.Add(entry);
        }

        if (allies.Count == 0 || enemies.Count == 0)
            throw new ArgumentException("아군과 적이 각각 한 명 이상 필요합니다.", nameof(entries));
        if (allies.Count > 4)
            throw new ArgumentException("아군은 최대 4명까지 참가할 수 있습니다.", nameof(entries));

        allies.Sort(CompareEntries);
        enemies.Sort(CompareEntries);

        if (encounterType == BattleEncounterType.Normal)
        {
            normalOrder = BuildNormalOrder();
            CurrentOrder = normalOrder;
        }
        else
        {
            CurrentOrder = BuildOpeningOrder();
        }
    }

    //첫 순환 또는 기본 순환을 끝내고 다음 순환으로 넘어감
    public void CompleteRound()
    {
        if (normalOrder == null)
            normalOrder = BuildNormalOrder();

        CurrentOrder = normalOrder;
    }

    //민첩 내림차순, 동률이면 배치 순서로 정렬함
    private static int CompareEntries(BattleTurnEntry left, BattleTurnEntry right)
    {
        int agility = right.StartAgility.CompareTo(left.StartAgility);
        return agility != 0 ? agility : left.TieOrder.CompareTo(right.TieOrder);
    }

    //선제·기습의 첫 순환을 만듦
    private IReadOnlyList<BattleTurnEntry> BuildOpeningOrder()
    {
        var result = new List<BattleTurnEntry>(allies.Count + enemies.Count);
        if (encounterType == BattleEncounterType.Advantage)
        {
            result.AddRange(allies);
            result.AddRange(enemies);
        }
        else
        {
            result.AddRange(enemies);
            result.AddRange(allies);
        }

        return result.AsReadOnly();
    }

    //확률표를 사용해 일반 순서를 한 번 계산함
    private IReadOnlyList<BattleTurnEntry> BuildNormalOrder()
    {
        var result = new List<BattleTurnEntry>(allies.Count + enemies.Count);
        int allyIndex = 0;
        int enemyIndex = 0;
        int consecutiveAllies = 0;

        while (allyIndex < allies.Count && enemyIndex < enemies.Count)
        {
            int difference = allies[allyIndex].StartAgility - enemies[enemyIndex].StartAgility;
            int row = GetChanceRow(difference);
            int column = Math.Min(consecutiveAllies, AllyChance.GetLength(1) - 1);
            int allyChance = AllyChance[row, column];

            if (random.Next(100) < allyChance)
            {
                result.Add(allies[allyIndex++]);
                consecutiveAllies++;
            }
            else
            {
                result.Add(enemies[enemyIndex++]);
                consecutiveAllies = 0;
            }
        }

        while (allyIndex < allies.Count)
            result.Add(allies[allyIndex++]);
        while (enemyIndex < enemies.Count)
            result.Add(enemies[enemyIndex++]);

        return result.AsReadOnly();
    }

    //민첩 차이를 확률표 행으로 바꿈
    private static int GetChanceRow(int difference)
    {
        if (difference >= 15) return 0;
        if (difference >= 10) return 1;
        if (difference >= 5) return 2;
        if (difference >= 3) return 3;
        if (difference >= 1) return 4;
        if (difference == 0) return 5;
        if (difference >= -2) return 6;
        if (difference >= -4) return 7;
        if (difference >= -9) return 8;
        return 9;
    }
}
