// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt;

using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Shared._Sunrise.HairDye;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.HairDye;

/// <summary>
/// Система, обрабатывающая красители для волос.
/// </summary>
public sealed class HairDyeSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<HairDyerComponent, AfterInteractEvent>(OnHairDyerInteract);
        SubscribeLocalEvent<HairDyerComponent, HairDyeDoAfterEvent>(OnHairDyeDoAfter);
        SubscribeLocalEvent<HairDyerComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    /// <summary>
    /// Функция вызывается для переключения цели с бороды на волосы и наоборот
    /// </summary>
    private void OnGetVerbs(EntityUid uid, HairDyerComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = (comp.Mode) ? Loc.GetString("hairdye-switch-hair") : Loc.GetString("hairdye-switch-facial"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () => { comp.Mode = !comp.Mode; },
        });
    }

    /// <summary>
    /// Функция вызывается когда кто-то пытается применить краситель для волос
    /// </summary>
    private void OnHairDyerInteract(EntityUid uid, HairDyerComponent comp, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;
        if (!TryComp<TransformComponent>(args.Target, out var targetXform))
            return;
        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out var humanoidAppearanceComponent))
        {
            _popup.PopupEntity("Нельзя использовать краситель", args.User, args.User);
            return;
        }

        if (comp.Mode)
        {
            // facial
            if (!humanoidAppearanceComponent.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var targetMarking))
            {
                _popup.PopupEntity("Нет лицевой растительности", args.User, args.User);
                return;
            }
            var calculatedcolor = Color.InterpolateBetween(targetMarking[0].MarkingColors[0], comp.TargetColor, 0.25f);

            var doAfterEvent = new HairDyeDoAfterEvent()
            {
                TargetColor = calculatedcolor,
            };

            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(1), doAfterEvent, eventTarget: uid, args.Target)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
        }
        else
        {
            // hair
            if (!humanoidAppearanceComponent.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var targetMarking))
            {
                _popup.PopupEntity("Нет волос", args.User, args.User);
                return;
            }

            var calculatedcolor = Color.InterpolateBetween(targetMarking[0].MarkingColors[0], comp.TargetColor, 0.25f);

            var doAfterEvent = new HairDyeDoAfterEvent()
            {
                TargetColor = calculatedcolor,
            };

            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(1), doAfterEvent, eventTarget: uid, args.Target)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
        }
    }

    private void OnHairDyeDoAfter(EntityUid uid, HairDyerComponent comp, HairDyeDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
            return;

        _humanoid.SetMarkingColor(args.Target.Value, (comp.Mode) ? MarkingCategories.FacialHair : MarkingCategories.Hair, 0, [args.TargetColor]);
    }
}
