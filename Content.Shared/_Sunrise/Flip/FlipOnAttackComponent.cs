using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Flip
{
    [NetworkedComponent, RegisterComponent]
    public sealed partial class FlipOnAttackComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("probability")]
        public float Probability = 1.0f;
    }
}
