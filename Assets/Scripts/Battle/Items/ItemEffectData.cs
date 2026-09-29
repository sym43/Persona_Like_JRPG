using System;

/// <summary>
/// 아이템 하나에 연결된 실행 효과. <br/>
/// 같은 아이템에 여러 개를 넣으면 Order 순서대로 실행됨.
/// </summary>
public sealed class ItemEffectData
{
    //효과를 가진 아이템 ID
    public string ItemId { get; }
    //아이템 안에서 효과를 실행할 순서
    public int Order { get; }
    //실행할 효과 종류
    public ItemEffectType Type { get; }
    //피해 효과의 속성
    public DamageType? DamageType { get; }
    //피해나 회복에 사용할 수치
    public int Amount { get; }
    //고정 수치 또는 최대 수치 기준 백분율
    public ItemAmountType AmountType { get; }
    //피해 효과의 명중률
    public int Accuracy { get; }
    //부여할 전투 효과 ID
    public string EffectId { get; }
    //전투 효과가 적용될 확률
    public int ApplyChance { get; }

    //아이템 효과 데이터를 만듦
    public ItemEffectData(string itemId, int order, ItemEffectType type,
        DamageType? damageType, int amount, ItemAmountType amountType,
        int accuracy, string effectId = null, int applyChance = 100)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(itemId);
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), "효과 순서는 0 이상이어야 합니다.");
        if (!Enum.IsDefined(typeof(ItemEffectType), type))
            throw new ArgumentOutOfRangeException(nameof(type), "알 수 없는 아이템 효과입니다.");
        if (damageType.HasValue && !Enum.IsDefined(typeof(DamageType), damageType.Value))
            throw new ArgumentOutOfRangeException(nameof(damageType), "알 수 없는 피해 속성입니다.");
        if (!Enum.IsDefined(typeof(ItemAmountType), amountType))
            throw new ArgumentOutOfRangeException(nameof(amountType), "알 수 없는 수치 계산 방식입니다.");
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "효과 수치는 0 이상이어야 합니다.");
        if (accuracy < 0 || accuracy > 100)
            throw new ArgumentOutOfRangeException(nameof(accuracy), "명중률은 0 이상 100 이하여야 합니다.");
        if (applyChance < 0 || applyChance > 100)
            throw new ArgumentOutOfRangeException(nameof(applyChance), "효과 부여율은 0 이상 100 이하여야 합니다.");

        bool damage = type == ItemEffectType.Damage;
        bool recovery = type == ItemEffectType.RecoverHp ||
                        type == ItemEffectType.RecoverSp ||
                        type == ItemEffectType.Revive;
        bool usesAmount = damage || recovery;
        bool apply = type == ItemEffectType.ApplyEffect;

        if (damage && !damageType.HasValue)
            throw new ArgumentException("피해 효과에는 피해 속성이 필요합니다.", nameof(damageType));
        if (!damage && damageType.HasValue)
            throw new ArgumentException("피해 효과가 아니면 피해 속성을 넣을 수 없습니다.", nameof(damageType));
        if (usesAmount && amount < 1)
            throw new ArgumentOutOfRangeException(nameof(amount), "피해·회복 수치는 1 이상이어야 합니다.");
        if (!usesAmount && (amount != 0 || amountType != ItemAmountType.Fixed))
            throw new ArgumentException("피해·회복 외 효과의 수치는 Fixed, 0이어야 합니다.");
        if (damage && amountType != ItemAmountType.Fixed)
            throw new ArgumentException("피해 아이템은 고정 수치만 사용할 수 있습니다.", nameof(amountType));
        if (amountType == ItemAmountType.Percent && amount > 100)
            throw new ArgumentOutOfRangeException(nameof(amount), "백분율 수치는 100 이하여야 합니다.");
        if (!damage && accuracy != 100)
            throw new ArgumentException("피해 효과가 아니면 명중률은 100이어야 합니다.", nameof(accuracy));
        if (apply) BattleDataChecks.CheckText(effectId);
        else if (effectId != null)
            throw new ArgumentException("전투 효과 부여가 아니면 효과 ID를 넣을 수 없습니다.", nameof(effectId));
        if (!apply && applyChance != 100)
            throw new ArgumentException("전투 효과 부여가 아니면 부여율은 100이어야 합니다.", nameof(applyChance));

        #endregion

        ItemId = itemId;
        Order = order;
        Type = type;
        DamageType = damageType;
        Amount = amount;
        AmountType = amountType;
        Accuracy = accuracy;
        EffectId = effectId;
        ApplyChance = applyChance;
    }
}
