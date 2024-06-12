using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using Content.Shared._Sunrise.TicketMachine;
using Content.Shared.Interaction;
using Robust.Shared.Random;
using Content.Shared.Throwing;
using Robust.Shared.Maths;

namespace Content.Server._Sunrise.TicketMachine
{
    public class TicketMachineSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TicketMachineComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, TicketMachineComponent machine, AfterInteractEvent args)
        {
            if (args.Target is not { } target || !args.CanReach || args.Handled)
                return;

            IssueTicket(machine, args.User, args.ClickLocation.ToMap(EntityManager));
            UpdateMachineState(machine);
        }

        public void IssueTicket(TicketMachineComponent machine, EntityUid user, MapCoordinates clickLocation)
        {
            if (machine.CurrentTicketNumber >= TicketMachineComponent.MaxTicketNumber)
            {
                return;
            }

            machine.SetCurrentTicketNumber(machine.CurrentTicketNumber + 1);

            // Spawn the ticket at the machine's location
            var spawnCoordinates = Transform(machine.Owner).Coordinates;
            var ticket = _entityManager.SpawnEntity("Ticket", spawnCoordinates);

            if (_entityManager.TryGetComponent(ticket, out TicketComponent? ticketComponent))
            {
                var currentTime = _gameTiming.CurTime;
                ticketComponent.SetNumber(machine.CurrentTicketNumber, currentTime);
            }

            machine.AddTicket(ticket);

            // Use ThrowingSystem to throw the ticket from the machine
            var range = 0.5f; // Set your desired range
            var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
            var force = 10f; // Set your desired force

            _throwingSystem.TryThrow(ticket, direction, force);
        }

        private void UpdateMachineState(TicketMachineComponent machine)
        {
            var currentTicketNumber = machine.CurrentTicketNumber;
            TicketMachineVisualState state;

            if (currentTicketNumber == 0)
            {
                state = TicketMachineVisualState.inactive;
            }
            else if (currentTicketNumber >= TicketMachineComponent.MaxTicketNumber)
            {
                state = TicketMachineVisualState.ticketmachine_0;
            }
            else if (currentTicketNumber >= 50)
            {
                state = TicketMachineVisualState.ticketmachine_50;
            }
            else
            {
                state = TicketMachineVisualState.ticketmachine_100;
            }

            var ev = new VisualsChangedEvent { VisualState = state };
            RaiseLocalEvent(machine.Owner, ev, true);
        }
    }
}