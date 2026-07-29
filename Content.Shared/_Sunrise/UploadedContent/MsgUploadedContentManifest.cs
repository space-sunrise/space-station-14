using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.UploadedContent;

/// <summary>
/// Описывает один runtime-ресурс, который сервер передаёт клиенту при подключении.
/// </summary>
public readonly record struct UploadedContentManifestEntry(ResPath Path, int SizeBytes);

/// <summary>
/// Передаёт клиенту упорядоченный полный снимок runtime-ресурсов сервера.
/// </summary>
public sealed class MsgUploadedContentManifest : NetMessage
{
    private const int MaxEntries = ushort.MaxValue;

    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    /// <summary>
    /// Полный список ресурсов в порядке их первого появления на сервере.
    /// </summary>
    public List<UploadedContentManifestEntry> Files { get; set; } = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        if (count < 0 || count > MaxEntries)
            throw new InvalidDataException($"Uploaded content manifest entry count {count} is invalid.");

        Files.Clear();
        Files.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            var pathText = buffer.ReadString();
            if (!ResPath.IsValidPath(pathText))
                throw new InvalidDataException("Uploaded content manifest contains an invalid resource path.");

            var sizeBytes = buffer.ReadInt32();
            if (sizeBytes < 0)
                throw new InvalidDataException($"Uploaded content manifest contains a negative file size {sizeBytes}.");

            var path = new ResPath(pathText).Clean().ToRelativePath();
            Files.Add(new UploadedContentManifestEntry(path, sizeBytes));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (Files.Count > MaxEntries)
            throw new InvalidDataException($"Uploaded content manifest contains more than {MaxEntries} entries.");

        buffer.WriteVariableInt32(Files.Count);
        for (var i = 0; i < Files.Count; i++)
        {
            var file = Files[i];
            if (file.SizeBytes < 0)
                throw new InvalidDataException($"Uploaded content manifest contains a negative file size {file.SizeBytes}.");

            buffer.Write(file.Path.Clean().ToRelativePath().CanonPath);
            buffer.Write(file.SizeBytes);
        }
    }
}
