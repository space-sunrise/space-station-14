using Content.Shared.Administration;

namespace Content.Client.Administration.Systems;

/// <summary>
/// Client system for handling admin who requests
/// </summary>
public sealed class AdminWhoSystem : EntitySystem
{
    public event Action<List<AdminWhoEntry>>? OnAdminWhoUpdate;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeNetworkEvent<AdminWhoResponseEvent>(OnAdminWhoResponse);
    }

    /// <summary>
    /// Request the list of online administrators from the server
    /// </summary>
    public void RequestAdminWho()
    {
        RaiseNetworkEvent(new RequestAdminWhoEvent());
    }

    private void OnAdminWhoResponse(AdminWhoResponseEvent args, EntitySessionEventArgs session)
    {
        OnAdminWhoUpdate?.Invoke(args.Admins);
    }
}