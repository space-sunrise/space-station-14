using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.StationGoal
{
    [RegisterComponent]
    public sealed partial class StationGoalComponent : Component
    {
        [DataField]
        public List<ProtoId<StationGoalPrototype>> Goals = new();
    }
}
