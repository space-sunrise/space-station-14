using System.Text.Json;
using System.Text.RegularExpressions;
using Robust.Packaging;

namespace Content.Packaging;

/// <summary>
/// Фильтрует неиспользуемые текстуры при упаковке клиента.
/// </summary>
public static class UnusedTextureFilter
{
    private static HashSet<string>? _unusedTextures;
    private static readonly object _lock = new();
    private static readonly Regex RsiPathRegex = new(@"^(.+\.rsi)/", RegexOptions.Compiled);

    /// <summary>
    /// Загружает список неиспользуемых текстур из JSON файла.
    /// </summary>
    private static void LoadUnusedTextures(IPackageLogger logger)
    {
        lock (_lock)
        {
            if (_unusedTextures != null)
                return;

            _unusedTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var unusedTexturesJson = Path.Combine("Resources", ".unused_textures.json");
            
            if (!File.Exists(unusedTexturesJson))
            {
                logger.Info($"Файл списка неиспользуемых текстур не найден: {unusedTexturesJson}");
                logger.Info("Для генерации списка выполните: python3 Tools/find_unused_textures.py --output Resources/.unused_textures.json");
                return;
            }

            try
            {
                var json = File.ReadAllText(unusedTexturesJson);
                using var document = JsonDocument.Parse(json);
                
                var root = document.RootElement;
                if (root.TryGetProperty("unused_textures", out var unusedArray))
                {
                    foreach (var item in unusedArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("path", out var pathElement))
                        {
                            var path = pathElement.GetString();
                            if (!string.IsNullOrEmpty(path))
                            {
                                _unusedTextures.Add(path);
                            }
                        }
                    }
                }

                if (root.TryGetProperty("summary", out var summary))
                {
                    if (summary.TryGetProperty("unused_texture_groups", out var count) &&
                        summary.TryGetProperty("unused_size_bytes", out var size))
                    {
                        var sizeMB = size.GetInt64() / (1024.0 * 1024.0);
                        logger.Info($"Загружено {count.GetInt32()} неиспользуемых текстур (~{sizeMB:F2} MB) для фильтрации");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка при загрузке списка неиспользуемых текстур: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Проверяет, является ли файл текстуры неиспользуемым.
    /// </summary>
    public static bool IsTextureUnused(string resourcePath, IPackageLogger logger)
    {
        LoadUnusedTextures(logger);

        if (_unusedTextures == null || _unusedTextures.Count == 0)
            return false;

        // Нормализуем путь (убираем начальный Resources/)
        var normalized = resourcePath.Replace('\\', '/');
        if (normalized.StartsWith("Resources/"))
            normalized = normalized.Substring(10);

        // Проверяем прямое совпадение
        if (_unusedTextures.Contains(normalized))
            return true;

        // Для файлов внутри RSI директорий проверяем родительскую RSI
        var rsiMatch = RsiPathRegex.Match(normalized);
        if (rsiMatch.Success)
        {
            var rsiPath = rsiMatch.Groups[1].Value;
            if (_unusedTextures.Contains(rsiPath))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Создает предикат для фильтрации неиспользуемых текстур.
    /// </summary>
    public static Func<string, bool> CreateFilterPredicate(IPackageLogger logger)
    {
        return path => IsTextureUnused(path, logger);
    }
}
