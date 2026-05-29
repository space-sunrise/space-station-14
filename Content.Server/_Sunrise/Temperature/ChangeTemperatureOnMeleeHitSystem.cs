using Content.Server.Temperature.Systems;
using Content.Shared._Sunrise.Temperature;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Sunrise.Temperature;

public sealed class ChangeTemperatureOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeTemperatureOnMeleeHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ChangeTemperatureOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
            _temperature.ChangeHeat(target, ent.Comp.Heat, ent.Comp.IgnoreHeatResistance);
    }
}
