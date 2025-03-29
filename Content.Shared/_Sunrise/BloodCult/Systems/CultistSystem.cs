using Content.Shared._Sunrise.BloodCult.Components;

namespace Content.Shared._Sunrise.BloodCult.Systems;

/// <summary>
/// Thats need for chat perms update
/// </summary>
public sealed class CultistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<BloodCultistComponent, ComponentShutdown>(OnRemove);
    }

    private void OnInit(EntityUid uid, BloodCultistComponent component, ComponentStartup args)
    {
        RaiseLocalEvent(new EventCultistComponentState(true));
    }

    private void OnRemove(EntityUid uid, BloodCultistComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(new EventCultistComponentState(false));
    }
}

public sealed class EventCultistComponentState
{
    public EventCultistComponentState(bool state)
    {
        Created = state;
    }

    public bool Created { get; }
}
