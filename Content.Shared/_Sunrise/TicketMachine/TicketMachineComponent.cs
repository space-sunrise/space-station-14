using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Shared._Sunrise.TicketMachine
{
    [RegisterComponent]
    public partial class TicketMachineComponent : Component
    {
        [ViewVariables]
        public const int MaxTicketNumber = 99;

        [ViewVariables]
        public int CurrentTicketNumber { get; private set; } = 1;

        private readonly List<EntityUid> _issuedTickets = new List<EntityUid>();

        [DataField("inactiveState")]
        public string? InactiveState;

        [DataField("ticketMachine100State")]
        public string? TicketMachine100State;

        [DataField("ticketMachine50State")]
        public string? TicketMachine50State;

        [DataField("ticketMachine0State")]
        public string? TicketMachine0State;

        public void AddTicket(EntityUid ticketId)
        {
            _issuedTickets.Add(ticketId);
        }

        public void BurnTickets(TimeSpan currentTime)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var ticketId in _issuedTickets)
            {
                if (entityManager.TryGetComponent(ticketId, out TicketComponent? ticketComponent))
                {
                    ticketComponent?.Burn(currentTime);
                }
            }
            _issuedTickets.Clear();
        }

        public void RefillMachine(TimeSpan currentTime)
        {
            BurnTickets(currentTime);
            SetCurrentTicketNumber(0);
        }

        public void SetCurrentTicketNumber(int number)
        {
            CurrentTicketNumber = number;
        }
    }
	
    [Serializable, NetSerializable]
    public enum VendingMachineVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum TicketMachineVisualState
    {
        inactive,
        ticketmachine_100,
        ticketmachine_50,
        ticketmachine_0,
    }

    [Serializable, NetSerializable]
    public enum TicketMachineVisualLayers : byte
    {
        Base,
        Numbers_1,
        Numbers_2,
        Numbers_3
    }
}
