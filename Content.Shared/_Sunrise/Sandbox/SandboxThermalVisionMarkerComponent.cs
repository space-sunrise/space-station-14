namespace Content.Shared._Sunrise.Sandbox;

/// <summary>
/// Marks that a player has sandbox thermal vision enabled.
/// Used to reconcile ThermalVisionComponent without conflicting
/// with other sources of thermal vision.
/// </summary>
[RegisterComponent]
public sealed partial class SandboxThermalVisionMarkerComponent : Component { }
