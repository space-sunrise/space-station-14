using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.TicketMachine
{
    [Serializable, NetSerializable]
    public class VisualsChangedEvent : EntityEventArgs
    {
        public TicketMachineVisualState VisualState { get; set; }
    }
}