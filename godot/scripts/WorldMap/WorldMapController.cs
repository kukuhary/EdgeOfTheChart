using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.Routing;
using EdgeOfTheChart.SimulationCore.Sailing;
using EdgeOfTheChart.SimulationCore.Survival;
using EdgeOfTheChart.SimulationCore.World;
using Godot;

namespace EdgeOfTheChart.Godot.WorldMap;

public partial class WorldMapController : Node2D
{
    private const double GameHoursPerRealSecond = 5.0; // 1x 배속 기준 4.8초 = 24게임시간(1일)

    private GameSession? _session;
    private SceneRouter? _router;
    private bool _nodesReady;
    private bool _mapBuilt;

    private TileMapLayer _oceanTileMap = null!;
    private TileMapLayer _fogTileMap = null!;
    private Node2D _stormsRoot = null!;
    private Node2D _portLabelsRoot = null!;
    private Sprite2D _playerShip = null!;
    private Camera2D _camera = null!;

    private Label _lblDate = null!;
    private Label _lblCoords = null!;
    private Label _lblExplore = null!;
    private Label _lblWind = null!;
    private Label _lblSpeed = null!;
    private Label _lblShipHeader = null!;
    private Label _lblMorale = null!;
    private Label _lblAlerts = null!;
    private Label _lblSupplies = null!;
    private Label _lblVoyageLog = null!;
    private TextureRect _minimapRect = null!;

    private Button _btnSpeed0 = null!;
    private Button _btnSpeed1 = null!;
    private Button _btnSpeed2 = null!;
    private Button _btnSpeed4 = null!;
    private Button _btnRation50 = null!;
    private Button _btnRation75 = null!;
    private Button _btnRation100 = null!;
    private Button _btnReef = null!;
    private Button _btnDockPort = null!;

    private PanelContainer _portDockModal = null!;
    private Label _lblPortTitle = null!;
    private Label _lblPortInfo = null!;
    private Label _lblPortStatus = null!;

    private PanelContainer _rescueModal = null!;
    private Label _lblRescueTitle = null!;
    private Label _lblRescueDesc = null!;

    private readonly Dictionary<string, Label> _portLabels = new();
    private readonly Dictionary<string, Sprite2D> _stormSprites = new();
    private bool[] _renderedExplored = Array.Empty<bool>();
    private Image _minimapImage = null!;
    private ImageTexture _minimapTexture = null!;

    private double _hourAccumulator;
    private int _lastProcessedDayStamp = -1;
    private int _lastNonZeroSpeed = 1;
    private string? _adjacentPortId;
    private double _minimapTimer;
    private SailingVelocityResult _lastVelocity;

    public override void _Ready()
    {
        _oceanTileMap = GetNode<TileMapLayer>("OceanTileMap");
        _fogTileMap = GetNode<TileMapLayer>("FogTileMap");
        _stormsRoot = GetNode<Node2D>("StormsRoot");
        _portLabelsRoot = GetNode<Node2D>("PortLabelsRoot");
        _playerShip = GetNode<Sprite2D>("PlayerShip");
        _camera = GetNode<Camera2D>("PlayerShip/Camera2D");

        _lblDate = GetNode<Label>("%LblDate");
        _lblCoords = GetNode<Label>("%LblCoords");
        _lblExplore = GetNode<Label>("%LblExplore");
        _lblWind = GetNode<Label>("%LblWind");
        _lblSpeed = GetNode<Label>("%LblSpeed");
        _lblShipHeader = GetNode<Label>("%LblShipHeader");
        _lblMorale = GetNode<Label>("%LblMorale");
        _lblAlerts = GetNode<Label>("%LblAlerts");
        _lblSupplies = GetNode<Label>("%LblSupplies");
        _lblVoyageLog = GetNode<Label>("%LblVoyageLog");
        _minimapRect = GetNode<TextureRect>("%MinimapRect");

        _btnSpeed0 = GetNode<Button>("%BtnSpeed0");
        _btnSpeed1 = GetNode<Button>("%BtnSpeed1");
        _btnSpeed2 = GetNode<Button>("%BtnSpeed2");
        _btnSpeed4 = GetNode<Button>("%BtnSpeed4");
        _btnRation50 = GetNode<Button>("%BtnRation50");
        _btnRation75 = GetNode<Button>("%BtnRation75");
        _btnRation100 = GetNode<Button>("%BtnRation100");
        _btnReef = GetNode<Button>("%BtnReef");
        _btnDockPort = GetNode<Button>("%BtnDockPort");

        _portDockModal = GetNode<PanelContainer>("%PortDockModal");
        _lblPortTitle = GetNode<Label>("%LblPortTitle");
        _lblPortInfo = GetNode<Label>("%LblPortInfo");
        _lblPortStatus = GetNode<Label>("%LblPortStatus");

        _rescueModal = GetNode<PanelContainer>("%RescueModal");
        _lblRescueTitle = GetNode<Label>("%LblRescueTitle");
        _lblRescueDesc = GetNode<Label>("%LblRescueDesc");

        WireButtons();
        _nodesReady = true;

        if (_session is not null)
        {
            InitializeWorldPresentation();
        }
        else
        {
            CallDeferred(nameof(EnsureStandaloneInitialized));
        }
    }

