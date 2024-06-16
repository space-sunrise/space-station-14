using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared._Sunrise.ElectricChair;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Popups;

namespace Content.Server._Sunrise.ElectricChair;

/// <summary>
/// This handles ElectricChairComponent
/// TODO: make electric chair drain power from a power grid
/// TODO: add delay
/// </summary>
public sealed class ElectricChairSystem : EntitySystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ElectricChairComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, ElectricChairComponent component, TriggerEvent args)
    {
        if (!HasComp<StrapComponent>(uid))
            return;
        var comp = Comp<StrapComponent>(uid);
        if (comp.BuckledEntities.Count == 0)
            return;
        var target = comp.BuckledEntities.First();
        if (HasComp<HumanoidAppearanceComponent>(target))
        {
            _electrocutionSystem.TryDoElectrocution(target, null, component.ShockDamage, TimeSpan.FromSeconds(5), true, 1F, null, true);
            _popup.PopupEntity(Loc.GetString("electrocution-success"), target, PopupType.Large);
            _adminLogManager.Add(
                LogType.Electrocution,
                LogImpact.Extreme,
                $"{ToPrettyString(args.User):entity} successfully electrocuted {ToPrettyString(target):entity}");
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("electrocution-failed"), target, PopupType.Medium);
        }
        args.Handled = true;
    }
}
