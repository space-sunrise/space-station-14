using Content.Sunrise.Interfaces.Shared;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Sunrise.Interfaces.Server;

public interface IServerDiscordAuthManager : ISharedDiscordAuthManager
{
    public event EventHandler<ICommonSession>? PlayerVerified;
    public Task<DiscordGenerateLinkResponse> GenerateAuthLink(NetUserId userId, CancellationToken cancel);
    public Task<bool> IsVerified(NetUserId userId, CancellationToken cancel);
}

public sealed record DiscordLinkResponse(string Url, byte[] Qrcode);
public sealed record DiscordGenerateLinkResponse(string Url, byte[] Qrcode);
