# Оптимизация текстур - Документация реализации

## Описание проблемы

Игроки скачивают контент с CDN, который включает все текстуры из папки `Resources/Textures`, но не все из них используются в игре. Это приводит к:
- Увеличенному размеру скачиваемого билда
- Повышенному использованию видеопамяти (текстуры загружаются в атлас)

## Решение

Создан инструментарий для автоматического обнаружения и исключения неиспользуемых текстур из сборки клиента.

## Архитектура решения

### 1. Анализ использования (Python)

**Файл:** `Tools/find_unused_textures.py`

Скрипт сканирует:
- **YAML прототипы** (`Resources/Prototypes/`, `Resources/Maps/`)
  - Ищет `sprite:`, `texture:`, `path:` с поддержкой кавычек
- **C# код** (`Content.Client/`, `Content.Server/`, `Content.Shared/`)
  - Ищет hardcoded пути: `"/Textures/..."`, `new ResPath(...)`, `SpriteSpecifier.Rsi(...)`
- **Текстурные файлы** (`Resources/Textures/`)
  - RSI директории (*.rsi с meta.json и PNG)
  - Отдельные PNG/SVG/JPG файлы

**Вывод:** JSON файл со списком неиспользуемых текстур и статистикой

### 2. Фильтрация при упаковке (C#)

**Файл:** `Content.Packaging/UnusedTextureFilter.cs`

- Загружает JSON с неиспользуемыми текстурами
- Предоставляет API `IsTextureUnused(path, logger)`
- Поддерживает case-sensitive и case-insensitive проверки
- Кэширует загруженные данные для производительности

**Файл:** `Content.Packaging/ClientPackaging.cs`

- Интегрирует `UnusedTextureFilter` через `AssetPassFilterDrop`
- Предзагружает список текстур для оптимизации
- Автоматически исключает неиспользуемые файлы из ZIP архива

### 3. Интеграция в CI/CD

**Файлы:** `.github/workflows/publish.yml`, `.github/workflows/test-packaging.yml`

Добавлен шаг перед упаковкой:
```yaml
- name: Generate unused textures list
  run: python3 Tools/find_unused_textures.py
```

## Использование

### Локальная разработка

```bash
# Сгенерировать список неиспользуемых текстур
python3 Tools/find_unused_textures.py

# Просмотреть результаты
cat Resources/.unused_textures.json

# Упаковать клиент с фильтрацией
dotnet run --project Content.Packaging client
```

### CI/CD

Автоматически выполняется при:
- Публикации релизов (`publish.yml`)
- Тестировании упаковки (`test-packaging.yml`)

## Результаты

### Статистика

- **Всего текстурных групп:** 5596
- **Используемых:** 4546 (88.73 MB)
- **Неиспользуемых:** 1050 (15.10 MB)
- **Процент экономии:** 14.54%

### Топ-10 неиспользуемых текстур

1. `_Sunrise/Interface/Misc/cutscenes.rsi` - 2.99 MB
2. `_Sunrise/Parallaxes/DeltaParallaxBG.png` - 2.00 MB
3. `_Sunrise/Lobby/Animations/jungle/animation.png` - 859 KB
4. `_Sunrise/Parallaxes/NES/BlueParallax.png` - 698 KB
5. `_Sunrise/Abductor/Parallaxes/Abyss.png` - 533 KB
6. `_Sunrise/Lobby/Animations/guts_vs_griffit/animation.png` - 404 KB
7. `_Sunrise/Lobby/Animations/sunny_PTL/animation.png` - 351 KB
8. `_Sunrise/Lobby/Animations/evening_sun5/animation.png` - 335 KB
9. `_Sunrise/Lobby/Animations/eclipse/animation.png` - 317 KB
10. `Mobs/Customization/undergarments.rsi` - 303 KB

### Экономия для игроков

- **Размер скачиваемого билда:** -15.10 MB
- **Использование видеопамяти:** -15.10 MB
- **Время загрузки:** ~5-10 секунд экономии (в зависимости от скорости интернета)

## Технические детали

### Формат JSON отчета

