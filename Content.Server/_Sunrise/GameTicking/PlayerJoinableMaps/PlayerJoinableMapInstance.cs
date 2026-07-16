using Robust.Shared.Map;

namespace Content.Server._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Identifies a map and its grids loaded by <see cref="PlayerJoinableMapSystem"/>.
/// </summary>
public readonly record struct PlayerJoinableMapInstance(
    MapId MapId,
    EntityUid MapEntity,
    IReadOnlyList<EntityUid> Grids);
