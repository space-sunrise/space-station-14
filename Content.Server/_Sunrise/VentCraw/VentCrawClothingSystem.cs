using Content.Shared._Sunrise.VentCraw;
using Content.Shared._Sunrise.VentCraw.Components;
using Content.Shared.Clothing;

namespace Content.Server._Sunrise.VentCraw;

public sealed class VentCrawClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawClothingComponent, ClothingGotEquippedEvent>(OnClothingEquip);
        SubscribeLocalEvent<VentCrawClothingComponent, ClothingGotUnequippedEvent>(OnClothingUnequip);
    }

    private void OnClothingEquip(Entity<VentCrawClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        AddComp<VentCrawlerComponent>(args.Wearer);
    }

    private void OnClothingUnequip(Entity<VentCrawClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemComp<VentCrawlerComponent>(args.Wearer);
    }
}
