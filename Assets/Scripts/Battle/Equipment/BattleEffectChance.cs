using System;

/// <summary>
/// 일반 공격에 붙는 전투 효과 ID와 적용 확률.
/// </summary>
public sealed class BattleEffectChance
{
    public string EffectId { get; }
    public int ApplyChance { get; }

    public BattleEffectChance(string effectId, int applyChance)
    {
        BattleDataChecks.CheckText(effectId);
        if (applyChance < 0 || applyChance > 100)
            throw new ArgumentOutOfRangeException(nameof(applyChance), "적용 확률은 0~100이어야 합니다.");

        EffectId = effectId;
        ApplyChance = applyChance;
    }
}
