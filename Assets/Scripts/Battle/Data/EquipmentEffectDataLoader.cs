using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 equipment_effects.csv를 읽어 장비별 추가 효과 목록으로 바꿈.
/// </summary>
public sealed class EquipmentEffectDataLoader
{
    private const string ResourcePath = "Data/Battle/equipment_effects";
    private static readonly string[] ColumnNames =
    {
        "equipmentId", "order", "effectType", "statType", "value",
        "damageType", "resistanceType", "mentalState", "mentalResistance",
        "effectId", "applyChance"
    };

    public IReadOnlyDictionary<string, IReadOnlyList<EquipmentEffectData>> Load()
    {
        TextAsset csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("장비 효과 CSV를 찾을 수 없습니다: Resources/Data/Battle/equipment_effects.csv");
        return Read(csv.text, "Resources/Data/Battle/equipment_effects.csv");
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<EquipmentEffectData>> Read(
        string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0) throw new FormatException($"{sourceName}에 헤더가 없습니다.");
        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);

        var grouped = new Dictionary<string, List<EquipmentEffectData>>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            int rowNumber = i + 1;
            string[] row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");
            try
            {
                var data = new EquipmentEffectData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadNumber(row, 1, ColumnNames[1], sourceName, rowNumber),
                    CsvTableReader.ReadEnum<EquipmentEffectType>(row, 2, ColumnNames[2], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<EquipmentStatType>(row, 3, ColumnNames[3], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 4, ColumnNames[4], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<DamageType>(row, 5, ColumnNames[5], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<ResistanceType>(row, 6, ColumnNames[6], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<BattleEffectType>(row, 7, ColumnNames[7], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<MentalResistanceType>(row, 8, ColumnNames[8], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalText(row, 9),
                    CsvTableReader.ReadNumber(row, 10, ColumnNames[10], sourceName, rowNumber));
                if (!grouped.TryGetValue(data.EquipmentId, out List<EquipmentEffectData> list))
                {
                    list = new List<EquipmentEffectData>();
                    grouped.Add(data.EquipmentId, list);
                }
                if (list.Exists(effect => effect.Order == data.Order))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 효과 순서가 있습니다: {data.EquipmentId} / {data.Order}");
                list.Add(data);
            }
            catch (FormatException) { throw; }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 장비 효과가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        var result = new Dictionary<string, IReadOnlyList<EquipmentEffectData>>(StringComparer.Ordinal);
        foreach (var pair in grouped)
        {
            pair.Value.Sort((left, right) => left.Order.CompareTo(right.Order));
            result.Add(pair.Key, new ReadOnlyCollection<EquipmentEffectData>(pair.Value));
        }
        return new ReadOnlyDictionary<string, IReadOnlyList<EquipmentEffectData>>(result);
    }
}
