using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SubFloor;
using Robust.Shared.Audio.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Shared._Sunrise.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public abstract class SharedMechPaintSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedMechSystem _mechSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<MechPaintComponent, PaintDoAfterEvent>(OnPaint);
    }
    
    private void OnPaint(Entity<MechPaintComponent> entity, ref PaintDoAfterEvent args)
    {
        if (args.Target == null || args.Used == null || !HasComp<MechComponent>(args.Target))
            return;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (!_openable.IsOpen(entity))
        {
            _popup.PopupEntity(Loc.GetString("paint-closed", ("used", args.Used)), args.User, args.User, PopupType.Medium);
            return;
        }
        
        if (entity.Comp.Whitelist != null && !_whitelist.IsValid(entity.Comp.Whitelist, target) || HasComp<HumanoidAppearanceComponent>(target) || HasComp<SubFloorHideComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("paint-failure", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            return;
        }


        if (TryPaint(entity, target))
        {
            EnsureComp<MechComponent>(target, out MechComponent? mech);
            EnsureComp<AppearanceComponent>(target, out AppearanceComponent? appearance);

            _audio.PlayPvs(entity.Comp.Spray, entity);

            _popup.PopupEntity(Loc.GetString("paint-success", ("target", args.Target)), args.User, args.User, PopupType.Medium);
            mech.BaseState = entity.Comp.BaseState;
            mech.OpenState = entity.Comp.OpenState;
            mech.BrokenState = entity.Comp.BrokenState;
            entity.Comp.Used = true;
            Dirty(target, mech);
            args.Handled = true;
            _mechSystem.UpdateAppearance(target, mech, appearance);
            return;
        }

        if (!TryPaint(entity, target))
        {
            _popup.PopupEntity(Loc.GetString("paint-empty", ("used", args.Used)), args.User, args.User, PopupType.Medium);
            return;
        }
    }
    
    private bool TryPaint(Entity<MechPaintComponent> entity, EntityUid target)
    {
        if (HasComp<HumanoidAppearanceComponent>(target) || HasComp<SubFloorHideComponent>(target) || entity.Comp.Used)
            return false;
        
        if (HasComp<MechComponent>(target))
            return true;
        
        return false;
    }
}
