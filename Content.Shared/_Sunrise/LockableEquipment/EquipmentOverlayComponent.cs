using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;


namespace Content.Shared._Sunrise.LockableEquipment
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class EquipmentOverlayComponent : Component
    {
        [DataField, AutoNetworkedField] public string Layer = "equipment";
        [DataField, AutoNetworkedField] public string SpritePath = "_Sunrise/Clothing/Locked/cage.rsi";
        [DataField, AutoNetworkedField] public string State = "equipped";
        [DataField, AutoNetworkedField] public bool Visible = false;
    }
}
