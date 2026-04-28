using Content.Server.Wires;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Wires;

namespace Content.Server.Access;

public sealed partial class AccessWireAction : ComponentWireAction<AccessReaderComponent>
{
    public override Color Color { get; set; } = Color.Green;
    public override string Name { get; set; } = "wire-name-access";

    [DataField("pulseTimeout")]
    private int _pulseTimeout = 30;

    public override StatusLightState? GetLightState(Wire wire, AccessReaderComponent comp)
    {
        return comp.Enabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override object StatusKey => AccessWireActionKey.Status;

    public override bool Cut(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        WiresSystem.TryCancelWireAction(wire, PulseTimeoutKey.Key);
        EntityManager.System<AccessReaderSystem>().SetActive((wire, comp), false);

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        EntityManager.System<AccessReaderSystem>().SetActive((wire, comp), true);

        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AccessReaderComponent comp)
    {
        EntityManager.System<AccessReaderSystem>().SetActive((wire, comp), false);
        WiresSystem.StartWireAction(wire, _pulseTimeout, PulseTimeoutKey.Key, new TimedWireEvent(AwaitPulseCancel, wire));
    }

    public override void Update(Wire wire)
    {
        if (!IsPowered(wire))
        {
            WiresSystem.TryCancelWireAction(wire, PulseTimeoutKey.Key);
        }
    }

    private void AwaitPulseCancel(Wire wire)
    {
        if (!wire.IsCut)
        {
            if (EntityManager.TryGetComponent<AccessReaderComponent>(wire, out var access))
            {
                EntityManager.System<AccessReaderSystem>().SetActive((wire, access), true);
            }
        }
    }

    private enum PulseTimeoutKey : byte
    {
        Key
    }
}
