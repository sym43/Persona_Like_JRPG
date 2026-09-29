using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 default_equipment.csv를 읽어 전투원별 최초 장착 상태로 바꿈.
/// </summary>
public sealed class DefaultEquipmentDataLoader
{
    private const string ResourcePath = "Data/Battle/default_equipment";
    private static readonly string[] ColumnNames =
    {
        "battleUnitId", "weaponId", "armorId", "shoesId", "accessoryId"
    };

    public IReadOnlyDictionary<string, EquipmentSet> Load()
    {
        TextAsset csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("기본 장비 CSV를 찾을 수 없습니다: Resources/Data/Battle/default_equipment.csv");
        var rows = CsvTableReader.ReadRows(csv.text, "Resources/Data/Battle/default_equipment.csv");
        if (rows.Count == 0) throw new FormatException("default_equipment.csv에 헤더가 없습니다.");
        CsvTableReader.CheckHeader(rows[0], ColumnNames, "Resources/Data/Battle/default_equipment.csv");

        var result = new Dictionary<string, EquipmentSet>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            int rowNumber = i + 1;
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"default_equipment.csv {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");
            string unitId = CsvTableReader.ReadText(row, 0);
            var equipment = new EquipmentSet(
                CsvTableReader.ReadOptionalText(row, 1),
                CsvTableReader.ReadOptionalText(row, 2),
                CsvTableReader.ReadOptionalText(row, 3),
                CsvTableReader.ReadOptionalText(row, 4));
            if (!result.TryAdd(unitId, equipment))
                throw new FormatException($"default_equipment.csv에 중복된 전투원 ID가 있습니다: {unitId}");
        }
        return new ReadOnlyDictionary<string, EquipmentSet>(result);
    }
}
