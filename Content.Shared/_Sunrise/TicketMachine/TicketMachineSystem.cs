using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using Content.Shared.Interaction;

namespace Content.Shared._Sunrise.TicketMachine
{
    public class TicketMachineSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TicketMachineComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, TicketMachineComponent machine, AfterInteractEvent args)
        {
            if (args.Target is not { } target || !args.CanReach || args.Handled)
                return;

            IssueTicket(machine, uid);
            UpdateMachineState(machine);
        }

        private void IssueTicket(TicketMachineComponent machine, EntityUid machineUid)
        {
            if (machine.CurrentTicketNumber >= TicketMachineComponent.MaxTicketNumber)
            {
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            machine.SetCurrentTicketNumber(machine.CurrentTicketNumber + 1);

            var coordinates = EntityManager.GetComponent<TransformComponent>(machineUid).Coordinates;
            var ticket = entityManager.SpawnEntity("Ticket", coordinates);
            if (entityManager.TryGetComponent(ticket, out TicketComponent? ticketComponent))
            {
                var currentTime = IoCManager.Resolve<IGameTiming>().CurTime;
                ticketComponent.SetNumber(machine.CurrentTicketNumber, currentTime);
            }
            machine.AddTicket(ticket);
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

    [Serializable, NetSerializable]
    public class VisualsChangedEvent : EntityEventArgs
    {
        public TicketMachineVisualState VisualState { get; set; }
    }
}