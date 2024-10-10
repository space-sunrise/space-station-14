using Content.Sunrise.Interfaces.Shared;
using Robust.Client.Graphics;

namespace Content.Sunrise.Interfaces.Client;

public interface IClientServiceAuthManager : ISharedServiceAuthManager
{
    public string ServiceJoinUrl { get; }
    public Texture? ServiceJoinQrcode { get; }
    public string ServiceUsername { get; }
    public List<ServiceAuthData> ServiceAuthDatas { get; set; }
}
