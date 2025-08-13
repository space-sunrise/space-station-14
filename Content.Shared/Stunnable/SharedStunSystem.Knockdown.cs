using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stunnable;

/// <summary>
/// This contains the knockdown logic for the stun system for organization purposes.
/// </summary>
public abstract partial class SharedStunSystem
{
    private EntityQuery<CrawlerComponent> _crawlerQuery;

    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public static readonly ProtoId<AlertPrototype> KnockdownAlert = "Knockdown";

    private void InitializeKnockdown()
    {
        _crawlerQuery = GetEntityQuery<CrawlerComponent>();

        SubscribeLocalEvent<KnockedDownComponent, RejuvenateEvent>(OnRejuvenate);

        // Startup and Shutdown
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);

        // Action blockers
        SubscribeLocalEvent<KnockedDownComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);

        // Updating movement a friction
        SubscribeLocalEvent<KnockedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshKnockedSpeed);
        SubscribeLocalEvent<KnockedDownComponent, RefreshFrictionModifiersEvent>(OnRefreshFriction);
        SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);

        // Handling Alternative Inputs
        SubscribeAllEvent<ForceStandUpEvent>(OnForceStandup);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Register<SharedStunSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<KnockedDownComponent>();

        while (query.MoveNext(out var uid, out var knockedDown))
        {
            // If it's null then we don't want to stand up
            if (!knockedDown.AutoStand || knockedDown.DoAfterId.HasValue || knockedDown.NextUpdate > GameTiming.CurTime)
                continue;

            _standing.TryStandUp(uid);
        }
    }

    private void OnRejuvenate(Entity<KnockedDownComponent> entity, ref RejuvenateEvent args)
    {
        SetKnockdownTime(entity, GameTiming.CurTime);

        if (entity.Comp.AutoStand)
            RemComp<KnockedDownComponent>(entity);
    }

    #region Startup and Shutdown

    private void OnKnockInit(Entity<KnockedDownComponent> entity, ref ComponentInit args)
    {
        // Other systems should handle dropping held items...
        _standing.Down(entity, true, false);
        RefreshKnockedMovement(entity);
    }

    private void OnKnockShutdown(Entity<KnockedDownComponent> entity, ref ComponentShutdown args)
    {
        // This is jank but if we don't do this it'll still use the knockedDownComponent modifiers for friction because it hasn't been deleted quite yet.
        entity.Comp.FrictionModifier = 1f;
        entity.Comp.SpeedModifier = 1f;

        _standing.Stand(entity);
        Alerts.ClearAlert(entity, KnockdownAlert);
    }

    #endregion

    #region API

    /// <summary>
    /// Sets the autostand property of a <see cref="KnockedDownComponent"/> on an entity to true or false and dirties it.
    /// Defaults to false.
    /// </summary>
    /// <param name="entity">Entity we want to edit the data field of.</param>
    /// <param name="autoStand">What we want to set the data field to.</param>
    public void SetAutoStand(Entity<KnockedDownComponent?> entity, bool autoStand = false)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.AutoStand = autoStand;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.AutoStand));
    }

    /// <summary>
    /// Cancels the DoAfter of an entity with the <see cref="KnockedDownComponent"/> who is trying to stand.
    /// </summary>
    /// <param name="entity">Entity who we are canceling the DoAfter for.</param>
    public void CancelKnockdownDoAfter(Entity<KnockedDownComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.DoAfterId == null)
            return;

        DoAfter.Cancel(entity.Owner, entity.Comp.DoAfterId.Value);
        entity.Comp.DoAfterId = null;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.DoAfterId));
    }

    /// <summary>
    /// Updates the knockdown timer of a knocked down entity with a given inputted time, then dirties the time.
    /// </summary>
    /// <param name="entity">Entity who's knockdown time we're updating.</param>
    /// <param name="time">The time we're updating with.</param>
    /// <param name="refresh">Whether we're resetting the timer or adding to the current timer.</param>
    public void UpdateKnockdownTime(Entity<KnockedDownComponent?> entity, TimeSpan time, bool refresh = true)
    {
        if (refresh)
            RefreshKnockdownTime(entity, time);
        else
            AddKnockdownTime(entity, time);
    }

    /// <summary>
    /// Sets the next update datafield of an entity's <see cref="KnockedDownComponent"/> to a specific time.
    /// </summary>
    /// <param name="entity">Entity whose timer we're updating</param>
    /// <param name="time">The exact time we're setting the next update to.</param>
    public void SetKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time)
    {
        entity.Comp.NextUpdate = time;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.NextUpdate));
        Alerts.ShowAlert(entity, KnockdownAlert, null, (GameTiming.CurTime, entity.Comp.NextUpdate));
    }

    /// <summary>
    /// Refreshes the amount of time an entity is knocked down to the inputted time, if it is greater than
    /// the current time left.
    /// </summary>
    /// <param name="entity">Entity whose timer we're updating</param>
    /// <param name="time">The time we want them to be knocked down for.</param>
    public void RefreshKnockdownTime(Entity<KnockedDownComponent?> entity, TimeSpan time)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        var knockedTime = GameTiming.CurTime + time;
        if (entity.Comp.NextUpdate < knockedTime)
            SetKnockdownTime((entity, entity.Comp), knockedTime);
    }

    /// <summary>
    /// Adds our inputted time to an entity's knocked down timer, or sets it to the given time if their timer has expired.
    /// </summary>
    /// <param name="entity">Entity whose timer we're updating</param>
    /// <param name="time">The time we want to add to their knocked down timer.</param>
    public void AddKnockdownTime(Entity<KnockedDownComponent?> entity, TimeSpan time)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.NextUpdate < GameTiming.CurTime)
        {
            SetKnockdownTime((entity, entity.Comp), GameTiming.CurTime + time);
            return;
        }

        entity.Comp.NextUpdate += time;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.NextUpdate));
        Alerts.ShowAlert(entity, KnockdownAlert, null, (GameTiming.CurTime, entity.Comp.NextUpdate));
    }

    #endregion

    #region Knockdown Logic

    private void HandleToggleKnockdown(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        ToggleKnockdown(playerEnt);
    }

    /// <summary>
    /// Handles an entity trying to make itself fall down.
    /// </summary>
    /// <param name="entity">Entity who is trying to fall down</param>
    private void ToggleKnockdown(Entity<CrawlerComponent?, KnockedDownComponent?> entity)
    {
        // We resolve here instead of using TryCrawling to be extra sure someone without crawler can't stand up early.
        if (!Resolve(entity, ref entity.Comp1, false))
            return;

        if (!Resolve(entity, ref entity.Comp2, false))
        {
            TryKnockdown(entity.Owner, entity.Comp1.DefaultKnockedDuration, true, false, false);
            return;
        }

        var stand = !entity.Comp2.DoAfterId.HasValue;
        SetAutoStand((entity, entity.Comp2), stand);

        if (!stand || !_standing.TryStandUp(entity))
            CancelKnockdownDoAfter((entity, entity.Comp2));
    }

    private void OnForceStandup(ForceStandUpEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        _standing.TryStandUp(user);
    }

    #endregion

    #region Crawling

    private void OnDamaged(Entity<CrawlerComponent> entity, ref DamageChangedEvent args)
    {
        // We only want to extend our knockdown timer if it would've prevented us from standing up
        if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta == null || GameTiming.ApplyingState)
            return;

        if (args.DamageDelta.GetTotal() >= entity.Comp.KnockdownDamageThreshold)
            RefreshKnockdownTime(entity.Owner, entity.Comp.DefaultKnockedDuration);
    }

    #endregion

    #region Action Blockers

    private void OnStandAttempt(Entity<KnockedDownComponent> entity, ref StandAttemptEvent args)
    {
        if (entity.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnBuckleAttempt(Entity<KnockedDownComponent> entity, ref BuckleAttemptEvent args)
    {
        if (args.User == entity && entity.Comp.NextUpdate > GameTiming.CurTime)
            args.Cancelled = true;
    }

    #endregion

    #region Movement and Friction

    private void RefreshKnockedMovement(Entity<KnockedDownComponent> ent)
    {
        var ev = new KnockedDownRefreshEvent();
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpeedModifier = ev.SpeedModifier;
        ent.Comp.FrictionModifier = ev.FrictionModifier;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
    }

    private void OnRefreshKnockedSpeed(Entity<KnockedDownComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(entity.Comp.SpeedModifier);
    }

    private void OnKnockedTileFriction(Entity<KnockedDownComponent> entity, ref TileFrictionEvent args)
    {
        args.Modifier *= entity.Comp.FrictionModifier;
    }

    private void OnRefreshFriction(Entity<KnockedDownComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }

    #endregion
}
