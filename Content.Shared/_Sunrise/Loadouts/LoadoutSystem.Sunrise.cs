using Content.Shared._Sunrise.Loadouts;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing;

public sealed partial class LoadoutSystem
{
    public static ProtoId<RoleLoadoutPrototype> GetEffectiveJobPrototype(string? loadout, IPrototypeManager protoMan,
        IConfigurationManager configManager)
    {
        var rolePrototype = GetJobPrototype(loadout);
        return GetEffectiveRolePrototype(rolePrototype, protoMan, configManager);
    }

    public static ProtoId<RoleLoadoutPrototype> GetEffectiveRolePrototype(ProtoId<RoleLoadoutPrototype> rolePrototype,
        IPrototypeManager protoMan, IConfigurationManager configManager)
    {
        if (!configManager.GetCVar(SunriseCCVars.CustomLoadoutEnabled))
            return rolePrototype;

        var poolId = configManager.GetCVar(SunriseCCVars.LoadoutPool);
        if (!protoMan.TryIndex<LoadoutPoolPrototype>(poolId, out var poolProto))
            return rolePrototype;

        if (poolProto.RoleLoadouts.TryGetValue(rolePrototype, out var overridePrototype) && protoMan.HasIndex(overridePrototype))
            return overridePrototype;

        return rolePrototype;
    }
}
