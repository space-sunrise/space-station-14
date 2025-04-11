namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed partial class CanSleepOnBuckleComponent : Component
    {
        public Dictionary<EntityUid, EntityUid> SleepAction = [];
    }
}
