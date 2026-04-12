using System;
using Content.Shared.Ghost;
using Content.Shared.Research.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private const int TargetPopulation = 45;
    private const int PopulationDeadzone = 4; // чтобы не обновлять при колебании онлайна
    private const float PopulationExponent = 0.5f;
    private const float MinPopulationModifier = 0.6f;
    private const float MaxPopulationModifier = 1.5f;

    public void ModifyServerResearchPoints(EntityUid uid, int points, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (points > 0)
            points = AdjustServerPointGainByPopulation(uid, points);

        ModifyServerPoints(uid, points, component);
    }

    private int AdjustServerPointGainByPopulation(EntityUid uid, int points)
    {
        var modifier = GetServerPointGainModifier(uid);
        if (modifier == 1f)
            return points;

        return Math.Max(0, (int) MathF.Round(points * modifier, MidpointRounding.AwayFromZero));
    }

    private float GetServerPointGainModifier(EntityUid uid)
    {
        if (!TryGetPopulationMap(uid, out var mapUid))
            return 1f;

        var population = CountResearchPopulation(mapUid);
        if (Math.Abs(population - TargetPopulation) <= PopulationDeadzone)
            return 1f;

        var ratio = TargetPopulation / (float) Math.Max(population, 1);
        var modifier = MathF.Pow(ratio, PopulationExponent);
        return Math.Clamp(modifier, MinPopulationModifier, MaxPopulationModifier);
    }

    private int CountResearchPopulation(EntityUid mapUid)
    {
        var population = 0;

        foreach (var session in _player.NetworkedSessions)
        {
            if (!IsResearchPopulationMember(session, mapUid))
                continue;

            population++;
        }

        return population;
    }

    private bool IsResearchPopulationMember(ICommonSession session, EntityUid mapUid)
    {
        if (session.Status != SessionStatus.InGame)
            return false;

        if (session.AttachedEntity is not { Valid: true } attached)
            return false;

        if (HasComp<GhostComponent>(attached))
            return false;

        if (!TryComp<TransformComponent>(attached, out var xform))
            return false;

        return xform.MapUid == mapUid;
    }

    // Подсчёт по карте, ибо 30 игроков могут быть на ивенте. 
    private bool TryGetPopulationMap(EntityUid uid, out EntityUid mapUid)
    {
        mapUid = EntityUid.Invalid;

        if (!TryComp<TransformComponent>(uid, out var xform))
            return false;

        if (xform.MapUid is not { } populationMap)
            return false;

        mapUid = populationMap;
        return mapUid != EntityUid.Invalid;
    }
}
