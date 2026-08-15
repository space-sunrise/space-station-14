using Content.Shared._Sunrise.Particles;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Gibbing;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Particles;

/// <summary>
/// Captures blood color before gib deletion and sends the generic gib particle visual.
/// </summary>
public sealed class GibMistParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<ParticleOrchestraPrototype> GibOrchestra = "GibMist";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodstreamComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnBeingGibbed(Entity<BloodstreamComponent> ent, ref BeingGibbedEvent args)
    {
        var color = Color.Red;
        var contents = ent.Comp.BloodReferenceSolution.Contents;
        if (contents.Count > 0 && _proto.TryIndex(contents[0].Reagent.Prototype, out ReagentPrototype? reagent))
            color = reagent.SubstanceColor;

        _orchestra.Send(GibOrchestra, ent, colorOverride: color);
    }
}
