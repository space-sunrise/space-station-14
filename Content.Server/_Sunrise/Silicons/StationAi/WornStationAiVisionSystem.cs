using Content.Shared.Inventory.Events;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;

namespace Content.Server._Sunrise.Silicons.StationAi;

/// <summary>
/// Позволяет ИИ видеть через предметы со <see cref="StationAiVisionComponent"/>, надетые на персонажей.
/// Когда игрок надевает такой предмет (например, бодикамеру), система добавляет ему
/// <see cref="StationAiVisionComponent"/> с нулевым радиусом — достаточным, чтобы
/// <c>StationAiVisionSystem</c> нашёл владельца в пространственном запросе и затем
/// через <c>AddContained</c> обнаружил сам предмет в инвентаре.
/// </summary>
public sealed class WornStationAiVisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiVisionComponent, GotEquippedEvent>(OnVisionItemEquipped);
        SubscribeLocalEvent<StationAiVisionComponent, GotUnequippedEvent>(OnVisionItemUnequipped);
    }

    private void OnVisionItemEquipped(EntityUid uid, StationAiVisionComponent visionComp, GotEquippedEvent args)
    {
        var wearer = args.Equipee;

        var tracker = EnsureComp<WornStationAiVisionTrackerComponent>(wearer);
        tracker.Count++;

        // Добавляем StationAiVisionComponent только если у владельца его ещё нет
        if (tracker.Count == 1 && !HasComp<StationAiVisionComponent>(wearer))
        {
            EnsureComp<StationAiVisionComponent>(wearer);
            tracker.AddedVisionComponent = true;
        }
    }

    private void OnVisionItemUnequipped(EntityUid uid, StationAiVisionComponent visionComp, GotUnequippedEvent args)
    {
        var wearer = args.Equipee;

        if (!TryComp<WornStationAiVisionTrackerComponent>(wearer, out var tracker))
            return;

        tracker.Count--;

        if (tracker.Count > 0)
            return;

        // Последний предмет снят
        if (tracker.AddedVisionComponent)
            RemComp<StationAiVisionComponent>(wearer);

        RemComp<WornStationAiVisionTrackerComponent>(wearer);
    }
}
