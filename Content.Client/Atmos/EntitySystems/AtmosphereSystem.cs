using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Client.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem : SharedAtmosphereSystem // Sunrise edit
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapAtmosphereComponent, ComponentHandleState>(OnMapHandleState);
        InitializeCVars(); // Sunrise edit
    }

    private void OnMapHandleState(EntityUid uid, MapAtmosphereComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MapAtmosphereComponentState state)
            return;

        // Struct so should just copy by value.
        component.OverlayData = state.Overlay;
    }
}
