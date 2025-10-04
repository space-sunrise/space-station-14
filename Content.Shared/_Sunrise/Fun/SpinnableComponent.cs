using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Fun
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SpinnerComponent : Component
    {
        [DataField("minSpinSeconds")] public float MinSpinSeconds = 3.0f;
        [DataField("maxSpinSeconds")] public float MaxSpinSeconds = 6.0f;
        [DataField("minDegreesPerSecond")] public float MinDegPerSec = 500f;
        [DataField("maxDegreesPerSecond")] public float MaxDegPerSec = 2000f;
        [DataField("brakeFactor")] public float BrakeFactor = 0.968f;

        [ViewVariables] public bool IsSpinning;
        [ViewVariables] public float RemainingSeconds;
        [ViewVariables] public float CurrentDegPerSec;
    }

    [Serializable, NetSerializable]
    public sealed class SpinnerComponentState : ComponentState
    {
        public bool IsSpinning;
        public float RemainingSeconds;
        public float CurrentDegPerSec;

        public SpinnerComponentState(bool spinning, float remaining, float degPerSec)
        {
            IsSpinning = spinning;
            RemainingSeconds = remaining;
            CurrentDegPerSec = degPerSec;
        }
    }
}
