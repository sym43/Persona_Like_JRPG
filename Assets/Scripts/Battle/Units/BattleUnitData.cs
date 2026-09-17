using System;

/// <summary>
/// 아군 또는 적 전투원의 고유 데이터. <br/>
/// 역할과 기본 HP/SP, 동료의 고정 아니마 연결을 담음.
/// </summary>
public sealed class BattleUnitData
{
    //전투원 데이터를 구분하는 ID
    public string Id { get; }
    //ui에 보여질 이름
    public string DisplayName { get; }
    //주인공, 동료, 적 구분
    public UnitRole Role { get; }
    //기본 레벨
    public int BaseLevel { get; }
    //기본 최대 HP
    public int BaseMaxHp { get; }
    //기본 최대 SP
    public int BaseMaxSp { get; }
    //동료가 전투에서 사용하는 고정 아니마 ID
    public string FixedAnimaId { get; }

    //전투원 데이터를 만듦
    public BattleUnitData(string id, string displayName, UnitRole role,
        int baseLevel, int baseMaxHp, int baseMaxSp, string fixedAnimaId)
    {
        #region 입력값 검사

        BattleDataChecks.CheckText(id);
        BattleDataChecks.CheckText(displayName);
        BattleDataChecks.CheckLevel(baseLevel);
        if (!Enum.IsDefined(typeof(UnitRole), role))
            throw new ArgumentOutOfRangeException(nameof(role), "알 수 없는 전투원 역할입니다.");
        if (baseMaxHp < 1) throw new ArgumentOutOfRangeException(nameof(baseMaxHp), "기본 최대 HP는 1 이상이어야 합니다.");
        if (baseMaxSp < 0) throw new ArgumentOutOfRangeException(nameof(baseMaxSp), "기본 최대 SP는 0 이상이어야 합니다.");
        if (fixedAnimaId != null) BattleDataChecks.CheckText(fixedAnimaId);
        if (role == UnitRole.Companion && fixedAnimaId == null)
            throw new ArgumentException("동료에게는 고정 아니마 ID가 필요합니다.");

        #endregion

        Id = id;
        DisplayName = displayName;
        Role = role;
        BaseLevel = baseLevel;
        BaseMaxHp = baseMaxHp;
        BaseMaxSp = baseMaxSp;
        FixedAnimaId = fixedAnimaId;
    }
}
