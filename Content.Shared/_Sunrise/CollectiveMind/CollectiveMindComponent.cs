using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CollectiveMind;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class CollectiveMindComponent : Component
{
    /// <summary>
    /// Коллективный разум для сообщений без явно указанного канала.
    /// Если поле не задано, автоматически выбирается единственный доступный для отправки разум.
    /// </summary>
    [DataField]
    public ProtoId<CollectiveMindPrototype>? DefaultMind;

    /// <summary>
    /// Перенаправляет обычную речь и шепот в коллективный разум.
    /// Не влияет на эмоуты и сообщения, отправленные через Action.
    /// </summary>
    [DataField]
    public bool RedirectSpeech;

    /// <summary>
    /// Коллективные разумы, доступные сущности.
    /// Каждая запись задаёт тип разума, независимую группу и права на отправку и получение сообщений.
    /// </summary>
    [DataField]
    public List<CollectiveMindMembership> Memberships = [];

    /// <summary>
    /// Суммарные права для отображения каналов чата на клиенте.
    /// </summary>
    [AutoNetworkedField]
    public CollectiveMindPermissions ClientPermissions;
}

[DataDefinition]
public partial record struct CollectiveMindMembership
{
    [DataField]
    public ProtoId<CollectiveMindPrototype> Mind { get; set; }

    /// <summary>
    /// Идентификатор независимой группы. Для общего разума равен <see langword="null"/>.
    /// Используется для разделения участников на группы в рамках одного коллективного разума.
    /// </summary>
    public EntityUid? Group { get; set; }

    /// <summary>
    /// Права участника в коллективном разуме.
    /// </summary>
    [DataField]
    public CollectiveMindPermissions Permissions { get; set; } = CollectiveMindPermissions.Full;
}

/// <summary>
/// Права доступа к коллективному разуму.
/// </summary>
[Flags, Serializable, NetSerializable]
public enum CollectiveMindPermissions : byte
{
    None = 0,
    Send = 1 << 0, // может только отправлять сообщения
    Receive = 1 << 1, // может только получать сообщения
    Full = Send | Receive, // может отправлять и получать сообщения
}

/// <summary>
/// Коллективный разум через Action System. Используется для отправки сообщений в разум через интерфейс (а не напрямую через чат).
/// </summary>
public sealed partial class CollectiveMindSendActionEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<CollectiveMindPrototype>? CollectiveMind;

    /// Локализация в полях интерфейса

    [DataField]
    public LocId DialogTitle = "collective-mind-action-dialog-title";

    [DataField]
    public LocId DialogPrompt = "collective-mind-action-dialog-prompt";
}
