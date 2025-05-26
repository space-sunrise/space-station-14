using Content.Server.GameTicking;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server._Sunrise.Research.Artifact.Effects.StartGamerule;

public sealed class ArtifactStartGameRuleSystem : BaseXAESystem<ArtifactStartGameRuleComponent>
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    protected override void OnActivated(Entity<ArtifactStartGameRuleComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        foreach (var (rule, amount) in ent.Comp.Rules)
        {
            for (var i = 0; i < amount; i++)
            {
                _gameTicker.StartGameRule(rule);
            }
        }
    }
}
