using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Dice
{
    [Serializable, NetSerializable]
    public sealed class ChangeDiceInterfaceState : BoundUserInterfaceState
    {
        public FixedPoint2 Max;
        public FixedPoint2 Min;

        public ChangeDiceInterfaceState(FixedPoint2 max, FixedPoint2 min)
        {
            Max = max;
            Min = min;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ChangeDiceSetValueMessage : BoundUserInterfaceMessage
    {
        public FixedPoint2 StartValue;
        public FixedPoint2 EndValue;

        public ChangeDiceSetValueMessage(FixedPoint2 startAmount, FixedPoint2 endAmount)
        {
            StartValue = startAmount;
            EndValue = endAmount;
        }
    }

    [Serializable, NetSerializable]
    public enum ChangeDiceUiKey
    {
        Key,
    }
}
