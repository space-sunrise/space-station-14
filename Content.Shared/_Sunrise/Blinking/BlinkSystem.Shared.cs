using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Blinking;

public abstract partial class BlinkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;


    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<SleepingComponent> _sleepQuery;

    private const float CriticalBlinkMultiplier = 3f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlinkComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BlinkComponent, SleepStateChangedEvent>(OnSleepStateChanged);

        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _sleepQuery = GetEntityQuery<SleepingComponent>();
    }

    private void OnMapInit(Entity<BlinkComponent> ent, ref MapInitEvent args)
        => UpdateBlinkState(ent);

    private void OnMobStateChanged(Entity<BlinkComponent> ent, ref MobStateChangedEvent args)
        => UpdateBlinkState(ent);

    private void OnSleepStateChanged(Entity<BlinkComponent> ent, ref SleepStateChangedEvent args)
        => UpdateBlinkState(ent);

    private void UpdateBlinkState(Entity<BlinkComponent> ent)
    {
        var uid = ent.Owner;

        var isDead = false;
        var isCritical = false;

        if (_mobStateQuery.TryComp(uid, out var mobState))
        {
            isDead = mobState.CurrentState == MobState.Dead;
            isCritical = mobState.CurrentState == MobState.Critical;
        }

        var isSleeping = _sleepQuery.HasComp(uid);

        Appearance.SetData(uid, BlinkVisuals.EyesClosed, isDead || isSleeping);

        var shouldBeEnabled = !isDead && !isSleeping;
        SetEnabled(ent.AsNullable(), shouldBeEnabled);

        if (isCritical && shouldBeEnabled)
        {
            ApplyCriticalDelay(ent);
        }
    }

    private void ApplyCriticalDelay(Entity<BlinkComponent> ent)
    {
        var remaining = ent.Comp.NextBlinkTime - _timing.CurTime;
        if (remaining > TimeSpan.Zero)
        {
            ent.Comp.NextBlinkTime = _timing.CurTime + TimeSpan.FromTicks((long)(remaining.Ticks * CriticalBlinkMultiplier));
            Dirty(ent);
        }
    }

    public virtual void Blink(Entity<BlinkComponent> ent)
        => ResetBlink(ent);

    private void ResetBlink(Entity<BlinkComponent> ent)
    {
        var baseDelay = _random.Next(ent.Comp.MinBlinkDelay, ent.Comp.MaxBlinkDelay);

        if (_mobStateQuery.TryComp(ent.Owner, out var mobState) && mobState.CurrentState == MobState.Critical)
        {
            baseDelay = TimeSpan.FromTicks((long)(baseDelay.Ticks * CriticalBlinkMultiplier));
        }

        ent.Comp.NextBlinkTime = _timing.CurTime + baseDelay;
        Dirty(ent);
    }

    public void SetEnabled(Entity<BlinkComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);

        if (enabled)
            ResetBlink((ent.Owner, ent.Comp));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<BlinkComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled || curTime < comp.NextBlinkTime)
                continue;

            Blink((uid, comp));
        }
    }
}
