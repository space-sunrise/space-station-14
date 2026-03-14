#pragma warning disable IDE0130

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
        if (lobbyType != null || lobbyParallax != null || lobbyAnimation != null || lobbyArt != null)
            HasLobbyStatus = true;

        if (lobbyType != null)
            LobbyType = lobbyType;

        if (lobbyParallax != null)
            LobbyParallax = lobbyParallax;

        if (lobbyAnimation != null)
            LobbyAnimation = lobbyAnimation;

        if (lobbyArt != null)
            LobbyArt = lobbyArt;
    }
}
