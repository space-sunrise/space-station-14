using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.ScanGate.Components;

/// <summary>
/// Marks an entity as detectable by a scan gate.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ScanDetectableComponent : Component;
