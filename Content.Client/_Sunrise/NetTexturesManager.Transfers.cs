using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._Sunrise.NetTextures;
using Robust.Shared.Network.Transfer;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using ByteHelpers = Robust.Shared.Utility.ByteHelpers;

namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    #region Transfer Intake
    /// <summary>
    /// Starts asynchronous processing for a high-bandwidth NetTextures transfer.
    /// </summary>
    /// <remarks>
    /// The transfer callback can run during a connect-sensitive window, so the stream is handed off to a
    /// background worker before any file parsing begins.
    /// </remarks>
    /// <param name="transfer">The transfer payload received from the server.</param>
    private void ReceiveNetTexturesTransfer(TransferReceivedEvent transfer)
    {
        var generation = _sessionGeneration;

        _ = Task.Run(() => ReceiveNetTexturesTransferWorker(transfer.DataStream, generation));
    }

#pragma warning disable CS0618
    /// <summary>
    /// Accepts the legacy whole-file fallback message and publishes it through the normal client pipeline.
    /// </summary>
    /// <param name="message">The legacy upload message containing a single resource file.</param>
    private void ReceiveFallbackUpload(NetworkResourceUploadMessage message)
#pragma warning restore CS0618
    {
        var files = new List<(ResPath Relative, byte[] Data)>(1)
        {
            (message.RelativePath, message.Data)
        };

        PublishFiles(files);
    }

    /// <summary>
    /// Accepts one chunk from the chunked fallback transport and assembles it into a complete uploaded file.
    /// </summary>
    /// <remarks>
    /// Assemblies are keyed by normalized relative path and are discarded on session reset, so partial fallback
    /// state cannot leak into a later reconnect attempt.
    /// </remarks>
    /// <param name="message">The incoming fallback chunk.</param>
    internal void ReceiveFallbackChunk(NetTextureResourceChunkMessage message)
    {
        if (message.TotalChunks <= 0 || message.ChunkIndex < 0 || message.ChunkIndex >= message.TotalChunks)
        {
            _sawmill.Warning($"Rejected malformed NetTextures fallback chunk for {message.RelativePath}: chunk {message.ChunkIndex + 1}/{message.TotalChunks}");
            return;
        }

        var relativePath = new ResPath(message.RelativePath).Clean().ToRelativePath();

        if (!_fallbackChunkAssemblies.TryGetValue(relativePath, out var assembly) ||
            assembly.TotalChunks != message.TotalChunks)
        {
            assembly?.Dispose();
            assembly = new FallbackChunkAssembly(message.TotalChunks);
            _fallbackChunkAssemblies[relativePath] = assembly;
        }

        assembly.StoreChunk(message.ChunkIndex, message.Data);

        if (!assembly.IsComplete)
            return;

        _fallbackChunkAssemblies.Remove(relativePath);
        try
        {
            var files = new List<(ResPath Relative, byte[] Data)>(1)
            {
                (relativePath, assembly.Combine())
            };

            PublishFiles(files);
        }
        finally
        {
            assembly.Dispose();
        }
    }

    /// <summary>
    /// Publishes received raw files into the mounted in-memory uploaded root and refreshes pending consumers.
    /// </summary>
    /// <param name="files">The files to publish under <c>/Uploaded</c>.</param>
    internal void PublishFiles(List<(ResPath Relative, byte[] Data)> files)
    {
        foreach (var (relative, data) in files)
        {
            _sawmill.Verbose($"Storing NetTexture: {relative} ({ByteHelpers.FormatBytes(data.Length)})");
            _netTexturesContentRoot.AddOrUpdateFile(relative, data);
            _failedResources.Remove(GetUploadedResourcePath(relative));
        }

        UpdatePendingResources();
    }
    #endregion

    #region Transfer Workers
    /// <summary>
    /// Parses a transfer stream on a worker thread and marshals publication back to the main thread.
    /// </summary>
    /// <param name="stream">The transfer stream returned by the HBT subsystem.</param>
    /// <param name="generation">The session generation captured when the transfer started.</param>
    private void ReceiveNetTexturesTransferWorker(Stream stream, int generation)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using (stream)
            {
                var files = ReadTransferStream(stream);
                var totalSize = 0L;
                foreach (var (_, data) in files)
                {
                    totalSize += data.Length;
                }

                _taskManager.RunOnMainThread(() =>
                {
                    if (generation != _sessionGeneration)
                        return;

                    PublishFiles(files);
                });

                var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _sawmill.Info($"[NetTextures] Received {files.Count} files ({ByteHelpers.FormatBytes(totalSize)}) via transfer in {totalTime:F0}ms");
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while receiving NetTextures transfer: {e}");
        }
    }
    #endregion

    #region Transfer Parsing
    /// <summary>
    /// Reads the NetTextures transfer stream into a list of uploaded files.
    /// </summary>
    /// <param name="stream">The transfer stream to parse.</param>
    /// <returns>The ordered list of files contained in the stream.</returns>
    private List<(ResPath Relative, byte[] Data)> ReadTransferStream(Stream stream)
    {
        var files = new List<(ResPath Relative, byte[] Data)>();
        var lengthBytes = new byte[4];
        var continueByte = new byte[1];

        while (true)
        {
            ReadExactly(stream, lengthBytes);
            var pathLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (pathLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer path length is too large: {pathLength}");

            ReadExactly(stream, lengthBytes);
            var dataLength = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
            if (dataLength > int.MaxValue)
                throw new InvalidDataException($"NetTextures transfer file length is too large: {dataLength}");

            var pathData = new byte[(int) pathLength];
            ReadExactly(stream, pathData);

            var data = new byte[(int) dataLength];
            ReadExactly(stream, data);

            files.Add((new ResPath(Encoding.UTF8.GetString(pathData)), data));

            ReadExactly(stream, continueByte);
            if (continueByte[0] == 0)
                break;
        }

        return files;
    }

    /// <summary>
    /// Reads the exact number of bytes required for one transfer field.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="buffer">The destination buffer that must be filled completely.</param>
    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
                throw new InvalidDataException("Unexpected end of NetTextures transfer stream");

            offset += read;
        }
    }
    #endregion
}
