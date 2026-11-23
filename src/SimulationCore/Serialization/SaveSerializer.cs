using System.Text.Json;
using System.Text.Json.Serialization;
using EdgeOfTheChart.SimulationCore.World;

namespace EdgeOfTheChart.SimulationCore.Serialization;

public static class SaveSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(WorldState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var snapshot = new SaveSnapshot
        {
            SaveVersion = WorldState.CurrentSaveVersion,
            SavedAtUtc = DateTime.UtcNow,
            World = state
        };

        return JsonSerializer.Serialize(snapshot, SerializerOptions);
    }

    public static WorldState Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidDataException("세이브 JSON 문자열이 비어 있습니다.");
        }

        SaveSnapshot? snapshot = JsonSerializer.Deserialize<SaveSnapshot>(json, SerializerOptions);
        if (snapshot is null || snapshot.World is null)
        {
            throw new InvalidDataException("세이브 스냅샷 역직렬화에 실패했습니다.");
        }

        if (snapshot.SaveVersion != WorldState.CurrentSaveVersion)
        {
            throw new InvalidDataException(
                $"지원하지 않는 세이브 버전입니다: {snapshot.SaveVersion} (현재 버전: {WorldState.CurrentSaveVersion})");
        }

        return snapshot.World;
    }

    public static void SaveToFile(WorldState state, string filePath)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("저장 경로가 비어 있습니다.", nameof(filePath));
        }

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = Serialize(state);
        File.WriteAllText(filePath, json);
    }

    public static WorldState LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"세이브 파일을 찾을 수 없습니다: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        return Deserialize(json);
    }
}
