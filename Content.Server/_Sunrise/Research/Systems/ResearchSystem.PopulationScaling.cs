using System;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Humanoid;
using Content.Shared.Research.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Research.Systems;

/// <summary>
/// Модификатор получения РНД очков исходя от количества игроков. 
/// </summary>
public sealed partial class ResearchSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const int TargetPopulation = 44;
    private const int PopulationDeadzone = 4;
    private const float MinPopulationModifier = 0.6f;
    private const float MaxPopulationModifier = 1.5f;

    private float _researchPointScalingMultiplier = SunriseCCVars.ResearchPointScalingMultiplier.DefaultValue;

    private void InitializePopulationScaling()
    {
        _researchPointScalingMultiplier = _cfg.GetCVar(SunriseCCVars.ResearchPointScalingMultiplier);
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

        if (Math.Abs(population - TargetPopulation) <= PopulationDeadzone)
            return 1f;

        var ratio = TargetPopulation / (float)Math.Max(population, 1);
        var baseModifier = MathF.Sqrt(ratio);
        var modifier = 1f + (baseModifier - 1f) * _researchPointScalingMultiplier;

        return Math.Clamp(modifier, MinPopulationModifier, MaxPopulationModifier);
    }
    private int CountResearchPopulation(EntityUid mapUid)
    {
        int population = 0;

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

        // Проверка игрока и сервера на одной карте
        if (Transform(attached).MapUid != mapUid)
            return false;

        // Не считаем игроков мышек, тараканов и т.д.
        if (!HasComp<HumanoidAppearanceComponent>(attached))
            return false;

        return true;
    }
}
