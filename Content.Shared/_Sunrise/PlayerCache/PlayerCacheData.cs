using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PlayerCache;

[Serializable, NetSerializable]
public sealed class PlayerCacheData
{
    public string? GhostTheme { get; set; }
    public string? Pet { get; set; }
    public bool? EmoteVisibility { get; set; }
    public string? OocTitle { get; set; }
    public string? OocColor { get; set; }
}
