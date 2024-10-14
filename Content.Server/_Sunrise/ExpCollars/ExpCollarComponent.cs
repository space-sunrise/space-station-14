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
    [DataField]
    public List<EntityUid> Linked = new();

    /// <summary>
    /// Взведен ли механизм
    /// </summary>
    [DataField]
    public bool Armed;

    [DataField]
    public SoundSpecifier BeepSound;

    [DataField]
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

    [DataField(readOnly: true)]
    public TimeSpan InitialStripDelay = TimeSpan.FromSeconds(0);

    [DataField(readOnly: true)]
    public TimeSpan ArmedStripDelay = TimeSpan.FromSeconds(30);

    public bool ActiveCooldown;
}
