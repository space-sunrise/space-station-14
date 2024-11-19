using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Rotation;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Standing
{
    public sealed class StandingStateSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
        [Dependency] private readonly SharedBuckleSystem _buckle = default!;

        private const int StandingCollisionLayer = (int) CollisionGroup.MidImpassable;

        public bool IsDown(EntityUid uid, StandingStateComponent? standingState = null)
        {
            if (!Resolve(uid, ref standingState, false))
                return false;

            return standingState.CurrentState is StandingState.Lying or StandingState.GettingUp;
        }

        public bool Down(EntityUid uid,
            bool playSound = true,
            bool dropHeldItems = true,
            bool force = false,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null,
            HandsComponent? hands = null,
            LayingDownComponent? layingDown = null)
        {
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional components
            Resolve(uid, ref appearance, ref hands, ref layingDown, false);

            if (standingState.CurrentState is StandingState.Lying)
                return true;

            // Even if we're getting up, we want to reset to lying down
            if (standingState.CurrentState is StandingState.GettingUp)
            {
                standingState.CurrentState = StandingState.Lying;
                Dirty(uid, standingState);
                return true;
            }

            if (dropHeldItems && hands != null)
                RaiseLocalEvent(uid, new DropHandItemsEvent(), false);

            // Only check buckle if we're not forcing
            if (!force)
            {
                if (TryComp(uid, out BuckleComponent? buckle) &&
                    buckle.Buckled &&
                    !_buckle.TryUnbuckle(uid, uid, buckleComp: buckle))
                    return false;

                var msg = new DownAttemptEvent();
                RaiseLocalEvent(uid, msg, false);

                if (msg.Cancelled)
                    return false;
            }

            standingState.CurrentState = StandingState.Lying;

            Dirty(uid, standingState);
            RaiseLocalEvent(uid, new DownedEvent(), false);

            _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Horizontal, appearance);

            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var (key, fixture) in fixtureComponent.Fixtures)
                {
                    if ((fixture.CollisionMask & StandingCollisionLayer) == 0)
                        continue;

                    standingState.ChangedFixtures.Add(key);
                    _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask & ~StandingCollisionLayer, manager: fixtureComponent);
                }
            }

            if (standingState.LifeStage > ComponentLifeStage.Starting && playSound)
                _audio.PlayPredicted(standingState.DownSound, uid, null);

            _movement.RefreshMovementSpeedModifiers(uid);
            return true;
        }

        public bool Stand(EntityUid uid,
            StandingStateComponent? standingState = null,
            AppearanceComponent? appearance = null,
            LayingDownComponent? layingDown = null,
            bool force = false)
        {
            if (!Resolve(uid, ref standingState, false))
                return false;

            Resolve(uid, ref appearance, ref layingDown, false);

            // Already standing
            if (standingState.CurrentState is StandingState.Standing)
                return true;

            if (!force && TryComp(uid, out BuckleComponent? buckle))
            {
                if (buckle.Buckled)
                {
                    if (!_buckle.TryUnbuckle(uid, uid, buckleComp: buckle))
                        return false;
                }
            }

            if (!force)
            {
                var msg = new StandAttemptEvent();
                RaiseLocalEvent(uid, msg, false);

                if (msg.Cancelled)
                    return false;
            }

            standingState.CurrentState = StandingState.Standing;
            Dirty(uid, standingState);
            RaiseLocalEvent(uid, new StoodEvent(), false);

            _appearance.SetData(uid, RotationVisuals.RotationState, RotationState.Vertical, appearance);

            if (TryComp(uid, out FixturesComponent? fixtureComponent))
            {
                foreach (var key in standingState.ChangedFixtures)
                {
                    if (fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                        _physics.SetCollisionMask(uid, key, fixture, fixture.CollisionMask | StandingCollisionLayer, fixtureComponent);
                }
            }
            standingState.ChangedFixtures.Clear();
            _movement.RefreshMovementSpeedModifiers(uid);

            return true;
        }
    }

    public sealed class DropHandItemsEvent : EventArgs { }
    public sealed class DownAttemptEvent : CancellableEntityEventArgs { }
    public sealed class StandAttemptEvent : CancellableEntityEventArgs { }
    public sealed class StoodEvent : EntityEventArgs { }
    public sealed class DownedEvent : EntityEventArgs { }
}
