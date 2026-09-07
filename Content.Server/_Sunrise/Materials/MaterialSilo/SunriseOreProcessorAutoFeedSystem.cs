using Content.Shared._Sunrise.Lathe;
using Content.Shared._Sunrise.Materials.MaterialSilo;
using Content.Shared.Materials;

namespace Content.Server._Sunrise.Materials.MaterialSilo;

/// <summary>
/// Ловит <see cref="SunriseLatheProductPrintedEvent"/> у переработчиков руды, подключённых к
/// <see cref="SunriseMaterialSiloComponent"/>, и сразу отправляет свежую продукцию в силос вместо того,
/// чтобы она падала кучей рядом с машиной.
/// </summary>
public sealed class SunriseOreProcessorAutoFeedSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedSunriseMaterialSiloSystem _silo = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SunriseOreProcessorAutoFeedComponent, SunriseLatheProductPrintedEvent>(OnProductPrinted);
    }

    private void OnProductPrinted(Entity<SunriseOreProcessorAutoFeedComponent> ent, ref SunriseLatheProductPrintedEvent args)
    {
        if (!TryComp<SunriseMaterialSiloClientComponent>(ent, out var client) || client.Silo is not { } silo)
            return;

        if (!Exists(args.Result) || Deleted(args.Result))
            return;

        if (!_silo.CanTransmitMaterials(silo, ent))
            return;

        _materialStorage.TryInsertMaterialEntity(ent, args.Result, silo);
    }
}
