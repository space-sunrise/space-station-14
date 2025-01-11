namespace Content.Server._Sunrise.Cleaning;

[RegisterComponent]
public sealed partial class FootprintAreaCleanerComponent : Component
{
    [DataField]
    public bool Enabled = true;
}
