using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

/// <summary>
/// Shared suffix used for counters that observe any entity when no target prototype is configured.
/// </summary>
public static class EventListenedConditionKeys
{
    public const string ObserveSuffix = ".Observe";
}

/// <summary>
/// Contract for tutorial conditions backed by recorded gameplay events.
/// </summary>
public interface IEventListenedCondition
{
    /// <summary>
    /// Counter key used to check completed events.
    /// </summary>
    string CounterKey { get; }

    /// <summary>
    /// Counter key used while registering entities that should be observed.
    /// </summary>
    string ObserveKey { get; }

    /// <summary>
    /// Whether this condition should observe any nearby/equipped entity when no target is configured.
    /// </summary>
    bool ObserveAnyWithoutTarget { get; }

    /// <summary>
    /// Optional entity prototype that event targets must match.
    /// </summary>
    EntProtoId? Target { get; }
}

/// <summary>
/// Base data for tutorial conditions that are satisfied by counting gameplay events.
/// </summary>
public abstract partial class EventListenedConditionBase<T> : TutorialConditionBase<T>, IEventListenedCondition
    where T : EventListenedConditionBase<T>
{
    /// <inheritdoc />
    public virtual string CounterKey => typeof(T).Name;

    /// <inheritdoc />
    public string ObserveKey => string.Concat(CounterKey, EventListenedConditionKeys.ObserveSuffix);

    /// <inheritdoc />
    public virtual bool ObserveAnyWithoutTarget => false;

    /// <summary>
    /// Optional entity prototype that must match the recorded event target.
    /// </summary>
    [DataField]
    public EntProtoId? Target { get; set; }

    /// <summary>
    /// Number of matching events required for this condition to pass.
    /// </summary>
    [DataField]
    public int Count = 1;
}
