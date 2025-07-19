using Robust.Shared.Network;

namespace Content.Server.Administration.Managers;

public sealed class BanIssuedEventArgs : EventArgs
{
    public NetUserId? Target { get; init; }
    public NetUserId? BanningAdmin { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset Time { get; init; }
}

public sealed class BanPardonedEventArgs : EventArgs
{
    public NetUserId? Target { get; init; }
    public NetUserId? PardoningAdmin { get; init; }
    public DateTimeOffset Time { get; init; }
}
