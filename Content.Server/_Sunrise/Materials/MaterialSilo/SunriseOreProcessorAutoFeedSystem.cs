using Content.Shared._Sunrise.Lathe;
using Content.Shared._Sunrise.Materials.MaterialSilo;
using Content.Shared.Materials;
using Content.Shared.Materials.OreSilo;

namespace Content.Server._Sunrise.Materials.MaterialSilo;

/// <summary>
/// Ловит <see cref="SunriseLatheProductPrintedEvent"/> у переработчиков руды, подключённых к силосу
/// через ванильный <see cref="OreSiloClientComponent"/>, и сразу отправляет свежую продукцию в силос
/// вместо того, чтобы она падала кучей рядом с машиной. Только слушает событие ванильного OreSilo,
/// ничего в нём не меняет.
/// </summary>
public sealed class SunriseOreProcessorAutoFeedSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedOreSiloSystem _oreSilo = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SunriseOreProcessorAutoFeedComponent, SunriseLatheProductPrintedEvent>(OnProductPrinted);
    }

    private void OnProductPrinted(Entity<SunriseOreProcessorAutoFeedComponent> ent, ref SunriseLatheProductPrintedEvent args)
    {
        if (!TryComp<OreSiloClientComponent>(ent, out var client) || client.Silo is not { } silo)
            return;

        if (!Exists(args.Result) || Deleted(args.Result))
            return;

        if (!_oreSilo.CanTransmitMaterials(silo, ent))
            return;

        _materialStorage.TryInsertMaterialEntity(ent, args.Result, silo);
    }
}
