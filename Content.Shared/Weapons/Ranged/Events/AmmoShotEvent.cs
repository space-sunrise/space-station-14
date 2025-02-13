namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when projectiles have been fired from it.
/// </summary>
public sealed class AmmoShotEvent : EntityEventArgs
{
    public List<EntityUid> FiredProjectiles = default!;
}

public sealed class HitscanAmmoShotEvent : EntityEventArgs
{
    public EntityUid Target = default!;

    public EntityUid? Shooter = default!;
}
