using System.Linq;
using Content.Server._Sunrise.Helpers;
using Content.Shared.Humanoid;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Research.Artifact.Effects.WhitelistSwap;

public sealed class ArtifactWhitelistSwapSystem : BaseXAESystem<ArtifactWhitelistSwapComponent>
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnActivated(Entity<ArtifactWhitelistSwapComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var humans = _helpers.GetAll<HumanoidAppearanceComponent, TransformComponent>().ToList();
        var targets = _helpers.GetAll<TransformComponent>()
            .Where(e => _whitelist.IsWhitelistPassOrNull(ent.Comp.TargetWhitelist, e))
            .ToList();

        var ent1 = _random.PickAndTake(humans);
        var ent2 = _random.PickAndTake(targets);

        _transform.SwapPositions((ent1, ent1.Comp2), (ent2, ent2));
    }
}
