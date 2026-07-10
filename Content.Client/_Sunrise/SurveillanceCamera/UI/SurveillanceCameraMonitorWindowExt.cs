using Robust.Client.GameObjects;

namespace Content.Client.SurveillanceCamera.UI;

public sealed partial class SurveillanceCameraMonitorWindow
{
    public void SetMapGrid(EntityUid gridUid)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        if (!entManager.EntityExists(gridUid))
            return;

        NavMap.MapUid = gridUid;
        NavMap.Visible = true;
    }
}
