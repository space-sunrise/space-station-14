using Content.Shared._Sunrise.UploadedContent;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.UploadedContent;

/// <summary>
/// Неизменяемое состояние прогресса runtime-ресурсов, доступное интерфейсу подключения.
/// </summary>
public readonly record struct UploadedContentProgressSnapshot(
    bool ManifestReceived,
    int TotalFiles,
    int CompletedFiles,
    long TotalBytes,
    long CompletedBytes,
    int CurrentFileIndex,
    bool HasEstimatedSpeed,
    double EstimatedBytesPerSecond,
    float CurrentFileProgress,
    bool IsComplete)
{
    public static UploadedContentProgressSnapshot Empty { get; } = new(
        false,
        0,
        0,
        0,
        0,
        -1,
        false,
        0,
        0,
        false);
}

/// <summary>
/// Рассчитывает подтверждённый и приблизительный прогресс без вмешательства в передачу движка.
/// </summary>
internal sealed class UploadedContentProgressTracker
{
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private const double SpeedEwmaCoefficient = 0.35;
    private const double AnimationCycleSeconds = 1.5;
    private const float InitialAnimationMinimum = 0.1f;
    private const float InitialAnimationMaximum = 0.9f;
    private const float EstimatedAnimationMinimum = 0.9f;
    private const float EstimatedProgressLimit = 0.95f;

    private readonly Func<ResPath, bool> _fileExists;
    private readonly List<TrackedFile> _files = [];
    private readonly Dictionary<ResPath, int> _fileIndices = [];

    private UploadedContentProgressSnapshot _snapshot = UploadedContentProgressSnapshot.Empty;
    private TimeSpan _lastCompletionAt;
    private TimeSpan _nextPollAt;
    private int _completedFiles;
    private int _currentFileIndex = -1;
    private long _completedBytes;
    private long _totalBytes;
    private double _estimatedBytesPerSecond;
    private bool _hasEstimatedSpeed;
    private bool _manifestReceived;

    public UploadedContentProgressTracker(Func<ResPath, bool> fileExists)
    {
        _fileExists = fileExists;
    }

    public UploadedContentProgressSnapshot Snapshot => _snapshot;

    /// <summary>
    /// Полностью заменяет отслеживаемый манифест и принимает уже загруженные файлы за исходную точку.
    /// </summary>
    public void ApplyManifest(IReadOnlyList<UploadedContentManifestEntry> manifest, TimeSpan now)
    {
        Reset();
        _manifestReceived = true;

        for (var i = 0; i < manifest.Count; i++)
        {
            var entry = manifest[i];
            ArgumentOutOfRangeException.ThrowIfNegative(entry.SizeBytes);

            var path = entry.Path.Clean().ToRelativePath();
            if (_fileIndices.TryGetValue(path, out var existingIndex))
            {
                var existing = _files[existingIndex];
                existing.SizeBytes = entry.SizeBytes;
                _files[existingIndex] = existing;
                continue;
            }

            _fileIndices.Add(path, _files.Count);
            _files.Add(new TrackedFile(path, entry.SizeBytes));
        }

        for (var i = 0; i < _files.Count; i++)
        {
            var file = _files[i];
            _totalBytes += file.SizeBytes;

            if (!_fileExists(file.Path))
                continue;

            file.Completed = true;
            _files[i] = file;
            _completedFiles++;
            _completedBytes += file.SizeBytes;
        }

        _currentFileIndex = FindNextIncomplete(0);
        _lastCompletionAt = now;
        _nextPollAt = now + PollInterval;
        RefreshSnapshot(now);
    }

    /// <summary>
    /// Обновляет анимацию каждый кадр, но опрашивает наличие файлов не чаще заданного интервала.
    /// </summary>
    public UploadedContentProgressSnapshot Update(TimeSpan now)
    {
        if (!_manifestReceived)
            return _snapshot;

        if (_completedFiles < _files.Count && now >= _nextPollAt)
        {
            _nextPollAt = now + PollInterval;
            PollCompletedFiles(now);
        }

        RefreshSnapshot(now);
        return _snapshot;
    }

