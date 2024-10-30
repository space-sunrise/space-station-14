using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server._Sunrise.ExpCollars;

/// <summary>
/// Компонент для взрывного ошейника
/// </summary>
[RegisterComponent, Access(typeof(ExpCollarsSystem))]
public sealed partial class ExpCollarComponent : Component
{
    /// <summary>
    /// Является ли красным
    /// </summary>
    [DataField(readOnly: true)]
    public bool IsHost;

    /// <summary>
    /// Привязанные ошейники
    /// </summary>
    [DataField(readOnly: true)]
    public List<EntityUid> Linked = new();

    /// <summary>
    /// Взведен ли механизм
    /// </summary>
    [DataField(readOnly: true)]
    public bool Armed;

    /// <summary>
    /// Звук
    /// </summary>
    [DataField(readOnly: true)]
    public SoundSpecifier BeepSound;

    /// <summary>
    /// Сколько урона наносится при взрыве носителю
    /// </summary>
    [DataField(readOnly: true)]
    public DamageSpecifier Damage;

    /// <summary>
    /// "Девственность" взрывного механизма
    /// </summary>
    [DataField(readOnly: true)]
    public bool Virgin = true;

    /// <summary>
    /// Айди сущности, которая считается текущим носителем
    /// </summary>
    [DataField(readOnly: true)]
    public EntityUid? Wearer;

    /// <summary>
    /// Разница между временем сколько снимает ошейник носитель и сосед если он не взведен
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan InitialStripDelay = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Разница между временем сколько снимает ошейник носитель и сосед если он взведен
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan ArmedStripDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Идет ли асинхронная функция кулдауна на ошейнике
    /// </summary>
    [DataField(readOnly: true)]
    public bool ActiveCooldown;
}
