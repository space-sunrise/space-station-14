using Content.Shared._Sunrise.Materials.MaterialSilo;
using Content.Shared.Materials.OreSilo;

namespace Content.Server._Sunrise.Materials.MaterialSilo;

/// <summary>
/// Не трогает ванильный OreSilo — просто следит за ним со стороны: любая сущность, способная подключиться
/// к ванильному <c>MachineMaterialSilo</c> (то есть имеющая <see cref="OreSiloClientComponent"/>),
/// автоматически получает и <see cref="SunriseMaterialSiloClientComponent"/>, чтобы её можно было
/// подключить и к <see cref="SunriseMaterialSiloComponent"/>. Работает через подписку на событие
/// ванильного компонента, поэтому не требует ни одной правки ванильных файлов.
/// </summary>
public sealed class SunriseMaterialSiloAutoClientSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreSiloClientComponent, ComponentStartup>(OnOreSiloClientStartup);
    }

    private void OnOreSiloClientStartup(Entity<OreSiloClientComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<SunriseMaterialSiloClientComponent>(ent);
    }
}