    public void Reset()
    {
        _files.Clear();
        _fileIndices.Clear();
        _snapshot = UploadedContentProgressSnapshot.Empty;
        _lastCompletionAt = default;
        _nextPollAt = default;
        _completedFiles = 0;
        _currentFileIndex = -1;
        _completedBytes = 0;
        _totalBytes = 0;
        _estimatedBytesPerSecond = 0;
        _hasEstimatedSpeed = false;
        _manifestReceived = false;
    }

    private void PollCompletedFiles(TimeSpan now)
    {
        var newlyCompletedBytes = 0L;
        var completionFound = false;

        for (var i = 0; i < _files.Count; i++)
        {
            var file = _files[i];
            if (file.Completed || !_fileExists(file.Path))
                continue;

            file.Completed = true;
            _files[i] = file;
            _completedFiles++;
            _completedBytes += file.SizeBytes;
            newlyCompletedBytes += file.SizeBytes;
            completionFound = true;
        }

        if (!completionFound)
            return;

        var elapsedSeconds = Math.Max(0, (now - _lastCompletionAt).TotalSeconds);
        if (newlyCompletedBytes > 0 && elapsedSeconds > 0)
        {
            var sample = newlyCompletedBytes / elapsedSeconds;
            if (double.IsFinite(sample))
            {
                _estimatedBytesPerSecond = _hasEstimatedSpeed
                    ? SpeedEwmaCoefficient * sample + (1 - SpeedEwmaCoefficient) * _estimatedBytesPerSecond
                    : sample;
                _hasEstimatedSpeed = true;
            }
        }

        _lastCompletionAt = now;

        if (_currentFileIndex >= 0 && _files[_currentFileIndex].Completed)
            _currentFileIndex = FindNextIncomplete(_currentFileIndex + 1);
    }

    private int FindNextIncomplete(int startIndex)
    {
        for (var i = startIndex; i < _files.Count; i++)
        {
            if (!_files[i].Completed)
                return i;
        }

        return -1;
    }

    private void RefreshSnapshot(TimeSpan now)
    {
        var isComplete = _manifestReceived && _completedFiles == _files.Count;
        var currentProgress = isComplete || _currentFileIndex < 0
            ? 0
            : CalculateCurrentFileProgress(now);

        _snapshot = new UploadedContentProgressSnapshot(
            _manifestReceived,
            _files.Count,
            _completedFiles,
            _totalBytes,
            _completedBytes,
            _currentFileIndex,
            _hasEstimatedSpeed,
            _estimatedBytesPerSecond,
            currentProgress,
            isComplete);
    }

    private float CalculateCurrentFileProgress(TimeSpan now)
    {
        var elapsedSeconds = Math.Max(0, (now - _lastCompletionAt).TotalSeconds);
        if (!_hasEstimatedSpeed)
        {
            var triangle = CalculateTriangleWave(elapsedSeconds);
            return InitialAnimationMinimum
                   + (InitialAnimationMaximum - InitialAnimationMinimum) * triangle;
        }

        var currentSize = _files[_currentFileIndex].SizeBytes;
        var estimatedProgress = currentSize == 0
            ? double.PositiveInfinity
            : elapsedSeconds * _estimatedBytesPerSecond / currentSize;

        if (estimatedProgress < EstimatedProgressLimit)
            return (float)Math.Clamp(estimatedProgress, 0, EstimatedProgressLimit);

        var waitingTriangle = CalculateTriangleWave(elapsedSeconds);
        return EstimatedAnimationMinimum
               + (EstimatedProgressLimit - EstimatedAnimationMinimum) * waitingTriangle;
    }

    private static float CalculateTriangleWave(double elapsedSeconds)
    {
        var phase = elapsedSeconds % AnimationCycleSeconds / AnimationCycleSeconds;
        var triangle = phase <= 0.5
            ? phase * 2
            : (1 - phase) * 2;
        return (float)triangle;
    }

    private struct TrackedFile
    {
        public readonly ResPath Path;
        public int SizeBytes;
        public bool Completed;

        public TrackedFile(ResPath path, int sizeBytes)
        {
            Path = path;
            SizeBytes = sizeBytes;
            Completed = false;
        }
    }
}
