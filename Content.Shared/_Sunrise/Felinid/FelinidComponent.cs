using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Felinid;

[RegisterComponent, NetworkedComponent]
public sealed partial class FelinidComponent : Component
{
    /// <summary>
    /// Бонусный урон если вы возьмете фелинида в руки и пизданете кого-то ним.
    /// </summary>
    [DataField("damageBonus")]
    public DamageSpecifier DamageBonus = new();

    /// <summary>
    /// Урон по фелиниду если вы ним кого-то пизданете.
    /// </summary>
    [DataField("felinidDamage")]
    public DamageSpecifier FelinidDamage = new();

    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(3.0);

    [DataField]
    public TimeSpan? NextPopupTime;
}
