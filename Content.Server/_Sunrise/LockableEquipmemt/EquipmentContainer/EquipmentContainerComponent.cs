using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server._Sunrise.LockableEquipment.EquipmentContainer
{
    [RegisterComponent]
    public sealed partial class EquipmentContainerComponent : Component
    {
        [DataField]
        public string ContainerId = "belt";
    }
}
