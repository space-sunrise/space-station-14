// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Server.Tracking;
using Content.Anticheat.Shared.Events;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Anticheat.Server.Systems;

/// <summary>
/// Data collection and processing
/// </summary>
public sealed partial class ServerAnticheatSystem
{
    [Dependency] private readonly ResponseTrackerSystem _respTracker = default!;

    /// <summary>
    /// Send a request for data to the client
    /// </summary>
    /// <param name="session">Target client to send the request to</param>
    /// <returns>True if request was successful</returns>
    private void SendJoinRequest(ICommonSession session)
    {
        Log.Info("Sending a join request");
        var ev = new AnticheatJoinReqEvent();

        _respTracker.RaiseExpectedReturnNetworkedEvent(ev, session);
    }

    /// <param name="response">The userid the client gave us</param>
    /// <param name="origin">The actual userid of the client</param>
    /// <returns></returns>
    public bool ValidateJoinReply(AnticheatJoinRespEvent response, NetUserId origin)
    {
        // Response was sent, doesn't matter if its true or false.
        _respTracker.MarkForClear(origin, response.GetType());
        return response.User == origin;
    }
}
