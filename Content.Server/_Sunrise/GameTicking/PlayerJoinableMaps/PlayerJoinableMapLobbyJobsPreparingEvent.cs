using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Requests external-map owner systems to create any stations required before lobby jobs are indexed.
/// </summary>
/// <remarks>
/// A loader that creates a player-joinable map lazily should subscribe to this event, verify its own
/// activation conditions, and load the map before the handler returns.
/// </remarks>
public sealed class PlayerJoinableMapLobbyJobsPreparingEvent : EntityEventArgs;
