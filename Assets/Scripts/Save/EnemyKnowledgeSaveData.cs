using System;

/// <summary>
/// 적 원본 하나에서 밝혀진 속성 목록을 저장함. <br/>
/// 실제 상성값은 저장하지 않음.
/// </summary>
[Serializable]
public sealed class EnemyKnowledgeSaveData
{
    //적 원본 데이터 ID
    public string enemyDataId;
    //공개된 피해 속성 목록
    public DamageType[] revealedDamageTypes;
}
