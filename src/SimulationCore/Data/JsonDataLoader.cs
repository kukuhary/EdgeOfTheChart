using System.Text.Json;

namespace EdgeOfTheChart.SimulationCore.Data;

public static class JsonDataLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static MasterDataCatalog LoadFromDirectory(string dataDirectory)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory) || !Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException($"마스터 데이터 디렉터리를 찾을 수 없습니다: {dataDirectory}");
        }

        var portsFile = LoadRequiredFile<PortsFileData>(Path.Combine(dataDirectory, "ports.json"));
        var shipsFile = LoadRequiredFile<ShipsFileData>(Path.Combine(dataDirectory, "ships.json"));
        var officersFile = LoadRequiredFile<OfficersFileData>(Path.Combine(dataDirectory, "officers.json"));
        var relicsFile = LoadRequiredFile<RelicsFileData>(Path.Combine(dataDirectory, "relics_and_figureheads.json"));
        var leviathansFile = LoadRequiredFile<LeviathansFileData>(Path.Combine(dataDirectory, "leviathans.json"));

        string scenariosDir = Path.Combine(dataDirectory, "scenarios");
        if (!Directory.Exists(scenariosDir))
        {
            throw new FileNotFoundException("시나리오 폴더를 찾을 수 없습니다.", scenariosDir);
        }

        string[] scenarioPaths = Directory.GetFiles(scenariosDir, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (scenarioPaths.Length == 0)
        {
            throw new FileNotFoundException("시나리오 JSON 파일이 하나도 없습니다.", scenariosDir);
        }

        var scenarios = new List<ScenarioData>(scenarioPaths.Length);
        foreach (string path in scenarioPaths)
        {
            scenarios.Add(LoadRequiredFile<ScenarioData>(path));
        }

        ValidateUniqueIds(portsFile.Ports.Select(p => p.Id), "항구(ports)");
        ValidateUniqueIds(shipsFile.Ships.Select(s => s.Id), "선박(ships)");
        ValidateUniqueIds(officersFile.Officers.Select(o => o.Id), "항해사(officers)");
        ValidateUniqueIds(relicsFile.Items.Select(r => r.Id), "유물/선수상(relics_and_figureheads)");
        ValidateUniqueIds(leviathansFile.Leviathans.Select(l => l.Id), "거대 괴수(leviathans)");
        ValidateUniqueIds(scenarios.Select(s => s.ScenarioId), "시나리오(scenarios)");

        foreach (ShipTemplateData ship in shipsFile.Ships)
        {
            CabinAllocationData cabins = ship.DefaultCabins;
            if (ship.TotalCapacity <= 0 || cabins.Crew < 0 || cabins.Guns < 0 || cabins.Cargo < 0)
            {
                throw new InvalidDataException($"선박 '{ship.Id}'의 용적 또는 선실 값이 올바르지 않습니다.");
            }

            if (cabins.Crew + cabins.Guns + cabins.Cargo > ship.TotalCapacity)
            {
                throw new InvalidDataException(
                    $"선박 '{ship.Id}'의 기본 선실 합계({cabins.Crew + cabins.Guns + cabins.Cargo})가 총 용적({ship.TotalCapacity})을 초과했습니다.");
            }
        }

        return new MasterDataCatalog
        {
            Ports = portsFile.Ports,
            Ships = shipsFile.Ships,
            Officers = officersFile.Officers,
            RelicsAndFigureheads = relicsFile.Items,
            Leviathans = leviathansFile.Leviathans,
            Scenarios = scenarios
        };
    }

    private static T LoadRequiredFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"필수 데이터 파일을 찾을 수 없습니다: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        T? data = JsonSerializer.Deserialize<T>(json, SerializerOptions);
        if (data is null)
        {
            throw new InvalidDataException($"JSON 역직렬화 결과가 비어 있습니다: {filePath}");
        }

        return data;
    }

    private static void ValidateUniqueIds(IEnumerable<string> ids, string category)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidDataException($"{category} 목록에 비어 있는 ID가 있습니다.");
            }

            if (!seen.Add(id))
            {
                throw new InvalidDataException($"{category} 목록에 중복된 ID가 있습니다: {id}");
            }
        }
    }
}
