using Content.Shared._Sunrise.Proton;
using Robust.Client.Graphics;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._Sunrise.Proton;

public sealed class ProtonManager
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    public static readonly ResPath ProtonPath = new ResPath("/Proton");

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _logManager.GetSawmill("proton");

        _resourceManager.UserData.CreateDir(ProtonPath);

        _sawmill.Debug($"Proton Manager successfully initialized");

        // Registration of messages
        _netManager.RegisterNetMessage<ProtonRequestScreenshotClient>(HandleRequestScreenshot);
        _netManager.RegisterNetMessage<ProtonRequestScreenshotServer>();
        _netManager.RegisterNetMessage<ProtonResponseScreenshotClient>();
        _netManager.RegisterNetMessage<ProtonResponseScreenshotServer>(HandleResponseScreenshot);
    }

    private void HandleRequestScreenshot(ProtonRequestScreenshotClient request)
    {
        _clyde.Screenshot(ScreenshotType.Final, pixels => {HandleScreenshot(ref request, pixels);});

        _sawmill.Debug($"Successfully handled screenshot request");
    }

    private void HandleScreenshot(ref ProtonRequestScreenshotClient request, Image<Rgb24> screenshot)
    {
        var response = new ProtonResponseScreenshotClient()
        {
            Screenshot = screenshot,
        };
        _netManager.ClientSendMessage(response);

        _sawmill.Debug($"Successfully sent screenshot request");
    }

    private void HandleResponseScreenshot(ProtonResponseScreenshotServer response)
    {
        if (response.Screenshot == null)
        {
            _sawmill.Debug($"Received response with null screenshot");
            return;
        }

        var path = ProtonPath / $"test-{response.Screenshot.GetHashCode()}.png";

        using var stream = _resourceManager.UserData.OpenWrite(path);
        response.Screenshot.SaveAsPng(stream);

        _sawmill.Debug($"Successfully received screenshot and saved it to {path}");
        stream.Close();

        using var imageStream = _resourceManager.UserData.OpenRead(path);
        var view = new ProtonScreenshotView(Texture.LoadFromPNGStream(imageStream, "screenshot"));
        view.OpenCentered();
        imageStream.Close();

        _sawmill.Debug($"Opened window (centered) successfully");
    }
}
