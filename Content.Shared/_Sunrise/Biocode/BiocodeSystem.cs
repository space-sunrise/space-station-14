using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Biocode;

public sealed class BiocodeSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiocodeComponent, AttemptThrowBiocodeEvent>(OnAttemptThrowBiocode);
        SubscribeLocalEvent<BiocodeComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
    }

    public bool CanUse(EntityUid user, HashSet<ProtoId<NpcFactionPrototype>> factions)
    {
        var canUse = false;
        if (!TryComp<NpcFactionMemberComponent>(user, out var npcFactionMemberComponent))
            return canUse;

        foreach (var faction in npcFactionMemberComponent.Factions)
        {
            if (factions.Contains(faction))
                canUse = true;
        }

        return canUse;
    }

    private void OnAttemptThrowBiocode(EntityUid uid, BiocodeComponent component, ref AttemptThrowBiocodeEvent args)
    {
        if (args.User == null || CanUse(args.User.Value, component.Factions))
            return;

        if (!string.IsNullOrEmpty(component.AlertText))
            _popup.PopupEntity(Loc.GetString(component.AlertText), args.User.Value, args.User.Value);

        args.Cancelled = true;
    }

    private void OnGettingPickedUpAttempt(EntityUid uid, BiocodeComponent component, ref GettingPickedUpAttemptEvent args)
    {
        if (CanUse(args.User, component.Factions))
            return;

        if (args.ShowPopup && !string.IsNullOrEmpty(component.AlertText))
            _popup.PopupEntity(Loc.GetString(component.AlertText), args.User, args.User);

        args.Cancel();
    }
}
