// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Server.Managers;
using Content.Anticheat.Shared;
using Content.Anticheat.Shared.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Content.Anticheat.Server.Systems;

public sealed partial class ServerAnticheatSystem : EntitySystem
{
    [Dependency] private readonly IClientDataManager _clientData = default!;
    [Dependency] private readonly ISharedPlayerManager _playMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        _playMan.PlayerStatusChanged += TryChallengeClient;

        SubscribeNetworkEvent<HeartbeatEvent>(OnHeartbeat);
    }

    private void TryChallengeClient(object? sender, SessionStatusEventArgs e)
    {
        if (!_clientData.TryRegisterClient(e.Session.UserId))
        {
            UpdateStamp(e.Session.UserId);
            return;
        }

        if (e is { OldStatus: SessionStatus.InGame, NewStatus: SessionStatus.Disconnected })
            _clientData.RemoveClient(e.Session.UserId);

        if (e.NewStatus == SessionStatus.Connected)
            SendJoinRequest(e.Session);
    }
}
