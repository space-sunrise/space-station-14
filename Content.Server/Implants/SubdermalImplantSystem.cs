using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Store.Components;

using Content.Shared.Implants.Components; // Starlight
using Content.Server.Polymorph.Systems; // Starlight
using Content.Shared.Zombies; // Starlight
using Robust.Shared.Player; // Starlight

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!; // Starlight

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);
        SubscribeLocalEvent<SubdermalImplantComponent, UseMagillitisSerumImplantEvent>(OnMagillitisSerumImplantImplant); // Starlight
    }

    // TODO: This shouldn't be in the SubdermalImplantSystem
    private void OnStoreRelay(EntityUid uid, StoreComponent store, ImplantRelayEvent<AfterInteractUsingEvent> implantRelay)
    {
        var args = implantRelay.Event;

        if (args.Handled)
            return;

        // can only insert into yourself to prevent uplink checking with renault
        if (args.Target != args.User)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency))
            return;

        // same as store code, but message is only shown to yourself
        if (!_store.TryAddCurrency((args.Used, currency), (uid, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
    }

    // Starlight-start
    private void OnMagillitisSerumImplantImplant(EntityUid uid, SubdermalImplantComponent component, UseMagillitisSerumImplantEvent args)
    {
        if (component.ImplantedEntity is not { } ent)
            return;

        if (HasComp<ZombieComponent>(ent))
            return;

        var polymorph = _polymorphSystem.PolymorphEntity(ent, "RampagingGorilla");

        if (!polymorph.HasValue)
            return;

        _popup.PopupEntity(Loc.GetString("magillitisserum-implant-activated-others", ("entity", polymorph.Value)), polymorph.Value, Filter.PvsExcept(polymorph.Value), true);
        _popup.PopupEntity(Loc.GetString("magillitisserum-implant-activated-user"), polymorph.Value, polymorph.Value);

        args.Handled = true;
        QueueDel(uid);
    }
    // Starlight-end
}
