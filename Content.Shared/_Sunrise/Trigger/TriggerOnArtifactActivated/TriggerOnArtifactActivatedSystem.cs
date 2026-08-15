using Content.Shared.Trigger;
using Content.Shared.Xenoarchaeology.Artifact;

namespace Content.Shared._Sunrise.Trigger.TriggerOnArtifactActivated;

/// <summary>
/// Bridges successful xenoartifact activations into the generic trigger pipeline.
/// </summary>
public sealed class TriggerOnArtifactActivatedSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnArtifactActivatedComponent, XenoArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(
        Entity<TriggerOnArtifactActivatedComponent> ent,
        ref XenoArtifactActivatedEvent args)
    {
        Trigger.Trigger(ent, args.User, ent.Comp.KeyOut, predicted: args.User.HasValue);
    }
}
