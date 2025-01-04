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

    [DataField]
    public HashSet<string> IgnoredDamageTypes = new ()
    {

    };

    /// <summary>
    /// Является ли ентити структурой
    /// </summary>
    /// TODO: Более адекватная реализация
    [DataField]
    public bool IsStructure;
}
