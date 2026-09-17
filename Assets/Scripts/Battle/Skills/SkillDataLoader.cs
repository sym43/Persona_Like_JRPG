using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 skills.csv를 읽어 스킬 원본 데이터 목록으로 바꿈.
/// </summary>
public sealed class SkillDataLoader
{
    private const string ResourcePath = "Data/Battle/skills";

    private static readonly string[] ColumnNames =
    {
        "id",
        "displayName",
        "useType",
        "damageType",
        "costType",
        "cost",
        "targetType",
        "power",
        "accuracy",
        "hitCount"
    };

    private IReadOnlyDictionary<string, SkillData> loadedSkills;

    //스킬 CSV를 처음 한 번 읽고 목록을 저장해서 반환함
    public IReadOnlyDictionary<string, SkillData> Load()
    {
        if (loadedSkills != null)
            return loadedSkills;

        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("스킬 CSV를 찾을 수 없습니다: Resources/Data/Battle/skills.csv");

        loadedSkills = ReadSkills(csv.text, "Resources/Data/Battle/skills.csv");
        return loadedSkills;
    }

    //CSV 문자열을 스킬 데이터 목록으로 바꿈
    private static IReadOnlyDictionary<string, SkillData> ReadSkills(string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0)
            throw new FormatException($"{sourceName}에 헤더가 없습니다.");

        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);

        var skills = new Dictionary<string, SkillData>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");

            try
            {
                var skill = new SkillData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadText(row, 1),
                    CsvTableReader.ReadEnum<SkillUseType>(row, 2, ColumnNames[2], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<DamageType>(row, 3, ColumnNames[3], sourceName, rowNumber),
                    CsvTableReader.ReadEnum<SkillCostType>(row, 4, ColumnNames[4], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 5, ColumnNames[5], sourceName, rowNumber),
                    CsvTableReader.ReadEnum<SkillTargetType>(row, 6, ColumnNames[6], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 7, ColumnNames[7], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 8, ColumnNames[8], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 9, ColumnNames[9], sourceName, rowNumber));

                if (!skills.TryAdd(skill.Id, skill))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 스킬 ID가 있습니다: {skill.Id}");
            }
            catch (FormatException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 스킬 데이터가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        return new ReadOnlyDictionary<string, SkillData>(skills);
    }
}
