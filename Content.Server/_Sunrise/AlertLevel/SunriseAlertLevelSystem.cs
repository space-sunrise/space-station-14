
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking;
using Content.Server._Sunrise.StationEvents.Events;

namespace Content.Server._Sunrise.AlertLevel;

public sealed class SunriseAlertLevelSystem : EntitySystem
{
	[Dependency] private readonly IConfigurationManager _cfg = default!;
	[Dependency] private readonly IPrototypeManager _prototypeManager = default!;
	[Dependency] private readonly ChatSystem _chatSystem = default!;
	[Dependency] private readonly SharedAudioSystem _audio = default!;
	[Dependency] private readonly StationSystem _stationSystem = default!;
	[Dependency] private readonly RoundEndSystem _roundEnd = default!;
	[Dependency] private readonly GameTicker _gameTicker = default!;

	private const string EpsilonAlertLevel = "epsilon";
	private const string EpsilonBorgLawChanges = "EpsilonDeathSquadLawset";
	private ISawmill _sawmill = default!;

	public override void Initialize()
	{
		base.Initialize();
		_sawmill = Logger.GetSawmill("alert-level");
		SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
		SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
	}

	public override void Update(float frameTime)
	{
		var query = EntityQueryEnumerator<AlertLevelComponent>();
		while (query.MoveNext(out var station, out var alert))
		{
			if (alert.CurrentDelay <= 0)
			{
				if (alert.ActiveDelay)
				{
					RaiseLocalEvent(new AlertLevelDelayFinishedEvent());
					alert.ActiveDelay = false;
				}
				continue;
			}
			alert.CurrentDelay -= frameTime;
		}
	}

	private void OnStationInitialize(StationInitializedEvent args)
	{
		if (!TryComp<AlertLevelComponent>(args.Station, out var alertLevelComponent))
			return;
		if (!_prototypeManager.TryIndex(alertLevelComponent.AlertLevelPrototype, out AlertLevelPrototype? alerts))
		{
			return;
		}
		alertLevelComponent.AlertLevels = alerts;
		var defaultLevel = alertLevelComponent.AlertLevels.DefaultLevel;
		if (string.IsNullOrEmpty(defaultLevel))
		{
			// Deterministic selection of defaultLevel
			defaultLevel = alertLevelComponent.AlertLevels.Levels.Keys.OrderBy(k => k).First();
		}
		SetLevel(args.Station, defaultLevel, false, false, true);
	}

	private void OnPrototypeReload(PrototypesReloadedEventArgs args)
	{
		if (!args.ByType.TryGetValue(typeof(AlertLevelPrototype), out var alertPrototypes)
			|| !alertPrototypes.Modified.TryGetValue("stationAlerts", out var alertObject)
			|| alertObject is not AlertLevelPrototype alerts)
		{
			return;
		}
		var query = EntityQueryEnumerator<AlertLevelComponent>();
		while (query.MoveNext(out var uid, out var comp))
		{
			comp.AlertLevels = alerts;
			if (!comp.AlertLevels.Levels.ContainsKey(comp.CurrentLevel))
			{
				var defaultLevel = comp.AlertLevels.DefaultLevel;
				if (string.IsNullOrEmpty(defaultLevel))
				{
					// Deterministic selection of defaultLevel
					defaultLevel = comp.AlertLevels.Levels.Keys.OrderBy(k => k).First();
				}
				SetLevel(uid, defaultLevel, true, true, true);
			}
		}
		RaiseLocalEvent(new AlertLevelPrototypeReloadedEvent());
	}

	public void SetLevel(EntityUid station, string level, bool playSound, bool announce, bool force = false,
		bool locked = false, MetaDataComponent? dataComponent = null, AlertLevelComponent? component = null)
	{
		if (!Resolve(station, ref component, ref dataComponent)
			|| component.AlertLevels == null
			|| !component.AlertLevels.Levels.TryGetValue(level, out var detail)
			|| component.CurrentLevel == level)
		{
			return;
		}
		if (!force)
		{
			if (!detail.Selectable
				|| component.CurrentDelay > 0
				|| component.IsLevelLocked)
			{
				return;
			}
			component.CurrentDelay = _cfg.GetCVar(CCVars.GameAlertLevelChangeDelay);
			component.ActiveDelay = true;
		}
		// Save previous level for auto access system
		var previousLevel = component.CurrentLevel;
		component.CurrentLevel = level;
		component.IsLevelLocked = locked;
		var stationName = dataComponent.EntityName;
		var name = level.ToLower();
		if (Loc.TryGetString($"alert-level-{level}", out var locName))
		{
			name = locName.ToLower();
		}
		// Announcement text. Is passed into announcementFull.
		var announcement = detail.Announcement;
		if (Loc.TryGetString(detail.Announcement, out var locAnnouncement))
		{
			announcement = locAnnouncement;
		}
		// The full announcement to be spat out into chat.
		var announcementFull = Loc.GetString("alert-level-announcement", ("name", name), ("announcement", announcement));
		var playDefault = false;
		if (playSound)
		{
			if (detail.Sound == null)
				playDefault = true;
		}
		if (announce)
		{
			_chatSystem.DispatchStationAnnouncement(station,
				announcementFull,
				announcementSound: detail.Sound,
				playDefault: playDefault,
				colorOverride: detail.Color,
				sender: stationName);
		}
		// Handle special alert level behaviors
		if (detail.ForceEndRound)
		{
			_roundEnd.EndRound();
		}
		// Handle Epsilon alert level
		if (level == EpsilonAlertLevel)
		{
			_sawmill.Info($"Epsilon alert level triggered on station {station}, adding Death Squad Lawset event");
			var eventEnt = _gameTicker.AddGameRule(EpsilonBorgLawChanges);
			var epsilonRule = EntityManager.System<EpsilonDeathSquadLawsetRule>();
			epsilonRule.SetTargetStation(eventEnt, station);
			_gameTicker.StartGameRule(eventEnt);
		}
		RaiseLocalEvent(new AlertLevelChangedEvent(station, level, previousLevel));
	}
}
