using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultBloodSpellComponent : Component
{
    [DataField]
    public EntProtoId BlodOrbSpawnId = "CultBloodOrb";

    [DataField]
    public SoundSpecifier BloodAbsorbSound = new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg");

    [DataField]
    public int BloodOrbMinCost = 50;

    [DataField]
    public int BloodSpearCost = 150;

    [DataField]
    public int BloodBoltBarrageCost = 300;

    [DataField]
    public EntProtoId BloodSpearSpawnId = "BloodSpear";

    [DataField]
    public EntProtoId BloodBoltBarrageSpawnId = "BloodBoltBarrage";

    [DataField("healingGroups")]
    public List<string> HealingGroups = new()
    {
        "Airloss",
        "Burn",
        "Brute",
        "Toxin",
    };

    [DataField]
    public float RadiusAbsorbBloodPools = 2.0f;
}

[Serializable, NetSerializable]
public sealed class CultBloodSpellCreateOrbBuiMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CultBloodSpellCreateBloodSpearBuiMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CultBloodSpellCreateBloodBoltBarrageBuiMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum CultBloodSpellUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum CountSelectorUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CountSelectorBuiState : BoundUserInterfaceState
{
    public CountSelectorBuiState(int count)
    {
        Count = count;
    }

    public int Count { get; set; }
}

[Serializable, NetSerializable]
public sealed class CountSelectorMessage : BoundUserInterfaceMessage
{
    public CountSelectorMessage(int count)
    {
        Count = count;
    }

    public int Count { get; set; }
}
