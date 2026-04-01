using System.Linq;
using Content.Server.Humanoid;
using RandomSpeciesChange = Content.Shared._Sunrise.EntityEffects.Effects.RandomSpeciesChange;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.EntityEffects.Effects;

/// <summary>
/// Randomly changes the species of a humanoid to another playable species.
/// </summary>
public sealed partial class RandomSpeciesChangeEntityEffectSystem
    : EntityEffectSystem<HumanoidAppearanceComponent, RandomSpeciesChange>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> entity, ref EntityEffectEvent<RandomSpeciesChange> args)
    {
        var currentSpecies = entity.Comp.Species;

        var candidates = _proto.EnumeratePrototypes<SpeciesPrototype>()
            .Where(s => s.ID != currentSpecies)
            .ToList();

        if (candidates.Count == 0)
            return;

        var newSpecies = _random.Pick(candidates);
        _humanoid.SetSpecies(entity, newSpecies.ID, true, entity.Comp);

        // Apply random appearance for new species so markings etc. are valid
        var profile = HumanoidCharacterProfile.RandomWithSpecies(newSpecies.ID);
        _humanoid.LoadProfile(entity, profile, entity.Comp);
    }
}
