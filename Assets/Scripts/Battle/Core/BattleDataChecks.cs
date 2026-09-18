using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// 전투 데이터에 잘못된 값이 들어오는지 검사함. <br/>
/// 목록은 검사 후 복사해서 반환함.
/// </summary>
internal static class BattleDataChecks
{
    //텍스트 값이 비어 있는지 검사함
    internal static void CheckText(string value)
    {
        #region 입력값 검사

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ID 또는 이름은 비워 둘 수 없습니다.");

        #endregion
    }

    //레벨 범위를 검사함
    internal static void CheckLevel(int level)
    {
        #region 입력값 검사

        if (level < 1 || level > 99)
            throw new ArgumentOutOfRangeException(nameof(level), "레벨은 1 이상 99 이하여야 합니다.");

        #endregion
    }

    //스킬 ID 목록을 검사하고 복사해서 반환함
    internal static ReadOnlyCollection<string> CheckAndCopySkillIds(IEnumerable<string> values,
        int maxCount = int.MaxValue)
    {
        #region 입력값 검사

        if (values == null) throw new ArgumentNullException(nameof(values), "입력 목록이 필요합니다.");
        var copy = new List<string>();
        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            CheckText(value);
            if (!usedIds.Add(value))
                throw new ArgumentException("중복된 스킬 ID가 있습니다.", nameof(values));
            copy.Add(value);
        }
        if (copy.Count > maxCount)
            throw new ArgumentException("스킬 개수가 허용된 최대 개수를 초과했습니다.", nameof(values));

        #endregion

        return copy.AsReadOnly();
    }

    //장비 ID 목록을 검사하고 복사해서 반환함
    internal static ReadOnlyCollection<string> CheckAndCopyEquipmentIds(IReadOnlyList<string> values)
    {
        #region 입력값 검사

        if (values == null) throw new ArgumentNullException(nameof(values), "장비 목록이 필요합니다.");
        if (values.Count != 4)
            throw new ArgumentException("장비 슬롯은 정확히 4개여야 합니다.", nameof(values));

        #endregion

        var copy = new string[4];
        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = values[i];
            if (copy[i] != null) CheckText(copy[i]);
        }

        return Array.AsReadOnly(copy);
    }
}
