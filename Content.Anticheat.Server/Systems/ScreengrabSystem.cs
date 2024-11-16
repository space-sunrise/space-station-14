// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Server.Tracking;
using Content.Anticheat.Shared.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Anticheat.Server.Systems;

public sealed class ScreengrabSystem : EntitySystem
{
    [Dependency] private readonly IServerNetManager _netMan = default!;
    [Dependency] private readonly ResponseTrackerSystem _respTracker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ScreengrabResponseEvent>(OnScreengrabReply);
    }
    public void RequestScreengrab(ICommonSession target)
    {
        var ev = new ScreengrabRequestEvent();
        _respTracker.RaiseExpectedReturnNetworkedEvent(ev, target);
    }

    public void OnScreengrabReply(ScreengrabResponseEvent ev, EntitySessionEventArgs args)
    {
        _respTracker.MarkForClear(args.SenderSession, ev.GetType());

        if (ev.Screengrab.Length == 0)
            return;

        var imagedata = ev.Screengrab;
        using var image = Image.Load<Rgb24>(imagedata);

        Log.Info($"Received a screengrab from {args.SenderSession.Name} -" +
                 $" {image.Size.Height} by {image.Size.Width}, It is {imagedata.Length} bytes long.");
    }
}