    public void Bind(GameSession session, SceneRouter router)
    {
        _session = session;
        _router = router;
        if (_nodesReady && !_mapBuilt)
        {
            InitializeWorldPresentation();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_session?.CurrentWorld is not { } world || !_mapBuilt)
        {
            return;
        }

        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        if (_rescueModal.Visible)
        {
            if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.Space)
            {
                OnRescueConfirmPressed();
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (keyEvent.Keycode == Key.E)
        {
            if (_portDockModal.Visible)
            {
                ClosePortModal();
                GetViewport().SetInputAsHandled();
            }
            else if (_adjacentPortId is not null)
            {
                OpenPortModal(_adjacentPortId);
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (_portDockModal.Visible)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                ClosePortModal();
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        switch (keyEvent.Keycode)
        {
            case Key.Space:
                TogglePause();
                GetViewport().SetInputAsHandled();
                break;
            case Key.Key1:
                SetSpeed(1);
                GetViewport().SetInputAsHandled();
                break;
            case Key.Key2:
                SetSpeed(2);
                GetViewport().SetInputAsHandled();
                break;
            case Key.Key3:
                SetSpeed(4);
                GetViewport().SetInputAsHandled();
                break;
            case Key.R:
                ToggleReefSails();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    public override void _Process(double delta)
    {
        if (!_mapBuilt || _session?.CurrentWorld is not { } world ||
            _session.SeaMap is not { } seaMap ||
            _session.WindField is not { } windField ||
            _session.FogGrid is not { } fogGrid ||
            _session.Catalog is null)
        {
            return;
        }

        foreach (Sprite2D stormSprite in _stormSprites.Values)
        {
            stormSprite.Rotation += (float)(delta * 1.4);
        }

        if (_portDockModal.Visible || _rescueModal.Visible || world.Clock.SpeedMultiplier == 0)
        {
            return;
        }

        double deltaHours = delta * GameHoursPerRealSecond * world.Clock.SpeedMultiplier;
        _hourAccumulator += deltaHours;

        while (_hourAccumulator >= 1.0)
        {
            _hourAccumulator -= 1.0;
            world.Clock.AdvanceHours(1);

            int currentDayStamp = world.Clock.DayStamp;
            if (currentDayStamp != _lastProcessedDayStamp)
            {
                _lastProcessedDayStamp = currentDayStamp;
                windField.AdvanceDay(seaMap);
                SyncStormSprites();

                DailySurvivalReport report = SurvivalEngine.ProcessDay(
                    world, seaMap, windField, _session.Rng, _session.Catalog);

                if (report.RequiresRescue && report.RescueReason is RescueReason reason)
                {
                    RescueReport rescue = RescueService.ExecuteRescue(world, _session.Catalog, reason);
                    SailingPhysics.RevealFogAndSync(world, seaMap, fogGrid, _session.Catalog);
                    SyncFogAndLabels();
                    SyncShipTransform();
                    RefreshMinimap(force: true);
                    RefreshHud();
                    ShowRescueModal(rescue);
                    return;
                }
            }
        }

        Vector2 dir = ReadMovementVector();
        MovementStepResult step = SailingPhysics.StepMovement(
            world,
            _session.Catalog,
            seaMap,
            windField,
            fogGrid,
            dir.X,
            dir.Y,
            deltaHours,
            _session.Rng);

        _lastVelocity = step.Velocity;
        _adjacentPortId = step.AdjacentPortId;

        if (world.PlayerFleet.Flagship is { HullHp: <= 0 })
        {
            RescueReport rescue = RescueService.ExecuteRescue(world, _session.Catalog, RescueReason.Shipwreck);
            SailingPhysics.RevealFogAndSync(world, seaMap, fogGrid, _session.Catalog);
            SyncFogAndLabels();
            SyncShipTransform();
            RefreshMinimap(force: true);
            RefreshHud();
            ShowRescueModal(rescue);
            return;
        }

        SyncShipTransform();
        SyncFogAndLabels();

        _minimapTimer += delta;
        if (_minimapTimer >= 0.25)
        {
            _minimapTimer = 0.0;
            RefreshMinimap(force: false);
        }

        RefreshHud();
    }

    private void EnsureStandaloneInitialized()
    {
        if (_session is not null)
        {
            return;
        }

        var session = new GameSession();
        session.Name = "StandaloneGameSession";
        AddChild(session);
        session.InitializeNewSession("hero_diego");
        _session = session;

        GD.Print($"[WorldMap] 단독 실행 초기화 완료: {session.GetCatalogSummaryText().Replace("\n", " | ")}");
        InitializeWorldPresentation();
    }

    private void InitializeWorldPresentation()
    {
        if (_session?.CurrentWorld is not { } world ||
            _session.SeaMap is not { } seaMap ||
            _session.WindField is not { } windField ||
            _session.FogGrid is not { } fogGrid ||
            _session.Catalog is null)
        {
            return;
        }

        TileSet tileSet = ProceduralOceanAtlas.BuildOceanTileSet();
        _oceanTileMap.TileSet = tileSet;
        _fogTileMap.TileSet = tileSet;

        _oceanTileMap.Clear();
        _fogTileMap.Clear();

        for (int y = 0; y < seaMap.Height; y++)
        {
            for (int x = 0; x < seaMap.Width; x++)
            {
                int atlasCol = ProceduralOceanAtlas.ToAtlasIndex(seaMap.GetTile(x, y));
                _oceanTileMap.SetCell(new Vector2I(x, y), 0, new Vector2I(atlasCol, 0));
            }
        }

        _renderedExplored = new bool[seaMap.Width * seaMap.Height];
        Array.Fill(_renderedExplored, true); // 초기 전체 안개 세팅을 위해 반전 초기화
        for (int y = 0; y < seaMap.Height; y++)
        {
            for (int x = 0; x < seaMap.Width; x++)
            {
                int idx = y * seaMap.Width + x;
                bool explored = fogGrid.IsExplored(x, y);
                _renderedExplored[idx] = explored;
                if (!explored)
                {
                    _fogTileMap.SetCell(new Vector2I(x, y), 0, new Vector2I(ProceduralOceanAtlas.TileFog, 0));
                }
            }
        }

        BuildPortLabels(world);
        BuildStormSprites(windField);

        _playerShip.Texture = ProceduralOceanAtlas.BuildShipTexture();
        _camera.LimitLeft = 0;
        _camera.LimitTop = 0;
        _camera.LimitRight = seaMap.Width * ProceduralOceanAtlas.TileSize + 244;
        _camera.LimitBottom = seaMap.Height * ProceduralOceanAtlas.TileSize + 76;

        _minimapImage = Image.CreateEmpty(seaMap.Width, seaMap.Height, false, Image.Format.Rgba8);
        _minimapTexture = ImageTexture.CreateFromImage(_minimapImage);
        _minimapRect.Texture = _minimapTexture;

        _lastProcessedDayStamp = world.Clock.DayStamp;
        _lastNonZeroSpeed = world.Clock.SpeedMultiplier > 0 ? world.Clock.SpeedMultiplier : 1;

        SyncShipTransform();
        SyncFogAndLabels();
        SyncStormSprites();

        int tx = seaMap.NormalizeX((int)Math.Floor(world.ShipX));
        int ty = (int)Math.Floor(world.ShipY);
        _adjacentPortId = seaMap.FindAdjacentPortId(tx, ty);

        WindSample wind = windField.SampleWind(world.ShipX, world.ShipY, world.Clock);
        CurrentSample current = windField.SampleCurrent(world.ShipX, world.ShipY);
        if (world.PlayerFleet.Flagship is { } flagship && _session.Catalog.FindShip(flagship.TemplateId) is { } tpl)
        {
            _lastVelocity = SailingPhysics.CalculateVelocity(
                flagship, tpl, world.HeadingDegrees, wind, current, world.PlayerFleet.SailsReefed);
        }

        _mapBuilt = true;
        RefreshMinimap(force: true);
        RefreshHud();
    }

    private void BuildPortLabels(WorldState world)
    {
        foreach (Node child in _portLabelsRoot.GetChildren())
        {
            child.QueueFree();
        }
        _portLabels.Clear();

        foreach (PortState port in world.Ports)
        {
            if (port.Phase1X is not int px || port.Phase1Y is not int py)
            {
                continue;
            }

            var label = new Label
            {
                Text = port.NameKo,
                Position = new Vector2(px * ProceduralOceanAtlas.TileSize - 10, py * ProceduralOceanAtlas.TileSize - 18),
                Visible = false
            };
            label.AddThemeFontSizeOverride("font_size", 11);
            label.AddThemeColorOverride("font_color", new Color(0.98f, 0.88f, 0.58f, 1f));
            label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.08f, 0.14f, 1f));
            label.AddThemeConstantOverride("outline_size", 3);

            _portLabelsRoot.AddChild(label);
            _portLabels[port.Id] = label;
        }
    }

    private void BuildStormSprites(WindAndCurrentField windField)
    {
        foreach (Node child in _stormsRoot.GetChildren())
        {
            child.QueueFree();
        }
        _stormSprites.Clear();

        ImageTexture stormTex = ProceduralOceanAtlas.BuildStormTexture();
        foreach (StormCell storm in windField.StormCells)
        {
            var sprite = new Sprite2D
            {
                Texture = stormTex,
                Scale = new Vector2((float)storm.Radius, (float)storm.Radius)
            };
            _stormsRoot.AddChild(sprite);
            _stormSprites[storm.Id] = sprite;
        }
    }

    private void SyncStormSprites()
    {
        if (_session?.WindField is null)
        {
            return;
        }

        foreach (StormCell storm in _session.WindField.StormCells)
        {
            if (_stormSprites.TryGetValue(storm.Id, out Sprite2D? sprite))
            {
                sprite.Position = new Vector2(
                    (float)(storm.X * ProceduralOceanAtlas.TileSize),
                    (float)(storm.Y * ProceduralOceanAtlas.TileSize));
            }
        }
    }

    private void SyncShipTransform()
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        _playerShip.Position = new Vector2(
            (float)(world.ShipX * ProceduralOceanAtlas.TileSize),
            (float)(world.ShipY * ProceduralOceanAtlas.TileSize));
        _playerShip.Rotation = Mathf.DegToRad((float)world.HeadingDegrees);
    }

    private void SyncFogAndLabels()
    {
        if (_session?.SeaMap is not { } seaMap || _session.FogGrid is not { } fogGrid || _session.CurrentWorld is null)
        {
            return;
        }

        for (int y = 0; y < seaMap.Height; y++)
        {
            for (int x = 0; x < seaMap.Width; x++)
            {
                int idx = y * seaMap.Width + x;
                bool explored = fogGrid.IsExplored(x, y);
                if (explored && !_renderedExplored[idx])
                {
                    _renderedExplored[idx] = true;
                    _fogTileMap.EraseCell(new Vector2I(x, y));
                }
            }
        }

        foreach (PortState port in _session.CurrentWorld.Ports)
        {
            if (port.Phase1X is int px && port.Phase1Y is int py && _portLabels.TryGetValue(port.Id, out Label? label))
            {
                label.Visible = fogGrid.IsExplored(px, py);
            }
        }
    }

    private void RefreshMinimap(bool force)
    {
        if (_session?.SeaMap is not { } seaMap || _session.FogGrid is not { } fogGrid || _session.CurrentWorld is not { } world)
        {
            return;
        }

        for (int y = 0; y < seaMap.Height; y++)
        {
            for (int x = 0; x < seaMap.Width; x++)
            {
                if (!fogGrid.IsExplored(x, y))
                {
                    _minimapImage.SetPixel(x, y, new Color(0.08f, 0.08f, 0.09f, 1f));
                    continue;
                }

                Color c = seaMap.GetTile(x, y) switch
                {
                    SeaTileType.DeepSea => new Color(0.08f, 0.20f, 0.38f, 1f),
                    SeaTileType.CoastalSea => new Color(0.16f, 0.36f, 0.56f, 1f),
                    SeaTileType.Reef => new Color(0.75f, 0.85f, 0.90f, 1f),
                    SeaTileType.Land => new Color(0.54f, 0.48f, 0.30f, 1f),
                    SeaTileType.PortWater => new Color(0.95f, 0.80f, 0.30f, 1f),
                    _ => Colors.Black
                };
                _minimapImage.SetPixel(x, y, c);
            }
        }

        int sx = Math.Clamp((int)Math.Floor(world.ShipX), 0, seaMap.Width - 1);
        int sy = Math.Clamp((int)Math.Floor(world.ShipY), 0, seaMap.Height - 1);
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int px = Math.Clamp(sx + dx, 0, seaMap.Width - 1);
                int py = Math.Clamp(sy + dy, 0, seaMap.Height - 1);
                _minimapImage.SetPixel(px, py, new Color(1.0f, 0.25f, 0.25f, 1f));
            }
        }

        _minimapTexture.Update(_minimapImage);
    }

