using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Fun
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SpinnerComponent : Component
    {
        [DataField]
        public float MinSpinSeconds = 3.0f;
        [DataField]
        public float MaxSpinSeconds = 6.0f;
        [DataField]
        public float MinDegPerSec = 500f;
        [DataField]
        public float MaxDegPerSec = 2000f;
        [DataField]
        public float BrakeFactor = 0.968f;
        [DataField]
        public float ForceStopSpeed = 10f;
        [DataField]
        public float SmoothStopAtSecond = 0.5f;
        [DataField]
        public float SmoothStopBrakeFactor = 0.995f;

        [ViewVariables]
        public bool IsSpinning;
        [ViewVariables]
        public float RemainingSeconds;
        [ViewVariables]
        public float CurrentDegPerSec;
    }
}
