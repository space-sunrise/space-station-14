using Content.Shared._Sunrise.SolutionRegenerationSwitcher;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;

namespace Content.Server._Sunrise.SolutionRegenerationSwitcherSystem
{
    public sealed class SolutionRegenerationSwitcherSystem : SharedSolutionRegenerationSwitcherSystem
    {
        [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = null!;
        [Dependency] private readonly SharedPopupSystem _popups = null!;

        private ISawmill _sawmill = null!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("chemistry");
        }

        protected override void SwitchToNextReagent(EntityUid uid,
            SolutionRegenerationSwitcherComponent component,
            EntityUid user)
        {
            component.CurrentIndex = (component.CurrentIndex + 1) % component.Options.Count;
            var nextReagent = component.Options[component.CurrentIndex];
            SwitchReagent(uid, nextReagent, component, user);
        }

        protected override void SwitchReagent(EntityUid uid,
            ReagentQuantity reagent,
            SolutionRegenerationSwitcherComponent component,
            EntityUid user)
        {
            if (!TryComp<SolutionRegenerationComponent>(uid, out var solutionRegenerationComponent))
            {
                _sawmill.Warning($"{ToPrettyString(uid)} has no SolutionRegenerationComponent.");
                return;
            }

            if (!_solutionSystem.TryGetSolution(uid, solutionRegenerationComponent.SolutionName, out var solution))
            {
                _sawmill.Error($"Can't get SolutionRegeneration.Solution for {ToPrettyString(uid)}");
                return;
            }

            if (!TryComp<SolutionRegenerationComponent>(uid, out var solutionRegeneration))
            {
                _sawmill.Error($"Entity {ToPrettyString(uid)} not have SolutionRegenerationComponent");
                return;
            }

            if (solutionRegeneration.Generated.ContainsReagent(reagent.Reagent))
            {
                _popups.PopupEntity(Loc.GetString("solution-regeneration-switcher-already-select"), user, user);
                return;
            }

            // Empty out the current solution.
            if (!component.KeepSolution)
                _solutionSystem.RemoveAllSolution(solution.Value);

            solutionRegeneration.ChangeGenerated(reagent);

            if (!PrototypeManager.TryIndex(reagent.Reagent.Prototype, out ReagentPrototype? proto))
            {
                _sawmill.Error(
                    $"Can't get get reagent prototype {reagent.Reagent.Prototype} for {ToPrettyString(uid)}");
                return;
            }

            _popups.PopupEntity(
                Loc.GetString("solution-regeneration-switcher-switched", ("reagent", proto.LocalizedName)),
                user,
                user);
        }
    }
}
