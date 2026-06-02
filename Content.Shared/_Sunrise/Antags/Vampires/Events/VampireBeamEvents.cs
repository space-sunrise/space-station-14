using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Antags.Vampires.Events;

/// <summary>
/// Network event to create/update a drain beam on the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class VampireDrainBeamEvent(
    NetEntity source,
    NetEntity target,
    bool create,
    string visualPrototype) : EntityEventArgs
{
    public NetEntity Source { get; } = source;
    public NetEntity Target { get; } = target;
    public bool Create { get; } = create;
    public string VisualPrototype { get; } = visualPrototype;
}

/// <summary>
/// Network event to create/update a blood bond beam on the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class VampireBloodBondBeamEvent(
    NetEntity source,
    NetEntity target,
    bool create,
    string visualPrototype) : EntityEventArgs
{
    public NetEntity Source { get; } = source;
    public NetEntity Target { get; } = target;
    public bool Create { get; } = create;
    public string VisualPrototype { get; } = visualPrototype;
}
