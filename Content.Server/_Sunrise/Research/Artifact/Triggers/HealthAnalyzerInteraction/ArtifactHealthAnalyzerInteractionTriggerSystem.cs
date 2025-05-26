using Content.Shared._Sunrise.Research.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Server._Sunrise.Research.Artifact.Triggers.HealthAnalyzerInteraction;

public sealed class ArtifactHealthAnalyzerInteractionTriggerSystem : BaseXATSystem<ArtifactHealthAnalyzerInteractionTriggerComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<EntityAnalyzedEvent>(OnAnalyzed);
    }

    private void OnAnalyzed(Entity<XenoArtifactComponent> artifact, Entity<ArtifactHealthAnalyzerInteractionTriggerComponent, XenoArtifactNodeComponent> node, ref EntityAnalyzedEvent args)
    {
        if (args.Handled)
            return;

        Trigger(artifact, node);
        args.Handled = true;
    }
}

