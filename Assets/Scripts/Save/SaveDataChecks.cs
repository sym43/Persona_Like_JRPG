using System;
using System.Collections.Generic;

/// <summary>
/// JSON 저장 데이터의 구조와 값 범위를 검사함.
/// </summary>
internal static class SaveDataChecks
{
    //저장 데이터를 검사함
    internal static void Check(GameSaveData saveData, int expectedVersion)
    {
        #region 기본값 검사

        if (saveData == null)
            throw new FormatException("저장 데이터가 비어 있습니다.");
        if (saveData.saveVersion != expectedVersion)
            throw new FormatException($"지원하지 않는 세이브 버전입니다: {saveData.saveVersion}");
        CheckText(saveData.contentVersion, "원본 데이터 버전");
        CheckText(saveData.saveLocation, "저장 위치");
        if (saveData.ownedAnimas == null)
            throw new FormatException("보유 아니마 목록이 없습니다.");

        #endregion

        #region 아니마 목록 검사

        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anima in saveData.ownedAnimas)
        {
            if (anima == null)
                throw new FormatException("보유 아니마 목록에 비어 있는 항목이 있습니다.");

            CheckText(anima.instanceId, "아니마 개체 ID");
            CheckText(anima.dataId, "아니마 데이터 ID");
            if (!instanceIds.Add(anima.instanceId))
                throw new FormatException($"아니마 개체 ID가 중복됩니다: {anima.instanceId}");
            if (anima.level < 1 || anima.level > 99)
                throw new FormatException($"아니마 레벨이 범위를 벗어났습니다: {anima.instanceId}");
            if (anima.experience < 0)
                throw new FormatException($"아니마 경험치가 잘못됐습니다: {anima.instanceId}");
            CheckStat(anima.strength, "힘", anima.instanceId);
            CheckStat(anima.magic, "마력", anima.instanceId);
            CheckStat(anima.endurance, "내구", anima.instanceId);
            CheckStat(anima.agility, "민첩", anima.instanceId);
            CheckStat(anima.luck, "운", anima.instanceId);
            CheckSkillIds(anima.skillIds, anima.instanceId);
        }

        #endregion
    }

    //텍스트 저장값을 검사함
    private static void CheckText(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException($"{label}이(가) 비어 있습니다.");
    }

    //능력치 범위를 검사함
    private static void CheckStat(int value, string label, string instanceId)
    {
        if (value < 1 || value > 99)
            throw new FormatException($"{instanceId}의 {label} 수치가 범위를 벗어났습니다.");
    }

    //스킬 ID 목록을 검사함
    private static void CheckSkillIds(IReadOnlyList<string> skillIds, string instanceId)
    {
        if (skillIds == null)
            throw new FormatException($"{instanceId}의 스킬 목록이 없습니다.");
        if (skillIds.Count > 8)
            throw new FormatException($"{instanceId}의 스킬 개수가 8개를 초과했습니다.");

        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var skillId in skillIds)
        {
            CheckText(skillId, "스킬 ID");
            if (!usedIds.Add(skillId))
                throw new FormatException($"{instanceId}의 스킬 ID가 중복됩니다: {skillId}");
        }
    }
}
