using Content.Server.Atmos.EntitySystems;
using Content.Shared._Sunrise.Flashbang;

namespace Content.Server._Sunrise.Flashbang;

/// <summary>
/// Серверное расширение: отменяет эффект вспышки в условиях низкого атмосферного давления.
/// Проверка выполняется один раз на источник — до применения эффекта к целям в зоне.
/// </summary>
public sealed class ServerFlashbangSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlashbangRadiusOnTriggerComponent, FlashbangAreaAttemptEvent>(OnFlashbangAreaAttempt);
    }

    private void OnFlashbangAreaAttempt(EntityUid uid, FlashbangRadiusOnTriggerComponent comp, ref FlashbangAreaAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // В вакууме/разреженной атмосфере GetContainingMixture возвращает GasMixture.SpaceGas (давление 0),
        // а не null — null означает лишь отсутствие валидного TransformComponent у источника.
        var mixture = _atmos.GetContainingMixture(uid);
        if (mixture == null || mixture.Pressure < comp.MinAmbientPressure)
            args.Cancelled = true;
    }
}
