// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using System.Diagnostics.CodeAnalysis;
using Content.Anticheat.Server.Data;
using Content.Anticheat.Shared.Events;
using Robust.Shared.Analyzers;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;
using Serilog;

namespace Content.Anticheat.Server.Managers;

public sealed class ClientDataManager : IClientDataManager
{
    [ViewVariables(VVAccess.ReadOnly)]
    private Dictionary<NetUserId, ClientInfo> _clients = [];

    /// <summary>
    /// Attempt to register a client
    /// </summary>
    /// <returns>False if we already have a registered client</returns>
    public bool TryRegisterClient(NetUserId client)
    {
        if (TryGetClientInfo(client, out _))
            return false;

        _clients.Add(client, new ClientInfo());
        return true;
    }

    public void UpdateClient(NetUserId client, ClientInfo info)
    {
        if (_clients.ContainsKey(client))
        {
            _clients[client] = info;
        }
        else
        {
            Log.Warning("Tried to update state for a non-existent client");
        }
    }

    public void RemoveClient(NetUserId client)
    {
        _clients.Remove(client);
    }

    public bool TryGetClientInfo(NetUserId session, [NotNullWhen(true)] out ClientInfo? info)
    {
        info = null;

        if (_clients.TryGetValue(session, out var clientInfo))
        {
            info = clientInfo;
            return true;
        }

        return false;
    }

    public void Initailize()
    {

    }
}
