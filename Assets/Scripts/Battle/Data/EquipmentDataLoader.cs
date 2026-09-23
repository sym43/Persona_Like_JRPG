using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 equipment.csv를 읽어 장비 원본 목록으로 바꿈.
/// </summary>
public sealed class EquipmentDataLoader
{
    private const string ResourcePath = "Data/Battle/equipment";
    private static readonly string[] ColumnNames =
    {
        "id", "displayName", "slot", "equipGroupId", "attackPower",
        "accuracy", "damageType", "defense", "evasion"
    };

    private IReadOnlyDictionary<string, EquipmentData> loadedData;

    public IReadOnlyDictionary<string, EquipmentData> Load()
    {
        if (loadedData != null) return loadedData;
        TextAsset csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("장비 CSV를 찾을 수 없습니다: Resources/Data/Battle/equipment.csv");
        loadedData = Read(csv.text, "Resources/Data/Battle/equipment.csv");
        return loadedData;
    }

    private static IReadOnlyDictionary<string, EquipmentData> Read(string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0) throw new FormatException($"{sourceName}에 헤더가 없습니다.");
        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);

        var result = new Dictionary<string, EquipmentData>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            int rowNumber = i + 1;
            string[] row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");
            try
            {
                var data = new EquipmentData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadText(row, 1),
                    CsvTableReader.ReadEnum<EquipmentSlot>(row, 2, ColumnNames[2], sourceName, rowNumber),
                    CsvTableReader.ReadText(row, 3),
                    CsvTableReader.ReadNumber(row, 4, ColumnNames[4], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 5, ColumnNames[5], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<DamageType>(row, 6, ColumnNames[6], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 7, ColumnNames[7], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 8, ColumnNames[8], sourceName, rowNumber));
                if (!result.TryAdd(data.Id, data))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 장비 ID가 있습니다: {data.Id}");
            }
            catch (FormatException) { throw; }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 장비 데이터가 잘못됐습니다: {exception.Message}", exception);
            }
        }
        return new ReadOnlyDictionary<string, EquipmentData>(result);
    }
}
