using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 unit_equip_groups.csv를 읽어 전투원별 장착 가능 그룹으로 묶음.
/// </summary>
public sealed class UnitEquipGroupDataLoader
{
    private const string ResourcePath = "Data/Battle/unit_equip_groups";
    private static readonly string[] ColumnNames = { "battleUnitId", "equipGroupId" };

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Load()
    {
        TextAsset csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("장착 그룹 CSV를 찾을 수 없습니다: Resources/Data/Battle/unit_equip_groups.csv");
        var rows = CsvTableReader.ReadRows(csv.text, "Resources/Data/Battle/unit_equip_groups.csv");
        if (rows.Count == 0) throw new FormatException("unit_equip_groups.csv에 헤더가 없습니다.");
        CsvTableReader.CheckHeader(rows[0], ColumnNames, "Resources/Data/Battle/unit_equip_groups.csv");

        var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            int rowNumber = i + 1;
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"unit_equip_groups.csv {rowNumber}행의 열 개수가 2개가 아닙니다.");
            string unitId = CsvTableReader.ReadText(row, 0);
            string groupId = CsvTableReader.ReadText(row, 1);
            if (!grouped.TryGetValue(unitId, out List<string> groups))
            {
                groups = new List<string>();
                grouped.Add(unitId, groups);
            }
            if (groups.Contains(groupId))
                throw new FormatException($"unit_equip_groups.csv에 중복된 장착 그룹이 있습니다: {unitId} / {groupId}");
            groups.Add(groupId);
        }

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var pair in grouped) result.Add(pair.Key, pair.Value.AsReadOnly());
        return new ReadOnlyDictionary<string, IReadOnlyList<string>>(result);
    }
}
