using Content.Shared.Damage;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared._Sunrise.Events;

/// <summary>
///     Вызывается на предметах в руках и сущности, которая отразила хитскан луч
/// </summary>
public sealed class ReflectedEvent : EntityEventArgs
{
    /// <summary>
    ///     Кто выстрелил
    /// </summary>
    public EntityUid? Shooter;

    /// <summary>
    ///     Чем выстрелил
    /// </summary>
    public EntityUid SourceItem;

    /// <summary>
    ///     Сколько урона нанес
    /// </summary>
    public DamageSpecifier? Damage;

    /// <summary>
    ///     Какой это был урон?
    /// </summary>
    public ReflectType? ReflectType;

    public ReflectedEvent(EntityUid? shooter,
        EntityUid sourceItem,
        DamageSpecifier? damage = null,
        ReflectType? reflectType = null)
    {
        Shooter = shooter;
        SourceItem = sourceItem;
        Damage = damage;
        ReflectType = reflectType;
    }
}
