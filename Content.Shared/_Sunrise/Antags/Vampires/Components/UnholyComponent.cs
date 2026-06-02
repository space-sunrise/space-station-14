#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared._Sunrise.Antags.Vampires.Components;

/// <summary>
/// Marker component for entities considered "unholy" by holy items like the Bible.
/// Vampires are marked unholy when their VampireComponent is present.
/// </summary>
[RegisterComponent]
public sealed partial class UnholyComponent : Component
{
}
