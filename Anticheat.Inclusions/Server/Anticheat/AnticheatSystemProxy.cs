// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Server.Systems;
using Content.Anticheat.Shared.Events;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Anticheat;

public sealed class AnticheatSystemProxy : EntitySystem
{
    /// <summary>
    /// Adding a custom type would be better but would hurt the "drag and drop" aim we are going for.
    /// </summary>
    private const LogType Type = LogType.EventRan;

    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ServerAnticheatSystem _serverAC = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AnticheatJoinRespEvent>(OnACJoinResponse);
    }

    private async void OnACJoinResponse(AnticheatJoinRespEvent ev, EntitySessionEventArgs args)
    {
        if (_serverAC.ValidateJoinReply(ev, args.SenderSession.UserId))
            return;

        var sus = await _locator.LookupIdAsync(ev.User);
        if (sus is null)
        {
            _adminLog.Add(Type, LogImpact.High, $"{args.SenderSession.Name} provided an invalid userid, might be deliberately tampering with residue file.");
            _chat.SendAdminAlert($"{args.SenderSession.Name} provided an invalid userid, might be deliberately tampering with residue file.");
            return;
        }

        _adminLog.Add(Type, LogImpact.High, $"{args.SenderSession.Name} provided userid of {sus.Username}, possible alt.");
        _chat.SendAdminAlert($"{args.SenderSession.Name} provided userid of {sus.Username}, possible alt.");
    }
}
