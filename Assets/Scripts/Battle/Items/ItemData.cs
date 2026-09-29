using System;

/// <summary>
/// 아이템 원본 데이터. <br/>
/// 아이템의 사용 장소와 대상을 담고 실제 효과는 ItemEffectData로 연결함.
/// </summary>
public sealed class ItemData
{
    //아이템을 구분하는 ID
    public string Id { get; }
    //ui에 보여질 이름
    public string DisplayName { get; }
    //ui에 보여질 설명
    public string Description { get; }
    //아이템을 사용할 수 있는 장소
    public ItemUseType UseType { get; }
    //아이템 대상
    public BattleTargetType TargetType { get; }

    //아이템 데이터를 만듦
    public ItemData(string id, string displayName, string description,
        ItemUseType useType, BattleTargetType targetType)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(id);
        BattleDataChecks.CheckText(displayName);
        BattleDataChecks.CheckText(description);
        if (!Enum.IsDefined(typeof(ItemUseType), useType))
            throw new ArgumentOutOfRangeException(nameof(useType), "알 수 없는 아이템 사용 장소입니다.");
        if (!Enum.IsDefined(typeof(BattleTargetType), targetType))
            throw new ArgumentOutOfRangeException(nameof(targetType), "알 수 없는 대상 종류입니다.");

        #endregion

        Id = id;
        DisplayName = displayName;
        Description = description;
        UseType = useType;
        TargetType = targetType;
    }
}
