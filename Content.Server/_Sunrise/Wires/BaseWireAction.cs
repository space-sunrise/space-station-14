namespace Content.Server.Wires;
using Content.Server._Sunrise.Particles;
using Content.Shared._Sunrise.Particles;
using Content.Shared.Doors.Components;
using Robust.Shared.Prototypes;

public abstract partial class BaseWireAction : IWireAction
{
    private static readonly ProtoId<ParticleOrchestraPrototype> AirlockHackOrchestra = "AirlockHackSparks";

    public void WireCutSparks(EntityUid uid)
    {
        if (!IsPowered(uid))
            return;

        if (!EntityManager.TryGetComponent<DoorComponent>(uid, out var door))
            return;

        if (!door.WireCutSparks)
            return;

        EntityManager.System<ParticleOrchestraSystem>().Send(AirlockHackOrchestra, uid);
    }
}
