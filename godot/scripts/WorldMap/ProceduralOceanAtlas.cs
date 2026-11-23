using EdgeOfTheChart.SimulationCore.World;
using Godot;

namespace EdgeOfTheChart.Godot.WorldMap;

public static class ProceduralOceanAtlas
{
    public const int TileSize = 32;

    public const int TileDeepSea = 0;
    public const int TileCoastalSea = 1;
    public const int TileReef = 2;
    public const int TileLand = 3;
    public const int TilePortWater = 4;
    public const int TileFog = 5;

    public static TileSet BuildOceanTileSet()
    {
        const int tileCount = 6;
        Image atlasImg = Image.CreateEmpty(TileSize * tileCount, TileSize, false, Image.Format.Rgba8);

        for (int t = 0; t < tileCount; t++)
        {
            int ox = t * TileSize;
            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    Color pixel = t switch
                    {
                        TileDeepSea => SampleDeepSeaPixel(x, y),
                        TileCoastalSea => SampleCoastalPixel(x, y),
                        TileReef => SampleReefPixel(x, y),
                        TileLand => SampleLandPixel(x, y),
                        TilePortWater => SamplePortPixel(x, y),
                        TileFog => SampleFogPixel(x, y),
                        _ => Colors.Black
                    };
                    atlasImg.SetPixel(ox + x, y, pixel);
                }
            }
        }

        ImageTexture texture = ImageTexture.CreateFromImage(atlasImg);
        var atlasSource = new TileSetAtlasSource
        {
            Texture = texture,
            TextureRegionSize = new Vector2I(TileSize, TileSize)
        };

        for (int t = 0; t < tileCount; t++)
        {
            atlasSource.CreateTile(new Vector2I(t, 0));
        }

        var tileSet = new TileSet
        {
            TileSize = new Vector2I(TileSize, TileSize)
        };
        tileSet.AddSource(atlasSource, 0);
        return tileSet;
    }

    public static int ToAtlasIndex(SeaTileType tileType) =>
        tileType switch
        {
            SeaTileType.DeepSea => TileDeepSea,
            SeaTileType.CoastalSea => TileCoastalSea,
            SeaTileType.Reef => TileReef,
            SeaTileType.Land => TileLand,
            SeaTileType.PortWater => TilePortWater,
            _ => TileDeepSea
        };

    public static ImageTexture BuildShipTexture()
    {
        Image img = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        Color hullDark = new(0.38f, 0.22f, 0.11f, 1f);
        Color hullLight = new(0.58f, 0.36f, 0.18f, 1f);
        Color sail = new(0.96f, 0.92f, 0.80f, 1f);
        Color cross = new(0.76f, 0.16f, 0.16f, 1f);

        // 동쪽(+X) 기준 선체 (Rotation으로 방향 회전)
        for (int y = 10; y <= 21; y++)
        {
            for (int x = 5; x <= 26; x++)
            {
                int halfH = x > 20 ? Math.Max(1, 26 - x) : (x < 8 ? x - 4 : 5);
                if (Math.Abs(y - 16) <= halfH)
                {
                    img.SetPixel(x, y, Math.Abs(y - 16) >= halfH - 1 ? hullDark : hullLight);
                }
            }
        }

        // 라틴 삼각돛 + 포르투갈 십자 문양 포인트
        for (int y = 8; y <= 23; y++)
        {
            for (int x = 12; x <= 19; x++)
            {
                if (x >= 13 && x <= 17 && y >= 11 && y <= 20)
                {
                    bool isCross = x == 15 || y == 15;
                    img.SetPixel(x, y, isCross ? cross : sail);
                }
            }
        }

        return ImageTexture.CreateFromImage(img);
    }

    public static ImageTexture BuildStormTexture()
    {
        Image img = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        img.Fill(new Color(0, 0, 0, 0));

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                double dx = x - 31.5;
                double dy = y - 31.5;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 30.0)
                {
                    continue;
                }

                double angle = Math.Atan2(dy, dx);
                double spiral = Math.Sin(dist * 0.45 - angle * 3.0);
                float alpha = (float)Math.Clamp((1.0 - dist / 30.0) * 0.65, 0.0, 0.65);
                if (spiral > 0.15)
                {
                    img.SetPixel(x, y, new Color(0.75f, 0.85f, 0.96f, alpha));
                }
                else
                {
                    img.SetPixel(x, y, new Color(0.16f, 0.22f, 0.34f, alpha * 0.85f));
                }
            }
        }

        return ImageTexture.CreateFromImage(img);
    }

    private static Color SampleDeepSeaPixel(int x, int y)
    {
        Color baseColor = new(0.06f, 0.15f, 0.30f, 1f);
        bool wave = ((y == 9 || y == 23) && ((x + y) % 8 < 3));
        return wave ? new Color(0.11f, 0.24f, 0.43f, 1f) : baseColor;
    }

    private static Color SampleCoastalPixel(int x, int y)
    {
        Color baseColor = new(0.11f, 0.28f, 0.46f, 1f);
        bool ripple = ((y == 7 || y == 21) && ((x + y * 2) % 7 < 2));
        return ripple ? new Color(0.18f, 0.38f, 0.56f, 1f) : baseColor;
    }

    private static Color SampleReefPixel(int x, int y)
    {
        int dx = x - 16;
        int dy = y - 16;
        if (dx * dx + dy * dy <= 42)
        {
            return (x + y) % 5 == 0 ? new Color(0.86f, 0.92f, 0.96f, 1f) : new Color(0.32f, 0.35f, 0.38f, 1f);
        }

        if (dx * dx + dy * dy <= 85 && (x + y) % 4 == 0)
        {
            return new Color(0.76f, 0.88f, 0.95f, 1f);
        }

        return SampleCoastalPixel(x, y);
    }

    private static Color SampleLandPixel(int x, int y)
    {
        bool border = x == 0 || y == 0 || x == 31 || y == 31;
        if (border && (x + y) % 2 == 0)
        {
            return new Color(0.38f, 0.35f, 0.21f, 1f);
        }

        bool grain = (x * 7 + y * 13) % 19 == 0;
        return grain ? new Color(0.49f, 0.44f, 0.26f, 1f) : new Color(0.56f, 0.50f, 0.31f, 1f);
    }

    private static Color SamplePortPixel(int x, int y)
    {
        if (x >= 9 && x <= 22 && y >= 10 && y <= 22)
        {
            bool roof = y <= 15;
            if (x == 9 || x == 22 || y == 10 || y == 22)
            {
                return new Color(0.95f, 0.80f, 0.35f, 1f);
            }

            return roof ? new Color(0.76f, 0.28f, 0.20f, 1f) : new Color(0.87f, 0.78f, 0.58f, 1f);
        }

        return SampleCoastalPixel(x, y);
    }

    private static Color SampleFogPixel(int x, int y)
    {
        bool stipple = (x * 3 + y * 5) % 11 == 0;
        return stipple ? new Color(0.14f, 0.12f, 0.10f, 0.92f) : new Color(0.08f, 0.08f, 0.09f, 0.94f);
    }
}
