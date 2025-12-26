using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared._Sunrise.BloodCult.Components;

[RegisterComponent]
public sealed partial class ConstructShellComponent : Component
{
    public readonly string ShardSlotId = "Shard";

    [DataField("constructForms", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> ConstructForms = new();

    [DataField("shardSlot", required: true)]
    public ItemSlot ShardSlot = new();
}
