using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.Survival;
using EdgeOfTheChart.SimulationCore.World;

namespace EdgeOfTheChart.SimulationCore.Sailing;

public readonly record struct SailingVelocityResult(
    double EffectiveTilesPerDay,
    double DeltaX,
    double DeltaY,
    string WindAlignmentLabel,
    double RelativeWindAngleDeg);

public readonly record struct MovementStepResult(
    bool Moved,
    bool HitLand,
    bool HitReef,
    int ReefDamage,
    string? AdjacentPortId,
    SailingVelocityResult Velocity);

public static class SailingPhysics
{
    public static SailingVelocityResult CalculateVelocity(
        Ship flagship,
        ShipTemplateData template,
        double headingDegrees,
        WindSample wind,
        CurrentSample current,
        bool sailsReefed,
        double headwindReductionBonus = 0.0)
    {
        ArgumentNullException.ThrowIfNull(flagship);
        ArgumentNullException.ThrowIfNull(template);

        double relAngle = ComputeSmallestAngleDifference(headingDegrees, wind.DirectionDegrees);
        string alignmentLabel;
        double rigFactor;

        if (wind.IsCalmZone || wind.Strength <= 0)
        {
            alignmentLabel = "무풍";
            rigFactor = template.OarAssisted ? 0.55 : 0.15;
        }
        else if (relAngle <= 45.0)
        {
            alignmentLabel = "순풍";
            rigFactor = flagship.Rig switch
            {
                RigType.Square => 1.35,
                RigType.Lateen => 1.05,
                RigType.Hybrid => 1.20,
                _ => 1.0
            };
        }
        else if (relAngle < 135.0)
        {
            alignmentLabel = "측풍";
            rigFactor = flagship.Rig switch
            {
                RigType.Square => 0.95,
                RigType.Lateen => 1.00,
                RigType.Hybrid => 1.00,
                _ => 1.0
            };
        }
        else
        {
            alignmentLabel = "역풍";
            double baseHeadwind = flagship.Rig switch
            {
                RigType.Square => 0.25,
                RigType.Lateen => 0.65,
                RigType.Hybrid => 0.80,
                _ => 0.50
            };
            rigFactor = Math.Clamp(baseHeadwind + (1.0 - baseHeadwind) * headwindReductionBonus, 0.20, 0.95);
        }

        double strengthFactor = wind.Strength switch
        {
            <= 0 => 1.0,
            1 => 0.85,
            2 => 1.00,
            _ => 1.18
        };

        double modifier = 1.0;
        if (sailsReefed)
        {
            modifier *= 0.35;
        }

        if (flagship.CrewCount < flagship.MinCrew)
        {
            modifier *= 0.35;
        }

        if (flagship.MaxHullHp > 0 && flagship.HullHp * 2 < flagship.MaxHullHp)
        {
            modifier *= 0.75;
        }

        double sailSpeed = Math.Max(0.15, template.BaseSpeed * 0.85 * rigFactor * strengthFactor * modifier);
        double rad = headingDegrees * Math.PI / 180.0;
        double vx = Math.Cos(rad) * sailSpeed + current.Dx;
        double vy = Math.Sin(rad) * sailSpeed + current.Dy;

        double totalSpeed = Math.Sqrt(vx * vx + vy * vy);
        if (totalSpeed < 0.15)
        {
            vx = Math.Cos(rad) * 0.15;
            vy = Math.Sin(rad) * 0.15;
            totalSpeed = 0.15;
        }

        return new SailingVelocityResult(totalSpeed, vx, vy, alignmentLabel, relAngle);
    }

