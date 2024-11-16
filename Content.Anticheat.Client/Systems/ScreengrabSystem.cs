// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Shared.Events;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Anticheat.Client.Systems;

public sealed class ScreengrabSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _netMan = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ScreengrabRequestEvent>(OnScreengrabRequest);
    }

    private async void OnScreengrabRequest(ScreengrabRequestEvent ev)
    {
        var image = await _clyde.ScreenshotAsync(ScreenshotType.Final);
        var array = ImageToByteArray(image);

        if (array.Length > 1_500_000)
            return;

        var msg = new ScreengrabResponseEvent { Screengrab = array };
        RaiseNetworkEvent(msg);
    }

    private byte[] ImageToByteArray(Image<Rgb24> image)
    {
        using var stream = new MemoryStream();
        //       - "void Save(SixLabors.ImageSharp.Image, System.IO.Stream, SixLabors.ImageSharp.Formats.IImageFormat)"
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }
}
