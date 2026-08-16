using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server._Sunrise.AlertArmory;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Station.Components;
using Content.Shared._Sunrise.SecureTerminal;
using Content.Server.GameTicking.Rules.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Radio.EntitySystems;
using Content.Server.Mind;
using Content.Server.Chat.Managers;
using Content.Shared.Roles.Jobs;
using Content.Server.Administration;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.Server.Nuke;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Toggleable;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;

namespace Content.Server._Sunrise.SecureTerminal;

/// <summary>
/// Drives the Secure Command Terminal — proposal creation, multi-party authorization,
/// countdown timers, station-budget charges, and final action execution.
/// </summary>
public sealed partial class SecureCommandTerminalSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly AlertArmorySystem _armory = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _jobs = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly NukeCodePaperSystem _nukeCode = default!;
    [Dependency] private readonly SharedAirlockSystem _airlock = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;

    private static readonly ProtoId<AccessLevelPrototype> CommandAccess = "Command";

    private readonly List<string> _expiredCooldowns = [];
    private readonly List<string> _proposalsToFire = [];
    private readonly List<string> _proposalsToExpire = [];

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SecureCommandTerminalConsoleComponent>(SecureCommandTerminalUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<SecureTerminalRequestMessage>(OnRequest);
            subs.Event<SecureTerminalAuthorizeMessage>(OnAuthorize);
            subs.Event<SecureTerminalDenyMessage>(OnDeny);
            subs.Event<SecureTerminalRecallMessage>(OnRecall);
        });
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        if (!TryComp<SecureCommandTerminalStationComponent>(ev.Station, out var stationComp))
            return;

        stationComp.AlertLevelSetAt = _timing.CurTime;
        UpdateAllConsolesForStation(ev.Station);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var stationQuery = EntityQueryEnumerator<SecureCommandTerminalStationComponent>();
        while (stationQuery.MoveNext(out var stationUid, out var stationComp))
        {
            _expiredCooldowns.Clear();
            foreach (var (key, endTime) in stationComp.Cooldowns)
            {
                if (endTime <= now)
                    _expiredCooldowns.Add(key);
            }

            foreach (var key in _expiredCooldowns)
                stationComp.Cooldowns.Remove(key);

            _proposalsToFire.Clear();
            _proposalsToExpire.Clear();
            SecureTerminalProposalData? pendingProposal = null;
            foreach (var (requestId, proposal) in stationComp.ActiveProposals)
            {
                if (proposal.Status == SecureTerminalProposalStatus.Pending)
                {
                    pendingProposal = proposal;
                    if (proposal.AuthTimer.HasValue && proposal.AuthTimer.Value <= now)
                        _proposalsToExpire.Add(requestId);
                }

                if (proposal.Status == SecureTerminalProposalStatus.Activating &&
                    proposal.ActivateAt.HasValue && proposal.ActivateAt.Value <= now)
                    _proposalsToFire.Add(requestId);
            }

            var consoleQuery = EntityQueryEnumerator<SecureCommandTerminalConsoleComponent>();
            while (consoleQuery.MoveNext(out var consoleUid, out var console))
            {
                if (!console.AuthTerminal || _stations.GetOwningStation(consoleUid) != stationUid)
                    continue;

                _appearance.SetData(consoleUid,
                    ToggleableVisuals.Enabled,
                    pendingProposal != null && !pendingProposal.UsedTerminals.Contains(consoleUid));
            }

            foreach (var requestId in _proposalsToExpire)
            {
                if (stationComp.ActiveProposals.TryGetValue(requestId, out var expiredProposal))
                    RefundFee(expiredProposal);
                stationComp.ActiveProposals.Remove(requestId);
            }

            foreach (var requestId in _proposalsToFire)
            {
                if (_protos.TryIndex<SecureCommandTerminalRequestPrototype>(requestId, out var proto))
                {
                    var requester = stationComp.ActiveProposals.TryGetValue(requestId, out var firingProposal)
                        ? firingProposal.Requester
                        : EntityUid.Invalid;

                    ExecuteAction(stationUid, proto);
                    stationComp.ActiveProposals.Remove(requestId);
                    if (proto.ActionType == SecureTerminalActionType.Armory)
                    {
                        var authorizedAt = now - TimeSpan.FromSeconds(proto.ActivationDelaySecs);
                        stationComp.DeployedArmories[requestId] = authorizedAt;
                        stationComp.DeployedArmoryRequesters[requestId] = requester;
                    }
                    else if (proto.OneTimeUse)
                        stationComp.UsedOnce.Add(requestId);
                    else
                        stationComp.Cooldowns[requestId] = now + TimeSpan.FromSeconds(proto.CooldownSecs);
                }
                else
                {
                    stationComp.ActiveProposals.Remove(requestId);
                    stationComp.Cooldowns[requestId] = now + TimeSpan.FromSeconds(1800);
                }
            }

            if (_expiredCooldowns.Count == 0 && _proposalsToExpire.Count == 0 && _proposalsToFire.Count == 0)
                continue;

            UpdateAllConsolesForStation(stationUid);
        }
    }

    private void OnUiOpened(EntityUid uid, SecureCommandTerminalConsoleComponent comp, BoundUIOpenedEvent ev)
    {
        if (!comp.Enabled)
        {
            _ui.CloseUi(uid, SecureCommandTerminalUiKey.Key, ev.Actor);
            return;
        }
        UpdateConsoleInterface(uid);
    }

    private void OnRequest(EntityUid uid, SecureCommandTerminalConsoleComponent comp, SecureTerminalRequestMessage msg)
    {
        TryRequest((uid, comp), msg);
    }

    /// <summary>
    /// Attempts to begin a terminal request, prompting for a reason when required.
    /// </summary>
    public bool TryRequest(Entity<SecureCommandTerminalConsoleComponent?> ent, SecureTerminalRequestMessage msg)
    {
        if (!CanRequest(ent, msg))
            return false;

        var actor = msg.Actor;
        var proto = _protos.Index<SecureCommandTerminalRequestPrototype>(msg.RequestId);
        if (!proto.RequireReason)
            return TryCreateProposal(ent, msg);

        if (!TryComp<ActorComponent>(actor, out var actorComp) || actorComp.PlayerSession is null)
            return false;

        _quickDialog.OpenDialog<string>(actorComp.PlayerSession,
            Loc.GetString("secure-terminal-reason"),
            string.Empty,
            reason => { TryCreateProposal((ent.Owner, null), msg, reason); },
            () => { });
        return true;
    }

    /// <summary>
    /// Checks whether an actor can start the request flow from this console.
    /// </summary>
    public bool CanRequest(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalRequestMessage msg,
        bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Enabled || !msg.Actor.IsValid())
            return false;

        if (!_protos.TryIndex<SecureCommandTerminalRequestPrototype>(msg.RequestId, out var proto))
            return false;

        var station = _stations.GetOwningStation(ent);
        if (station == null || !HasComp<SecureCommandTerminalStationComponent>(station.Value))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-no-station"), msg.Actor);
            return false;
        }

        if (ent.Comp.AuthTerminal)
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-auth-note"), msg.Actor, PopupType.MediumCaution);
            return false;
        }

        if (HasRequestAccess(msg.Actor, proto))
            return true;

        if (!quiet)
            _popup.PopupCursor(Loc.GetString("secure-terminal-request-denied"), msg.Actor, PopupType.Medium);
        return false;
    }

    /// <summary>
    /// Attempts to validate and create a proposal after any reason prompt has completed.
    /// </summary>
    public bool TryCreateProposal(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalRequestMessage msg,
        string? reason = null)
    {
        if (!CanCreateProposal(ent, msg))
            return false;

        Resolve(ent, ref ent.Comp);
        return DoCreateProposal((ent.Owner, ent.Comp!), msg, reason);
    }

    /// <summary>
    /// Checks request conditions and station funds without changing proposal state.
    /// </summary>
    public bool CanCreateProposal(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalRequestMessage msg,
        bool quiet = false)
    {
        if (!CanRequest(ent, msg, quiet))
            return false;

        var actor = msg.Actor;
        var proto = _protos.Index<SecureCommandTerminalRequestPrototype>(msg.RequestId);
        var stationUid = _stations.GetOwningStation(ent)!.Value;
        var stationComp = Comp<SecureCommandTerminalStationComponent>(stationUid);

        if (proto.RequiresWarDeclared && !IsWarDeclared())
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-requires-war"), actor, PopupType.Medium);
            return false;
        }

        if (proto.RequiresWarNotDeclared && IsWarDeclared())
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-requires-no-war-note"), actor, PopupType.Medium);
            return false;
        }

        if (proto.RequiresAlertLevel != null &&
            TryComp<AlertLevelComponent>(stationUid, out var alertComp) &&
            alertComp.CurrentLevel != proto.RequiresAlertLevel)
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-wrong-alert"), actor, PopupType.Medium);
            return false;
        }

        if (proto.RequiresAlertActiveMinutes > 0 &&
            (_timing.CurTime - stationComp.AlertLevelSetAt).TotalMinutes < proto.RequiresAlertActiveMinutes)
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-alert-not-long-enough"), actor, PopupType.Medium);
            return false;
        }

        if (stationComp.Cooldowns.ContainsKey(msg.RequestId))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-on-cooldown"), actor, PopupType.Medium);
            return false;
        }

        if (stationComp.UsedOnce.Contains(msg.RequestId))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-already-used"), actor, PopupType.Medium);
            return false;
        }

        if (stationComp.ActiveProposals.ContainsKey(msg.RequestId))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-already-pending"), actor, PopupType.Medium);
            return false;
        }

        if (stationComp.DeployedArmories.ContainsKey(msg.RequestId))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-used-note"), actor, PopupType.Medium);
            return false;
        }

        if (stationComp.ActiveProposals.Count > 0)
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-already-active"), actor, PopupType.Medium);
            return false;
        }

        if (proto.Fee > 0)
        {
            if (!TryComp<StationBankAccountComponent>(stationUid, out var bank) ||
                !_cargo.TryGetAccount((stationUid, bank), bank.PrimaryAccount, out var balance) ||
                balance < proto.Fee)
            {
                if (!quiet)
                    _popup.PopupCursor(Loc.GetString("secure-terminal-insufficient-funds", ("fee", proto.Fee)), actor, PopupType.Medium);
                return false;
            }
        }

        return true;
    }

    private bool DoCreateProposal(Entity<SecureCommandTerminalConsoleComponent> ent,
        SecureTerminalRequestMessage msg,
        string? reason)
    {
        var actor = msg.Actor;
        var proto = _protos.Index<SecureCommandTerminalRequestPrototype>(msg.RequestId);
        var stationUid = _stations.GetOwningStation(ent)!.Value;
        var stationComp = Comp<SecureCommandTerminalStationComponent>(stationUid);

        if (proto.Fee > 0)
        {
            var bank = Comp<StationBankAccountComponent>(stationUid);
            if (!_cargo.TryAdjustBankAccount((stationUid, bank), bank.PrimaryAccount, -proto.Fee))
            {
                _popup.PopupCursor(Loc.GetString("secure-terminal-insufficient-funds", ("fee", proto.Fee)), actor, PopupType.Medium);
                return false;
            }

            _popup.PopupCursor(Loc.GetString("secure-terminal-fee-held", ("fee", proto.Fee)), actor, PopupType.Medium);
        }

        var proposal = new SecureTerminalProposalData
        {
            RequestId = msg.RequestId,
            Requester = actor,
            Station = stationUid,
        };
        stationComp.ActiveProposals[msg.RequestId] = proposal;

        if (reason is not null)
            proposal.Reason = reason;

        var groupIndex = FindAvailableAuthGroup(actor, proposal, proto);
        if (groupIndex >= 0)
            AddAuthorization(actor, proposal, groupIndex, ent);

        if (proto.AuthTimer > 0)
            proposal.AuthTimer = _timing.CurTime + TimeSpan.FromSeconds(proto.AuthTimer);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(actor):player} created secure terminal proposal: {msg.RequestId}");

        _chatManager.SendAdminAnnouncement(
            Loc.GetString("secure-terminal-admin-proposed",
                ("actor", MetaData(actor).EntityName),
                ("job", GetJobName(actor)),
                ("request", Loc.GetString(proto.Name))));

        var proposalAnnounce = Loc.GetString("secure-terminal-proposal-created", ("request", Loc.GetString(proto.Name)));
        if (reason is not null)
            proposalAnnounce = Loc.GetString("secure-terminal-proposal-created-reason", ("request", Loc.GetString(proto.Name)), ("reason", reason));

        if (proto.ProposalAnnouncement)
            _chat.DispatchGlobalAnnouncement(proposalAnnounce, colorOverride: proto.AnnouncementColor);

        var proposalRadio = Loc.GetString("secure-terminal-radio-proposal", ("request", Loc.GetString(proto.Name)));
        if (reason is not null)
            proposalRadio = Loc.GetString("secure-terminal-radio-proposal-reason", ("request", Loc.GetString(proto.Name)), ("reason", reason));

        _radio.SendRadioMessage(ent, proposalRadio, "Command", ent);

        CheckAndStartCountdown(stationUid, stationComp, msg.RequestId, proto);
        UpdateAllConsolesForStation(stationUid);
        return true;
    }

    private void OnAuthorize(EntityUid uid, SecureCommandTerminalConsoleComponent comp, SecureTerminalAuthorizeMessage msg)
    {
        TryAuthorizeProposal((uid, comp), msg);
    }

    /// <summary>
    /// Attempts to authorize a pending proposal from this console.
    /// </summary>
    public bool TryAuthorizeProposal(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalAuthorizeMessage msg)
    {
        if (!CanAuthorizeProposal(ent, msg))
            return false;

        Resolve(ent, ref ent.Comp);
        DoAuthorizeProposal((ent.Owner, ent.Comp!), msg);
        return true;
    }

    /// <summary>
    /// Checks whether the actor can claim an authorization slot for the proposal.
    /// </summary>
    public bool CanAuthorizeProposal(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalAuthorizeMessage msg,
        bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Enabled || !msg.Actor.IsValid() ||
            !_protos.TryIndex<SecureCommandTerminalRequestPrototype>(msg.RequestId, out var proto))
            return false;

        var stationUid = _stations.GetOwningStation(ent);
        if (stationUid == null || !TryComp<SecureCommandTerminalStationComponent>(stationUid.Value, out var stationComp))
            return false;

        if (!stationComp.ActiveProposals.TryGetValue(msg.RequestId, out var proposal) ||
            proposal.Status != SecureTerminalProposalStatus.Pending)
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-no-active-proposal"), msg.Actor, PopupType.Medium);
            return false;
        }

        if (ent.Comp.Admin)
            return true;

        if (proposal.Authorizers.Any(authorizer => authorizer.PlayerUid == msg.Actor))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-already-authorized"), msg.Actor, PopupType.Medium);
            return false;
        }

        if (ent.Comp.AuthTerminal && proposal.UsedTerminals.Contains(ent))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-already-activated"), msg.Actor, PopupType.Medium);
            return false;
        }

        if (FindAvailableAuthGroup(msg.Actor, proposal, proto) >= 0)
            return true;

        if (!quiet)
            _popup.PopupCursor(Loc.GetString("secure-terminal-authorize-denied"), msg.Actor, PopupType.Medium);
        return false;
    }

    private void DoAuthorizeProposal(Entity<SecureCommandTerminalConsoleComponent> ent,
        SecureTerminalAuthorizeMessage msg)
    {
        var actor = msg.Actor;
        var proto = _protos.Index<SecureCommandTerminalRequestPrototype>(msg.RequestId);
        var stationUid = _stations.GetOwningStation(ent)!.Value;
        var stationComp = Comp<SecureCommandTerminalStationComponent>(stationUid);
        var proposal = stationComp.ActiveProposals[msg.RequestId];

        if (ent.Comp.Admin)
        {
            proposal.AdminApproved = true;
        }
        else
        {
            var groupIndex = FindAvailableAuthGroup(actor, proposal, proto);
            AddAuthorization(actor, proposal, groupIndex, ent);
        }

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(actor):player} authorized secure terminal proposal: {msg.RequestId}");

        _chatManager.SendAdminAnnouncement(
            Loc.GetString("secure-terminal-admin-authorized",
                ("actor", MetaData(actor).EntityName),
                ("job", GetJobName(actor)),
                ("request", Loc.GetString(proto.Name))));

        CheckAndStartCountdown(stationUid, stationComp, msg.RequestId, proto);
        UpdateAllConsolesForStation(stationUid);
    }

    private void OnDeny(EntityUid uid, SecureCommandTerminalConsoleComponent comp, SecureTerminalDenyMessage msg)
    {
        TryDenyProposal((uid, comp), msg);
    }

    /// <summary>
    /// Attempts to deny and cancel an active proposal.
    /// </summary>
    public bool TryDenyProposal(Entity<SecureCommandTerminalConsoleComponent?> ent, SecureTerminalDenyMessage msg)
    {
        if (!CanDenyProposal(ent, msg))
            return false;

        Resolve(ent, ref ent.Comp);
        DoDenyProposal((ent.Owner, ent.Comp!), msg);
        return true;
    }

    /// <summary>
    /// Checks whether the actor can deny the active proposal.
    /// </summary>
    public bool CanDenyProposal(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalDenyMessage msg,
        bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Enabled || !msg.Actor.IsValid())
            return false;

        var stationUid = _stations.GetOwningStation(ent);
        if (stationUid == null || !TryComp<SecureCommandTerminalStationComponent>(stationUid.Value, out var stationComp))
            return false;

        if (!stationComp.ActiveProposals.ContainsKey(msg.RequestId))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-no-active-proposal"), msg.Actor, PopupType.Medium);
            return false;
        }

        if (ent.Comp.Admin || _access.FindAccessTags(msg.Actor).Contains(CommandAccess))
            return true;

        if (!quiet)
            _popup.PopupCursor(Loc.GetString("secure-terminal-request-denied"), msg.Actor, PopupType.Medium);
        return false;
    }

    private void DoDenyProposal(Entity<SecureCommandTerminalConsoleComponent> ent, SecureTerminalDenyMessage msg)
    {
        var actor = msg.Actor;
        var stationUid = _stations.GetOwningStation(ent)!.Value;
        var stationComp = Comp<SecureCommandTerminalStationComponent>(stationUid);
        var deniedProposal = stationComp.ActiveProposals[msg.RequestId];
        stationComp.ActiveProposals.Remove(msg.RequestId);
        RefundFee(deniedProposal);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(actor):player} denied secure terminal proposal: {msg.RequestId}");

        if (_protos.TryIndex<SecureCommandTerminalRequestPrototype>(msg.RequestId, out var proto))
        {
            _chatManager.SendAdminAnnouncement(
                Loc.GetString("secure-terminal-admin-denied",
                    ("actor", MetaData(actor).EntityName),
                    ("job", GetJobName(actor)),
                    ("request", Loc.GetString(proto.Name))));

            if (proto.ProposalAnnouncement)
            {
                var locKey = ent.Comp.Admin
                    ? "secure-terminal-proposal-denied-cc"
                    : "secure-terminal-proposal-denied";
                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString(locKey, ("request", Loc.GetString(proto.Name))),
                    colorOverride: proto.AnnouncementColor);
            }

            if (!ent.Comp.Admin)
                _radio.SendRadioMessage(ent,
                    Loc.GetString("secure-terminal-radio-denied",
                        ("request", Loc.GetString(proto.Name))),
                    "Command", ent);
        }

        UpdateAllConsolesForStation(stationUid);
    }

    private void OnRecall(EntityUid uid, SecureCommandTerminalConsoleComponent comp, SecureTerminalRecallMessage msg)
    {
        TryRecallArmory((uid, comp), msg);
    }

    /// <summary>
    /// Attempts to recall an armory requested through the terminal.
    /// </summary>
    public bool TryRecallArmory(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalRecallMessage msg)
    {
        if (!CanRecallArmory(ent, msg))
            return false;

        Resolve(ent, ref ent.Comp);
        return DoRecallArmory((ent.Owner, ent.Comp!), msg);
    }

    /// <summary>
    /// Checks whether the actor can recall the selected armory.
    /// </summary>
    public bool CanRecallArmory(Entity<SecureCommandTerminalConsoleComponent?> ent,
        SecureTerminalRecallMessage msg,
        bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp) || !ent.Comp.Enabled || !msg.Actor.IsValid())
            return false;

        var stationUid = _stations.GetOwningStation(ent);
        if (stationUid == null || !TryComp<SecureCommandTerminalStationComponent>(stationUid.Value, out var stationComp) ||
            !_protos.TryIndex<SecureCommandTerminalRequestPrototype>(msg.RequestId, out var proto) ||
            proto.ActionType != SecureTerminalActionType.Armory)
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-no-active-proposal"), msg.Actor, PopupType.Medium);
            return false;
        }

        var hasActivatingArmory = stationComp.ActiveProposals.TryGetValue(msg.RequestId, out var proposal) &&
                                  proposal.Status == SecureTerminalProposalStatus.Activating;
        var hasDeployedArmory = stationComp.DeployedArmories.ContainsKey(msg.RequestId);
        if (!hasActivatingArmory && !hasDeployedArmory)
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-no-active-proposal"), msg.Actor, PopupType.Medium);
            return false;
        }

        if (!ent.Comp.Admin && !_access.FindAccessTags(msg.Actor).Contains(CommandAccess))
        {
            if (!quiet)
                _popup.PopupCursor(Loc.GetString("secure-terminal-request-denied"), msg.Actor, PopupType.Medium);
            return false;
        }

        if (proto.RecallMinDelaySecs <= 0)
            return true;

        TimeSpan authorizedAt;
        if (proposal != null && proposal.ActivateAt.HasValue)
            authorizedAt = proposal.ActivateAt.Value - TimeSpan.FromSeconds(proto.ActivationDelaySecs);
        else if (stationComp.DeployedArmories.TryGetValue(msg.RequestId, out var deployedAuthorizedAt))
            authorizedAt = deployedAuthorizedAt;
        else
            authorizedAt = TimeSpan.Zero;

        var recallAvailableAt = authorizedAt + TimeSpan.FromSeconds(proto.RecallMinDelaySecs);
        if (_timing.CurTime >= recallAvailableAt)
            return true;

        if (!quiet)
            _popup.PopupCursor(Loc.GetString("secure-terminal-recall-too-soon"), msg.Actor, PopupType.Medium);
        return false;
    }

    private bool DoRecallArmory(Entity<SecureCommandTerminalConsoleComponent> ent,
        SecureTerminalRecallMessage msg)
    {
        var actor = msg.Actor;
        var stationUid = _stations.GetOwningStation(ent)!.Value;
        var stationComp = Comp<SecureCommandTerminalStationComponent>(stationUid);
        var proto = _protos.Index<SecureCommandTerminalRequestPrototype>(msg.RequestId);

        if (proto.ArmoryKey != null && !_armory.TryRecallArmory((stationUid, null), proto.ArmoryKey))
        {
            return false;
        }

        stationComp.ActiveProposals.Remove(msg.RequestId);
        stationComp.DeployedArmories.Remove(msg.RequestId);
        stationComp.DeployedArmoryRequesters.Remove(msg.RequestId);
        stationComp.UsedOnce.Add(msg.RequestId);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(actor):player} recalled armory via secure terminal: {msg.RequestId}");

        _chatManager.SendAdminAnnouncement(
            Loc.GetString("secure-terminal-admin-recalled",
                ("actor", MetaData(actor).EntityName),
                ("job", GetJobName(actor)),
                ("request", Loc.GetString(proto.Name))));

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("secure-terminal-armory-recalled",
                ("request", Loc.GetString(proto.Name))),
            colorOverride: proto.AnnouncementColor);

        _radio.SendRadioMessage(ent,
            Loc.GetString("secure-terminal-armory-recalled",
                ("request", Loc.GetString(proto.Name))),
            "Command", ent);

        UpdateAllConsolesForStation(stationUid);
        return true;
    }

    private int FindAvailableAuthGroup(EntityUid actor,
        SecureTerminalProposalData proposal,
        SecureCommandTerminalRequestPrototype proto)
    {
        var accessTags = _access.FindAccessTags(actor);
        var satisfied = BuildSatisfiedGroups(proposal, proto);

        for (var i = 0; i < proto.AuthGroups.Count; i++)
        {
            if (satisfied[i])
                continue;

            var group = proto.AuthGroups[i];
            if (group.Any(accessTags.Contains))
                return i;
        }

        return -1;
    }

    private void AddAuthorization(EntityUid actor,
        SecureTerminalProposalData proposal,
        int groupIndex,
        Entity<SecureCommandTerminalConsoleComponent> terminal)
    {
        string name;
        string job;
        if (_idCard.TryFindIdCard(actor, out var idCard))
        {
            name = idCard.Comp.FullName ?? MetaData(actor).EntityName;
            job = idCard.Comp.LocalizedJobTitle ?? GetJobName(actor);
        }
        else
        {
            name = MetaData(actor).EntityName;
            job = GetJobName(actor);
        }

        if (terminal.Comp.AuthTerminal)
            proposal.UsedTerminals.Add(terminal);

        proposal.Authorizers.Add((actor, name, job, groupIndex));
    }

    /// <summary>
    /// If all auth groups are satisfied, begins the countdown and applies the station budget penalty.
    /// </summary>
    private void CheckAndStartCountdown(EntityUid stationUid,
        SecureCommandTerminalStationComponent stationComp,
        string requestId, SecureCommandTerminalRequestPrototype proto)
    {
        if (!stationComp.ActiveProposals.TryGetValue(requestId, out var proposal)) return;
        if (proposal.Status != SecureTerminalProposalStatus.Pending) return;

        var satisfied = BuildSatisfiedGroups(proposal, proto);
        if (!satisfied.All(s => s)) return;

        if (proto.RequiresAdminApproval && !proposal.AdminApproved)
        {
            if (_adminManager.ActiveAdmins.Count() > 0 || !proto.BypassIfNoAdmin)
            {
                proposal.AuthTimer = null;
                _chat.DispatchGlobalAnnouncement(Loc.GetString("secure-terminal-awaiting-admin", ("request", Loc.GetString(proto.Name))), colorOverride: proto.AnnouncementColor);
                _chatManager.SendAdminAlert(Loc.GetString("secure-terminal-admin", ("request", Loc.GetString(proto.Name)), ("reason", proposal.Reason)));
                _audio.PlayGlobal("/Audio/Misc/adminlarm.ogg",
                    Filter.Empty().AddPlayers(_adminManager.ActiveAdmins),
                    false,
                    AudioParams.Default.WithVolume(-8f));
                return;
            }
        }

        proposal.Status = SecureTerminalProposalStatus.Activating;
        proposal.ActivateAt = _timing.CurTime + TimeSpan.FromSeconds(proto.ActivationDelaySecs);

        if (proto.ProposalAnnouncement)
        {
            var signatories = string.Join(", ",
                proposal.Authorizers.Select(a => $"{a.Name} ({a.Job})"));
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("secure-terminal-authorized-by",
                    ("request", Loc.GetString(proto.Name)),
                    ("signatories", signatories)),
                colorOverride: proto.AnnouncementColor);
        }

        if (proto.Announcement != null)
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString(proto.Announcement),
                colorOverride: proto.AnnouncementColor);

        ApplyBudgetPenalty(stationUid, proto.BudgetPenalty);
    }

    /// <summary>
    /// Refunds some or all of the fee held when a request was created.
    /// </summary>
    private void RefundFee(SecureTerminalProposalData proposal, float fraction = 1f)
    {
        if (_protos.TryIndex<SecureCommandTerminalRequestPrototype>(proposal.RequestId, out var proto))
            RefundFee(proposal.Station, proto, fraction);
    }

    private void RefundFee(EntityUid stationUid, SecureCommandTerminalRequestPrototype proto, float fraction = 1f)
    {
        if (proto.Fee <= 0 || !stationUid.IsValid() ||
            !TryComp<StationBankAccountComponent>(stationUid, out var bank))
            return;

        var amount = (int)(proto.Fee * fraction);
        if (amount > 0)
            _cargo.TryAdjustBankAccount((stationUid, bank), bank.PrimaryAccount, amount);
    }

    private void ApplyBudgetPenalty(EntityUid stationUid, float fraction)
    {
        if (fraction <= 0f || !TryComp<StationBankAccountComponent>(stationUid, out var bank))
            return;

        foreach (var (account, balance) in bank.Accounts.ToArray())
        {
            var amount = (int) MathF.Round(balance * MathF.Min(fraction, 1f));
            if (amount > 0)
                _cargo.TryAdjustBankAccount((stationUid, bank), account, -amount);
        }
    }

    /// <summary>
    /// Executes the prototype's configured action against the station.
    /// </summary>
    private void ExecuteAction(EntityUid stationUid, SecureCommandTerminalRequestPrototype proto)
    {
        switch (proto.ActionType)
        {
            case SecureTerminalActionType.GameRule:
                if (proto.GameruleId != null)
                    _gameTicker.StartGameRule(proto.GameruleId);
                break;

            case SecureTerminalActionType.AlertLevel:
                if (proto.AlertLevel != null)
                    _alertLevel.SetLevel(stationUid, proto.AlertLevel, true, true, true, false);
                break;

            case SecureTerminalActionType.Armory:
                if (proto.ArmoryKey != null)
                    _armory.TrySendArmory((stationUid, null), proto.ArmoryKey);
                break;

            case SecureTerminalActionType.NukeCodes:
                _nukeCode.SendNukeCodes(stationUid);
                break;

            case SecureTerminalActionType.AirlockAccess:
                var airlockQuery = AllEntityQuery<AirlockComponent, TransformComponent>();
                while (airlockQuery.MoveNext(out var ent, out var airlockcomp, out var xform))
                {
                    if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != stationUid)
                        continue;

                    if (HasComp<FirelockComponent>(ent))
                        continue;

                    if (proto.AllowedAccesses is null || proto.AllowedAccesses.Count == 0)
                    {
                        _airlock.SetEmergencyAccess((ent, airlockcomp), proto.AccessEnabled);
                    }
                    else if (_access.GetMainAccessReader(ent, out var accessEnt) &&
                             _access.AreAccessTagsAllowed(proto.AllowedAccesses, accessEnt.Value.Comp))
                    {
                        _airlock.SetEmergencyAccess((ent, airlockcomp), proto.AccessEnabled);
                    }
                }
                break;
        }
    }

    private static List<bool> BuildSatisfiedGroups(SecureTerminalProposalData proposal,
        SecureCommandTerminalRequestPrototype proto)
    {
        var result = new List<bool>(new bool[proto.AuthGroups.Count]);
        foreach (var (_, _, _, groupIdx) in proposal.Authorizers)
            if (groupIdx >= 0 && groupIdx < result.Count)
                result[groupIdx] = true;
        return result;
    }

    private bool HasRequestAccess(EntityUid actor, SecureCommandTerminalRequestPrototype proto)
    {
        var tags = _access.FindAccessTags(actor);
        return proto.AuthGroups.Any(group =>
            group.Any(tags.Contains));
    }

    private bool IsWarDeclared()
    {
        var query = EntityQueryEnumerator<NukeopsRuleComponent>();
        while (query.MoveNext(out _, out var nukeops))
            if (nukeops.WarDeclaredTime != null) return true;
        return false;
    }

    private string GetJobName(EntityUid actor)
    {
        if (_mind.TryGetMind(actor, out var mindUid, out _)
            && _jobs.MindTryGetJobName(mindUid, out var jobName)
            && jobName != null)
            return jobName;
        return Loc.GetString("secure-terminal-unknown-job");
    }

    private void UpdateConsoleInterface(EntityUid consoleUid)
    {
        var stationUid = _stations.GetOwningStation(consoleUid);

        var proposals = new List<SecureTerminalProposalState>();
        var coolingDown = new Dictionary<string, TimeSpan>();
        var usedOnce = new HashSet<string>();
        string? currentAlertLevel = null;
        var alertLevelSetAt = TimeSpan.Zero;
        SecureCommandTerminalStationComponent? stationComp = null;

        if (stationUid != null && TryComp(stationUid.Value, out stationComp))
        {
            coolingDown = new Dictionary<string, TimeSpan>(stationComp.Cooldowns);
            usedOnce = stationComp.UsedOnce;
            alertLevelSetAt = stationComp.AlertLevelSetAt;

            if (TryComp<AlertLevelComponent>(stationUid.Value, out var alertComp))
                currentAlertLevel = alertComp.CurrentLevel;

            foreach (var (requestId, data) in stationComp.ActiveProposals)
            {
                if (!_protos.TryIndex<SecureCommandTerminalRequestPrototype>(requestId, out var proto))
                    continue;

                var satisfiedGroups = BuildSatisfiedGroups(data, proto);
                var labels = proto.AuthGroupLabels.Count == proto.AuthGroups.Count
                    ? proto.AuthGroupLabels.Select(label => Loc.GetString(label)).ToList()
                    : proto.AuthGroups.Select(g => string.Join(" / ", g)).ToList();

                var authByGroup = data.Authorizers.ToDictionary(a => a.GroupIndex, a => (a.Name, a.Job));
                var authorizedBy = Enumerable.Range(0, proto.AuthGroups.Count)
                    .Select(i => authByGroup.TryGetValue(i, out var auth) ? auth : (string.Empty, string.Empty))
                    .ToList();

                proposals.Add(new SecureTerminalProposalState
                {
                    RequestId = requestId,
                    AuthorizedBy = authorizedBy,
                    GroupsSatisfied = satisfiedGroups,
                    GroupLabels = labels,
                    ActivateAt = data.ActivateAt,
                    AuthTimer = data.AuthTimer,
                    Status = data.Status,
                });
            }
        }

        _ui.SetUiState(consoleUid, SecureCommandTerminalUiKey.Key,
            new SecureCommandTerminalInterfaceState(proposals, IsWarDeclared(), coolingDown, currentAlertLevel, usedOnce, alertLevelSetAt,
                stationComp != null ? new Dictionary<string, TimeSpan>(stationComp.DeployedArmories) : new Dictionary<string, TimeSpan>()));
    }

    private void UpdateAllConsolesForStation(EntityUid stationUid)
    {
        var query = EntityQueryEnumerator<SecureCommandTerminalConsoleComponent>();
        while (query.MoveNext(out var consoleUid, out var comp))
            if (_stations.GetOwningStation(consoleUid) == stationUid)
                UpdateConsoleInterface(consoleUid);
    }
}
