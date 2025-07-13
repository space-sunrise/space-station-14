// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared._Sunrise.PlayerCache;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.PlayerCache;

public sealed class PlayerCacheManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    private PlayerCacheData _cache = new();

    public event Action? CacheChanged;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgPlayerCacheSync>();
        _netManager.RegisterNetMessage<MsgPlayerCacheRequest>(OnCacheRequest);
    }

    private void OnCacheRequest(MsgPlayerCacheRequest msg)
    {
        var cfg = IoCManager.Resolve<Robust.Shared.Configuration.IConfigurationManager>();
        var data = new PlayerCacheData
        {
            GhostTheme = cfg.GetCVar(SunriseCCVars.SponsorGhostTheme),
            Pet = cfg.GetCVar(SunriseCCVars.SponsorPet)
        };
        var sync = new MsgPlayerCacheSync { Cache = data };
        _netManager.ClientSendMessage(sync);
        SetCache(data);
    }

    public PlayerCacheData GetCache() => _cache;
    public void SetCache(PlayerCacheData data)
    {
        _cache = data;
        CacheChanged?.Invoke();
    }

    public bool TryGetCachedGhostTheme(out string? theme)
    {
        if (!string.IsNullOrEmpty(_cache.GhostTheme))
        {
            theme = _cache.GhostTheme;
            return true;
        }
        theme = null;
        return false;
    }
    public bool TryGetCachedPet(out string? pet)
    {
        if (!string.IsNullOrEmpty(_cache.Pet))
        {
            pet = _cache.Pet;
            return true;
        }
        pet = null;
        return false;
    }
}
