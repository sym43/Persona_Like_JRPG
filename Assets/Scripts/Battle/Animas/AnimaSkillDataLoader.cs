using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 anima_skills.csv를 읽어 아니마별 습득 스킬 목록으로 바꿈.
/// </summary>
public sealed class AnimaSkillDataLoader
{
    private const string ResourcePath = "Data/Battle/anima_skills";

    private static readonly string[] ColumnNames =
    {
        "animaId",
        "skillId",
        "learnLevel"
    };

    private IReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>> loadedSkills;

    //아니마 스킬 CSV를 처음 한 번 읽고 목록을 저장해서 반환함
    public IReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>> Load()
    {
        if (loadedSkills != null)
            return loadedSkills;

        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("아니마 스킬 CSV를 찾을 수 없습니다: Resources/Data/Battle/anima_skills.csv");

        loadedSkills = ReadSkills(csv.text, "Resources/Data/Battle/anima_skills.csv");
        return loadedSkills;
    }

    //CSV 문자열을 아니마별 습득 스킬 목록으로 바꿈
    private static IReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>> ReadSkills(
        string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0)
            throw new FormatException($"{sourceName}에 헤더가 없습니다.");

        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);

        var groupedSkills = new Dictionary<string, List<SkillLearnData>>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");

            try
            {
                var animaId = CsvTableReader.ReadText(row, 0);
                var skillId = CsvTableReader.ReadText(row, 1);
                var learnLevel = CsvTableReader.ReadNumber(row, 2, ColumnNames[2], sourceName, rowNumber);
                BattleDataChecks.CheckText(animaId);
                var learnData = new SkillLearnData(skillId, learnLevel);

                List<SkillLearnData> skills;
                if (!groupedSkills.TryGetValue(animaId, out skills))
                {
                    skills = new List<SkillLearnData>();
                    groupedSkills.Add(animaId, skills);
                }

                foreach (var existingSkill in skills)
                {
                    if (string.Equals(existingSkill.SkillId, learnData.SkillId, StringComparison.Ordinal))
                        throw new FormatException($"{sourceName} {rowNumber}행에 같은 아니마의 중복된 스킬 ID가 있습니다: {skillId}");
                }

                skills.Add(learnData);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 아니마 스킬 데이터가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        var result = new Dictionary<string, IReadOnlyList<SkillLearnData>>(StringComparer.Ordinal);
        foreach (var pair in groupedSkills)
            result.Add(pair.Key, pair.Value.AsReadOnly());

        return new ReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>>(result);
    }
}
