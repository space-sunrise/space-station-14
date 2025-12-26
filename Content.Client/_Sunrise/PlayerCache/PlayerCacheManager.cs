// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Content.Shared._Sunrise.InteractionsPanel.Data.UI;
using Content.Shared._Sunrise.PlayerCache;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.PlayerCache;

public sealed class PlayerCacheManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private PlayerCacheData _cache = new();

    public event Action? CacheChanged;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgPlayerCacheSync>();
        _netManager.RegisterNetMessage<MsgPlayerCacheRequest>(OnCacheRequest);

        _cfg.OnValueChanged(SunriseCCVars.SponsorGhostTheme,
            s =>
            {
                var cache = GetCache();
                cache.GhostTheme = s;
                SetCache(cache);
            });
        _cfg.OnValueChanged(SunriseCCVars.SponsorPet,
            s =>
            {
                var cache = GetCache();
                cache.Pet = s;
                SetCache(cache);
            });
        _cfg.OnValueChanged(InteractionsCVars.EmoteVisibility,
            b =>
            {
                var cache = GetCache();
                cache.EmoteVisibility = b;
                SetCache(cache);
            });
    }

    private void OnCacheRequest(MsgPlayerCacheRequest msg)
    {
        var data = new PlayerCacheData
        {
            GhostTheme = _cfg.GetCVar(SunriseCCVars.SponsorGhostTheme),
            Pet = _cfg.GetCVar(SunriseCCVars.SponsorPet),
            EmoteVisibility = _cfg.GetCVar(InteractionsCVars.EmoteVisibility),
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
