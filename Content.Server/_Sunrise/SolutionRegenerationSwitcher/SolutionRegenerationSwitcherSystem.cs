using Content.Server.Chemistry.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.SolutionRegenerationSwitcher
{
    public sealed class SolutionRegenerationSwitcherSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("chemistry");

            SubscribeLocalEvent<SolutionRegenerationSwitcherComponent, GetVerbsEvent<Verb>>(AddSwitchVerb);
        }

        private void AddSwitchVerb(EntityUid uid, SolutionRegenerationSwitcherComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            foreach (var componentOption in component.Options)
            {
                if (!_prototypeManager.TryIndex(componentOption.Reagent.Prototype, out ReagentPrototype? proto))
                {
                    _sawmill.Error($"Can't get get reagent prototype {componentOption.Reagent.Prototype} for {ToPrettyString(uid)}");
                    return;
                }

                Verb reagent = new()
                {
                    Text = proto.LocalizedName,
                    Category = VerbCategory.ReagentSwitch,
                    Act = () =>
                    {
                        SwitchReagent(uid, componentOption, component, args.User);
                    },
                    Priority = 2,
                    Message = Loc.GetString("solution-regeneration-switcher-switch-verb-text"),
                };
                args.Verbs.Add(reagent);
            }
        }

        private void SwitchReagent(EntityUid uid, ReagentQuantity reagent, SolutionRegenerationSwitcherComponent component, EntityUid user)
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

            if (!_prototypeManager.TryIndex(reagent.Reagent.Prototype, out ReagentPrototype? proto))
            {
                _sawmill.Error($"Can't get get reagent prototype {reagent.Reagent.Prototype} for {ToPrettyString(uid)}");
                return;
            }

            _popups.PopupEntity(Loc.GetString("solution-regeneration-switcher-switched", ("reagent", proto.LocalizedName)), user, user);
        }
    }
}
