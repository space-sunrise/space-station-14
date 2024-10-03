using Robust.Shared.Player;
using Robust.Shared.Input.Binding;
using JetBrains.Annotations;
using Content.Shared.Humanoid;
using Content.Shared.Rotation;
using Content.Shared.Popups;
using Content.Shared.Standing;

namespace Content.Shared._Sunrise.SharedLieDownPressingButtonSystem
{
    public class SharedLieDownPressingButtonSystem : EntitySystem
    {
        [Dependency] private readonly StandingStateSystem _standing = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        static private Angle _defaultAngleRotation = 0;

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
            .Bind(KeyFunctions.LieDown, InputCmdHandler.FromDelegate(HandlePressButtonLieDown, handle: false, outsidePrediction: false))
            .Register<SharedLieDownPressingButtonSystem>();
        }
        private void HandlePressButtonLieDown(ICommonSession? session)
        {
            if (session?.AttachedEntity is var attachedEnt && attachedEnt is null)
                return;

            var ent = attachedEnt.Value;

            if (TryComp<StandingStateComponent>(ent, out var standingStateComponent))
                if (!standingStateComponent.Standing)
                {
                    TryStand((ent, standingStateComponent));
                    return;
                }
            TryLieDown((ent, standingStateComponent));
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
            _standing.Down(ent, playSound: false, dropHeldItems: false, force: true);

            _popup.PopupPredicted("Ложится прямо как в матрице!", ent, ent);

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
            /*_rotationVisuals.ResetHorizontalAngle(ent.Owner);*/

            /*Dirty(ent);*/
        }
    }
}
