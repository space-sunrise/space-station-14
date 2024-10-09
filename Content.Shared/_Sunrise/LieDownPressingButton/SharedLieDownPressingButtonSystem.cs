using Content.Shared.DoAfter;
using Robust.Shared.Player;
using Robust.Shared.Input.Binding;
using JetBrains.Annotations;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;

namespace Content.Shared._Sunrise.SharedLieDownPressingButtonSystem
{
    public abstract class SharedLieDownPressingButtonSystem : EntitySystem
    {
        [Dependency] private readonly StandingStateSystem _standing = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

        static private Angle _defaultAngleRotation = 0;

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
            .Bind(KeyFunctions.LieDown, InputCmdHandler.FromDelegate(HandlePressButtonLieDown, handle: false, outsidePrediction: false))
            .Register<SharedLieDownPressingButtonSystem>();

            SubscribeLocalEvent<StandingStateComponent, LieDownDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<StandingStateComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        }

        private void OnDoAfter(Entity<StandingStateComponent> ent, ref LieDownDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (!TryComp<StandingStateComponent>(ent, out var standingStateComponent))
                return;

            if (!standingStateComponent.Standing)
            {
                TryStand((ent, standingStateComponent));
                return;
            }

            TryLieDown((ent, standingStateComponent));
        }


        private void OnRefreshMovementSpeed(Entity<StandingStateComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
        {
            if (_standing.IsDown(ent))
                args.ModifySpeed(0.4f, 0.4f);
            else
                args.ModifySpeed(1f, 1f);
        }

        private void HandlePressButtonLieDown(ICommonSession? session)
        {
            if (session?.AttachedEntity is var attachedEnt && attachedEnt is null)
                return;

            var ent = attachedEnt.Value;

            var doAfterArgs = new DoAfterArgs(EntityManager, ent, 1, new LieDownDoAfterEvent(), ent, ent)
            {
                BreakOnDamage = true,
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
        }

        [PublicAPI]
        public bool TryLieDown(Entity<StandingStateComponent?> ent)
        {
            if (!HasComp<LieDownPressingButtonComponent>(ent) && !HasComp<HumanoidAppearanceComponent>(ent))
                return false;

            /*if (ent.Comp is null)
                ent.Comp = AddComp<StandingStateComponent>(ent);*/

            LieDown((ent, ent.Comp));
            return true;
        }

        // Default lie down handler, for LieDownPressingButtonComponent should be wrote something different
        private void LieDown(Entity<StandingStateComponent?> ent)
        {
            /*_rotationVisuals.SetHorizontalAngle(ent.Owner, _defaultAngleRotation);*/
            _standing.Down(ent, playSound: true, dropHeldItems: false, force: true);
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
            /*Dirty(ent);*/
        }

        [PublicAPI]
        public bool TryStand(Entity<StandingStateComponent?> ent)
        {
            if (ent.Comp is null)
                return false;

            Stand((ent, ent.Comp));
            return true;
        }

        private void Stand(Entity<StandingStateComponent> ent)
        {
            _standing.Stand(ent, standingState: ent.Comp);
            _movementSpeed.RefreshMovementSpeedModifiers(ent);
            /*_rotationVisuals.ResetHorizontalAngle(ent.Owner);*/

            /*Dirty(ent);*/
        }
    }
}
