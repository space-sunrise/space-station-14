using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.DetailExaminable;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    private ISharedSponsorsManager? _sponsorsManager;

    partial void InitializeStationSpawningPortal()
    {
        IoCManager.Instance!.TryResolveType(out _sponsorsManager);
    }

    partial void GetEffectiveRoleLoadoutPortal(string jobLoadout, ref ProtoId<RoleLoadoutPrototype> effectiveJobLoadout)
    {
        effectiveJobLoadout = LoadoutSystem.GetEffectiveRolePrototype(jobLoadout, _prototypeManager);
    }

    partial void GetDefaultLoadoutPrototypeIdsPortal(EntityUid? entity, ref string[] prototypeIds)
    {
        var session = _actors.GetSession(entity);
        if (_sponsorsManager == null || session == null)
            return;

        if (_sponsorsManager.TryGetPrototypes(session.UserId, out var prototypes))
            prototypeIds = prototypes.ToArray();
    }

    partial void TryApplyFlavorTextPortal(EntityUid entity, HumanoidCharacterProfile profile)
    {
        if (string.IsNullOrEmpty(profile.FlavorText) || !_configurationManager.GetCVar(CCVars.FlavorText))
            return;

        var session = _actors.GetSession(entity);
        var flavorText = profile.FlavorText;

        if (_sponsorsManager != null && session != null)
        {
            var maxDescLength = _sponsorsManager.GetSizeFlavor(session.UserId);
            if (flavorText.Length > maxDescLength)
                flavorText = FormattedMessage.RemoveMarkupOrThrow(flavorText)[..maxDescLength];
        }

        if (!_configurationManager.GetCVar(SunriseCCVars.FlavorTextSponsorOnly) ||
            _sponsorsManager != null && session != null && _sponsorsManager.IsAllowedFlavor(session.UserId))
        {
            AddComp<DetailExaminableComponent>(entity).Content = flavorText;
        }
    }
}
