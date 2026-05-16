using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Sandbox;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.ThermalVision;
using Content.Shared._Sunrise.Sandbox;

namespace Content.Server.Sandbox
{
    public partial class SandboxSystem : SharedSandboxSystem
    {
        partial void SandboxThermalVisionHandler(MsgSandboxThermalVision ev, EntitySessionEventArgs args)
        {
            var player = args.SenderSession.AttachedEntity;
            if (player is null)
                return;

            if (HasComp<ThermalVisionComponent>(player.Value))
                RemCompDeferred<ThermalVisionComponent>(player.Value);
            else
                EnsureComp<ThermalVisionComponent>(player.Value);
        }

        partial void ClearAllSandboxThermalVision()
        {
            var query = EntityQueryEnumerator<SandboxThermalVisionMarkerComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                RemComp<SandboxThermalVisionMarkerComponent>(uid);
                RemCompDeferred<ThermalVisionComponent>(uid);
            }
        }
    }
}
