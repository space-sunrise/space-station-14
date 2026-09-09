using Content.Shared.Atmos;
using Content.Shared.Clothing.Components;

namespace Content.Shared._Sunrise.Abilities.Demon;

/// <summary>
/// Ванильный FireProtectionSystem обрабатывает <see cref="FireProtectionComponent"/> только как надетую одежду
/// (подписка идёт лишь на InventoryRelayedEvent). Эта система добавляет недостающую прямую подписку,
/// чтобы FireProtection можно было повесить прямо на тело существа как врождённую защиту от огня, без брони.
/// </summary>
public sealed class InnateFireProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireProtectionComponent, GetFireProtectionEvent>(OnGetProtection);
    }

    private void OnGetProtection(Entity<FireProtectionComponent> ent, ref GetFireProtectionEvent args)
    {
        args.Reduce(ent.Comp.Reduction);
    }
}
