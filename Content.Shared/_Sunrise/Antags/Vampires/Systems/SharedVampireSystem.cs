using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Sunrise.Antags.Vampires.Systems;

public abstract class SharedVampireSystem : EntitySystem
{
    // Общие проверки и модификаторы вампира.

    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<VampireComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
    }

    private static void OnRefreshMovementSpeed(
        Entity<VampireComponent> ent,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.BloodFullness > 0f)
            return;

        args.ModifySpeed(ent.Comp.StarvationWalkSpeedModifier, ent.Comp.StarvationSprintSpeedModifier);
    }

    private void OnBeforeInteractHand(Entity<VampireComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !ent.Comp.FangsExtended)
            return;

        var target = args.Target;
        if (!Exists(target) || target == ent.Owner)
            return;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
        {
            if (HasComp<InteractionPopupComponent>(target))
                args.Handled = true;

            return;
        }

        if (!HasBloodToDrink((target, bloodstream)))
            return;

        args.Handled = true;
        TryStartDrinkBlood(ent, target);
    }

    protected bool HasBloodToDrink(Entity<BloodstreamComponent?> target)
        => _bloodstream.GetBloodLevel(target) > 0f;

    protected virtual bool TryStartDrinkBlood(Entity<VampireComponent> ent, EntityUid target)
        => true;
}
