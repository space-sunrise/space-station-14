using System.Globalization;
using Content.Server._Sunrise.NewLife;
using Content.Server._Sunrise.Station;
using Content.Server._Sunrise.TraitorTarget;
using Content.Server.Chat.Systems;
using Content.Server.Preferences.Managers;
using Content.Server.Speech.Components;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.GameTicking;

/// <summary>
/// Handles Sunrise-specific reactions around the standard player spawn lifecycle.
/// </summary>
public sealed class SunrisePlayerSpawnSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NewLifeSystem _newLife = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnPlayerBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerBeforeSpawn(PlayerBeforeSpawnEvent args)
    {
        _newLife.AddUsedCharactersForRespawn(
            args.Player.UserId,
            _preferences.GetPreferences(args.Player.UserId).SelectedCharacterIndex);
        _newLife.SetNextAllowRespawn(
            args.Player.UserId,
            _timing.CurTime + TimeSpan.FromMinutes(_newLife.NewLifeTimeout));
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (HasComp<StationAntagsTargetsComponent>(args.Station))
            EnsureComp<AntagTargetComponent>(args.Mob);

        if (args.Player.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            EnsureComp<OwOAccentComponent>(args.Mob);

        if (!args.LateJoin || args.Silent || args.JobId == null ||
            !_prototype.TryIndex<JobPrototype>(args.JobId, out var job) ||
            !job.JoinNotifyCrew)
        {
            return;
        }

        _chat.DispatchStationAnnouncement(args.Station,
            Loc.GetString("latejoin-arrival-announcement-special",
                ("character", MetaData(args.Mob).EntityName),
                ("gender", args.Profile.Gender),
                ("entity", args.Mob),
                ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job.LocalizedName))),
            Loc.GetString("latejoin-arrival-sender"),
            playDefault: false,
            colorOverride: Color.Gold);
    }
}
