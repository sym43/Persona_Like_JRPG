using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 진행 상태의 저장용 데이터. <br/>
/// CSV 원본을 복사하지 않고, 플레이 중 변한 값만 담음.
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    //저장 구조 버전
    public int saveVersion;
    //저장할 때 사용한 원본 데이터 버전
    public string contentVersion;
    //저장 위치 ID
    public string saveLocation;
    //보유 아니마 상태 목록
    public AnimaSaveData[] ownedAnimas;

    //현재 보유 상태를 저장 데이터로 묶음
    public static GameSaveData Capture(string contentVersion, string saveLocation,
        IEnumerable<Anima> ownedAnimas)
    {
        #region 입력값 검사

        if (string.IsNullOrWhiteSpace(contentVersion))
            throw new ArgumentException("원본 데이터 버전은 비워 둘 수 없습니다.", nameof(contentVersion));
        if (string.IsNullOrWhiteSpace(saveLocation))
            throw new ArgumentException("저장 위치는 비워 둘 수 없습니다.", nameof(saveLocation));
        if (ownedAnimas == null)
            throw new ArgumentNullException(nameof(ownedAnimas), "보유 아니마 목록이 필요합니다.");

        #endregion

        var saves = new List<AnimaSaveData>();
        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anima in ownedAnimas)
        {
            if (anima == null)
                throw new ArgumentException("보유 아니마 목록에 비어 있는 항목이 있습니다.", nameof(ownedAnimas));

            var save = AnimaSaveConverter.Capture(anima);
            if (!instanceIds.Add(save.instanceId))
                throw new ArgumentException("보유 아니마 개체 ID가 중복됩니다.", nameof(ownedAnimas));
            saves.Add(save);
        }

        return new GameSaveData
        {
            saveVersion = JsonSaveStore.CurrentSaveVersion,
            contentVersion = contentVersion,
            saveLocation = saveLocation,
            ownedAnimas = saves.ToArray()
        };
    }
}
