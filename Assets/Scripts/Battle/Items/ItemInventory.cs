using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 파티가 가진 아이템 수량. <br/>
/// 전투원별로 나누지 않고 파티 전체가 같은 목록을 사용함.
/// </summary>
public sealed class ItemInventory
{
    private readonly Dictionary<string, int> counts =
        new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, int> readOnlyCounts;

    //현재 가진 아이템과 수량
    public IReadOnlyDictionary<string, int> Counts => readOnlyCounts;

    //빈 아이템 목록을 만듦
    public ItemInventory()
    {
        readOnlyCounts = new ReadOnlyDictionary<string, int>(counts);
    }

    //저장된 아이템 수량으로 목록을 만듦
    public ItemInventory(IReadOnlyDictionary<string, int> itemCounts) : this()
    {
        if (itemCounts == null)
            throw new ArgumentNullException(nameof(itemCounts), "아이템 수량 목록이 필요합니다.");

        foreach (var pair in itemCounts)
            Add(pair.Key, pair.Value);
    }

    //아이템을 가진 수량을 가져옴
    public int GetCount(string itemId)
    {
        BattleDataChecks.CheckText(itemId);
        return counts.TryGetValue(itemId, out int count) ? count : 0;
    }

    //아이템을 한 개 이상 가지고 있는지 확인함
    public bool CanUse(string itemId)
    {
        return GetCount(itemId) > 0;
    }

    //아이템 수량을 늘림
    public void Add(string itemId, int amount = 1)
    {
        BattleDataChecks.CheckText(itemId);
        if (amount < 1)
            throw new ArgumentOutOfRangeException(nameof(amount), "추가할 수량은 1 이상이어야 합니다.");

        int current = GetCount(itemId);
        if (current > int.MaxValue - amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "아이템 수량이 너무 큽니다.");

        counts[itemId] = current + amount;
    }

    //아이템을 한 개 줄이고 성공 여부를 반환함
    public bool RemoveOne(string itemId)
    {
        int count = GetCount(itemId);
        if (count == 0) return false;

        if (count == 1) counts.Remove(itemId);
        else counts[itemId] = count - 1;
        return true;
    }
}
