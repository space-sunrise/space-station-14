using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class CultBloodSpellComponent : Component
{
    [DataField("bloodAbsorbSound")]
    public SoundSpecifier BloodAbsorbSound = new SoundPathSpecifier("/Audio/_Sunrise/BloodCult/enter_blood.ogg");

    [DataField("healingGroups")]
    public List<string> HealingGroups = new()
    {
        "Airloss",
        "Burn",
        "Brute",
        "Toxin",
    };

    [DataField("radiusAbsorbBloodPools")]
    public float RadiusAbsorbBloodPools = 2.0f;

    [DataField("bloodOrbMinCost")]
    public int BloodOrbMinCost = 50;

    [DataField("bloodSpearCost")]
    public int BloodSpearCost = 150;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("blodOrbSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BlodOrbSpawnId = "CultBloodOrb";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("bloodSpearSpawnId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BloodSpearSpawnId = "BloodSpear";

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
    public int Count { get; set; }

    public CountSelectorBuiState(int count)
    {
        Count = count;
    }
}

[Serializable, NetSerializable]
public sealed class CountSelectorMessage : BoundUserInterfaceMessage
{
    public int Count { get; set; }

    public CountSelectorMessage(int count)
    {
        Count = count;
    }
}
