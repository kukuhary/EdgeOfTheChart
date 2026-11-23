using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.Routing;
using Godot;

namespace EdgeOfTheChart.Godot;

public partial class ScenePlaceholder : Control
{
    private GameSession? _session;
    private SceneRouter? _router;
    private GameSceneId _sceneId;
    private Label? _sceneTitleLabel;
    private Label? _sceneDescLabel;
    private Label? _fleetSummaryLabel;

    public override void _Ready()
    {
        _sceneTitleLabel = GetNode<Label>("%SceneTitleLabel");
        _sceneDescLabel = GetNode<Label>("%SceneDescLabel");
        _fleetSummaryLabel = GetNode<Label>("%FleetSummaryLabel");

        GetNode<Button>("%BtnBackToTitle").Pressed += () => _router?.NavigateTo(GameSceneId.Title);
        RefreshContent();
    }

    public void Bind(GameSession session, SceneRouter router, GameSceneId sceneId)
    {
        _session = session;
        _router = router;
        _sceneId = sceneId;
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (_sceneTitleLabel is null || _sceneDescLabel is null || _fleetSummaryLabel is null)
        {
            return;
        }

        (string title, string desc) = _sceneId switch
        {
            GameSceneId.WorldMap => ("대양 항해 씬 (WorldMapScene)", "Phase 1 예정: 32×32 실시간-틱 항해, 풍향/해류 물리, 안개 해제 및 생존 시스템"),
            GameSceneId.Port => ("2D 항구 도보 탐색 씬 (PortScene)", "Phase 2 예정: 낮/밤 사이클, 9대 항구 시설, 문화권 모듈 템플릿 및 야간 암시장"),
            GameSceneId.NavalBattle => ("함대 전술전 씬 (NavalBattleScene)", "Phase 3 예정: 8방향 사각 그리드 턴제, 최대 3척 함대, 요함 AI 명령 5종 및 유령선"),
            GameSceneId.LeviathanBattle => ("거대 괴수전 씬 (LeviathanBattleScene)", "Phase 5 예정: 멀티타일 괴수, 부위 파괴, 위험 구역 예고 회피 및 작살포 공략"),
            GameSceneId.PortSiege => ("3단계 항구 공성전 씬 (PortSiegeScene)", "Phase 5 예정: 외항 포대 돌파 → 내항 상륙·성채 제압 → 총독 본부 백병 결전"),
            _ => ("미정의 씬", "Phase 0 플레이스홀더 화면입니다.")
        };

        _sceneTitleLabel.Text = title;
        _sceneDescLabel.Text = desc;

        if (_session?.CurrentWorld is { } world)
        {
            string flagshipName = world.PlayerFleet.Flagship?.Name ?? "없음";
            _fleetSummaryLabel.Text =
                $"기함: {flagshipName} | 보유 함대: {world.PlayerFleet.Ships.Count}/{Fleet.MaxShips}척 | 날짜: {world.Clock.FormattedDate} | 소지금: {world.Gold:N0} G";
        }
    }
}
