using System.Linq;
using Content.Shared.Maps;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared.Construction.Conditions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class TileNotBlocked : IConstructionCondition
{
    [DataField("filterMobs")] private bool _filterMobs = false;
    [DataField("failIfSpace")] private bool _failIfSpace = true;
    [DataField("failIfNotSturdy")] private bool _failIfNotSturdy = true;

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var tileRef = location.GetTileRef();
        var entManager = IoCManager.Resolve<IEntityManager>();
        var sysMan = entManager.EntitySysManager;
        var lookupSys = sysMan.GetEntitySystem<EntityLookupSystem>();
        if (tileRef == null)
        {
            return false;
        }

        if (tileRef.Value.IsSpace() && _failIfSpace)
        {
            return false;
        }

        if (!tileRef.Value.GetContentTileDefinition().Sturdy && _failIfNotSturdy)
        {
            return false;
        }
        // Sunrise-start, Временное решение. У оффов много что поломано с методом IsTileBlocked
        // return !tileRef.Value.IsBlockedTurf(_filterMobs);
        return !lookupSys.GetEntitiesIntersecting(location, LookupFlags.Static).Any();
        // Sunrise-end
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "construction-step-condition-tile-not-blocked",
        };
    }
}
