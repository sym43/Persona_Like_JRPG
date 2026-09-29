using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 item_effects.csv를 읽어 아이템별 실행 효과 목록으로 바꿈.
/// </summary>
public sealed class ItemEffectDataLoader
{
    private const string ResourcePath = "Data/Battle/item_effects";

    private static readonly string[] ColumnNames =
    {
        "itemId",
        "order",
        "effectType",
        "damageType",
        "amount",
        "amountType",
        "accuracy",
        "effectId",
        "applyChance"
    };

    private IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> loadedEffects;

    //아이템 효과 CSV를 처음 한 번 읽고 목록을 저장해서 반환함
    public IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> Load()
    {
        if (loadedEffects != null) return loadedEffects;

        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("아이템 효과 CSV를 찾을 수 없습니다: Resources/Data/Battle/item_effects.csv");

        loadedEffects = ReadEffects(csv.text, "Resources/Data/Battle/item_effects.csv");
        return loadedEffects;
    }

    //CSV 문자열을 아이템별 효과 목록으로 바꿈
    private static IReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>> ReadEffects(
        string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0)
            throw new FormatException($"{sourceName}에 헤더가 없습니다.");

        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);
        var grouped = new Dictionary<string, List<ItemEffectData>>(StringComparer.Ordinal);

        for (int i = 1; i < rows.Count; i++)
        {
            int rowNumber = i + 1;
            string[] row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");

            try
            {
                var effect = new ItemEffectData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadNumber(row, 1, ColumnNames[1], sourceName, rowNumber),
                    CsvTableReader.ReadEnum<ItemEffectType>(row, 2, ColumnNames[2], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<DamageType>(row, 3, ColumnNames[3], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 4, ColumnNames[4], sourceName, rowNumber),
                    CsvTableReader.ReadEnum<ItemAmountType>(row, 5, ColumnNames[5], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 6, ColumnNames[6], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalText(row, 7),
                    CsvTableReader.ReadNumber(row, 8, ColumnNames[8], sourceName, rowNumber));

                if (!grouped.TryGetValue(effect.ItemId, out List<ItemEffectData> effects))
                {
                    effects = new List<ItemEffectData>();
                    grouped.Add(effect.ItemId, effects);
                }
                if (effects.Exists(item => item.Order == effect.Order))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 효과 순서가 있습니다: {effect.ItemId} / {effect.Order}");
                effects.Add(effect);
            }
            catch (FormatException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 아이템 효과가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        var result = new Dictionary<string, IReadOnlyList<ItemEffectData>>(StringComparer.Ordinal);
        foreach (var pair in grouped)
        {
            pair.Value.Sort((left, right) => left.Order.CompareTo(right.Order));
            result.Add(pair.Key, new ReadOnlyCollection<ItemEffectData>(pair.Value));
        }
        return new ReadOnlyDictionary<string, IReadOnlyList<ItemEffectData>>(result);
    }
}
