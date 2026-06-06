using Content.Server.Atmos.Components;
using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    private EntityQuery<ApcPowerReceiverComponent> _powerReceiverQuery;
    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<BatteryComponent> _batteryQuery;
    private EntityQuery<ChargedElectrovaeAffectedComponent> _chargedElectrovaeQuery;

    private void InitializeChargedElectrovaeSunrise()
    {
        InitializeChargedElectrovae();

        _powerReceiverQuery = GetEntityQuery<ApcPowerReceiverComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();
        _batteryQuery = GetEntityQuery<BatteryComponent>();
        _chargedElectrovaeQuery = GetEntityQuery<ChargedElectrovaeAffectedComponent>();
    }
}