    private void RefreshHud()
    {
        if (_session?.CurrentWorld is not { } world ||
            _session.SeaMap is not { } seaMap ||
            _session.WindField is not { } windField ||
            _session.FogGrid is not { } fogGrid)
        {
            return;
        }

        Fleet fleet = world.PlayerFleet;
        Ship? flagship = fleet.Flagship;

        _lblDate.Text = world.Clock.FormattedDate;

        // 위도·경도 환산 (이베리아·지중해 근사 표기)
        double lonDeg = -18.0 + world.ShipX * 0.35;
        double latDeg = 46.5 - world.ShipY * 0.22;
        string lonDir = lonDeg >= 0 ? "E" : "W";
        _lblCoords.Text = $"{latDeg:0.0}°N {Math.Abs(lonDeg):0.0}°{lonDir}";

        double explorePct = fogGrid.CalculateExplorationPercent(seaMap);
        _lblExplore.Text = $"탐험 {explorePct:0.1}%";

        WindSample wind = windField.SampleWind(world.ShipX, world.ShipY, world.Clock);
        string stormTag = wind.IsInStorm ? " [폭풍!]" : string.Empty;
        _lblWind.Text = $"{wind.DirectionLabel} F{wind.Strength} · {_lastVelocity.WindAlignmentLabel}{stormTag}";
        _lblSpeed.Text = $"{_lastVelocity.EffectiveTilesPerDay:0.0} 타일/일";

        if (flagship is not null)
        {
            _lblShipHeader.Text = $"{flagship.Name} ({fleet.Ships.Count}/{Fleet.MaxShips}척) · 내구 {flagship.HullHp}/{flagship.MaxHullHp} · 선원 {flagship.CrewCount}";
        }

        _lblMorale.Text = $"사기 {fleet.Morale} · 반란위험 {fleet.MutinyRisk}% · 금화 {world.Gold:N0} G";

        var alerts = new List<string>();
        if (fleet.HasScurvy) alerts.Add("괴혈병 발병!");
        if (wind.IsInStorm) alerts.Add("폭풍권 진입!");
        if (fleet.Food <= 25) alerts.Add("식량 부족");
        if (fleet.Water <= 25) alerts.Add("식수 부족");
        if (world.Debt > 0) alerts.Add($"채무 {world.Debt:N0}G");
        _lblAlerts.Text = alerts.Count > 0 ? "경고: " + string.Join(" · ", alerts) : $"항해 {fleet.DaysAtSea}일째 (상태 양호)";

        _lblSupplies.Text = $"식량 {fleet.Food} · 식수 {fleet.Water} · 목재 {fleet.RepairWood} · 라임 {fleet.LimeJuice}";

        _btnReef.Text = fleet.SailsReefed ? "돛 펴기 (R)" : "돛 접기 (R)";

        if (_adjacentPortId is not null)
        {
            PortState? port = world.Ports.FirstOrDefault(p => p.Id == _adjacentPortId);
            _btnDockPort.Disabled = false;
            _btnDockPort.Text = $"입항: {port?.NameKo ?? _adjacentPortId} (E)";
        }
        else
        {
            _btnDockPort.Disabled = true;
            _btnDockPort.Text = "인접 항구 없음";
        }

        UpdateSpeedButtonLabels(world.Clock.SpeedMultiplier);
        UpdateRationButtonLabels(fleet.RationPercent);

        int count = world.VoyageLog.Count;
        int skip = Math.Max(0, count - 3);
        _lblVoyageLog.Text = count > 0 ? string.Join("\n", world.VoyageLog.Skip(skip)) : "항해 기록이 없습니다.";
    }

