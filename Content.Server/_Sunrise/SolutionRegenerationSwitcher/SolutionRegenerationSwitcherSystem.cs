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
            SubscribeLocalEvent<SolutionRegenerationSwitcherComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternateSwitchVerb);
        }

        private void AddSwitchVerb(EntityUid uid, SolutionRegenerationSwitcherComponent component, GetVerbsEvent<Verb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            for (var i = 0; i < component.Options.Count; i++)
            {
                var componentOption = component.Options[i];
                if (!_prototypeManager.TryIndex(componentOption.Reagent.Prototype, out ReagentPrototype? proto))
                {
                    _sawmill.Error($"Can't get reagent prototype {componentOption.Reagent.Prototype} for {ToPrettyString(uid)}");
                    return;
                }

                var index = i;
                Verb reagent = new()
                {
                    Text = proto.LocalizedName,
                    Category = VerbCategory.ReagentSwitch,
                    Act = () =>
                    {
                        component.CurrentIndex = index;
                        SwitchReagent(uid, componentOption, component, args.User);
                    },
                    Priority = 2,
                    Message = Loc.GetString("solution-regeneration-switcher-switch-verb-text"),
                };
                args.Verbs.Add(reagent);
            }
        }

        private void AddAlternateSwitchVerb(EntityUid uid, SolutionRegenerationSwitcherComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (component.Options.Count <= 1)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    SwitchToNextReagent(uid, component, args.User);
                },
                Text = Loc.GetString("solution-regeneration-switcher-switch-reagent-alt"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void SwitchToNextReagent(EntityUid uid, SolutionRegenerationSwitcherComponent component, EntityUid user)
        {
            component.CurrentIndex = (component.CurrentIndex + 1) % component.Options.Count;
            var nextReagent = component.Options[component.CurrentIndex];
            SwitchReagent(uid, nextReagent, component, user);
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
