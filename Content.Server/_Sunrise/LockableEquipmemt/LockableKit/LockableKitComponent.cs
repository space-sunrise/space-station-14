using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.LockableEquipment.LockableKit
{
    [RegisterComponent]
    public sealed partial class LockableKitComponent : Component
    {
        [DataField]
        public string ContainerId = "kit";

        [DataField]
        public bool AutoLink = true;
    }
}