    private void UpdateSpeedButtonLabels(int speed)
    {
        _btnSpeed0.Text = speed == 0 ? "[⏸]" : "⏸";
        _btnSpeed1.Text = speed == 1 ? "[1×]" : "1×";
        _btnSpeed2.Text = speed == 2 ? "[2×]" : "2×";
        _btnSpeed4.Text = speed == 4 ? "[4×]" : "4×";
    }

    private void UpdateRationButtonLabels(int ration)
    {
        _btnRation50.Text = ration == 50 ? "[50%]" : "50%";
        _btnRation75.Text = ration == 75 ? "[75%]" : "75%";
        _btnRation100.Text = ration == 100 ? "[100%]" : "100%";
    }

    private static Vector2 ReadMovementVector()
    {
        float x = 0f;
        float y = 0f;

        if (Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A)) x -= 1f;
        if (Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.D)) x += 1f;
        if (Input.IsKeyPressed(Key.Up) || Input.IsKeyPressed(Key.W)) y -= 1f;
        if (Input.IsKeyPressed(Key.Down) || Input.IsKeyPressed(Key.S)) y += 1f;

        var v = new Vector2(x, y);
        return v.LengthSquared() > 1f ? v.Normalized() : v;
    }

    private void WireButtons()
    {
        _btnSpeed0.Pressed += () => SetSpeed(0);
        _btnSpeed1.Pressed += () => SetSpeed(1);
        _btnSpeed2.Pressed += () => SetSpeed(2);
        _btnSpeed4.Pressed += () => SetSpeed(4);

        _btnRation50.Pressed += () => SetRation(50);
        _btnRation75.Pressed += () => SetRation(75);
        _btnRation100.Pressed += () => SetRation(100);

        _btnReef.Pressed += ToggleReefSails;
        GetNode<Button>("%BtnForage").Pressed += OnForagePressed;
        GetNode<Button>("%BtnRepair").Pressed += OnEmergencyRepairPressed;
        GetNode<Button>("%BtnLime").Pressed += OnLimePressed;
        _btnDockPort.Pressed += OnDockPressed;
        GetNode<Button>("%BtnTitle").Pressed += () => _router?.NavigateTo(GameSceneId.Title);

        GetNode<Button>("%BtnBuyFood").Pressed += () => RunPortAction(w => SurvivalEngine.ResupplyAtPort(w, 50, 0, 0, 0));
        GetNode<Button>("%BtnBuyWater").Pressed += () => RunPortAction(w => SurvivalEngine.ResupplyAtPort(w, 0, 50, 0, 0));
        GetNode<Button>("%BtnBuyWood").Pressed += () => RunPortAction(w => SurvivalEngine.ResupplyAtPort(w, 0, 0, 10, 0));
        GetNode<Button>("%BtnBuyLime").Pressed += () => RunPortAction(w => SurvivalEngine.ResupplyAtPort(w, 0, 0, 0, 2));
        GetNode<Button>("%BtnPortRepair").Pressed += () => RunPortAction(SurvivalEngine.RepairAtPort);
        GetNode<Button>("%BtnPortRest").Pressed += () => RunPortAction(SurvivalEngine.RestCrewAtPort);
        GetNode<Button>("%BtnPortRecruit").Pressed += () => RunPortAction(w => SurvivalEngine.RecruitCrewAtPort(w, 10));
        GetNode<Button>("%BtnPortPayDebt").Pressed += () => RunPortAction(w => SurvivalEngine.RepayDebtAtPort(w, 500));
        GetNode<Button>("%BtnPortSave").Pressed += OnPortSavePressed;
        GetNode<Button>("%BtnDepart").Pressed += ClosePortModal;

        GetNode<Button>("%BtnRescueConfirm").Pressed += OnRescueConfirmPressed;
    }

    private void TogglePause()
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        if (world.Clock.SpeedMultiplier == 0)
        {
            SetSpeed(_lastNonZeroSpeed > 0 ? _lastNonZeroSpeed : 1);
        }
        else
        {
            SetSpeed(0);
        }
    }

    private void SetSpeed(int speed)
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        world.Clock.SetSpeed(speed);
        if (speed > 0)
        {
            _lastNonZeroSpeed = speed;
        }
        RefreshHud();
    }

    private void SetRation(int percent)
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        world.PlayerFleet.SetRationPercent(percent);
        world.AddVoyageLog($"선원 배급률을 {percent}%로 변경했습니다.");
        RefreshHud();
    }

    private void ToggleReefSails()
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        world.PlayerFleet.SailsReefed = !world.PlayerFleet.SailsReefed;
        world.AddVoyageLog(world.PlayerFleet.SailsReefed
            ? "돛을 접고 감속 항해합니다 (폭풍·암초 피해 경감)."
            : "모든 돛을 활짝 펼쳤습니다.");
        RefreshHud();
    }

    private void OnForagePressed()
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        (bool ok, string msg) = SurvivalEngine.PerformEmergencyForage(world, _session.Rng);
        if (!ok)
        {
            world.AddVoyageLog(msg);
        }
        RefreshHud();
    }

    private void OnEmergencyRepairPressed()
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        (bool ok, string msg) = SurvivalEngine.PerformEmergencyRepair(world);
        if (!ok)
        {
            world.AddVoyageLog(msg);
        }
        RefreshHud();
    }

    private void OnLimePressed()
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        (bool ok, string msg) = SurvivalEngine.UseLimeJuice(world);
        if (!ok)
        {
            world.AddVoyageLog(msg);
        }
        RefreshHud();
    }

    private void OnDockPressed()
    {
        if (_adjacentPortId is not null)
        {
            OpenPortModal(_adjacentPortId);
        }
    }

    private void OpenPortModal(string portId)
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        PortState? port = world.Ports.FirstOrDefault(p => p.Id == portId);
        if (port is null)
        {
            return;
        }

        world.CurrentPortId = port.Id;
        world.PlayerFleet.DaysAtSea = 0;
        _lblPortTitle.Text = $"{port.NameKo} 부두";
        _lblPortStatus.Text = "보급품 구매, 선체 수리, 선원 휴식 및 채무 상환을 진행할 수 있습니다.";
        RefreshPortModalInfo(port, world);
        _portDockModal.Visible = true;
        RefreshHud();
    }

    private void RefreshPortModalInfo(PortState port, WorldState world)
    {
        string factionName = world.Factions
            .FirstOrDefault(f => f.Id == port.OwnerFactionId)?.NameKo ?? port.OwnerFactionId;
        Ship? flagship = world.PlayerFleet.Flagship;
        _lblPortInfo.Text =
            $"소속: {factionName} | 문화권: {port.Culture} | 상업 {port.CommerceDev} / 공업 {port.IndustryDev}\n" +
            $"소지금: {world.Gold:N0} G | 채무: {world.Debt:N0} G | 기함 내구: {flagship?.HullHp}/{flagship?.MaxHullHp} | 선원: {flagship?.CrewCount}/{flagship?.CabinCrewSlots}";
    }

    private void RunPortAction(Func<WorldState, (bool Success, string Message)> action)
    {
        if (_session?.CurrentWorld is not { } world)
        {
            return;
        }

        (_, string msg) = action(world);
        _lblPortStatus.Text = msg;

        PortState? port = world.Ports.FirstOrDefault(p => p.Id == world.CurrentPortId);
        if (port is not null)
        {
            RefreshPortModalInfo(port, world);
        }
        RefreshHud();
    }

    private void OnPortSavePressed()
    {
        if (_session is null)
        {
            return;
        }

        _lblPortStatus.Text = _session.SaveToUserSlot("phase1_quicksave.json");
    }

    private void ClosePortModal()
    {
        _portDockModal.Visible = false;
        RefreshHud();
    }

    private void ShowRescueModal(RescueReport rescue)
    {
        _portDockModal.Visible = false;
        _lblRescueTitle.Text = $"해상 조난 구조 — {rescue.RescuePortName} 귀환";
        _lblRescueDesc.Text =
            $"{rescue.SummaryMessage}\n\n" +
            $"누적 채무: {rescue.NewTotalDebt:N0} G\n" +
            "동료 항해사와 탐험 기록·스토리 플래그는 모두 보존되었습니다.";
        _rescueModal.Visible = true;
    }

    private void OnRescueConfirmPressed()
    {
        _rescueModal.Visible = false;
        RefreshHud();
    }
}
