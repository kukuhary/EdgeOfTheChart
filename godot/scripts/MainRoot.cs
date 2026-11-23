using EdgeOfTheChart.SimulationCore.Routing;
using Godot;

namespace EdgeOfTheChart.Godot;

public partial class MainRoot : Node
{
    private GameSession? _session;
    private SceneRouter? _router;

    public override void _Ready()
    {
        _session = GetNode<GameSession>("GameSession");
        _router = GetNode<SceneRouter>("SceneRouter");

        try
        {
            _session.InitializeNewSession("hero_diego");
            GD.Print($"[MainRoot] {_session.GetCatalogSummaryText().Replace("\n", " | ")}");

            _router.Initialize(_session);
            _router.NavigateTo(GameSceneId.Title);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MainRoot] 초기화 실패: {ex}");
        }
    }
}
