// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using System.Diagnostics.CodeAnalysis;
using Content.Anticheat.Server.Data;
using Content.Anticheat.Shared.Events;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Anticheat.Server.Managers;

/// <summary>
/// Manages data given by the clients
/// </summary>
public interface IClientDataManager
{

    public bool TryRegisterClient(NetUserId client);

    public void UpdateClient(NetUserId client, ClientInfo info);

    public void RemoveClient(NetUserId client);

    /// <summary>
    /// Gets client information from a session
    /// </summary>
    public bool TryGetClientInfo(NetUserId client, [NotNullWhen(true)] out ClientInfo? info);
}

