using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.AntagObjectives;

[Serializable, NetSerializable]
public sealed class RequestAntagObjectivesEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestAntagObjectivesEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class AntagObjectivesEvent : EntityEventArgs
{
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;

    public AntagObjectivesEvent(Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing)
    {
        Objectives = objectives;
        Briefing = briefing;
    }
}
