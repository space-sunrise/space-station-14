namespace Content.Server._Sunrise.Fugitive
{
    [RegisterComponent]
    public sealed partial class FugitiveCountdownComponent : Component
    {
        public TimeSpan? AnnounceTime = null;

        [DataField("AnnounceCD")]
        public TimeSpan AnnounceCD = TimeSpan.FromMinutes(5);
    }
}
