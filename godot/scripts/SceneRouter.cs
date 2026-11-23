using EdgeOfTheChart.Godot.WorldMap;
using EdgeOfTheChart.SimulationCore.Routing;
using Godot;

namespace EdgeOfTheChart.Godot;

public partial class SceneRouter : Control
{
    private const string TitleScenePath = "res://scenes/ui/TitleScreen.tscn";
    private const string WorldMapScenePath = "res://scenes/world_map/WorldMapScene.tscn";
    private const string PlaceholderScenePath = "res://scenes/placeholders/ScenePlaceholder.tscn";

    private GameSession? _session;
    private Node? _activeScene;

    public void Initialize(GameSession session)
    {
        _session = session;
    }

    public void NavigateTo(GameSceneId sceneId)
    {
        if (_session is null)
        {
            GD.PrintErr("[SceneRouter] GameSession이 초기화되지 않았습니다.");
            return;
        }

        if (_session.CurrentWorld is not null)
        {
            _session.CurrentWorld.CurrentScene = sceneId;
        }

        if (_activeScene is not null)
        {
            RemoveChild(_activeScene);
            _activeScene.QueueFree();
            _activeScene = null;
        }

        string targetPath = sceneId switch
        {
            GameSceneId.Title => TitleScenePath,
            GameSceneId.WorldMap => WorldMapScenePath,
            _ => PlaceholderScenePath
        };

        PackedScene? packed = GD.Load<PackedScene>(targetPath);
        if (packed is null)
        {
            GD.PrintErr($"[SceneRouter] 씬 리소스를 로드할 수 없습니다: {targetPath}");
            return;
        }

        Node instance = packed.Instantiate();
        AddChild(instance);
        _activeScene = instance;

        if (instance is TitleScreen titleScreen)
        {
            titleScreen.Bind(_session, this);
        }
        else if (instance is WorldMapController worldMap)
        {
            worldMap.Bind(_session, this);
        }
        else if (instance is ScenePlaceholder placeholder)
        {
            placeholder.Bind(_session, this, sceneId);
        }
    }
}
