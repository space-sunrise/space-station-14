#pragma warning disable IDE0130
using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.GameTicking.Managers;

public sealed partial class ClientGameTicker
{
    /// <summary>
    /// Test-only hook for integration tests that need deterministic lobby fallback values.
    /// </summary>
    internal void SetTestFallbacks(
        string? lobbyType = null,
        string? lobbyParallax = null,
        string? lobbyAnimation = null,
        string? lobbyArt = null)
    {
        if (lobbyType != null)
            LobbyType = lobbyType;

        if (lobbyParallax != null)
            LobbyParallax = lobbyParallax;

        if (lobbyAnimation != null)
            LobbyAnimation = new ProtoId<LobbyBackgroundPrototype>(lobbyAnimation);

        if (lobbyArt != null)
            LobbyArt = lobbyArt;
    }
}
