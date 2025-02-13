using Content.Shared.Popups;

namespace Content.Server._Sunrise.DamageOverlay;

[RegisterComponent, Access(typeof(DamageOverlaySystem))]
public sealed partial class DamageOverlayComponent : Component
{
    [DataField]
    public PopupType DamagePopupType = PopupType.MediumCautionFloating;

    [DataField]
    public PopupType HealPopupType = PopupType.LargeGreen;

    [DataField]
    public float Radius = 0.5f;

    /// SUNRISE-TODO: Более адекватная реализация
    [DataField]
    public bool IsStructure;
}
