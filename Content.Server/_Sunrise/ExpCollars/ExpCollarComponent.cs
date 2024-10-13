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
    /// Включены ли болты
    /// </summary>
    [DataField]
    public bool Bolts;

    /// <summary>
    /// Взведен ли механизм
    /// </summary>
    [DataField]
    public bool Armed;

    /// <summary>
    /// "Девственность" взрывного механизма
    /// </summary>
    [DataField(readOnly: true)]
    public bool Virgin = true;

    [DataField(readOnly: true)]
    public EntityUid? Wearer;

    public bool ActiveCooldown;
}
