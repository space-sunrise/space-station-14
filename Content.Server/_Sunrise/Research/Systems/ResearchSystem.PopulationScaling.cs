using System;
using Content.Server._Sunrise.Research.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Ghost;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.Research.Prototypes;
using Content.Shared.Research.Components;
using Content.Shared.Roles;
using Content.Shared.SSDIndicator;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Systems;

/// <summary>
/// Модификатор получения РНД очков исходя от количества игроков. 
/// </summary>
public sealed partial class ResearchSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const int PopulationDeadzone = 4;
    private static readonly ProtoId<ResearchPopulationWeightsPrototype> PopulationWeightsPrototypeId = "SunriseResearchPopulationWeights";

    private int _targetPopulation = SunriseCCVars.ResearchPointScalingTargetPopulation.DefaultValue;
    private float _minPopulationModifier = SunriseCCVars.ResearchPointScalingMinModifier.DefaultValue;
    private float _maxPopulationModifier = SunriseCCVars.ResearchPointScalingMaxModifier.DefaultValue;
    private float _researchPointScalingMultiplier = SunriseCCVars.ResearchPointScalingMultiplier.DefaultValue;
    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<SSDIndicatorComponent> _ssdIndicatorQuery;

    private void InitializePopulationScaling()
    {
        _cfg.OnValueChanged(SunriseCCVars.ResearchPointScalingTargetPopulation, value => _targetPopulation = value, true);
        _cfg.OnValueChanged(SunriseCCVars.ResearchPointScalingMinModifier, value => _minPopulationModifier = value, true);
        _cfg.OnValueChanged(SunriseCCVars.ResearchPointScalingMaxModifier, value => _maxPopulationModifier = value, true);
        _cfg.OnValueChanged(SunriseCCVars.ResearchPointScalingMultiplier, value => _researchPointScalingMultiplier = value, true);

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _ssdIndicatorQuery = GetEntityQuery<SSDIndicatorComponent>();

        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, after: [typeof(SpawnPointSystem)]);
    }

    public void ModifyServerResearchPoints(EntityUid uid, int points, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (points > 0)
        {
            var modifier = GetServerPointGainModifier(uid);

            if (modifier != 1f)
                points = (int) MathF.Round(points * modifier, MidpointRounding.AwayFromZero);
        }

        ModifyServerPoints(uid, points, component);
    }

    private float GetServerPointGainModifier(EntityUid uid)
    {
        if (_researchPointScalingMultiplier <= 0f)
            return 1f;

        if (Transform(uid).MapUid is not { } mapUid)
            return 1f;

        var population = CountResearchPopulation(mapUid);

        if (MathF.Abs(population - _targetPopulation) <= PopulationDeadzone)
            return 1f;

        var ratio = _targetPopulation / MathF.Max(population, 1f);
        var baseModifier = MathF.Sqrt(ratio);
        var modifier = 1f + (baseModifier - 1f) * _researchPointScalingMultiplier;

        return Math.Clamp(modifier, _minPopulationModifier, _maxPopulationModifier);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult is not { } mob)
            return;

        TrySetResearchPopulation((mob, null), ev.Job);
    }

    private void TrySetResearchPopulation(Entity<ResearchPopulationComponent?> ent, ProtoId<JobPrototype>? jobId)
    {
        if (jobId == null || _ghostQuery.HasComp(ent))
        {
            RemComp<ResearchPopulationComponent>(ent);
            return;
        }

        var weight = GetResearchPopulationWeight(jobId);

        if (weight <= 0f)
        {
            RemComp<ResearchPopulationComponent>(ent);
            return;
        }

        EnsureComp<ResearchPopulationComponent>(ent).Weight = weight;
    }

    private float CountResearchPopulation(EntityUid mapUid)
    {
        var population = 0f;

        var query = EntityQueryEnumerator<ResearchPopulationComponent, ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var researchPopulation, out _, out var xform))
        {
            if (xform.MapUid != mapUid)
                continue;

            if (_ghostQuery.HasComp(uid))
                continue;

            if (_ssdIndicatorQuery.TryComp(uid, out var ssdIndicator) && ssdIndicator.IsSSD)
                continue;

            population += researchPopulation.Weight;
        }

        return population;
    }

    private float GetResearchPopulationWeight(ProtoId<JobPrototype>? jobId)
    {
        if (jobId == null)
            return 1f;

        if (!_prototype.TryIndex(PopulationWeightsPrototypeId, out var prototype))
            return 1f;

        return prototype.Weights.GetValueOrDefault(jobId.Value, 1f);
    }
}
