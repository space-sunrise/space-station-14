using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Сообщение для отправки захваченного изображения с клиента на сервер
/// </summary>
public sealed class PdaPhotoCaptureMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    /// <summary>
    /// Байты изображения (PNG или WebP)
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Ширина изображения в пикселях
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Высота изображения в пикселях
    /// </summary>
    public int Height { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Width = buffer.ReadInt32();
        Height = buffer.ReadInt32();
        var dataLength = buffer.ReadInt32();
        ImageData = buffer.ReadBytes(dataLength);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Width);
        buffer.Write(Height);
        buffer.Write(ImageData.Length);
        buffer.Write(ImageData);
    }
}
