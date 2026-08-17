using Content.Server.Atmos.Components;
using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Power.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;

    [Dependency] private readonly EntityQuery<ApcPowerReceiverComponent> _powerReceiverQuery = default!;
    [Dependency] private readonly EntityQuery<MobStateComponent> _mobQuery = default!;
    [Dependency] private readonly EntityQuery<BatteryComponent> _batteryQuery = default!;
    [Dependency] private readonly EntityQuery<ChargedElectrovaeAffectedComponent> _chargedElectrovaeQuery = default!;

    private void InitializeChargedElectrovaeSunrise()
    {
        InitializeChargedElectrovae();

    }
}
