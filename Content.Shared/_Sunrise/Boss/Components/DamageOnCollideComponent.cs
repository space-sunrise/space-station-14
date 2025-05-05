using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;

namespace Content.Shared._Sunrise.Boss.Components;

/// <summary>
/// Компонент, обозначающий, что сущность будет пытаться продамажить тех, кто с ней соприкасается
/// </summary>
[RegisterComponent]
public sealed partial class DamageOnCollideComponent : Component
{
    [DataField("damageOnCollide")]
    public DamageSpecifier Damage = new DamageSpecifier()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Slash", 10 },
            { "Pierce", 10 },
        }
    };

    [DataField]
    public EntityWhitelist? Blacklist;
}
