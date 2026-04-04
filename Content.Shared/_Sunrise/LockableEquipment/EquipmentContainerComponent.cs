using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Sunrise.LockableEquipment
{
    [RegisterComponent]
    public sealed partial class EquipmentContainerComponent : Component
    {
        [DataField]
        public string ContainerId = "belt";
    }
}
