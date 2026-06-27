using Content.Shared._Sunrise.Movement.Standing;

namespace Content.Shared._Sunrise.Abilities.Resomi;

public sealed partial class SharedResomiAbilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiActiveAbilityComponent, FallAttemptEvent>(OnFallAttempt);
    }

    /// <summary>
    /// Prevents falling while in an active Resomi ability.
    /// </summary>
    private void OnFallAttempt(Entity<ResomiActiveAbilityComponent> ent, ref FallAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
