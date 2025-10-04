using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Dice
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
        public FixedPoint2 startValue;
        public FixedPoint2 endValue;

        public ChangeDiceSetValueMessage(FixedPoint2 startAmount, FixedPoint2 endAmount)
        {
            startValue = startAmount;
            endValue = endAmount;
        }
    }

    [Serializable, NetSerializable]
    public enum ChangeDiceUiKey
    {
        Key,
    }
}
