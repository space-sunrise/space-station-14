namespace Content.Shared._Sunrise.Boss.Components;

/// <summary>
///     This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class HellSpawnConsoleComponent : Component
{
    [DataField]
    public TimeSpan? ActivationTime;
}
