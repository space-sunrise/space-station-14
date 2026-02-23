---
name: ss14-databases
description: Руководство по работе с системой баз данных SS14 (PostgreSQL и SQLite)
---

# 🗄️ Базы Данных SS14 (Databases)

Этот навык описывает архитектуру баз данных в Space Station 14, включая поддержку двух движков (PostgreSQL и SQLite), наследование `DbContext` и лучшие практики для моделей данных.

## 🎯 Когда использовать этот навык
- При создании новых таблиц или сущностей базы данных.
- При изменении существующих моделей данных.
- При написании запросов для получения или сохранения данных игроков, банов или логов.
- При решении проблем, связанных с типами данных (IP-адреса, JSON и т.д.).

## 🏗️ Обзор Архитектуры

SS14 использует **Entity Framework Core** с архитектурой двойного движка для поддержки как **PostgreSQL** (продакшн), так и **SQLite** (разработка/тесты).

### Ключевые Компоненты
1.  **`IServerDbManager`**: Основной интерфейс для операций с БД (`Content.Server/Database/ServerDbManager.cs`).
2.  **`ServerDbBase`**: Абстрактный базовый класс, содержащий общую логику для обоих движков.
3.  **`ServerDbSqlite` и `ServerDbPostgres`**: Конкретные реализации для специфичных движков.
4.  **`ServerDbContext`**: Абстрактный контекст EF Core, определяющий `DbSet`'ы.
5.  **`SqliteServerDbContext` и `PostgresServerDbContext`**: Специфичные контексты, управляющие конфигурацией (например, Value Converters).

### Логика БД vs Модели
-   **Логика**: Находится в `Content.Server/Database`. Содержит `ServerDbManager`, `ServerDbBase` и т.д.
-   **Модели**: Находятся в `Content.Server.Database`. Содержит `Model.cs` (таблицы), миграции и определения контекста.

## 📝 Работа с Моделями Данных

### Определение Сущностей
Сущности определяются в `Content.Server.Database/Model.cs` (или в отдельных файлах в этом пространстве имен).

```csharp
public class MyNewEntity
{
    [Key] // Первичный ключ
    public int Id { get; set; }

    [Required] // Поле обязательно для заполнения
    public string Name { get; set; } = default!;

    // Внешний ключ или просто ID пользователя
    public Guid PlayerUserId { get; set; }
}
```

### Добавление в Контекст
Добавьте `DbSet` в абстрактный `ServerDbContext` в `Content.Server.Database/Model.cs`:

```csharp
public abstract class ServerDbContext : DbContext
{
    // ... существующие наборы
    public DbSet<MyNewEntity> MyNewEntities { get; set; } = default!;
}
```

## ⚠️ Ограничения Движков и Типы Данных

Поскольку SQLite и PostgreSQL поддерживают разные функции, нужно аккуратно работать с типами.

### 1. IP-адреса 🌐
-   **Postgres**: Имеет нативный тип `inet` (`NpgsqlInet`).
-   **SQLite**: **Не** поддерживает IP. Должны храниться как `TEXT`.
-   **Решение**: Использовать `ValueConverter` в `SqliteServerDbContext.OnModelCreating`.

### 2. JSON Данные 📜
-   **Postgres**: Имеет нативный тип `jsonb`.
-   **SQLite**: Должны храниться как `TEXT` или `BLOB`.
-   **Решение**: Использовать раздельные вызовы `db.PgDbContext.Add()` и `db.SqliteDbContext.Add()`, если используются сложные типы, или ValueConverters для общей логики.

### 3. Массивы/Списки 📚
-   **Postgres**: Поддерживает примитивные массивы (например, `List<string>` мапится в `text[]`).
-   **SQLite**: **Не** поддерживает массивы.
-   **Решение**:
    -   **Предпочтительно**: Использовать отдельную таблицу со связью 1:N.
    -   **Альтернатива**: Сериализовать в JSON строку (если не нужен поиск внутри списка).

## 💡 Примеры Использования

### 1. Сохранение Данных (Обработка Различий)
При сохранении данных, использующих специфичные типы (как IP), может потребоваться переопределить метод в `ServerDbSqlite` и `ServerDbPostgres`.

**Абстрактная База (`ServerDbBase.cs`)**:
```csharp
public abstract Task AddServerBanAsync(ServerBanDef serverBan);
```

**Реализация Postgres (`ServerDbPostgres.cs`)**:
```csharp
public override async Task AddServerBanAsync(ServerBanDef serverBan)
{
    await using var db = await GetDbImpl();
    // Postgres поддерживает NpgsqlInet напрямую, конвертация не нужна
    db.PgDbContext.Ban.Add(new ServerBan
    {
        Address = serverBan.Address.ToNpgsqlInet(),
        // ... другие поля
    });
    await db.PgDbContext.SaveChangesAsync();
}
```

