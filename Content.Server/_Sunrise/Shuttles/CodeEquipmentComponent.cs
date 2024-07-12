using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Shuttles;

[RegisterComponent]
public sealed partial class CodeEquipmentComponent : Component
{
    public List<EntityUid> Shuttles = [];

    [DataField("shuttlePath")]
    public ResPath ShuttlePath = new("Maps/_Sunrise/Shuttles/gamma_armory.yml");

    [DataField]
    public string TargetCode = "gamma";

    [DataField]
    public string PriorityTag = "DockGamma";
}
