using System;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private const int TargetPopulation = 45;
    private const int PopulationDeadzone = 4;
    private const float PopulationExponent = 0.5f;
    private const float MinPopulationModifier = 0.6f;
    private const float MaxPopulationModifier = 1.5f;

    private int AdjustServerPointGainByPopulation(EntityUid uid, int points)
    {
        var modifier = GetServerPointGainModifier(uid);
        if (modifier == 1f)
            return points;

        return Math.Max(0, (int) MathF.Round(points * modifier, MidpointRounding.AwayFromZero));
    }

    private float GetServerPointGainModifier(EntityUid uid)
    {
        var population = CountResearchPopulation(uid);
        if (Math.Abs(population - TargetPopulation) <= PopulationDeadzone)
            return 1f;

        var ratio = TargetPopulation / (float) Math.Max(population, 1);
        var modifier = MathF.Pow(ratio, PopulationExponent);
        return Math.Clamp(modifier, MinPopulationModifier, MaxPopulationModifier);
    }

    private int CountResearchPopulation(EntityUid uid)
    {
        var station = _stationSystem.GetOwningStation(uid);
        var population = 0;

        foreach (var session in _player.NetworkedSessions)
        {
            if (!IsResearchPopulationMember(session, station))
                continue;

            population++;
        }

        return Math.Max(population, 1);
    }

    private bool IsResearchPopulationMember(ICommonSession session, EntityUid? station)
    {
        if (session.Status != SessionStatus.InGame)
            return false;

        if (session.AttachedEntity is not { Valid: true } attached)
            return false;

        if (HasComp<GhostComponent>(attached))
            return false;

        return station == null || _stationSystem.GetOwningStation(attached) == station;
    }
}
