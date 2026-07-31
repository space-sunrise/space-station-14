using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared._Sunrise.Silicons.Borgs.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._Sunrise.Silicons.Borgs;

public sealed class BorgModuleBlacklistSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgModuleBlacklistComponent, AfterInteractUsingEvent>(
            OnAfterInteractUsing,
            before: [typeof(SharedBorgSystem)]);
    }

    private void OnAfterInteractUsing(Entity<BorgModuleBlacklistComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !_whitelist.IsValid(ent.Comp.Blacklist, args.Used))
            return;

        _popup.PopupClient(Loc.GetString("borg-module-whitelist-deny"), ent, args.User);
        args.Handled = true;
    }
}
