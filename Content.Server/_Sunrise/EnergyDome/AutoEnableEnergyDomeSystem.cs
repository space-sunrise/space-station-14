using Content.Server.EnergyDome;
using Content.Shared.PowerCell;

namespace Content.Server._Sunrise.EnergyDome;

public sealed class AutoEnableEnergyDomeSystem : EntitySystem
{
    [Dependency] private readonly EnergyDomeSystem _energyDome = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoEnableEnergyDomeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AutoEnableEnergyDomeComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnMapInit(Entity<AutoEnableEnergyDomeComponent> ent, ref MapInitEvent args)
    {
        TryEnable(ent);
    }

    private void OnPowerCellChanged(Entity<AutoEnableEnergyDomeComponent> ent, ref PowerCellChangedEvent args)
    {
        if (!args.Ejected)
            TryEnable(ent);
    }

    private void TryEnable(Entity<AutoEnableEnergyDomeComponent> ent)
    {
        // Стартовая батарея может быть вставлена ItemSlots после MapInit этого компонента.
        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out _) ||
            !TryComp<EnergyDomeGeneratorComponent>(ent, out var generator) ||
            generator.Enabled)
            return;

        _energyDome.AttemptToggle((ent, generator), true);
    }
}
