using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 battle_effects.csv를 읽어 공통 전투 효과 목록으로 바꿈.
/// </summary>
public sealed class BattleEffectDataLoader
{
    private const string ResourcePath = "Data/Battle/battle_effects";

    private static readonly string[] ColumnNames =
    {
        "id", "effectType", "conflictGroupId", "value", "minTurns", "maxTurns"
    };

    private IReadOnlyDictionary<string, BattleEffectData> loadedEffects;

    //전투 효과 CSV를 처음 한 번 읽고 목록을 저장해서 반환함
    public IReadOnlyDictionary<string, BattleEffectData> Load()
    {
        if (loadedEffects != null) return loadedEffects;

        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("전투 효과 CSV를 찾을 수 없습니다: Resources/Data/Battle/battle_effects.csv");

        var rows = CsvTableReader.ReadRows(csv.text, "Resources/Data/Battle/battle_effects.csv");
        if (rows.Count == 0)
            throw new FormatException("battle_effects.csv에 헤더가 없습니다.");
        CsvTableReader.CheckHeader(rows[0], ColumnNames, "battle_effects.csv");

        var result = new Dictionary<string, BattleEffectData>(StringComparer.Ordinal);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            int rowNumber = i + 1;
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"battle_effects.csv {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");

            try
            {
                var effect = new BattleEffectData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadEnum<BattleEffectType>(row, 1, ColumnNames[1], "battle_effects.csv", rowNumber),
                    CsvTableReader.ReadText(row, 2),
                    CsvTableReader.ReadNumber(row, 3, ColumnNames[3], "battle_effects.csv", rowNumber),
                    CsvTableReader.ReadNumber(row, 4, ColumnNames[4], "battle_effects.csv", rowNumber),
                    CsvTableReader.ReadNumber(row, 5, ColumnNames[5], "battle_effects.csv", rowNumber));
                if (!result.TryAdd(effect.Id, effect))
                    throw new FormatException($"battle_effects.csv {rowNumber}행에 중복된 효과 ID가 있습니다: {effect.Id}");
            }
            catch (FormatException)
            {
                throw;
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"battle_effects.csv {rowNumber}행의 효과가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        loadedEffects = new ReadOnlyDictionary<string, BattleEffectData>(result);
        return loadedEffects;
    }
}
