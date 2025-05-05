using Content.Sunrise.Interfaces.Shared;

namespace Content.Sunrise.Interfaces.Client;

public interface IClientServiceAuthManager : ISharedServiceAuthManager
{
    public List<ServiceAuthData> ServiceAuthDataList { get; set; }
    public List<LinkedServiceData> ServiceLinkedServices { get; set; }
    public event Action<List<LinkedServiceData>>? LoadedServiceLinkedServices;
    public event Action<ServiceAuthData>? LoadedAuthData;
    public void ToggleWindow(ServiceType serviceType);
    public void ResetServiceLink(ServiceType serviceType);
}
