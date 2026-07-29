using Content.Client._Sunrise.UploadedContent;
using Robust.Shared.Network;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Sunrise: partial расширяет vanilla-класс из fork-папки.
namespace Content.Client.Launcher;

public sealed partial class LauncherConnectingGui
{
    [Dependency] private readonly UploadedContentProgressManager _uploadedContentProgress = default!;

    private UploadedContentUiMode _uploadedContentUiMode = UploadedContentUiMode.Unknown;
    private bool? _uploadedContentLastHadEstimatedSpeed;
    private int _uploadedContentLastPercent = -1;
    private long _uploadedContentLastSpeedBytes = -1;
    private long _uploadedContentLastCompletedBytes = -1;
    private long _uploadedContentLastTotalBytes = -1;
    private int _uploadedContentLastCompletedFiles = -1;
    private int _uploadedContentLastTotalFiles = -1;

    /// <summary>
    /// Обновляет опрос runtime-ресурсов только пока открыт активный экран подключения.
    /// </summary>
    private void UpdateUploadedContentProgress()
    {
        if (_state.CurrentPage != LauncherConnecting.Page.Connecting)
        {
            _uploadedContentProgress.Reset();
            SetUploadedContentUiMode(UploadedContentUiMode.Hidden);
            return;
        }

        if (_state.ConnectionState != ClientConnectionState.Connected)
        {
            SetUploadedContentUiMode(UploadedContentUiMode.Hidden);
            return;
        }

        RenderUploadedContentProgress(_uploadedContentProgress.Update());
    }

    /// <summary>
    /// Подменяет только текст состояния Connected и немедленно скрывает бары в остальных состояниях.
    /// </summary>
    private void UpdateUploadedContentConnectionState(ClientConnectionState state)
    {
        if (state != ClientConnectionState.Connected)
        {
            _uploadedContentProgress.Reset();
            SetUploadedContentUiMode(UploadedContentUiMode.Hidden);
            return;
        }

        _uploadedContentUiMode = UploadedContentUiMode.Unknown;
        RenderUploadedContentProgress(_uploadedContentProgress.Snapshot);
    }

    private void RenderUploadedContentProgress(UploadedContentProgressSnapshot snapshot)
    {
        if (!snapshot.ManifestReceived)
        {
            SetUploadedContentUiMode(UploadedContentUiMode.Checking);
            return;
        }

        if (snapshot.IsComplete)
        {
            SetUploadedContentUiMode(UploadedContentUiMode.Connected);
            return;
        }

        SetUploadedContentUiMode(UploadedContentUiMode.Downloading);
        UploadedContentCurrentBar.Value = snapshot.CurrentFileProgress;
        UploadedContentTotalBar.Value = CalculateTotalProgress(snapshot);
        UpdateUploadedContentCurrentLabel(snapshot);
        UpdateUploadedContentTotalLabel(snapshot);
    }

    private void SetUploadedContentUiMode(UploadedContentUiMode mode)
    {
        if (_uploadedContentUiMode == mode)
            return;

        _uploadedContentUiMode = mode;
        UploadedContentProgress.Visible = mode == UploadedContentUiMode.Downloading;

        switch (mode)
        {
            case UploadedContentUiMode.Checking:
                ConnectStatus.Text = Loc.GetString("connecting-uploaded-content-checking");
                break;
            case UploadedContentUiMode.Connected:
                ConnectStatus.Text = Loc.GetString("connecting-state-Connected");
                break;
            case UploadedContentUiMode.Downloading:
                ConnectStatus.Text = Loc.GetString("connecting-uploaded-content-downloading");
                ResetUploadedContentLabelCache();
                break;
        }
    }

    private void UpdateUploadedContentCurrentLabel(UploadedContentProgressSnapshot snapshot)
    {
        if (!snapshot.HasEstimatedSpeed)
        {
            if (_uploadedContentLastHadEstimatedSpeed != false)
                UploadedContentCurrentLabel.Text =
                    Loc.GetString("connecting-uploaded-content-current-calculating");

            _uploadedContentLastHadEstimatedSpeed = false;
            return;
        }

        var percent = Math.Clamp((int)Math.Round(snapshot.CurrentFileProgress * 100), 0, 95);
        var speedBytes = snapshot.EstimatedBytesPerSecond >= long.MaxValue
            ? long.MaxValue
            : (long)Math.Max(0, snapshot.EstimatedBytesPerSecond);
        if (_uploadedContentLastHadEstimatedSpeed == true
            && _uploadedContentLastPercent == percent
            && _uploadedContentLastSpeedBytes == speedBytes)
        {
            return;
        }

        UploadedContentCurrentLabel.Text = Loc.GetString(
            "connecting-uploaded-content-current-estimated",
            ("percent", percent),
            ("speed", ByteHelpers.FormatBytes(speedBytes)));

        _uploadedContentLastHadEstimatedSpeed = true;
        _uploadedContentLastPercent = percent;
        _uploadedContentLastSpeedBytes = speedBytes;
    }

    private void UpdateUploadedContentTotalLabel(UploadedContentProgressSnapshot snapshot)
    {
        if (_uploadedContentLastCompletedBytes == snapshot.CompletedBytes
            && _uploadedContentLastTotalBytes == snapshot.TotalBytes
            && _uploadedContentLastCompletedFiles == snapshot.CompletedFiles
            && _uploadedContentLastTotalFiles == snapshot.TotalFiles)
        {
            return;
        }

        UploadedContentTotalLabel.Text = Loc.GetString(
            "connecting-uploaded-content-total",
            ("completedBytes", ByteHelpers.FormatBytes(snapshot.CompletedBytes)),
            ("totalBytes", ByteHelpers.FormatBytes(snapshot.TotalBytes)),
            ("completedFiles", snapshot.CompletedFiles),
            ("totalFiles", snapshot.TotalFiles));

        _uploadedContentLastCompletedBytes = snapshot.CompletedBytes;
        _uploadedContentLastTotalBytes = snapshot.TotalBytes;
        _uploadedContentLastCompletedFiles = snapshot.CompletedFiles;
        _uploadedContentLastTotalFiles = snapshot.TotalFiles;
    }

    private static float CalculateTotalProgress(UploadedContentProgressSnapshot snapshot)
    {
        if (snapshot.TotalBytes > 0)
            return (float)snapshot.CompletedBytes / snapshot.TotalBytes;

        if (snapshot.TotalFiles > 0)
            return (float)snapshot.CompletedFiles / snapshot.TotalFiles;

        return 1;
    }

    private void ResetUploadedContentLabelCache()
    {
        _uploadedContentLastHadEstimatedSpeed = null;
        _uploadedContentLastPercent = -1;
        _uploadedContentLastSpeedBytes = -1;
        _uploadedContentLastCompletedBytes = -1;
        _uploadedContentLastTotalBytes = -1;
        _uploadedContentLastCompletedFiles = -1;
        _uploadedContentLastTotalFiles = -1;
    }

    private enum UploadedContentUiMode : byte
    {
        Unknown,
        Hidden,
        Checking,
        Connected,
        Downloading,
    }
}
