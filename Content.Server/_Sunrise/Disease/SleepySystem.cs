// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Bed.Sleep;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.Traits.Assorted;

public sealed class SleepySystem : EntitySystem
{
    [Dependency] private readonly Shared.StatusEffectNew.StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SleepyComponent, ComponentStartup>(SetupNarcolepsy);
    }

    private void SetupNarcolepsy(EntityUid uid, SleepyComponent component, ComponentStartup args)
    {
        component.NextIncidentTime =
            _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }

    public void AdjustNarcolepsyTimer(EntityUid uid, int TimerReset, SleepyComponent? narcolepsy = null)
    {
        if (!Resolve(uid, ref narcolepsy, false))
            return;

        narcolepsy.NextIncidentTime = TimerReset;
    }

    public void SetNarcolepsy(EntityUid uid, Vector2 timeBetweenIncidents, Vector2 durationOfIncident, SleepyComponent? narcolepsy = null)
    {
        if (!Resolve(uid, ref narcolepsy, false))
            return;
        narcolepsy.DurationOfIncident = durationOfIncident;
        narcolepsy.TimeBetweenIncidents = timeBetweenIncidents;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SleepyComponent>();
        while (query.MoveNext(out var uid, out var narcolepsy))
        {
            narcolepsy.NextIncidentTime -= frameTime;

            if (narcolepsy.NextIncidentTime >= 0)
                continue;

            // Set the new time.
            narcolepsy.NextIncidentTime +=
                _random.NextFloat(narcolepsy.TimeBetweenIncidents.X, narcolepsy.TimeBetweenIncidents.Y);

            var duration = _random.NextFloat(narcolepsy.DurationOfIncident.X, narcolepsy.DurationOfIncident.Y);

            // Make sure the sleep time doesn't cut into the time to next incident.
            narcolepsy.NextIncidentTime += duration;

            _statusEffects.TryAddStatusEffectDuration(uid, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(duration));
        }
    }
}
