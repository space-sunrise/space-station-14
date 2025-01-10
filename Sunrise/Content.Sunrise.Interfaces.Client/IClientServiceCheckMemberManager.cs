using Content.Sunrise.Interfaces.Shared;
using Robust.Client.Graphics;

namespace Content.Sunrise.Interfaces.Client;

public interface IClientServiceCheckMemberManager : ISharedServiceAuthManager
{
    public string ServiceJoinUrl { get; }
    public Texture? ServiceJoinQrcode { get; }
    public string ServiceUsername { get; }
    public ServiceType SelectedService { get; }
}
