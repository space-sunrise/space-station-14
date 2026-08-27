using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Sunrise.Antags.Vampires.Systems;

/// <summary>
/// Общая логика вампира.
/// </summary>
public abstract class SharedVampireSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private static void OnRefreshMovementSpeed(Entity<VampireComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.BloodFullness > 0f)
            return;

        args.ModifySpeed(ent.Comp.StarvationWalkSpeedModifier, ent.Comp.StarvationSprintSpeedModifier);
    }

    protected bool HasBloodToDrink(Entity<BloodstreamComponent?> target)
        => _bloodstream.GetBloodLevel(target) > 0f;
}
