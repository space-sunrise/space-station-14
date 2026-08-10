using Content.Shared.Inventory.Events;
using Content.Shared.StationAi;

namespace Content.Server._Sunrise.Silicons.StationAi;

/// <summary>
/// Позволяет ИИ видеть через предметы со <see cref="StationAiVisionComponent"/>, надетые на персонажей.
/// </summary>
public sealed class WornStationAiVisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiVisionComponent, GotEquippedEvent>(OnVisionItemEquipped);
        SubscribeLocalEvent<StationAiVisionComponent, GotUnequippedEvent>(OnVisionItemUnequipped);
    }

    private void OnVisionItemEquipped(Entity<StationAiVisionComponent> ent, ref GotEquippedEvent args)
    {
        var wearer = args.Equipee;
        var tracker = EnsureComp<WornStationAiVisionTrackerComponent>(wearer);
        tracker.Count++;

        if (tracker.Count == 1 && !HasComp<StationAiVisionComponent>(wearer))
        {
            EnsureComp<StationAiVisionComponent>(wearer);
            tracker.AddedVisionComponent = true;
        }
    }

    private void OnVisionItemUnequipped(Entity<StationAiVisionComponent> ent, ref GotUnequippedEvent args)
    {
        var wearer = args.Equipee;

        if (!TryComp<WornStationAiVisionTrackerComponent>(wearer, out var tracker))
            return;

        tracker.Count--;

        if (tracker.Count > 0)
            return;

        if (tracker.AddedVisionComponent)
            RemComp<StationAiVisionComponent>(wearer);

        RemComp<WornStationAiVisionTrackerComponent>(wearer);
    }
}
