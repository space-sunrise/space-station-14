using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server.Sunrise.SolutionRegenerationSwitcher
{
    public sealed class SolutionRegenerationSwitcherSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("chemistry");

            SubscribeLocalEvent<SolutionRegenerationSwitcherComponent, GetVerbsEvent<AlternativeVerb>>(AddSwitchVerb);
        }

        private void AddSwitchVerb(EntityUid uid, SolutionRegenerationSwitcherComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (component.Options.Count <= 1)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    SwitchReagent(uid, component, args.User);
                },
                Text = Loc.GetString("autoreagent-switch"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void SwitchReagent(EntityUid uid, SolutionRegenerationSwitcherComponent component, EntityUid user)
        {
            if (!TryComp<SolutionRegenerationComponent>(uid, out var solutionRegenerationComponent))
            {
                _sawmill.Warning($"{ToPrettyString(uid)} has no SolutionRegenerationComponent.");
                return;
            }

            if (component.CurrentIndex + 1 == component.Options.Count)
                component.CurrentIndex = 0;
            else
                component.CurrentIndex++;

            if (!_solutionSystem.TryGetSolution(uid, solutionRegenerationComponent.SolutionName, out var solution))
            {
                _sawmill.Error($"Can't get SolutionRegeneration.Solution for {ToPrettyString(uid)}");
                return;
            }

            // Empty out the current solution.
            if (!component.KeepSolution)
                _solutionSystem.RemoveAllSolution(solution.Value);

            // Replace the generating solution with the newly selected solution.
            var newReagent = component.Options[component.CurrentIndex];

            if (TryComp<SolutionRegenerationComponent>(uid, out var solutionRegeneration))
            {
                solutionRegeneration.ChangeGenerated(newReagent);
            }

            if (!_prototypeManager.TryIndex(newReagent.Reagent.Prototype, out ReagentPrototype? proto))
            {
                _sawmill.Error($"Can't get get reagent prototype {newReagent.Reagent.Prototype} for {ToPrettyString(uid)}");
                return;
            }

            _popups.PopupEntity(Loc.GetString("autoregen-switched", ("reagent", proto.LocalizedName)), user, user);
        }
    }
}
