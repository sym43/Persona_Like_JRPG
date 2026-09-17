using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 anima.csv를 읽어 아니마 원본 데이터 목록으로 바꿈.
/// </summary>
public sealed class AnimaDataLoader
{
    private const string ResourcePath = "Data/Battle/anima";

    private static readonly string[] ColumnNames =
    {
        "id",
        "displayName",
        "baseLevel",
        "strength",
        "magic",
        "endurance",
        "agility",
        "luck",
        "slashResistance",
        "strikeResistance",
        "pierceResistance",
        "joyResistance",
        "angerResistance",
        "despairResistance",
        "fearResistance",
        "loveResistance",
        "hatredResistance",
        "desireResistance"
    };

    private static readonly DamageType[] ResistanceColumns =
    {
        DamageType.Slash,
        DamageType.Strike,
        DamageType.Pierce,
        DamageType.Joy,
        DamageType.Anger,
        DamageType.Despair,
        DamageType.Fear,
        DamageType.Love,
        DamageType.Hatred,
        DamageType.Desire
    };

    //아니마 CSV를 읽고 습득 스킬을 붙여 목록으로 반환함
    public IReadOnlyDictionary<string, AnimaData> Load(
        IReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>> learnableSkills)
    {
        if (learnableSkills == null)
            throw new ArgumentNullException(nameof(learnableSkills), "아니마 스킬 목록이 필요합니다.");

        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("아니마 CSV를 찾을 수 없습니다: Resources/Data/Battle/anima.csv");

        return ReadAnimas(csv.text, "Resources/Data/Battle/anima.csv", learnableSkills);
    }

    //CSV 문자열을 아니마 데이터 목록으로 바꿈
    private static IReadOnlyDictionary<string, AnimaData> ReadAnimas(
        string text,
        string sourceName,
        IReadOnlyDictionary<string, IReadOnlyList<SkillLearnData>> learnableSkills)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0)
            throw new FormatException($"{sourceName}에 헤더가 없습니다.");

        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);

        var animas = new Dictionary<string, AnimaData>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");

            try
            {
                var id = CsvTableReader.ReadText(row, 0);
                IReadOnlyList<SkillLearnData> skills;
                if (!learnableSkills.TryGetValue(id, out skills))
                    skills = Array.Empty<SkillLearnData>();

                var resistances = new List<KeyValuePair<DamageType, ResistanceType>>(ResistanceColumns.Length);
                for (int resistanceIndex = 0; resistanceIndex < ResistanceColumns.Length; resistanceIndex++)
                {
                    var columnIndex = 8 + resistanceIndex;
                    resistances.Add(new KeyValuePair<DamageType, ResistanceType>(
                        ResistanceColumns[resistanceIndex],
                        CsvTableReader.ReadEnum<ResistanceType>(
                            row, columnIndex, ColumnNames[columnIndex], sourceName, rowNumber)));
                }

                var anima = new AnimaData(
                    id,
                    CsvTableReader.ReadText(row, 1),
                    CsvTableReader.ReadNumber(row, 2, ColumnNames[2], sourceName, rowNumber),
                    new BattleStats(
                        CsvTableReader.ReadNumber(row, 3, ColumnNames[3], sourceName, rowNumber),
                        CsvTableReader.ReadNumber(row, 4, ColumnNames[4], sourceName, rowNumber),
                        CsvTableReader.ReadNumber(row, 5, ColumnNames[5], sourceName, rowNumber),
                        CsvTableReader.ReadNumber(row, 6, ColumnNames[6], sourceName, rowNumber),
                        CsvTableReader.ReadNumber(row, 7, ColumnNames[7], sourceName, rowNumber)),
                    new ResistanceTable(resistances),
                    skills);

                if (!animas.TryAdd(anima.Id, anima))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 아니마 ID가 있습니다: {anima.Id}");
            }
            catch (FormatException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 아니마 데이터가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        return new ReadOnlyDictionary<string, AnimaData>(animas);
    }
}
