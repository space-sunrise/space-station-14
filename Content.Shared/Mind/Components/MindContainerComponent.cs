using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;

namespace Content.Shared.Mind.Components;

/// <summary>
/// This component indicates that this entity may have mind, which is simply an entity with a <see cref="MindComponent"/>.
/// The mind entity is not actually stored in a "container", but is simply stored in nullspace.
/// </summary>
[RegisterComponent, Access(typeof(SharedMindSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindContainerComponent : Component
{
    // Sunrise edit start - для гостпанельки. Сохраняет последний разум, который был в теле
    [AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public EntityUid? LastMindStored;
    // Sunrise edit end

    /// <summary>
    /// The mind controlling this mob. Can be null.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mind;

    /// <summary>
    /// True if we have a mind, false otherwise.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Mind))]
    public bool HasMind => Mind != null;

    /// <summary>
    ///     Whether examining should show information about the mind or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("showExamineInfo"), AutoNetworkedField]
    public bool ShowExamineInfo { get; set; }

    /// <summary>
    /// Whether the mind will be put on a ghost after this component is shutdown.
    /// </summary>
    [DataField]
    public bool GhostOnShutdown = true;

    /// <summary>
    /// Last mind that had control of this mob. If null, it was never controlled by a player.
    /// </summary>
    /// <remarks>
    /// Because minds only get networked to their owners, this field will be <see cref="EntityUid.Invalid"/> on client unless the last mind was the one belonging to the local client.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public EntityUid? LastMind;
}

/// <summary>
/// Base event for all other mind related events.
/// </summary>
public abstract class MindEvent : EntityEventArgs
{
    /// <summary>
    /// <see cref="MindComponent"/> entity currently being handled by the event.
    /// </summary>
    public readonly Entity<MindComponent> Mind;

    /// <summary>
    /// <see cref="MindContainerComponent"/> entity currently being handled by the event.
    /// </summary>
    public readonly Entity<MindContainerComponent> Container;

    public MindEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container)
    {
        Mind = mind;
        Container = container;
    }
}

/// <summary>
/// Event raised directed at a mind-container when a mind gets removed.
/// </summary>
/// <remarks>
/// Called after the owned entity is already set to null. TransferEntity is the entity this mind will be added to afterward, if any.
/// </remarks>
public sealed class MindRemovedMessage : MindEvent
{
    public MindRemovedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container)
        : base(mind, container)
    {
        // Sunrise edit start - для гостпанельки, чтобы сохранить то, что в теле кто-то был
        container.Comp.LastMindStored = mind; // Holy shit это самый курсед кодинг, который я делал намеренно
        // Sunrise edit end
    }
}

/// <summary>
/// Event raised directed at a mind when it gets removed from a mind-container.
/// </summary>
public sealed class MindGotRemovedEvent : MindEvent
{
    public MindGotRemovedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container)
        : base(mind, container)
    {
    }
}

/// <summary>
/// Event raised directed at a mind-container when a mind gets added.
/// </summary>
public sealed class MindAddedMessage : MindEvent
{
    public MindAddedMessage(Entity<MindComponent> mind, Entity<MindContainerComponent> container)
        : base(mind, container)
    {
    }
}

/// <summary>
/// Event raised directed at a mind when it gets added to a mind-container.
/// </summary>
public sealed class MindGotAddedEvent : MindEvent
{
    public MindGotAddedEvent(Entity<MindComponent> mind, Entity<MindContainerComponent> container)
        : base(mind, container)
    {
    }
}
