using EdgeOfTheChart.SimulationCore.Routing;
using Godot;

namespace EdgeOfTheChart.Godot;

public partial class TitleScreen : Control
{
    private GameSession? _session;
    private SceneRouter? _router;
    private Label? _summaryLabel;
    private Label? _statusLabel;

    public override void _Ready()
    {
        _summaryLabel = GetNode<Label>("%SummaryLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");

        GetNode<Button>("%BtnWorldMap").Pressed += () => Navigate(GameSceneId.WorldMap);
        GetNode<Button>("%BtnPort").Pressed += () => Navigate(GameSceneId.Port);
        GetNode<Button>("%BtnNavalBattle").Pressed += () => Navigate(GameSceneId.NavalBattle);
        GetNode<Button>("%BtnLeviathan").Pressed += () => Navigate(GameSceneId.LeviathanBattle);
        GetNode<Button>("%BtnPortSiege").Pressed += () => Navigate(GameSceneId.PortSiege);
        GetNode<Button>("%BtnSaveTest").Pressed += OnSaveTestPressed;
        GetNode<Button>("%BtnQuit").Pressed += () => GetTree().Quit();

        RefreshLabels();
    }

    public void Bind(GameSession session, SceneRouter router)
    {
        _session = session;
        _router = router;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (_summaryLabel is null || _session is null)
        {
            return;
        }

        _summaryLabel.Text = _session.GetCatalogSummaryText();
    }

    private void Navigate(GameSceneId sceneId)
    {
        _router?.NavigateTo(sceneId);
    }

    private void OnSaveTestPressed()
    {
        if (_session is null || _statusLabel is null)
        {
            return;
        }

        try
        {
            _statusLabel.Text = _session.RunSaveLoadSelfTest();
            RefreshLabels();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"오류: {ex.Message}";
        }
    }
}
