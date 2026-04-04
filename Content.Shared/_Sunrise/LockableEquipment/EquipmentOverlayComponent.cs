using Robust.Shared.GameObjects;

namespace Content.Shared._Sunrise.LockableEquipment
{
    [RegisterComponent]
    public sealed partial class EquipmentOverlayComponent : Component
    {
        [DataField]
        public string Layer = "equipment";

        [DataField]
        public string SpritePath = "_Sunrise/Clothing/Locked/cage.rsi";

        [DataField]
        public string State = "equipped";

        [DataField]
        public bool Visible = false;
    }
}
