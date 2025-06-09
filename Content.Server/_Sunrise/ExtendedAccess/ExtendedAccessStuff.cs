namespace Content.Server._Sunrise.ExtendedAccess;

[DataDefinition]
public partial record struct ExtendedAccessOptions
{
    [DataField] public string? Announcement;
    [DataField] public TimeSpan Delay = TimeSpan.FromSeconds(60);
}
