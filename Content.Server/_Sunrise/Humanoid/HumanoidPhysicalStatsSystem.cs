using Content.Shared._Sunrise.Humanoid.Events;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Humanoid;

public sealed class HumanoidPhysicalStatsSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidProfileLoadedEvent>(OnProfileLoaded);
    }

    private void OnProfileLoaded(Entity<HumanoidAppearanceComponent> ent, ref HumanoidProfileLoadedEvent args)
    {
        ApplyPhysicalStats(ent, args.Profile);
    }

    private void ApplyPhysicalStats(Entity<HumanoidAppearanceComponent> ent, HumanoidCharacterProfile profile)
    {
        if (!_proto.TryIndex(profile.Species, out var species))
            return;

        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        var weightMultiplier = GetWeightMultiplier(species, profile.Appearance.Width, profile.Appearance.Height);
        var physicalStats = EnsureComp<HumanoidPhysicalStatsComponent>(ent);

        foreach (var (fixtureId, fixture) in fixtures.Fixtures)
        {
            if (!physicalStats.BaseDensities.TryGetValue(fixtureId, out var baseDensity))
            {
                baseDensity = fixture.Density;
                physicalStats.BaseDensities[fixtureId] = baseDensity;
            }

            _physics.SetDensity(ent, fixtureId, fixture, baseDensity * weightMultiplier, manager: fixtures);
        }
    }

    private static float GetWeightMultiplier(SpeciesPrototype species, float width, float height)
    {
        var defaultWeight = GetProfileWeight(species, species.DefaultWidth, species.DefaultHeight);
        if (MathF.Abs(defaultWeight) < 0.0001f)
            return 1f;

        return MathF.Max(0.01f, GetProfileWeight(species, width, height) / defaultWeight);
    }

    private static float GetProfileWeight(SpeciesPrototype species, float width, float height)
    {
        return species.StandardWeight + species.StandardDensity * (width * height - 1f);
    }
}
