using Content.Shared.Preferences;

namespace Content.Shared._Sunrise.Humanoid.Events;

[ByRefEvent]
public readonly record struct HumanoidProfileLoadedEvent(HumanoidCharacterProfile Profile);
