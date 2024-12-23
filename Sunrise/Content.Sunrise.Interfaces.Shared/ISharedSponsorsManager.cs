using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Sunrise.Interfaces.Shared;

public interface ISharedSponsorsManager
{
    public void Initialize();

    public event Action? LoadedSponsorInfo;
    public event Action<List<SponsorInfo>>? LoadedSponsorTiers;

    // Client
    public List<string> GetClientPrototypes();
    public bool ClientAllowedRespawn();

    public bool ClientIsSponsor();
    public List<SponsorInfo> GetSponsorTiers();

    // Server
    public bool TryGetPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public bool TryGetOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title);
    public bool TryGetOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public bool TryGetGhostThemes(NetUserId userId, [NotNullWhen(true)] out List<string>? ghostTheme);
    public bool TryGetBypassRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? bypassRoles);
    public int GetExtraCharSlots(NetUserId userId);
    public bool HavePriorityJoin(NetUserId userId);
    public bool IsSponsor(NetUserId userId);
    public bool IsAllowedRespawn(NetUserId userId);
    public List<ICommonSession> PickPrioritySessions(List<ICommonSession> sessions, string roleId);
    public NetUserId? PickRoleSession(HashSet<NetUserId> users, string roleId);
    public bool TryGetPriorityGhostRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityAntags);
    public bool TryGetPriorityAntags(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityAntags);
    public bool TryGetPriorityRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityRoles);
    public void SetCachedGhostTheme(NetUserId userId, string ghostTheme);
    public bool TryGetCachedGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme);
    public void Update();
}

[Serializable, NetSerializable]
public sealed class SponsorInfo
{
    [JsonPropertyName("tier")]
    public int Tier { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("oocColor")]
    public string? OOCColor { get; set; }

    [JsonPropertyName("priorityJoin")]
    public bool HavePriorityJoin { get; set; } = false;

    [JsonPropertyName("extraSlots")]
    public int ExtraSlots { get; set; }

    [JsonPropertyName("allowedRespawn")]
    public bool AllowedRespawn { get; set; } = false;

    [JsonPropertyName("ghostThemes")]
    public string[] GhostThemes { get; set; } = [];

    [JsonPropertyName("allowedMarkings")]
    public string[] AllowedMarkings { get; set; } = [];

    [JsonPropertyName("allowedVoices")]
    public string[] AllowedVoices { get; set; } = [];

    [JsonPropertyName("allowedLoadouts")]
    public string[] AllowedLoadouts { get; set; } = [];

    [JsonPropertyName("allowedSpecies")]
    public string[] AllowedSpecies { get; set; } = [];

    [JsonPropertyName("openAntags")]
    public string[] OpenAntags { get; set; } = [];

    [JsonPropertyName("openRoles")]
    public string[] OpenRoles { get; set; } = [];

    [JsonPropertyName("openGhostRoles")]
    public string[] OpenGhostRoles { get; set; } = [];

    [JsonPropertyName("priorityAntags")]
    public string[] PriorityAntags { get; set; } = [];

    [JsonPropertyName("priorityRoles")]
    public string[] PriorityRoles { get; set; } = [];

    [JsonPropertyName("priorityGhostRoles")]
    public string[] PriorityGhostRoles { get; set; } = [];

    [JsonPropertyName("BypassRoles")]
    public string[] BypassRoles { get; set; } = [];
}
