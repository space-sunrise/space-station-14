// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Server.Data;
using Content.Anticheat.Server.Managers;
using Content.Anticheat.Shared;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Anticheat.Server.Tracking;

public sealed class ResponseTrackerSystem : EntitySystem
{
    // This entire thing should be a manager in all due honesty

    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ClientDataManager _cdm = default!;

    /// <summary>
    /// Events from clients which we are awaiting from
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    private List<ResponseTrackerState> _states = [];

    /// <summary>
    /// Events we already received and are awaiting to be cleared from states
    /// </summary>
    private Dictionary<NetUserId, Type> _cleared = [];

    /// <summary>
    /// Time given for the client to respond to a request, in seconds
    /// </summary>
    private float _timeout = 30f;

    private int _timer = 0;
    private int _tickrate = 30;

    public override void Initialize()
    {
        base.Initialize();

        // I am not resolving CVars every tick
        Subs.CVar(_cfg, AntiCheatCVars.AntiCheatResponseTimeout, OnResponseTimeoutCVarChange, true);
        Subs.CVar(_cfg, CVars.NetTickrate, OnTickrateCVarChange, true);

        // But, just to be sure
        _timeout = _cfg.GetCVar(AntiCheatCVars.AntiCheatResponseTimeout);
        _tickrate = _cfg.GetCVar(CVars.NetTickrate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timer != _tickrate)
        {
            _timer++;
            return;
        }

        _timer = 0;
        RecheckAllStates(_timing.CurTime);
    }

    private void RecheckAllStates(TimeSpan curtime)
    {
        foreach (var state in _states.ToList())
        {
            foreach (var clearKey in _cleared.Keys.ToList())
            {
                if (state.ResponseEvent != _cleared[clearKey] || state.Client != clearKey)
                    continue;

                _states.Remove(state);
                _cleared.Remove(clearKey);
                break;
            }

            // Do not kick the player if we just removed the awaited state
            if (!_states.Contains(state))
                continue;

            if (state.AwaitBy < curtime)
                ResponseTimeout(state.Client);
        }
    }

    /// <summary>
    /// Start a timer for a response coming from a client
    /// </summary>
    /// <param name="user">Client that has to send a response event</param>
    /// <param name="expectedEvent">The event we are expecting</param>
    /// <param name="timeoutOverride">Optional - time allotted to receive that event</param>
    /// <returns>True if event was queued</returns>
    private bool TryQueueResponse(ICommonSession user, Type expectedEvent, float? timeoutOverride = null)
    {
        if (AreAlreadyAwaiting(user.UserId, expectedEvent))
        {
            Log.Warning($"Already awaiting event {expectedEvent.Name} from {user.Name}");
            return false;
        }

        _states.Add(new ResponseTrackerState
        {
            AwaitBy = _timing.CurTime +
                      (timeoutOverride != null
                          ? TimeSpan.FromSeconds(timeoutOverride.Value)
                          : TimeSpan.FromSeconds(_timeout)),
            ResponseEvent = expectedEvent,
            Client = user.UserId,
        });

        return true;
    }

    public bool MarkForClear(ICommonSession user, Type eventType)
    {
        return MarkForClear(user.UserId, eventType);
    }

    public bool MarkForClear(NetUserId user, Type eventType)
    {
        return _cleared.TryAdd(user, eventType);
    }

    public IEnumerable<ResponseTrackerState> GetAwaitedResponses(NetUserId user)
    {
        foreach (var resp in _states)
        {
            if (resp.Client == user)
                yield return resp;
        }
    }

    private bool AreAlreadyAwaiting(NetUserId user, Type awaitedEvent)
    {
        foreach (var resp in _states)
        {
            if (resp.Client == user && resp.ResponseEvent == awaitedEvent)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Raises a networked event to a target and starts a response timer
    /// </summary>
    public void RaiseExpectedReturnNetworkedEvent(
        ExpectedReplyEntityEventArgs ev,
        ICommonSession session,
        float? timerOverride = null)
    {
        RaiseNetworkEvent(ev, session);
        TryQueueResponse(session, ev.ExpectedReplyType, timerOverride);
    }

    /// <summary>
    /// Respond to a client timing out on sending a response to server
    /// </summary>
    /// <param name="user"></param>
    private void ResponseTimeout(NetUserId user)
    {
        var session = _player.GetSessionById(user);
        if (_cdm.TryGetClientInfo(user, out var info))
        {
            var cinfo = info.Value;

            cinfo.Checks.Timeouted.Flag = true;
            _cdm.UpdateClient(user, cinfo);
        }

        if (_cfg.GetCVar(AntiCheatCVars.AntiCheatKickResponseTimeout))
            _netManager.DisconnectChannel(session.Channel, _cfg.GetCVar(AntiCheatCVars.AntiCheatKickResonseTimeoutReason));
    }

    private void OnResponseTimeoutCVarChange(float value)
    {
        _timeout = value;
    }

    private void OnTickrateCVarChange(int value)
    {
        _tickrate = value;
    }
}
