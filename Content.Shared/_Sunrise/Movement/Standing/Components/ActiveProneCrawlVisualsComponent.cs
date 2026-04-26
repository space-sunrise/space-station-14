using Content.Shared._Sunrise.Movement.Standing.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Standing.Components;

/// <summary>
/// Marks entities that should update prone crawl visuals on rotation changes.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSunriseStandingStateSystem))]
public sealed partial class ActiveProneCrawlVisualsComponent : Component;
