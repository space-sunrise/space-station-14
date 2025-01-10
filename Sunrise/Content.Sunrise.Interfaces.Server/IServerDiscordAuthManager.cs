using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Sunrise.Interfaces.Server;

public interface IServerServiceAuthManager : ISharedServiceAuthManager
{
    public event EventHandler<ICommonSession>? PlayerVerified;
    public Task<ServiceAuthDataResponse?> GenerateDiscordAuthData(NetUserId userId, CancellationToken cancel);
    public Task<ServiceAuthDataResponse?> GenerateTelegramAuthData(NetUserId userId, CancellationToken cancel);
    public Task<List<LinkedServiceData>> CheckLinkedServices(NetUserId userId, string username, CancellationToken cancel);
    public Task<string?> GetDiscordUserId(NetUserId? userId, CancellationToken cancel = default);
}

