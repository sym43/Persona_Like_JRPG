using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// CSV 표를 행과 값으로 읽는 공통 도구.
/// </summary>
internal static class CsvTableReader
{
    //CSV를 따옴표와 줄바꿈까지 처리해서 행으로 나눔
    internal static List<string[]> ReadRows(string text, string sourceName)
    {
        if (text == null) throw new ArgumentNullException(nameof(text), "CSV 내용이 필요합니다.");
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new ArgumentException("CSV 파일 이름이 필요합니다.", nameof(sourceName));

        var rows = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var rowNumber = 1;
        var columnNumber = 1;
        var quoted = false;
        var fieldStarted = false;

        for (int i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (quoted)
            {
                if (character == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == '"')
            {
                if (fieldStarted || field.Length > 0)
                    throw new FormatException($"{sourceName} {rowNumber}행 {columnNumber}열의 따옴표가 잘못됐습니다.");

                quoted = true;
                fieldStarted = true;
            }
            else if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
                columnNumber++;
            }
            else if (character == '\r' || character == '\n')
            {
                fields.Add(field.ToString());
                AddRow(rows, fields);
                fields.Clear();
                field.Clear();
                fieldStarted = false;
                columnNumber = 1;
                rowNumber++;

                if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
            }
            else
            {
                field.Append(character);
                fieldStarted = true;
            }
        }

        if (quoted)
            throw new FormatException($"{sourceName} {rowNumber}행의 닫히지 않은 따옴표가 있습니다.");

        if (fieldStarted || field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            AddRow(rows, fields);
        }

        return rows;
    }

    //CSV 헤더 이름과 순서를 확인함
    internal static void CheckHeader(string[] header, IReadOnlyList<string> columnNames, string sourceName)
    {
        if (header == null) throw new ArgumentNullException(nameof(header));
        if (columnNames == null) throw new ArgumentNullException(nameof(columnNames));
        if (header.Length != columnNames.Count)
            throw new FormatException($"{sourceName} 헤더의 열 개수가 {columnNames.Count}개가 아닙니다.");

        for (int i = 0; i < columnNames.Count; i++)
        {
            var name = header[i].Trim().TrimStart('\uFEFF');
            if (!string.Equals(name, columnNames[i], StringComparison.Ordinal))
                throw new FormatException($"{sourceName} 헤더 {i + 1}번째 열이 {columnNames[i]}가 아닙니다.");
        }
    }

    //일반 텍스트 값을 가져옴
    internal static string ReadText(string[] row, int index)
    {
        return row[index].Trim();
    }

    //빈 값이면 null, 아니면 텍스트 값을 가져옴
    internal static string ReadOptionalText(string[] row, int index)
    {
        var value = ReadText(row, index);
        return value.Length == 0 ? null : value;
    }

    //enum 값을 가져옴
    internal static T ReadEnum<T>(string[] row, int index, string columnName, string sourceName, int rowNumber)
        where T : struct
    {
        var text = row[index].Trim();
        T value;
        if (!Enum.TryParse(text, false, out value) ||
            !Enum.IsDefined(typeof(T), value) ||
            !string.Equals(value.ToString(), text, StringComparison.Ordinal))
            throw new FormatException($"{sourceName} {rowNumber}행의 {columnName} 값이 올바르지 않습니다: {text}");

        return value;
    }

    //빈 값이면 null, 아니면 enum 값을 가져옴
    internal static T? ReadOptionalEnum<T>(string[] row, int index, string columnName, string sourceName, int rowNumber)
        where T : struct
    {
        if (string.IsNullOrWhiteSpace(row[index]))
            return null;

        return ReadEnum<T>(row, index, columnName, sourceName, rowNumber);
    }

    //숫자 값을 가져옴
    internal static int ReadNumber(string[] row, int index, string columnName, string sourceName, int rowNumber)
    {
        int value;
        if (!int.TryParse(row[index].Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value))
            throw new FormatException($"{sourceName} {rowNumber}행의 {columnName} 값이 숫자가 아닙니다: {row[index]}");

        return value;
    }

    //빈 줄을 제외하고 행을 추가함
    private static void AddRow(List<string[]> rows, List<string> fields)
    {
        foreach (var field in fields)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                rows.Add(fields.ToArray());
                return;
            }
        }
    }
}
