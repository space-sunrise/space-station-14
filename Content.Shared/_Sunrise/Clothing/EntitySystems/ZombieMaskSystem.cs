using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Sunrise.Clothing.EntitySystems;
using Content.Shared.Sunrise.Clothing.Components;
using Content.Shared.Clothing;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Database;

namespace Content.Shared.Sunrise.Clothing.EntitySystems;

public sealed class ZombieMaskSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieMaskComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }
    private void OnRefreshNameModifiers(Entity<ZombieMaskComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("equipped-name-prefix");
    }
}
