using Content.Shared.Damage;
using Content.Shared._Sunrise.Weapons.Melee.Systems;

namespace Content.Shared._Sunrise.Weapons.Melee.Components;

[RegisterComponent, Access(typeof(SharedBackstabOnHitSystem))]
public sealed partial class BackstabOnHitComponent : Component
{
    /// <summary>
    /// Additional <see cref="DamageSpecifier"/> loaded from the required <c>bonusDamage</c> <see cref="DataFieldAttribute"/>
    /// and applied when the hit qualifies as a backstab.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier BonusDamage = new();

    /// <summary>
    /// Optional localized popup identifiers loaded from the <c>popupMessages</c> <see cref="DataFieldAttribute"/>.
    /// The server shows one of these messages to the struck target after a successful non-wide backstab.
    /// </summary>
    [DataField]
    public List<LocId> PopupMessages = [];

    /// <summary>
    /// Optional weights loaded from the <c>popupWeights</c> <see cref="DataFieldAttribute"/>.
    /// Entries map by index to <see cref="PopupMessages"/>; when the counts differ, the list is empty,
    /// or every weight is non-positive, popup selection falls back to an unweighted random pick.
    /// </summary>
    [DataField]
    public List<float> PopupWeights = [];
}
