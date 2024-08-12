using Content.Shared.DoAfter;

namespace Content.Server._Sunrise.HairDye;

/// <summary>
/// Компонент используется для краски для волос.
/// </summary>
[RegisterComponent]
public sealed partial class HairDyerComponent : Component
{
    [DataField(required: true, readOnly: true)]
    public Color TargetColor;

    [DataField]
    public bool Mode;  // false - волосы true - борода
}
