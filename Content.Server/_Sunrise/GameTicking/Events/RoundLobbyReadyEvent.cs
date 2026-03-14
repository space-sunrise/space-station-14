#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server._Sunrise.GameTicking.Events;

[ByRefEvent]
public record struct RoundLobbyReadyEvent(int RoundId);
