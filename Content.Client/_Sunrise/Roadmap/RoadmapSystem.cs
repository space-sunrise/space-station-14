using Content.Shared._Sunrise.Roadmap;

namespace Content.Client._Sunrise.Roadmap;

public sealed class RoadmapSystem : EntitySystem
{
    public event Action<RoadmapLikesStateEvent>? OnRoadmapLikesUpdated;
    public RoadmapLikesStateEvent? CachedLikesState { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RoadmapLikesStateEvent>(OnRoadmapLikesState);
    }

    public void RequestLikes()
    {
        RaiseNetworkEvent(new RequestRoadmapLikesEvent());
    }

    public void RequestLikeToggle(string itemId)
    {
        RaiseNetworkEvent(new RequestRoadmapLikeEvent(itemId));
    }

    private void OnRoadmapLikesState(RoadmapLikesStateEvent msg, EntitySessionEventArgs args)
    {
        CachedLikesState = msg;
        OnRoadmapLikesUpdated?.Invoke(msg);
    }
}
