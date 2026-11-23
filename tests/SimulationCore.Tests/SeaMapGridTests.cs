using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.World;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

public sealed class SeaMapGridTests
{
    [Fact]
    public void WrapHorizontal_WrapsXCoordinatesCorrectly()
    {
        var wrapped = new SeaMapGrid(120, 72, wrapHorizontal: true);
        Assert.Equal(119, wrapped.NormalizeX(-1));
        Assert.Equal(0, wrapped.NormalizeX(120));
        Assert.True(wrapped.IsInBounds(-1, 10));
        Assert.True(wrapped.IsInBounds(120, 10));
        Assert.False(wrapped.IsInBounds(10, -1));
        Assert.False(wrapped.IsInBounds(10, 72));
    }

    [Fact]
    public void IberiaMedMap_ConnectsLisbonToAllFourteenOtherPhase1PortsViaWater()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        SeaMapGrid map = IberiaMedMapBuilder.Build(catalog.Ports);

        Assert.Equal(120, map.Width);
        Assert.Equal(72, map.Height);

        List<PortData> phase1Ports = catalog.Ports.Where(p => p.Phase1Tile is not null).ToList();
        Assert.Equal(15, phase1Ports.Count);

        PortData lisbon = catalog.FindPort("port_lisbon")!;
        var start = (lisbon.Phase1Tile!.X, lisbon.Phase1Tile!.Y);
        Assert.True(map.IsSailable(start.X, start.Y));

        var visited = new HashSet<(int X, int Y)> { start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);

        (int dx, int dy)[] dirs =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        };

        while (queue.Count > 0)
        {
            (int cx, int cy) = queue.Dequeue();
            foreach ((int dx, int dy) in dirs)
            {
                int nx = cx + dx;
                int ny = cy + dy;
                if (!map.IsSailable(nx, ny))
                {
                    continue;
                }

                if (visited.Add((nx, ny)))
                {
                    queue.Enqueue((nx, ny));
                }
            }
        }

        foreach (PortData port in phase1Ports)
        {
            var coord = (port.Phase1Tile!.X, port.Phase1Tile!.Y);
            Assert.True(
                visited.Contains(coord),
                $"리스본에서 항구 '{port.Id}'({coord.X},{coord.Y})까지 해로가 연결되어 있지 않습니다.");
        }

        // 지브롤터 해협(x = 36, y = 41..46) 최소 폭 4타일 이상 확보 검증
        int straitWaterCount = 0;
        for (int y = 40; y <= 48; y++)
        {
            if (map.IsSailable(36, y))
            {
                straitWaterCount++;
            }
        }

        Assert.True(straitWaterCount >= 4, $"지브롤터 해협 폭이 4타일 미만입니다: {straitWaterCount}");
    }
}
