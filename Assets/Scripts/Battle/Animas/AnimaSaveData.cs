using System;

/// <summary>
/// 아니마 저장용 데이터. <br/>
/// 개체 ID, 아니마 데이터 ID와 현재 성장, 보유 스킬 정보를 담음.
/// </summary>
[Serializable]
public sealed class AnimaSaveData
{
    //아니마 개체 ID
    public string instanceId;
    //아니마 데이터 ID
    public string dataId;
    //저장 시점 레벨
    public int level;
    //저장 시점 경험치
    public long experience;
    //저장 시점 힘
    public int strength;
    //저장 시점 마력
    public int magic;
    //저장 시점 내구
    public int endurance;
    //저장 시점 민첩
    public int agility;
    //저장 시점 운
    public int luck;
    //현재 배운 스킬 ID 목록
    public string[] skillIds;
}
