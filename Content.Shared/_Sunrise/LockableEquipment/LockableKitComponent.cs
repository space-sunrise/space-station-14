using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.LockableEquipment
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
