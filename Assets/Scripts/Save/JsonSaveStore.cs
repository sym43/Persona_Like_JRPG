using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// JSON 세이브 파일을 슬롯별로 저장하고 불러옴. <br/>
/// 저장 중 오류가 나면 기존 정상 파일을 유지함.
/// </summary>
public sealed class JsonSaveStore
{
    //현재 저장 구조 버전
    public const int CurrentSaveVersion = 1;

    private readonly string folderPath;

    //기본 저장 폴더를 사용함
    public JsonSaveStore() : this(Application.persistentDataPath)
    {
    }

    //지정한 저장 폴더를 사용함
    public JsonSaveStore(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("저장 폴더는 비워 둘 수 없습니다.", nameof(folderPath));

        this.folderPath = Path.GetFullPath(folderPath);
    }

    //슬롯 파일이 있는지 확인함
    public bool Exists(string slotId)
    {
        return File.Exists(GetFilePath(slotId));
    }

    //세이브 파일 경로를 반환함
    public string GetFilePath(string slotId)
    {
        CheckSlotId(slotId);
        return Path.Combine(folderPath, slotId + ".json");
    }

    //저장 데이터를 JSON 파일로 씀
    public void Save(string slotId, GameSaveData saveData)
    {
        SaveDataChecks.Check(saveData, CurrentSaveVersion);

        var filePath = GetFilePath(slotId);
        var temporaryPath = filePath + ".tmp";
        var backupPath = filePath + ".bak";

        try
        {
            Directory.CreateDirectory(folderPath);
            var json = JsonUtility.ToJson(saveData, true);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("JSON으로 바꿀 저장 데이터가 없습니다.");

            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            if (File.Exists(filePath))
                File.Replace(temporaryPath, filePath, backupPath, true);
            else
                File.Move(temporaryPath, filePath);
        }
        catch (Exception exception)
        {
            DeleteTemporaryFile(temporaryPath);
            throw new IOException("세이브 파일을 저장하지 못했습니다.", exception);
        }
    }

    //JSON 파일을 읽어 저장 데이터로 바꿈
    public GameSaveData Load(string slotId)
    {
        var filePath = GetFilePath(slotId);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("세이브 파일이 없습니다.", filePath);

        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("세이브 파일이 비어 있습니다.");

            var saveData = JsonUtility.FromJson<GameSaveData>(json);
            SaveDataChecks.Check(saveData, CurrentSaveVersion);
            return saveData;
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new FormatException("세이브 파일을 읽거나 검증하지 못했습니다.", exception);
        }
    }

    //슬롯 이름으로 경로 탈출을 막음
    private static void CheckSlotId(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("슬롯 ID는 비워 둘 수 없습니다.", nameof(slotId));

        foreach (var character in slotId)
        {
            if (!char.IsLetterOrDigit(character) && character != '_' && character != '-')
                throw new ArgumentException("슬롯 ID에는 문자, 숫자, 밑줄, 하이픈만 사용할 수 있습니다.", nameof(slotId));
        }
    }

    //남은 임시 파일을 지움
    private static void DeleteTemporaryFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            //원래 저장 오류를 유지함
        }
    }
}
