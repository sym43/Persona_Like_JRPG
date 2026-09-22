using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 items.csv를 읽어 아이템 원본 데이터 목록으로 바꿈.
/// </summary>
public sealed class ItemDataLoader
{
    private const string ResourcePath = "Data/Battle/items";

    private static readonly string[] ColumnNames =
    {
        "id",
        "displayName",
        "useType",
        "targetType"
    };

    private IReadOnlyDictionary<string, ItemData> loadedItems;

    //아이템 CSV를 처음 한 번 읽고 목록을 저장해서 반환함
    public IReadOnlyDictionary<string, ItemData> Load()
    {
        if (loadedItems != null) return loadedItems;

        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("아이템 CSV를 찾을 수 없습니다: Resources/Data/Battle/items.csv");

        loadedItems = ReadItems(csv.text, "Resources/Data/Battle/items.csv");
        return loadedItems;
    }

    //CSV 문자열을 아이템 데이터 목록으로 바꿈
    private static IReadOnlyDictionary<string, ItemData> ReadItems(string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0)
            throw new FormatException($"{sourceName}에 헤더가 없습니다.");

        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);

        var items = new Dictionary<string, ItemData>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            int rowNumber = i + 1;
            string[] row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");

            try
            {
                var item = new ItemData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadText(row, 1),
                    CsvTableReader.ReadEnum<ItemUseType>(row, 2, ColumnNames[2], sourceName, rowNumber),
                    CsvTableReader.ReadEnum<BattleTargetType>(row, 3, ColumnNames[3], sourceName, rowNumber));

                if (!items.TryAdd(item.Id, item))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 아이템 ID가 있습니다: {item.Id}");
            }
            catch (FormatException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 아이템 데이터가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        return new ReadOnlyDictionary<string, ItemData>(items);
    }
}
