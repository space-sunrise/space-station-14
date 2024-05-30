using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Content.Shared.Synth.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Synth.EntitySystems;

public abstract class SharedSynthSystem : EntitySystem
{
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SynthComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, SynthComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.SlowState == SynthComponent.SlowStates.Off)
            return;

        if (_jetpack.IsUserFlying(uid))
            return;

        args.ModifySpeed(component.EnergyLowSlowdownModifier, component.EnergyLowSlowdownModifier);
    }
}

[Serializable, NetSerializable]
public sealed partial class SynthDrainPowerDoAfterEvent : SimpleDoAfterEvent
{

}
