using Content.Shared._Sunrise.UploadedContent;
using Robust.Client.Upload;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.UploadedContent;

/// <summary>
/// Получает серверный манифест и предоставляет UI безопасный снимок прогресса runtime-ресурсов.
/// </summary>
public sealed class UploadedContentProgressManager
{
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly NetworkResourceManager _networkResources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private UploadedContentProgressTracker _tracker = default!;

    public UploadedContentProgressSnapshot Snapshot => _tracker.Snapshot;

    public void Initialize()
    {
        _tracker = new UploadedContentProgressTracker(_networkResources.FileExists);

        _net.RegisterNetMessage<MsgUploadedContentManifest>(OnManifest, NetMessageAccept.Client);
        _net.ClientConnectStateChanged += OnConnectionStateChanged;
        _net.ConnectFailed += OnConnectFailed;
        _net.Disconnect += OnDisconnected;
    }

    /// <summary>
    /// Обновляет ограниченный опрос и возвращает текущий неизменяемый снимок.
    /// </summary>
    public UploadedContentProgressSnapshot Update()
    {
        return _tracker.Update(_timing.RealTime);
    }

    public void Reset()
    {
        _tracker.Reset();
    }

    private void OnManifest(MsgUploadedContentManifest message)
    {
        _tracker.ApplyManifest(message.Files, _timing.RealTime);
    }

    private void OnConnectionStateChanged(ClientConnectionState state)
    {
        if (state != ClientConnectionState.Connected)
            Reset();
    }

    private void OnConnectFailed(object? sender, NetConnectFailArgs args)
    {
        Reset();
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs args)
    {
        Reset();
    }
}
