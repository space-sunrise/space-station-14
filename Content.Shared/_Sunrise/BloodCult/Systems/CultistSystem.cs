namespace Content.Shared._Sunrise.BloodCult.Systems;

/// <summary>
/// Thats need for chat perms update
/// </summary>
public sealed class CultistSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<_Sunrise.BloodCult.Components.BloodCultistComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<_Sunrise.BloodCult.Components.BloodCultistComponent, ComponentShutdown>(OnRemove);
    }

    private void OnInit(EntityUid uid, _Sunrise.BloodCult.Components.BloodCultistComponent component, ComponentStartup args)
    {
        RaiseLocalEvent(new EventCultistComponentState(true));
    }

    private void OnRemove(EntityUid uid, _Sunrise.BloodCult.Components.BloodCultistComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(new EventCultistComponentState(false));
    }
}

public sealed class EventCultistComponentState
{
    public bool Created { get; }
    public EventCultistComponentState(bool state)
    {
        Created = state;
    }
}
