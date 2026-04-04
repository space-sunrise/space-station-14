using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server._Sunrise.LockableEquipment.AttachmentContainer
{
    [RegisterComponent]
    public sealed partial class AttachmentContainerComponent : Component
    {
        [DataField]
        public string ContainerId = "belt";
    }
}
