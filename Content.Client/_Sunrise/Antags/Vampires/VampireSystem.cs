using Content.Client.Alerts;
using Content.Shared._Sunrise.Antags.Vampires;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Prototypes;
using Content.Shared.Popups;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Antags.Vampires;

public sealed class VampireSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<FactionIconPrototype> ThrallIcon = "VampireThrallIcon";
    private static readonly ProtoId<FactionIconPrototype> MasterIcon = "VampireFaction";
    private const string VampireBloodAlert = "VampireBlood";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VampireComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
        SubscribeLocalEvent<VampireThrallComponent, GetStatusIconsEvent>(OnThrallIcons);
        SubscribeLocalEvent<VampireComponent, GetStatusIconsEvent>(OnVampireIcons);
        SubscribeLocalEvent<VampireActionUseAttemptEvent>(OnVampireActionUseAttempt);
    }

    private void OnVampireActionUseAttempt(ref VampireActionUseAttemptEvent args)
    {
        args.Allowed = CanUseGrantedVampireAction(args.User, args.ActionEntity, args.BloodCost, args.ShowPopup);
    }

    private void OnUpdateAlert(Entity<VampireComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        var comp = ent.Comp;
        var key = args.Alert.AlertKey.AlertType;

        if (key == VampireBloodAlert)
        {
            // Background is set by the alert -> only set the digit layers from the counter value.
            var value = Math.Clamp(comp.DrunkBlood, 0, 9999);
            var d1 = value / 1000 % 10;
            var d2 = value / 100 % 10;
            var d3 = value / 10 % 10;
            var d4 = value % 10;

            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit1, d1.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit2, d2.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit3, d3.ToString());
            _sprite.LayerSetRsiState((args.SpriteViewEnt, args.SpriteViewEnt.Comp), VampireVisualLayers.Digit4, d4.ToString());
        }
    }

    private void OnThrallIcons(Entity<VampireThrallComponent> ent, ref GetStatusIconsEvent ev)
    {
        if (_prototype.TryIndex(ThrallIcon, out var icon))
            ev.StatusIcons.Add(icon);
    }

    private void OnVampireIcons(Entity<VampireComponent> ent, ref GetStatusIconsEvent ev)
    {
        if (_prototype.TryIndex(MasterIcon, out var icon))
            ev.StatusIcons.Add(icon);
    }

    internal bool CanUseGrantedVampireAction(EntityUid uid, EntityUid? actionEntity = null, int bloodCost = 0, bool showPopup = true)
    {
        if (TryComp<VampireComponent>(uid, out var comp))
            return CanUseVampireAbility((uid, comp), actionEntity, bloodCost, showPopup);

        return CanUseNonVampireGrantedAction(actionEntity);
    }

    private bool CanUseVampireAbility(Entity<VampireComponent> ent, EntityUid? actionEntity, int bloodCost, bool showPopup)
    {
        return TryResolveVampireActionCost(ent, actionEntity, bloodCost, out var resolvedCost, showPopup)
            && CanSpendBlood(ent, resolvedCost, showPopup);
    }

    private bool CanSpendBlood(Entity<VampireComponent> ent, int bloodCost, bool showPopup)
    {
        if (bloodCost <= 0 || ent.Comp.DrunkBlood >= bloodCost)
            return true;

        if (showPopup)
            _popup.PopupPredicted(Loc.GetString("vampire-not-enough-blood"), ent.Owner, ent.Owner, PopupType.MediumCaution);

        return false;
    }

    private bool TryResolveVampireActionCost(
        Entity<VampireComponent> ent,
        EntityUid? actionEntity,
        int bloodCost,
        out int resolvedCost,
        bool showPopup)
    {
        resolvedCost = Math.Max(0, bloodCost);

        if (actionEntity is not { } action)
            return true;

        if (!Exists(action))
            return false;

        if (!TryComp<VampireActionComponent>(action, out var vac))
            return true;

        if (ent.Comp.TotalBlood < vac.BloodToUnlock)
            return false;

        if (!ValidateVampireClass(ent.Comp, vac.RequiredClass))
            return false;

        if (vac.RequiresFullPower && !ent.Comp.FullPower)
        {
            if (showPopup)
                _popup.PopupPredicted(Loc.GetString("action-vampire-not-enough-power"), ent.Owner, ent.Owner, PopupType.MediumCaution);

            return false;
        }

        if (resolvedCost <= 0 && vac.BloodCost > 0)
            resolvedCost = (int) vac.BloodCost;

        return true;
    }

    private static bool ValidateVampireClass(VampireComponent comp, ProtoId<VampireClassPrototype>? requiredClass)
    {
        if (requiredClass is null)
            return true;

        return string.Equals(comp.ChosenClassId, requiredClass.Value.Id, StringComparison.Ordinal);
    }

    private bool CanUseNonVampireGrantedAction(EntityUid? actionEntity)
    {
        if (actionEntity is not { } action)
            return true;

        if (!Exists(action))
            return false;

        return !TryComp<VampireActionComponent>(action, out var vac) || vac.AllowNonVampireUsers;
    }
}
