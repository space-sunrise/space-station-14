#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Vampire.Components;

/// <summary>
/// Marker component for entities considered "unholy" by holy items like the Bible.
/// Vampires are marked unholy when their VampireComponent is present.
/// </summary>
[RegisterComponent]
public sealed partial class UnholyComponent : Component
{
}
