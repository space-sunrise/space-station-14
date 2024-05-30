using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Synth.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SynthComponent : Component
{
    [DataField("drainEfficiency")]
    public float DrainEfficiency = 0.01f;

    [DataField("drainPerUse")]
    public float DrainPerUse = 200f;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Energy = 0;

    [ViewVariables(VVAccess.ReadWrite), DataField("hungerСonsumption")]
    public FixedPoint2 EnergyСonsumption = -0.6; // 1080 per 30 minutes

    [DataField("maxEnergy")]
    public FixedPoint2 MaxEnergy = 0;

    [DataField("deathSound")]
    public SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/_Sunrise/Synth/deathsound.ogg", AudioParams.Default.WithVolume(4f));

    [DataField("sparkSound")]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks", AudioParams.Default.WithVolume(10f));

    [DataField("powerDrainDelay")]
    public float PowerDrainDelay = 2;

    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier EmpDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Heat", 20 },
        }
    };

    [DataField("empParalyzeTime")]
    public float EmpParalyzeTime = 15;

    [DataField("energyLowSlowdownModifier"), ViewVariables(VVAccess.ReadWrite)]
    public float EnergyLowSlowdownModifier = 0.5f;

    [DataField("energyLowSlowdownPercent"), ViewVariables(VVAccess.ReadWrite)]
    public float EnergyLowSlowdownPercent = 0.05f;

    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    [ViewVariables(VVAccess.ReadWrite)]
    public SlowStates SlowState = SlowStates.Off;

    public enum SlowStates
    {
        On,
        Off
    }
}

[Serializable, NetSerializable]
public enum SynthScreenUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SynthScreenBoundUserInterfaceState(List<string> screenList)
    : BoundUserInterfaceState
{
    public readonly List<string> ScreenList = screenList;
}

[Serializable, NetSerializable]
public sealed class SynthScreenPrototypeSelectedMessage(string selectedId) : BoundUserInterfaceMessage
{
    public readonly string SelectedId = selectedId;
}

public sealed partial class SynthChangeScreenActionEvent : InstantActionEvent
{

}
