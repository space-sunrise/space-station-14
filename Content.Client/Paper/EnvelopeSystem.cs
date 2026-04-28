using Content.Shared.Paper;
using Robust.Client.GameObjects;

namespace Content.Client.Paper;

public sealed class EnvelopeSystem : VisualizerSystem<EnvelopeComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnvelopeComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<EnvelopeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<EnvelopeComponent> ent, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent, ref sprite))
            return;

        SpriteSystem.LayerSetVisible((ent, sprite), EnvelopeVisualLayers.Open, ent.Comp.State == EnvelopeComponent.EnvelopeState.Open);
        SpriteSystem.LayerSetVisible((ent, sprite), EnvelopeVisualLayers.Sealed, ent.Comp.State == EnvelopeComponent.EnvelopeState.Sealed);
        SpriteSystem.LayerSetVisible((ent, sprite), EnvelopeVisualLayers.Torn, ent.Comp.State == EnvelopeComponent.EnvelopeState.Torn);
    }

    public enum EnvelopeVisualLayers : byte
    {
        Open,
        Sealed,
        Torn
    }
}
