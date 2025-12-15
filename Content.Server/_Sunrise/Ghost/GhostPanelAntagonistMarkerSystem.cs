using Content.Shared._Sunrise.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._Sunrise.Ghost;

public sealed class GhostPanelAntagonistMarkerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostPanelAntagonistMarkerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GhostPanelAntagonistMarkerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnStartup(Entity<GhostPanelAntagonistMarkerComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobState))
            return;

        var enabled = mobState.CurrentState != MobState.Dead;
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent, ent.Comp);
    }

    private void OnMobStateChanged(Entity<GhostPanelAntagonistMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        var enabled = args.NewMobState != MobState.Dead;
        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent, ent.Comp);
    }
}
