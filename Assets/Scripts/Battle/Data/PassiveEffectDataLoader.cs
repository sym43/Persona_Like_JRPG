using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Resources의 passive_effects.csv를 읽어 스킬별 패시브 효과 목록으로 바꿈.
/// </summary>
public sealed class PassiveEffectDataLoader
{
    private const string ResourcePath = "Data/Battle/passive_effects";
    private static readonly string[] ColumnNames =
    {
        "skillId", "order", "effectType", "damageType", "value",
        "resistanceType", "effectId"
    };

    private IReadOnlyDictionary<string, IReadOnlyList<PassiveEffectData>> loadedEffects;

    public IReadOnlyDictionary<string, IReadOnlyList<PassiveEffectData>> Load()
    {
        if (loadedEffects != null) return loadedEffects;
        var csv = Resources.Load<TextAsset>(ResourcePath);
        if (csv == null)
            throw new InvalidOperationException("패시브 효과 CSV를 찾을 수 없습니다: Resources/Data/Battle/passive_effects.csv");
        loadedEffects = ReadEffects(csv.text, "Resources/Data/Battle/passive_effects.csv");
        return loadedEffects;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<PassiveEffectData>> ReadEffects(
        string text, string sourceName)
    {
        var rows = CsvTableReader.ReadRows(text, sourceName);
        if (rows.Count == 0) throw new FormatException($"{sourceName}에 헤더가 없습니다.");
        CsvTableReader.CheckHeader(rows[0], ColumnNames, sourceName);
        var grouped = new Dictionary<string, List<PassiveEffectData>>(StringComparer.Ordinal);

        for (int i = 1; i < rows.Count; i++)
        {
            int rowNumber = i + 1;
            string[] row = rows[i];
            if (row.Length != ColumnNames.Length)
                throw new FormatException($"{sourceName} {rowNumber}행의 열 개수가 {ColumnNames.Length}개가 아닙니다.");
            try
            {
                var effect = new PassiveEffectData(
                    CsvTableReader.ReadText(row, 0),
                    CsvTableReader.ReadNumber(row, 1, ColumnNames[1], sourceName, rowNumber),
                    CsvTableReader.ReadEnum<PassiveEffectType>(row, 2, ColumnNames[2], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<DamageType>(row, 3, ColumnNames[3], sourceName, rowNumber),
                    CsvTableReader.ReadNumber(row, 4, ColumnNames[4], sourceName, rowNumber),
                    CsvTableReader.ReadOptionalEnum<ResistanceType>(row, 5, ColumnNames[5], sourceName, rowNumber),
                    string.IsNullOrWhiteSpace(row[6]) ? null : row[6].Trim());

                if (!grouped.TryGetValue(effect.SkillId, out List<PassiveEffectData> effects))
                {
                    effects = new List<PassiveEffectData>();
                    grouped.Add(effect.SkillId, effects);
                }
                if (effects.Exists(item => item.Order == effect.Order))
                    throw new FormatException($"{sourceName} {rowNumber}행에 중복된 효과 순서가 있습니다: {effect.SkillId} / {effect.Order}");
                effects.Add(effect);
            }
            catch (FormatException) { throw; }
            catch (ArgumentException exception)
            {
                throw new FormatException($"{sourceName} {rowNumber}행의 패시브 효과가 잘못됐습니다: {exception.Message}", exception);
            }
        }

        var result = new Dictionary<string, IReadOnlyList<PassiveEffectData>>(StringComparer.Ordinal);
        foreach (var pair in grouped)
        {
            pair.Value.Sort((left, right) => left.Order.CompareTo(right.Order));
            result.Add(pair.Key, new ReadOnlyCollection<PassiveEffectData>(pair.Value));
        }
        return new ReadOnlyDictionary<string, IReadOnlyList<PassiveEffectData>>(result);
    }
}
