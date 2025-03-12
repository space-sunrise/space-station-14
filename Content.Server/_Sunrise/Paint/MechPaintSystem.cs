using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared._Sunrise.Paint;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Sprite;
using Content.Shared.SubFloor;
using Content.Shared.Verbs;
using Content.Shared.Mech.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using PaintComponent = Content.Shared._Sunrise.Paint.PaintComponent;
using PaintDoAfterEvent = Content.Shared._Sunrise.Paint.PaintDoAfterEvent;
using PaintedComponent = Content.Shared._Sunrise.Paint.PaintedComponent;

namespace Content.Server._Sunrise.Paint;

/// <summary>
/// Colors target and consumes reagent on each color success.
/// </summary>
public sealed class MechPaintSystem : SharedMechPaintSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechPaintComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<MechPaintComponent, GetVerbsEvent<UtilityVerb>>(OnPaintVerb);
    }

    private void OnInteract(EntityUid uid, MechPaintComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (!HasComp<MechComponent>(args.Target))
            return;

        PrepPaint(uid, component, target, args.User);
    }

    private void OnPaintVerb(EntityUid uid, MechPaintComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!HasComp<MechComponent>(args.Target))
            return;

        var paintText = Loc.GetString("paint-verb");

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                PrepPaint(uid, component, args.Target, args.User);
            },

            Text = paintText,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/paint.svg.192dpi.png"))
        };
        args.Verbs.Add(verb);
    }
    private void PrepPaint(EntityUid uid, MechPaintComponent component, EntityUid target, EntityUid user)
    {

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, component.Delay, new PaintDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }
}
