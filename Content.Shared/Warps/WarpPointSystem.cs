using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Warps;
using Robust.Shared.Localization; //Sunrise-Edit

namespace Content.Shared.Warps;

public sealed class WarpPointSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarpPointComponent, ExaminedEvent>(OnWarpPointExamine);
    }

    private void OnWarpPointExamine(EntityUid uid, WarpPointComponent component, ExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner))
            return;

        //Sunrise-Start
        var locationKey = component.Location;

        if (locationKey != null)
        {
            var loc = Loc.GetString($"location-{locationKey.Replace(" ", "-")}");
            args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", loc)));
        }
        else
        {
            args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", "<null>")));
        }
        //Sunrise-End
    }
}
