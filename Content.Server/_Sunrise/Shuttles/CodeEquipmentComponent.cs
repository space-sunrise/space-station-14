using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Shuttles;

[RegisterComponent]
public sealed partial class CodeEquipmentComponent : Component
{
    public List<EntityUid> Shuttles = [];

    [DataField(required: true)]
    public ResPath ShuttlePath;

    [DataField(required: true)]
    public string TargetCode;

    [DataField(required: true)]
    public string PriorityTag;
}
