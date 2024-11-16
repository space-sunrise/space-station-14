// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Server.Data;
using Content.Anticheat.Shared.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Anticheat.Server.Systems;

public sealed partial class ServerAnticheatSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    private void OnHeartbeat(HeartbeatEvent ev, EntitySessionEventArgs args)
    {
        if (!_clientData.TryGetClientInfo(args.SenderSession.UserId, out var info))
        {
            Log.Warning($"Client {args.SenderSession.Name} has sent a heartbeat " +
                        $"without being registered with the anticheat first.");
            return;
        }

        UpdateStamp(args.SenderSession.UserId, info.Value);
    }

    private void UpdateStamp(NetUserId client)
    {
        if (!_clientData.TryGetClientInfo(client, out var info))
        {
            Log.Warning($"Tring to update stamp from {client}, but no client info.");
            return;
        }

        var updatedInfo = info.Value with { Last = _timing.RealTime };
        _clientData.UpdateClient(client, updatedInfo);
    }

    private void UpdateStamp(NetUserId client, ClientInfo info)
    {
        info.Last = _timing.RealTime;
        _clientData.UpdateClient(client, info);
    }
}
