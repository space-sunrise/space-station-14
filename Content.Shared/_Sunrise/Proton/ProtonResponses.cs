using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Shared._Sunrise.Proton;

// Maybe we could use one type of message to both get screenshot from client to server and send screenshot from server
// to admin, who requested this screenshot.

/// <summary>
/// Response, containing screenshot of client's window
/// </summary>
public sealed class ProtonResponseScreenshotClient : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    // Data sent
    public Image<Rgb24>? Screenshot;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var imageDataLength = buffer.ReadInt32();

        if (imageDataLength == 0)
        {
            Screenshot = null;
            return;
        }

        var imageData = buffer.ReadBytes(imageDataLength);

        using var memoryStream = new MemoryStream(imageData);
        Screenshot = Image.Load<Rgb24>(memoryStream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (Screenshot == null)
        {
            buffer.Write(0);
            return;
        }

        using var memoryStream = new MemoryStream();
        Screenshot.SaveAsPng(memoryStream);
        var imageData = memoryStream.ToArray();

        buffer.Write(imageData.Length);

        buffer.Write(imageData);
    }
}

/// <summary>
/// Screenshot, sent from server to target admin
/// </summary>
public sealed class ProtonResponseScreenshotServer : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    // Data sent
    public Image<Rgb24>? Screenshot;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var imageDataLength = buffer.ReadInt32();

        if (imageDataLength == 0)
        {
            Screenshot = null;
            return;
        }

        var imageData = buffer.ReadBytes(imageDataLength);

        using var memoryStream = new MemoryStream(imageData);
        Screenshot = Image.Load<Rgb24>(memoryStream);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (Screenshot == null)
        {
            buffer.Write(0);
            return;
        }

        using var memoryStream = new MemoryStream();
        Screenshot.SaveAsPng(memoryStream);
        var imageData = memoryStream.ToArray();

        buffer.Write(imageData.Length);

        buffer.Write(imageData);
    }
}
