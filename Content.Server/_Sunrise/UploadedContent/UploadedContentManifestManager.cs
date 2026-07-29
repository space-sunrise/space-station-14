using System.Collections.Immutable;
using Content.Shared._Sunrise.UploadedContent;
using Robust.Server.Upload;
using Robust.Shared.Asynchronous;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.UploadedContent;

/// <summary>
/// Сопровождает передачу движка полным манифестом runtime-ресурсов для клиентского прогресса.
/// </summary>
public sealed class UploadedContentManifestManager
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly NetworkResourceManager _networkResources = default!;
    [Dependency] private readonly ITaskManager _task = default!;

    private readonly UploadedContentCatalog _catalog = new();
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = _log.GetSawmill("uploaded-content");

        _net.RegisterNetMessage<MsgUploadedContentManifest>();
        _net.Connected += OnConnected;
        _networkResources.ResourcesUploaded += OnResourcesUploaded;
    }

    private void OnConnected(object? sender, NetChannelArgs args)
    {
        SendManifest(args.Channel);
    }

    private void OnResourcesUploaded(NetworkResourcesUploadedEvent args)
    {
        var files = args.Files;

        // Движок завершает чтение передачи вне главного потока, поэтому каталог и отправку возвращаем в его очередь.
        _task.RunOnMainThread(() => ApplyUploadedResources(files));
    }

    private void ApplyUploadedResources(ImmutableArray<(ResPath Relative, byte[] Data)> files)
    {
        for (var i = 0; i < files.Length; i++)
        {
            var (path, data) = files[i];
            _catalog.AddOrUpdate(path, data.Length);
        }

        BroadcastManifest();
    }

    /// <summary>
    /// Применяет одну группу ресурсов и рассылает новый полный снимок.
    /// </summary>
    internal void ApplyUploadedResources(IReadOnlyList<UploadedContentManifestEntry> files)
    {
        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            _catalog.AddOrUpdate(file.Path, file.SizeBytes);
        }

        BroadcastManifest();
    }

    private void SendManifest(INetChannel channel)
    {
        _net.ServerSendMessage(_catalog.CreateManifest(), channel);
        LogManifestSent();
    }

    private void BroadcastManifest()
    {
        _net.ServerSendToAll(_catalog.CreateManifest());
        LogManifestSent();
    }

    private void LogManifestSent()
    {
        _sawmill.Debug($"Sent uploaded content manifest: {_catalog.Count} files, {_catalog.TotalBytes} bytes.");
    }
}