```json
{
  "summary": {
    "total_texture_groups": 5596,
    "used_texture_groups": 4546,
    "unused_texture_groups": 1050,
    "total_size_bytes": 108865707,
    "used_size_bytes": 93035534,
    "unused_size_bytes": 15830173,
    "unused_percentage": 14.54
  },
  "unused_textures": [
    {
      "path": "относительный/путь/к/текстуре.rsi",
      "size_bytes": 12345,
      "file_count": 5,
      "files": ["файл1", "файл2"]
    }
  ]
}
```

### Алгоритм фильтрации

1. Сканирование YAML:
   - Regex паттерны с поддержкой кавычек
   - Обработка многострочных значений
   - Извлечение путей к RSI и PNG

2. Сканирование C#:
   - Поиск строковых литералов с путями
   - Анализ конструкторов `ResPath`, `SpriteSpecifier`
   - Поддержка относительных и абсолютных путей

3. Проверка текстуры:
   - Case-sensitive проверка (основная)
   - Case-insensitive проверка (fallback для Windows)
   - Проверка родительской RSI для файлов внутри

### Производительность

- **Анализ:** ~5-10 секунд (однократно)
- **Загрузка JSON:** <100ms (при упаковке)
- **Проверка текстуры:** O(1) (HashSet lookup)
- **Предзагрузка:** Выполняется один раз перед фильтрацией

## Известные ограничения

### Ложноположительные срабатывания

Текстуры могут быть помечены как неиспользуемые, если:
- Загружаются динамически через конфигурацию
- Используются внешними модами
- Имеют нестандартные паттерны загрузки

**Решение:** Добавить ссылку в прототип или модифицировать скрипт

### Ложноотрицательные срабатывания

Используемые текстуры могут не обнаружиться, если:
- Используется нестандартный формат ссылки
- Путь формируется динамически в рантайме

**Решение:** Добавить новые паттерны в скрипт

## Обслуживание

### Регулярное обновление списка

Рекомендуется обновлять список:
- После добавления новых текстур
- После удаления прототипов
- Раз в месяц (плановое обслуживание)

```bash
python3 Tools/find_unused_textures.py
git add Resources/.unused_textures.json
git commit -m "Update unused textures list"
```

### Мониторинг

После деплоя следить за:
- Логами клиента (ошибки загрузки текстур)
- Размером SS14.Client.zip (должен уменьшиться)
- Отзывами игроков (отсутствующие текстуры)

### Откат изменений

Если обнаружена критичная ошибка:

1. Удалить `.unused_textures.json`:
   ```bash
   rm Resources/.unused_textures.json
   ```

2. Упаковать клиент без фильтрации:
   ```bash
   dotnet run --project Content.Packaging client
   ```

## Дальнейшие улучшения

### Возможные оптимизации

1. **Анализ shader файлов** - поиск текстур, используемых в шейдерах
2. **Анализ XML/XAML** - поиск текстур в UI разметке
3. **Whitelist механизм** - принудительное включение текстур
4. **Incremental scanning** - обновление только изменённых файлов
5. **Compression analysis** - приоритизация по степени сжатия

### Метрики для отслеживания

- Динамика процента неиспользуемых текстур
- Топ неиспользуемых текстур по размеру
- Время упаковки с фильтрацией vs без
- Размер финального архива клиента

## Связанные файлы

- `Tools/find_unused_textures.py` - Скрипт анализа
- `Tools/README_unused_textures.md` - Подробная документация
- `Content.Packaging/UnusedTextureFilter.cs` - Фильтр текстур
- `Content.Packaging/ClientPackaging.cs` - Интеграция в упаковку
- `.github/workflows/publish.yml` - CI/CD для релизов
- `.github/workflows/test-packaging.yml` - CI/CD для тестирования
- `.gitignore` - Исключение `.unused_textures.json` из Git

## Вопросы и поддержка

При возникновении вопросов:
1. Изучить `Tools/README_unused_textures.md`
2. Проверить логи упаковки
3. Запустить тесты вручную
4. Создать issue в репозитории

---

**Дата последнего обновления:** 2025-11-21  
**Версия:** 1.0  
**Автор:** GitHub Copilot AI
