using EdgeOfTheChart.SimulationCore.Data;

namespace EdgeOfTheChart.SimulationCore.World;

public static class IberiaMedMapBuilder
{
    public const int MapWidth = 120;
    public const int MapHeight = 72;

    public static SeaMapGrid Build(IReadOnlyList<PortData> ports)
    {
        ArgumentNullException.ThrowIfNull(ports);

        var grid = new SeaMapGrid(MapWidth, MapHeight, wrapHorizontal: false);

        // 1. 기본 타일을 원양(DeepSea)으로 초기화
        Array.Fill(grid.Tiles, SeaTileType.DeepSea);

        // 2. 이베리아 반도 (서쪽 x:25~55, 북쪽 y:12 ~ 남쪽 y:41)
        FillPolygon(grid, SeaTileType.Land, new (double X, double Y)[]
        {
            (25, 13), (38, 12), (55, 14), (56, 22), (51, 29),
            (48, 35), (43, 39), (37, 41), (31, 40), (26, 38),
            (25, 31), (26, 21), (24, 15)
        });
        // 리스본·세비야 해안선 미세 요철 보정
        SetLandBox(grid, 25, 29, 27, 33);
        SetLandBox(grid, 31, 37, 34, 40);

        // 3. 프랑스 남부 & 북부 이탈리아/알프스 육지 (y:0..17, 베네치아 북쪽 y:0..11)
        FillPolygon(grid, SeaTileType.Land, new (double X, double Y)[]
        {
            (25, 0), (119, 0), (119, 11), (100, 11),
            (89, 13), (82, 15), (75, 16), (67, 17), (58, 18), (48, 13), (25, 13)
        });

        // 4. 이탈리아 반도 (아드리아해와 티레니아해 분리, 베네치아/나폴리 해안 유지)
        FillPolygon(grid, SeaTileType.Land, new (double X, double Y)[]
        {
            (85, 14), (91, 14), (94, 21), (98, 28), (101, 35),
            (103, 40), (99, 42), (97, 36), (97, 31), (92, 26), (87, 20)
        });
        // 나폴리(96, 33) 동쪽 육지 보장
        SetLandBox(grid, 97, 31, 100, 36);

        // 5. 발칸 / 달마티아 동부 육지 (아드리아해 동쪽 경계)
        FillPolygon(grid, SeaTileType.Land, new (double X, double Y)[]
        {
            (111, 10), (119, 10), (119, 55), (113, 52), (111, 36), (110, 20)
        });

        // 6. 북아프리카 연안 (세우타 y=47..71 ~ 알제 ~ 튀니스 y=50..71)
        // 지브롤터 해협(x=32..40, y=42..45)은 최소 4타일 폭의 항로 확보
        FillPolygon(grid, SeaTileType.Land, new (double X, double Y)[]
        {
            (24, 48), (35, 47), (46, 48), (62, 49), (78, 49),
            (87, 50), (92, 57), (108, 60), (119, 59), (119, 71), (22, 71)
        });
        SetLandBox(grid, 35, 47, 38, 52);
        SetLandBox(grid, 58, 49, 63, 54);
        SetLandBox(grid, 82, 50, 86, 55);

        // 7. 도서 지역: 마데이라, 발레아레스(팔마), 코르시카, 사르데냐, 시칠리아
        // 마데이라 섬 (항구 10, 51 바로 남동쪽)
        SetLandBox(grid, 9, 52, 12, 53);
        // 팔마 / 발레아레스 제도 (항구 60, 34 바로 동쪽)
        SetLandBox(grid, 61, 33, 63, 35);
        // 코르시카
        SetLandBox(grid, 82, 23, 84, 28);
        // 사르데냐
        SetLandBox(grid, 81, 31, 85, 38);
        // 시칠리아 (항구 팔레르모 94, 44 바로 남쪽)
        FillPolygon(grid, SeaTileType.Land, new (double X, double Y)[]
        {
            (91, 45), (97, 45), (96, 49), (91, 48)
        });

        // 8. 육지 인접 2타일을 연안(CoastalSea)으로 지정
        MarkCoastalSea(grid);

        // 9. 항구 배치 (Phase1Tile 보유 항구 15곳)
        var portCoords = new List<(int X, int Y)>();
        foreach (PortData port in ports)
        {
            if (port.Phase1Tile is null)
            {
                continue;
            }

            int px = port.Phase1Tile.X;
            int py = port.Phase1Tile.Y;
            grid.RegisterPort(px, py, port.Id);
            portCoords.Add((px, py));
        }

        // 10. 주요 곶/해역 주변 암초(Reef) 클러스터 배치 (항구 반경 3타일 이내 및 지브롤터 중앙 제외)
        (int X, int Y)[] reefClusters =
        {
            (23, 14), (22, 15), (23, 16), // 갈리시아(피니스테레 곶) 암초
            (22, 38), (22, 39), (23, 39), // 상비센트 곶 암초
            (58, 31), (59, 31),           // 발레아레스 북쪽 암초
            (79, 41), (80, 41), (80, 42), // 사르데냐 남서 암초
            (99, 44), (100, 44),          // 메시나 외곽 암초
            (102, 25), (103, 25)          // 아드리아 중부 여울 암초
        };

        foreach ((int rx, int ry) in reefClusters)
        {
            if (grid.GetTile(rx, ry) == SeaTileType.Land || grid.GetTile(rx, ry) == SeaTileType.PortWater)
            {
                continue;
            }

            bool tooCloseToPort = portCoords.Any(p => Math.Abs(p.X - rx) <= 3 && Math.Abs(p.Y - ry) <= 3);
            if (!tooCloseToPort)
            {
                grid.SetTile(rx, ry, SeaTileType.Reef);
            }
        }

        return grid;
    }

    private static void SetLandBox(SeaMapGrid grid, int xMin, int yMin, int xMax, int yMax)
    {
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                grid.SetTile(x, y, SeaTileType.Land);
            }
        }
    }

    private static void MarkCoastalSea(SeaMapGrid grid)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (grid.GetTile(x, y) != SeaTileType.DeepSea)
                {
                    continue;
                }

                bool nearLand = false;
                for (int dy = -2; dy <= 2 && !nearLand; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        if (grid.GetTile(x + dx, y + dy) == SeaTileType.Land)
                        {
                            nearLand = true;
                            break;
                        }
                    }
                }

                if (nearLand)
                {
                    grid.SetTile(x, y, SeaTileType.CoastalSea);
                }
            }
        }
    }

    private static void FillPolygon(SeaMapGrid grid, SeaTileType tileType, IReadOnlyList<(double X, double Y)> polygon)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                if (IsPointInPolygon(x + 0.5, y + 0.5, polygon))
                {
                    grid.SetTile(x, y, tileType);
                }
            }
        }
    }

    private static bool IsPointInPolygon(double px, double py, IReadOnlyList<(double X, double Y)> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;
        for (int i = 0; i < polygon.Count; i++)
        {
            double xi = polygon[i].X;
            double yi = polygon[i].Y;
            double xj = polygon[j].X;
            double yj = polygon[j].Y;

            bool intersect = ((yi > py) != (yj > py)) &&
                             (px < (xj - xi) * (py - yi) / ((yj - yi) == 0 ? 1e-9 : (yj - yi)) + xi);
            if (intersect)
            {
                inside = !inside;
            }

            j = i;
        }

        return inside;
    }
}
