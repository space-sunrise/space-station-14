using Content.Shared._Sunrise.AntagObjectives;
using Content.Shared.Objectives;

namespace Content.Client._Sunrise.AntagObjectives;

public sealed class AntagObjectivesSystem : EntitySystem
{
    public event Action<AntagObjectivesData>? OnAntagObjectivesUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagObjectivesEvent>(OnAntagObjectivesEvent);
    }

    public void RequestAntagObjectives(NetEntity? netEntity = null)
    {
        if (netEntity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestAntagObjectivesEvent(netEntity.Value));
    }

    private void OnAntagObjectivesEvent(AntagObjectivesEvent msg, EntitySessionEventArgs args)
    {
        var data = new AntagObjectivesData(msg.Objectives, msg.Briefing);

        OnAntagObjectivesUpdate?.Invoke(data);
    }

    public readonly record struct AntagObjectivesData(
        Dictionary<string, List<ObjectiveInfo>> Objectives,
        string? Briefing
    );
}
