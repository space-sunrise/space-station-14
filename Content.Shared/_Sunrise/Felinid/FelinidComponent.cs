using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Felinid;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class FelinidComponent : Component
{
    /// <summary>
    /// Бонусный урон если вы возьмете фелинида в руки и пизданете кого-то ним.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageBonus = new();

    /// <summary>
    /// Урон по фелиниду если вы ним кого-то пизданете.
    /// </summary>
    [DataField]
    public DamageSpecifier FelinidDamage = new();

    [DataField]
    public SoundSpecifier DamageSound = new SoundPathSpecifier("/Audio/Effects/hit_kick.ogg");

    [DataField, AutoNetworkedField]
    public bool InContainer;
}
