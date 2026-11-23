using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.Sailing;
using EdgeOfTheChart.SimulationCore.Survival;
using EdgeOfTheChart.SimulationCore.World;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

internal sealed class StubRandomProvider : IRandomProvider
{
    private readonly double _nextDouble;
    private readonly int _nextInt;

    public StubRandomProvider(double nextDouble = 0.99, int nextInt = 15)
    {
        _nextDouble = nextDouble;
        _nextInt = nextInt;
    }

    public double NextDouble() => _nextDouble;

    public int NextInt(int minInclusive, int maxExclusive) =>
        Math.Clamp(_nextInt, minInclusive, maxExclusive - 1);
}

public sealed class SailingPhysicsTests
{
    [Fact]
    public void CalculateVelocity_DifferentiatesSquareLateenHybridAcrossWindAngles()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        ShipTemplateData squareTpl = catalog.FindShip("ship_early_galleon")!;
        ShipTemplateData lateenTpl = catalog.FindShip("ship_caravel_latina")!;
        ShipTemplateData hybridTpl = catalog.FindShip("ship_joseon_haeryong_hybrid")!;

        Ship squareShip = Ship.FromTemplate(squareTpl, "sq", "갤리온");
        Ship lateenShip = Ship.FromTemplate(lateenTpl, "lt", "카라벨");
        Ship hybridShip = Ship.FromTemplate(hybridTpl, "hb", "해룡");

        var eastWind = new WindSample(DirectionDegrees: 0.0, Strength: 2, IsCalmZone: false, IsInStorm: false);
        var noCurrent = new CurrentSample(0.0, 0.0);

        // 순풍(동쪽 0°) vs 역풍(서쪽 180°)
        SailingVelocityResult sqTailwind = SailingPhysics.CalculateVelocity(squareShip, squareTpl, 0.0, eastWind, noCurrent, false);
        SailingVelocityResult sqHeadwind = SailingPhysics.CalculateVelocity(squareShip, squareTpl, 180.0, eastWind, noCurrent, false);

        SailingVelocityResult ltHeadwind = SailingPhysics.CalculateVelocity(lateenShip, lateenTpl, 180.0, eastWind, noCurrent, false);
        SailingVelocityResult hbHeadwind = SailingPhysics.CalculateVelocity(hybridShip, hybridTpl, 180.0, eastWind, noCurrent, false);

        Assert.Equal("순풍", sqTailwind.WindAlignmentLabel);
        Assert.Equal("역풍", sqHeadwind.WindAlignmentLabel);
        Assert.True(sqTailwind.EffectiveTilesPerDay > sqHeadwind.EffectiveTilesPerDay * 4.0);
        Assert.True(ltHeadwind.EffectiveTilesPerDay > sqHeadwind.EffectiveTilesPerDay);
        Assert.True(hbHeadwind.EffectiveTilesPerDay > sqHeadwind.EffectiveTilesPerDay);
    }

    [Fact]
    public void CalculateVelocity_AppliesOarCalmBonusAndReefCrewDamagePenalties()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        ShipTemplateData lateenTpl = catalog.FindShip("ship_caravel_latina")!;
        ShipTemplateData oarTpl = catalog.FindShip("ship_joseon_haeryong_hybrid")!;

        Ship lateenShip = Ship.FromTemplate(lateenTpl, "lt", "카라벨");
        Ship oarShip = Ship.FromTemplate(oarTpl, "hb", "해룡");

        var calmWind = new WindSample(0.0, 0, IsCalmZone: true, IsInStorm: false);
        var noCurrent = new CurrentSample(0.0, 0.0);

        SailingVelocityResult calmLateen = SailingPhysics.CalculateVelocity(lateenShip, lateenTpl, 0.0, calmWind, noCurrent, false);
        SailingVelocityResult calmOar = SailingPhysics.CalculateVelocity(oarShip, oarTpl, 0.0, calmWind, noCurrent, false);
        Assert.Equal("무풍", calmOar.WindAlignmentLabel);
        Assert.True(calmOar.EffectiveTilesPerDay > calmLateen.EffectiveTilesPerDay * 2.0);

        var normalWind = new WindSample(0.0, 2, false, false);
        double normalSpeed = SailingPhysics.CalculateVelocity(lateenShip, lateenTpl, 0.0, normalWind, noCurrent, false).EffectiveTilesPerDay;
        double reefSpeed = SailingPhysics.CalculateVelocity(lateenShip, lateenTpl, 0.0, normalWind, noCurrent, true).EffectiveTilesPerDay;
        Assert.InRange(reefSpeed / normalSpeed, 0.34, 0.36);

        lateenShip.CrewCount = lateenShip.MinCrew - 1;
        lateenShip.HullHp = lateenShip.MaxHullHp / 3;
        double crippledSpeed = SailingPhysics.CalculateVelocity(lateenShip, lateenTpl, 0.0, normalWind, noCurrent, false).EffectiveTilesPerDay;
        Assert.True(crippledSpeed < normalSpeed * 0.30);
    }

    [Fact]
    public void StepMovement_AvoidsLandAndAppliesReefDamageOnlyWhenUnreefed()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState state = WorldState.CreateNewGameSample(catalog, "hero_diego");
        var map = new SeaMapGrid(20, 20, wrapHorizontal: false);
        Array.Fill(map.Tiles, SeaTileType.DeepSea);
        map.SetTile(12, 10, SeaTileType.Reef);
        map.SetTile(13, 10, SeaTileType.Land);

        var field = new WindAndCurrentField(new List<StormCell>());
        var fog = new FogOfWarGrid(20, 20);
        var rng = new StubRandomProvider(nextDouble: 0.0, nextInt: 12);

        // 암초 타일 (12, 10) 직전으로 이동 후 암초 진입
        state.ShipX = 11.8;
        state.ShipY = 10.5;
        int hpBefore = state.PlayerFleet.Flagship!.HullHp;
        MovementStepResult reefHit = SailingPhysics.StepMovement(state, catalog, map, field, fog, 1.0, 0.0, 2.0, rng);

        Assert.True(reefHit.HitReef);
        Assert.Equal(12, reefHit.ReefDamage);
        Assert.Equal(hpBefore - 12, state.PlayerFleet.Flagship.HullHp);

        // 돛을 접고 암초 재진입 시 피해 없음
        state.ShipX = 11.8;
        state.PlayerFleet.SailsReefed = true;
        int hpAfterReef = state.PlayerFleet.Flagship.HullHp;
        MovementStepResult reefSafe = SailingPhysics.StepMovement(state, catalog, map, field, fog, 1.0, 0.0, 4.0, rng);
        Assert.False(reefSafe.HitReef);
        Assert.Equal(hpAfterReef, state.PlayerFleet.Flagship.HullHp);

        // 육지 (13, 10) 진입 시도 시 차단
        state.ShipX = 12.9;
        MovementStepResult landHit = SailingPhysics.StepMovement(state, catalog, map, field, fog, 1.0, 0.0, 4.0, rng);
        Assert.True(landHit.HitLand);
        Assert.True(state.ShipX < 13.0);
    }
}
