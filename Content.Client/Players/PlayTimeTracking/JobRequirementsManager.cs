using System.Diagnostics.CodeAnalysis;
using Content.Client.Lobby;
using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Content.Shared.Players.JobWhitelist;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Sunrise.Interfaces.Shared; // Sunrise-Sponsors

namespace Content.Client.Players.PlayTimeTracking;

public sealed class JobRequirementsManager : ISharedPlaytimeManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IClientPreferencesManager _clientPreferences = default!;

    private readonly Dictionary<string, TimeSpan> _roles = new();
    private readonly List<BanInfo> _roleBans = new();
    private readonly List<string> _jobWhitelists = new();

    private ISawmill _sawmill = default!;

    private ISharedSponsorsManager? _sponsorsMgr;  // Sunrise-Sponsors

    public event Action? Updated;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("job_requirements");

        // Yeah the client manager handles role bans and playtime but the server ones are separate DEAL.
        _net.RegisterNetMessage<MsgRoleBans>(RxRoleBans);
        _net.RegisterNetMessage<MsgPlayTime>(RxPlayTime);
        _net.RegisterNetMessage<MsgJobWhitelist>(RxJobWhitelist);

        _client.RunLevelChanged += ClientOnRunLevelChanged;

        IoCManager.Instance!.TryResolveType(out _sponsorsMgr);  // Sunrise-Sponsors
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // Reset on disconnect, just in case.
            _roles.Clear();
            _jobWhitelists.Clear();
            _roleBans.Clear();
        }
    }

    private void RxRoleBans(MsgRoleBans message)
    {
        _sawmill.Debug($"Received roleban info containing {message.Bans.Count} entries.");

        _roleBans.Clear();
        _roleBans.AddRange(message.Bans);

        Updated?.Invoke();
    }

    private void RxPlayTime(MsgPlayTime message)
    {
        _roles.Clear();

        // NOTE: do not assign _roles = message.Trackers due to implicit data sharing in integration tests.
        foreach (var (tracker, time) in message.Trackers)
        {
            _roles[tracker] = time;
        }

        /*var sawmill = Logger.GetSawmill("play_time");
        foreach (var (tracker, time) in _roles)
        {
            sawmill.Info($"{tracker}: {time}");
        }*/
        Updated?.Invoke();
    }

    private void RxJobWhitelist(MsgJobWhitelist message)
    {
        _jobWhitelists.Clear();
        _jobWhitelists.AddRange(message.Whitelist);
        Updated?.Invoke();
    }

    public bool IsAllowed(JobPrototype job, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason, bool skipBanCheck = false)
    {
        reason = null;

        if (!skipBanCheck && _roleBans.Any(ban => ban.Role == $"Job:{job.ID}"))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-ban"));
            return false;
        }

        if (!CheckWhitelist(job, out reason))
            return false;

        var player = _playerManager.LocalSession;
        if (player == null)
            return true;

        // Sunrise-Start
        if (profile != null)
        {
            if (job.SpeciesBlacklist.Contains(profile.Species))
            {
                reason = FormattedMessage.FromUnformatted(Loc.GetString("species-job-fail", ("name", Loc.GetString($"species-name-{profile.Species.Id.ToLower()}"))));
                return false;
            }
        }
        // Sunrise-End

        return CheckRoleRequirements(job, profile, out reason);
    }

    public bool CheckRoleRequirements(JobPrototype job, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var reqs = _entManager.System<SharedRoleSystem>().GetJobRequirement(job);
        return CheckRoleRequirements(reqs, job.ID, profile, out reason); // Sunrise-Edit
    }

    public bool CheckRoleRequirements(HashSet<JobRequirement>? requirements, string protoId, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason) // Sunrise-Edit
    {
        reason = null;

        if (requirements == null || !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        var sponsorPrototypes = _sponsorsMgr?.GetClientPrototypes().ToArray() ?? []; // Sunrise-Sponsors

        var reasons = new List<string>();
        foreach (var requirement in requirements)
        {
            if (requirement.Check(_entManager, _prototypes, profile, _roles, protoId, sponsorPrototypes, out var jobReason)) // Sunrise-Sponsors
                continue;

            reasons.Add(jobReason.ToMarkup());
        }

        reason = reasons.Count == 0 ? null : FormattedMessage.FromMarkupOrThrow(string.Join('\n', reasons));
        return reason == null;
    }

    public bool CheckWhitelist(JobPrototype job, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;
        if (!_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (job.Whitelisted && !_jobWhitelists.Contains(job.ID))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-whitelisted"));
            return false;
        }

        return true;
    }

    public TimeSpan FetchOverallPlaytime()
    {
        return _roles.TryGetValue("Overall", out var overallPlaytime) ? overallPlaytime : TimeSpan.Zero;
    }

    public IEnumerable<KeyValuePair<string, TimeSpan>> FetchPlaytimeByRoles()
    {
        var jobsToMap = _prototypes.EnumeratePrototypes<JobPrototype>();
        var antagsToMap = _prototypes.EnumeratePrototypes<AntagPrototype>();

        foreach (var job in jobsToMap)
        {
            if (_roles.TryGetValue(job.PlayTimeTracker, out var locJobName))
            {
                yield return new KeyValuePair<string, TimeSpan>(job.Name, locJobName);
            }
        }
        foreach (var antag in antagsToMap)
        {
            if (_roles.TryGetValue(antag.PlayTimeTracker, out var locAntagName))
            {
                yield return new KeyValuePair<string, TimeSpan>(antag.Name, locAntagName);
            }
        }
    }

    public IReadOnlyDictionary<string, TimeSpan> GetPlayTimes(ICommonSession session)
    {
        if (session != _playerManager.LocalSession)
        {
            return new Dictionary<string, TimeSpan>();
        }

        return _roles;
    }

    /// <summary>
    /// Checks if any of the specified ban types (jobs or antags) are currently active for the provided bans.
    /// </summary>
    /// <param name="bans">The collection of bans to check against.</param>
    /// <param name="banTypes">The collection of ban types (roles or antags) to check.</param>
    /// <param name="banReason">The reason for the ban, if found.</param>
    /// <param name="expirationTime">The expiration time of the ban, if found.</param>
    /// <returns>True if an active ban is found, otherwise false.</returns>
    private bool IsBanned(IEnumerable<BanInfo> bans, IEnumerable<string> banTypes, [NotNullWhen(true)] out string? banReason, out DateTime? expirationTime)
    {
        banReason = null;
        expirationTime = null;

        foreach (var banType in banTypes)
        {
            var ban = bans.FirstOrDefault(b => b.Role == banType);
            if (ban != null)
            {
                banReason = ban.Reason ?? string.Empty;
                expirationTime = ban.ExpirationTime ?? null;
                return true;
            }
        }

        return false;
    }

    public List<BanInfo> GetAntagBans()
    {
        return _roleBans.Where(ban => ban.Role != null && ban.Role.StartsWith("Antag:")).ToList();
    }

    public List<BanInfo> GetRoleBans()
    {
        return _roleBans.Where(ban => ban.Role != null && ban.Role.StartsWith("Job:")).ToList();
    }

    public bool IsAntagBanned(IEnumerable<string> antags, [NotNullWhen(true)] out string? banReason, out DateTime? expirationTime)
    {
        return IsBanned(GetAntagBans(), antags, out banReason, out expirationTime);
    }

    public bool IsRoleBanned(IEnumerable<string> roles, [NotNullWhen(true)] out string? banReason, out DateTime? expirationTime)
    {
        return IsBanned(GetRoleBans(), roles, out banReason, out expirationTime);
    }
}
