using Content.Shared._Sunrise.UploadedContent;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.UploadedContent;

/// <summary>
/// Хранит полный упорядоченный каталог runtime-ресурсов текущего процесса сервера.
/// </summary>
internal sealed class UploadedContentCatalog
{
    private readonly Dictionary<ResPath, int> _sizes = [];
    private readonly List<ResPath> _order = [];

    public int Count => _order.Count;
    public long TotalBytes { get; private set; }

    /// <summary>
    /// Добавляет новый путь или заменяет размер уже известного пути без изменения его позиции.
    /// </summary>
    public void AddOrUpdate(ResPath path, int sizeBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        path = path.Clean().ToRelativePath();
        if (_sizes.TryGetValue(path, out var previousSize))
        {
            _sizes[path] = sizeBytes;
            TotalBytes += (long)sizeBytes - previousSize;
            return;
        }

        _sizes.Add(path, sizeBytes);
        _order.Add(path);
        TotalBytes += sizeBytes;
    }

    /// <summary>
    /// Создаёт сетевой полный снимок каталога.
    /// </summary>
    public MsgUploadedContentManifest CreateManifest()
    {
        var manifest = new MsgUploadedContentManifest();
        manifest.Files.EnsureCapacity(_order.Count);

        for (var i = 0; i < _order.Count; i++)
        {
            var path = _order[i];
            manifest.Files.Add(new UploadedContentManifestEntry(path, _sizes[path]));
        }

        return manifest;
    }
}
