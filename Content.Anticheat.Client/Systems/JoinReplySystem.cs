// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Shared.Events;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Anticheat.Client.Systems;

public sealed class JoinReplySystem : EntitySystem
{
    private const string UserIdPath = "/userid";

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IResourceManager _resource = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AnticheatJoinReqEvent>(OnJoinRequest);
    }

    private void OnJoinRequest(AnticheatJoinReqEvent _)
    {
        Log.Info("Received join request");

        var userData = _resource.UserData;
        var path = new ResPath(UserIdPath);
        if (userData.TryReadAllText(path, out var userId) && Guid.TryParse(userId, out var guid))
        {
            var netUserId = new NetUserId(guid);
            RaiseNetworkEvent(new AnticheatJoinRespEvent(netUserId));
            return;
        }

        if (_playerManager.LocalSession != null)
        {
            var netUserId = _playerManager.LocalSession.UserId;
            userData.WriteAllText(path, netUserId.UserId.ToString());
        }

        OnJoinRequest(_);
    }
}
