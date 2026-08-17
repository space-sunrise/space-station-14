using System.Diagnostics.CodeAnalysis;
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

    public bool TryEnable(Entity<AutoEnableEnergyDomeComponent> ent)
    {
        if (!CanEnable(ent, out var generator))
            return false;

        return DoEnable((ent.Owner, generator));
    }

    public bool CanEnable(
        Entity<AutoEnableEnergyDomeComponent> ent,
        [NotNullWhen(true)] out EnergyDomeGeneratorComponent? generator)
    {
        generator = null;

        // Стартовая батарея может быть вставлена ItemSlots после MapInit этого компонента.
        return _powerCell.TryGetBatteryFromSlot(ent.Owner, out _) &&
               TryComp(ent.Owner, out generator) &&
               !generator.Enabled;
    }

    private bool DoEnable(Entity<EnergyDomeGeneratorComponent> ent)
    {
        return _energyDome.AttemptToggle(ent, true);
    }
}
