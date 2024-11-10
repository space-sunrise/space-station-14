using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CentCom;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CentComGiftsTableComponent : Component
{
    [DataField]
    public List<ProtoId<EntityPrototype>> Gifts;
}
