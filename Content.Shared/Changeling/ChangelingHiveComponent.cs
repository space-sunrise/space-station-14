namespace Content.Shared.Changeling;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingHiveComponent : Component
{
    [DataField]
    public ChangelingHive? Hive = null;
}
