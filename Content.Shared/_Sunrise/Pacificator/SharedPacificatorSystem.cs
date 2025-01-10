using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Pacificator;

public abstract partial class SharedPacificatorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
}

[Serializable, NetSerializable]
public sealed class SwitchGeneratorMessage : BoundUserInterfaceMessage
{
    public bool On;

    public SwitchGeneratorMessage(bool on)
    {
        On = on;
    }
}

[Serializable, NetSerializable]
public sealed class GeneratorState : BoundUserInterfaceState
{
    public bool On;
    // 0 -> 255
    public byte Charge;
    public PacificatorPowerStatus PowerStatus;
    public short PowerDraw;
    public short PowerDrawMax;
    public short EtaSeconds;

    public GeneratorState(
        bool on,
        byte charge,
        PacificatorPowerStatus powerStatus,
        short powerDraw,
        short powerDrawMax,
        short etaSeconds)
    {
        On = on;
        Charge = charge;
        PowerStatus = powerStatus;
        PowerDraw = powerDraw;
        PowerDrawMax = powerDrawMax;
        EtaSeconds = etaSeconds;
    }
}

[Serializable, NetSerializable]
public enum PacificatorUiKey
{
    Key
}

[Serializable, NetSerializable]
public enum PacificatorVisuals
{
    State,
    Charge
}

[Serializable, NetSerializable]
public enum PacificatorStatus
{
    Broken,
    Unpowered,
    Off,
    On
}

[Serializable, NetSerializable]
public enum PacificatorPowerStatus : byte
{
    Off,
    Discharging,
    Charging,
    FullyCharged
}
