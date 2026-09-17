using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 battle_units.csv를 읽어 전투원 원본 데이터 목록으로 바꿈.
/// </summary>
public sealed class BattleUnitDataLoader
{
    private const string ResourcePath = "Data/Battle/battle_units";

    private static readonly string[] ColumnNames =
    {
        "id",
        "displayName",
        "role",
        "baseLevel",
        "baseMaxHp",
        "baseMaxSp",
        "fixedAnimaId"
    };

    private IReadOnlyDictionary<string, BattleUnitData> loadedUnits;

    //전투원 CSV를 처음 한 번 읽고 목록을 저장해서 반환함
    public IReadOnlyDictionary<string, BattleUnitData> Load()
    {
        if (loadedUnits != null)
            return loadedUnits;

        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("전투원 CSV를 찾을 수 없습니다: Resources/Data/Battle/battle_units.csv");

        loadedUnits = ReadUnits(csv.text, "Resources/Data/Battle/battle_units.csv");
        return loadedUnits;
    }

    //CSV 문자열을 전투원 데이터 목록으로 바꿈
    private static IReadOnlyDictionary<string, BattleUnitData> ReadUnits(string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0)
            throw new FormatException($"{sourceName}에 헤더가 없습니다.");

        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);

        var units = new Dictionary<string, BattleUnitData>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");

            try
            {
                var unit = new BattleUnitData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadText(row, 1),
                    CsvTableReader.ReadEnum<UnitRole>(row, 2, ColumnNames[2], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 3, ColumnNames[3], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 4, ColumnNames[4], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 5, ColumnNames[5], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalText(row, 6));

                if (!units.TryAdd(unit.Id, unit))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 전투원 ID가 있습니다: {unit.Id}");
            }
            catch (FormatException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 전투원 데이터가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        return new ReadOnlyDictionary<string, BattleUnitData>(units);
    }
}
