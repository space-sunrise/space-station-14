using System.Linq;
using Content.Server.Humanoid;
using Content.Shared._Sunrise.EntityEffects.Effects;
using Content.Shared._Sunrise.TTS;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.EntityEffects.Effects;

/// <summary>
/// Randomly swaps the sex of a humanoid, updates hair markings and TTS voice.
/// </summary>
public sealed partial class RandomSexChangeEntityEffectSystem
    : EntityEffectSystem<HumanoidAppearanceComponent, RandomSexChange>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> entity, ref EntityEffectEvent<RandomSexChange> args)
    {
        var humanoid = entity.Comp;
        var newSex = humanoid.Sex == Sex.Male ? Sex.Female : Sex.Male;

        _humanoid.SetSex(entity, newSex, true, humanoid);

        // Swap hair to a random style valid for new sex
        var hairStyles = _markingManager.MarkingsByCategoryAndSpeciesAndSex(
            MarkingCategories.Hair, humanoid.Species, newSex);

        if (hairStyles.Count > 0)
        {
            var newHair = _random.Pick(hairStyles.Keys.ToArray());
            _humanoid.SetMarkingId(entity, MarkingCategories.Hair, 0, newHair, humanoid);
        }

        // Pick a random TTS voice matching the new sex from all available round-start voices
        var voices = _proto.EnumeratePrototypes<TTSVoicePrototype>()
            .Where(v => v.Sex == newSex && v.RoundStart)
            .ToList();

        ProtoId<TTSVoicePrototype> newVoice;
        if (voices.Count > 0)
            newVoice = _random.Pick(voices).ID;
        else
            newVoice = SharedHumanoidAppearanceSystem.DefaultSexVoice.TryGetValue(newSex, out var fallback)
                ? fallback
                : SharedHumanoidAppearanceSystem.DefaultVoice;

        _humanoid.SetTTSVoice(entity, newVoice, humanoid);
    }
}