    public static MovementStepResult StepMovement(
        WorldState state,
        MasterDataCatalog catalog,
        SeaMapGrid map,
        WindAndCurrentField field,
        FogOfWarGrid fog,
        double inputX,
        double inputY,
        double deltaHours,
        IRandomProvider rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(fog);
        ArgumentNullException.ThrowIfNull(rng);

        Ship flagship = state.PlayerFleet.Flagship
            ?? throw new InvalidOperationException("기함이 존재하지 않습니다.");

        ShipTemplateData template = catalog.FindShip(flagship.TemplateId)
            ?? throw new InvalidDataException($"선박 템플릿을 찾을 수 없습니다: {flagship.TemplateId}");

        int curTileX = map.NormalizeX((int)Math.Floor(state.ShipX));
        int curTileY = (int)Math.Floor(state.ShipY);

        WindSample wind = field.SampleWind(state.ShipX, state.ShipY, state.Clock);
        CurrentSample current = field.SampleCurrent(state.ShipX, state.ShipY);
        double headwindBonus = ComputeHeadwindBonus(flagship, catalog);

        double inputMag = Math.Sqrt(inputX * inputX + inputY * inputY);
        if (inputMag < 0.01 || deltaHours <= 0.0)
        {
            SailingVelocityResult idleVel = CalculateVelocity(
                flagship, template, state.HeadingDegrees, wind, current, state.PlayerFleet.SailsReefed, headwindBonus);
            RevealFogAndSync(state, map, fog, catalog);
            return new MovementStepResult(
                Moved: false,
                HitLand: false,
                HitReef: false,
                ReefDamage: 0,
                AdjacentPortId: map.FindAdjacentPortId(curTileX, curTileY),
                Velocity: idleVel);
        }

        double heading = WindAndCurrentField.NormalizeAngle(Math.Atan2(inputY, inputX) * 180.0 / Math.PI);
        state.HeadingDegrees = heading;

        SailingVelocityResult vel = CalculateVelocity(
            flagship, template, heading, wind, current, state.PlayerFleet.SailsReefed, headwindBonus);

        double dtDays = deltaHours / 24.0;
        double targetX = state.ShipX + vel.DeltaX * dtDays;
        double targetY = state.ShipY + vel.DeltaY * dtDays;

        bool hitLand = false;

        int nextTileX = (int)Math.Floor(targetX);
        int curYForX = (int)Math.Floor(state.ShipY);
        if (map.IsSailable(nextTileX, curYForX))
        {
            state.ShipX = map.WrapHorizontal ? WrapContinuousX(targetX, map.Width) : targetX;
        }
        else
        {
            hitLand = true;
        }

        int curXForY = (int)Math.Floor(state.ShipX);
        int nextTileY = (int)Math.Floor(targetY);
        if (map.IsSailable(curXForY, nextTileY))
        {
            state.ShipY = targetY;
        }
        else
        {
            hitLand = true;
        }

        int newTileX = map.NormalizeX((int)Math.Floor(state.ShipX));
        int newTileY = (int)Math.Floor(state.ShipY);
        bool moved = Math.Abs(vel.DeltaX) > 1e-6 || Math.Abs(vel.DeltaY) > 1e-6;

        bool hitReef = false;
        int reefDamage = 0;
        if ((newTileX != curTileX || newTileY != curTileY) &&
            map.GetTile(newTileX, newTileY) == SeaTileType.Reef &&
            !state.PlayerFleet.SailsReefed)
        {
            hitReef = true;
            reefDamage = rng.NextInt(8, 17);
            flagship.HullHp = Math.Max(0, flagship.HullHp - reefDamage);
            state.AddVoyageLog($"암초 충돌! 선체 내구도 -{reefDamage} (남은 내구: {flagship.HullHp}/{flagship.MaxHullHp})");
        }

        RevealFogAndSync(state, map, fog, catalog);

        return new MovementStepResult(
            Moved: moved && !hitLand || (newTileX != curTileX || newTileY != curTileY),
            HitLand: hitLand,
            HitReef: hitReef,
            ReefDamage: reefDamage,
            AdjacentPortId: map.FindAdjacentPortId(newTileX, newTileY),
            Velocity: vel);
    }

    public static void RevealFogAndSync(
        WorldState state,
        SeaMapGrid map,
        FogOfWarGrid fog,
        MasterDataCatalog catalog)
    {
        int radius = 3;
        bool hasNavigator = state.Officers.Any(o => o.AssignedRole == Officers.OfficerRole.Navigator);
        if (hasNavigator)
        {
            radius += 1;
        }

        Ship? flagship = state.PlayerFleet.Flagship;
        if (flagship is not null)
        {
            foreach (string relicId in flagship.RelicIds)
            {
                RelicOrFigureheadData? item = catalog.RelicsAndFigureheads
                    .FirstOrDefault(r => string.Equals(r.Id, relicId, StringComparison.Ordinal));
                if (item is null)
                {
                    continue;
                }

                foreach (StatModifierData mod in item.Modifiers)
                {
                    if (string.Equals(mod.Stat, "fog_reveal_radius_bonus", StringComparison.Ordinal))
                    {
                        radius += (int)Math.Round(mod.Value);
                    }
                }
            }
        }

        int tx = map.NormalizeX((int)Math.Floor(state.ShipX));
        int ty = (int)Math.Floor(state.ShipY);
        int newlyRevealed = fog.RevealAround(tx, ty, radius, map.WrapHorizontal);
        if (newlyRevealed > 0 || state.ExploredTileIndices.Count == 0)
        {
            state.ExploredTileIndices = fog.ToIndices();
        }
    }

    private static double ComputeHeadwindBonus(Ship flagship, MasterDataCatalog catalog)
    {
        if (string.IsNullOrWhiteSpace(flagship.FigureheadId))
        {
            return 0.0;
        }

        RelicOrFigureheadData? fig = catalog.RelicsAndFigureheads
            .FirstOrDefault(r => string.Equals(r.Id, flagship.FigureheadId, StringComparison.Ordinal));
        if (fig is null)
        {
            return 0.0;
        }

        double bonus = 0.0;
        foreach (StatModifierData mod in fig.Modifiers)
        {
            if (string.Equals(mod.Stat, "headwind_penalty_reduction", StringComparison.Ordinal))
            {
                bonus += mod.Value;
            }
        }

        return Math.Clamp(bonus, 0.0, 0.9);
    }

    private static double ComputeSmallestAngleDifference(double aDeg, double bDeg)
    {
        double diff = Math.Abs(WindAndCurrentField.NormalizeAngle(aDeg) - WindAndCurrentField.NormalizeAngle(bDeg));
        return diff > 180.0 ? 360.0 - diff : diff;
    }

    private static double WrapContinuousX(double x, int width)
    {
        double m = x % width;
        return m < 0 ? m + width : m;
    }
}
