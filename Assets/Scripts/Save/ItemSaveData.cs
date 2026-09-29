using System;

/// <summary>
/// 아이템 저장용 데이터. <br/>
/// 아이템 ID와 현재 보유 수량만 담음.
/// </summary>
[Serializable]
public sealed class ItemSaveData
{
    //아이템 ID
    public string itemId;
    //현재 보유 수량
    public int count;
}
