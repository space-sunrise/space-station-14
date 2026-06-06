namespace Content.Shared._Sunrise.ScanGate.Components;

/// <summary>
/// Marks an entity as able to bypass scan gate detection.
/// </summary>
[RegisterComponent]
public sealed partial class ScanByPassComponent : Component
{
    /// <summary>
    /// Whether the entity needs to be powered to bypass the scan gate.
    /// </summary>
    [DataField]
    public bool Powered = true;

    /// <summary>
    /// Whether the bypass ability can be toggled on and off.
    /// </summary>
    [DataField]
    public bool Toggleable;
}
