using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Shared._Sunrise.Humanoid.Events;

/// <summary>
/// Raised on a humanoid entity after <see cref="SharedHumanoidAppearanceSystem.LoadProfile"/>
/// has applied all profile-derived appearance fields and marked the component dirty.
/// Subscribers must treat <paramref name="Profile"/> as read-only.
/// </summary>
[ByRefEvent]
public readonly record struct HumanoidProfileLoadedEvent(HumanoidCharacterProfile Profile);