**Реализация SQLite (`ServerDbSqlite.cs`)**:
```csharp
public override async Task AddServerBanAsync(ServerBanDef serverBan)
{
    await using var db = await GetDbImpl();
    // SQLite требует конвертации, которая обрабатывается ValueConverter'ом в контексте
    db.SqliteDbContext.Ban.Add(new ServerBan
    {
        Address = serverBan.Address.ToNpgsqlInet(),
        // ... другие поля
    });
    await db.SqliteDbContext.SaveChangesAsync();
}
```

### 2. Загрузка Данных (Общая Логика)
Чтение данных часто идентично для обоих движков и может быть реализовано в `ServerDbBase`.

```csharp
// Content.Server/Database/ServerDbBase.cs
public async Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId, CancellationToken cancel)
{
    await using var db = await GetDb(cancel);

    // Используем .Include() для "жадной" загрузки связанных данных
    var prefs = await db.DbContext
        .Preference
        .Include(p => p.Profiles)
        .ThenInclude(h => h.Jobs) // Загружаем вложенные связи (Профили -> Работы)
        .AsSplitQuery() // Оптимизация: Избегает "взрыва" декартова произведения при множественных include
        .SingleOrDefaultAsync(p => p.UserId == userId.UserId, cancel);

    if (prefs is null) return null;

    // Конвертируем модель БД в Shared модель (логика игры)
    return new PlayerPreferences(...);
}
```

### 3. Запросы с Поиском 🔍
Для текстового поиска используйте `EF.Functions` для поддержки фич конкретного движка, где это возможно, или фоллбек на стандартный LINQ.

```csharp
// ServerDbContext.cs (Абстрактный метод)
public virtual IQueryable<AdminLog> SearchLogs(IQueryable<AdminLog> query, string searchText)
{
    // По умолчанию/SQLite: Простой LIKE
    return query.Where(log => EF.Functions.Like(log.Message, "%" + searchText + "%"));
}

// PostgresServerDbContext.cs (Переопределение)
public override IQueryable<AdminLog> SearchLogs(IQueryable<AdminLog> query, string searchText)
{
    // Postgres: Полнотекстовый поиск используя ToTsVector
    return query.Where(log => EF.Functions.ToTsVector("english", log.Message).Matches(searchText));
}
```

## 🚫 Антипаттерны (Чего НЕ делать)

### ❌ 1. Использование специфичных для Postgres типов в общей логике
**Плохо**: Пытаться сохранить `NpgsqlInet` или массив строк прямо в `ServerDbBase` без проверки движка.
**Почему**: Это сломает SQLite, так как он не знает этих типов.
**Как надо**: Использовать абстрактные методы или ValueConverter'ы.

### ❌ 2. Синхронные вызовы БД (`.Result`, `.Wait()`)
**Плохо**: `db.DbContext.SaveChangeAsync().Result;`
**Почему**: Это блокирует основной поток сервера, вызывая лаги (фризы) у игроков.
**Как надо**: Всегда используйте `await` и асинхронные методы (`ToListAsync`, `FirstOrDefaultAsync`).

### ❌ 3. Загрузка всей таблицы в память
**Плохо**: `var allUsers = db.Player.ToList();` затем фильтрация `allUsers.Where(...)`.
**Почему**: Таблица может содержать десятки тысяч записей. Это убьет память и CPU.
**Как надо**: Фильтруйте **до** материализации запроса: `db.Player.Where(...).ToListAsync();`.

### ❌ 4. Отсутствие индексов для частых запросов
**Плохо**: Искать игрока по `LastSeenUserName` без индекса на этом поле.
**Почему**: Полное сканирование таблицы (Full Table Scan) на каждом входе игрока создаст нагрузку.
**Как надо**: Добавьте индексацию в `OnModelCreating`.

## ✅ Лучшие Практики
1.  **Всегда поддерживайте оба движка**: Никогда не пишите код, который работает только на Postgres. SQLite обязателен для локальной разработки.
2.  **Используйте `CCVars`**: Конфигурация БД (хост, порт, движок) управляется через `CCVars.Database*`.
3.  **Валидация**: Используйте атрибуты `[Required]`, `[MaxLength]` и `[Key]` в моделях.
4.  **Метрики**: Используйте `DbReadOpsMetric.Inc()` и `DbWriteOpsMetric.Inc()` в `ServerDbManager` для отслеживания активности.
