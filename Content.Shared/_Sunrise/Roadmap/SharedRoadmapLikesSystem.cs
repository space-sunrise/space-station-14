using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Roadmap;

[Serializable, NetSerializable]
public sealed class RequestRoadmapLikesEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class RequestRoadmapLikeEvent(string itemId) : EntityEventArgs
{
    public readonly string ItemId = itemId;
}

[Serializable, NetSerializable]
public sealed class RoadmapLikesStateEvent(RoadmapLikeState[] itemStates) : EntityEventArgs
{
    public readonly RoadmapLikeState[] ItemStates = itemStates;
}

[Serializable, NetSerializable]
public sealed class RoadmapLikeState(string itemId, int likeCount, bool likedByPlayer)
{
    public readonly string ItemId = itemId;
    public readonly int LikeCount = likeCount;
    public readonly bool LikedByPlayer = likedByPlayer;
}
