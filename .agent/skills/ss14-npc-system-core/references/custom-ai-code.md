# Custom AI Code Guide

## Назначение

Используй этот файл, когда прототипов уже недостаточно и нужно писать свой код ИИ.

## Содержание

1. Быстрый выбор extension point
2. Где писать код
3. Контракт HTNOperator
4. Шаблон оператора
5. Шаблон предусловия
6. Подключение нового C# к YAML
7. Практический паттерн “оператор + система”
8. Анти-паттерны при написании своего AI-кода

## Быстрый выбор extension point

1. Нужна новая проверка условия:
добавляй `HTNPrecondition`.
2. Нужен новый атомарный шаг поведения:
добавляй `HTNOperator`.
3. Нужен новый способ выбора целей:
добавляй `UtilityQuery`/`UtilityConsideration`/`UtilityCurve`.
4. Нужна тяжелая runtime-логика, тикающая отдельно:
добавляй/расширяй систему + runtime-компонент, а оператор используй как “шлюз”.
5. Нужна fork-специфика:
добавляй код в форковый сегмент (`Content.Server/_Sunrise/...`) и подключай через YAML.

## Где писать код

1. Операторы:
`Content.Server/NPC/HTN/PrimitiveTasks/Operators/**`
2. Предусловия:
`Content.Server/NPC/HTN/Preconditions/**`
3. Utility API:
`Content.Server/NPC/Queries/**`
4. Runtime-системы NPC:
`Content.Server/NPC/Systems/**`
5. Форковые расширения (пример):
`Content.Server/_Sunrise/NPC/HTN/**`

## Контракт HTNOperator

1. `Initialize(...)`:
инициализировать зависимости/системы.
2. `Plan(...)`:
проверять валидность шага и опционально возвращать `effects`.
3. `Startup(...)`:
запускать runtime-часть шага.
4. `Update(...)`:
возвращать `Continuing`, `Finished` или `Failed`.
5. `TaskShutdown(...)`/`PlanShutdown(...)`:
чистить компоненты/blackboard/state.

## Шаблон оператора

```csharp
public sealed partial class MyOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    [DataField("targetKey")]
    public string TargetKey = "Target";

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(
        NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) || target == EntityUid.Invalid)
            return (false, null);

        return (true, null);
    }

    public override void Startup(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        // Запустить runtime-логику или выставить компонент.
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        // Привести статус runtime-логики к HTNOperatorStatus.
        return HTNOperatorStatus.Finished;
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        // Очистить runtime-компоненты/keys.
    }
}
```

## Шаблон предусловия

```csharp
public sealed partial class MyPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    [DataField("key", required: true)]
    public string Key = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.TryGetValue<EntityUid>(Key, out var ent, _entManager) && ent != EntityUid.Invalid;
    }
}
```

## Подключение нового C# к YAML

1. Создай класс оператора/предусловия в нужном namespace.
2. Используй его в YAML через `!type:MyOperator` или `!type:MyPrecondition`.
3. Вставь новый примитив/предусловие в нужный `htnCompound`.
4. При необходимости расширь `utility_queries.yml`.

## Практический паттерн “оператор + система”

1. В операторе:
минимум логики, только orchestration и контракт с blackboard.
2. В системе:
весь тяжелый runtime, обновление компонентов, работа с физикой/боем/actions.
3. В shutdown:
гарантированный cleanup, чтобы NPC не оставлял “висящие” runtime-состояния.

## Анти-паттерны при написании своего AI-кода

1. Держать сложный mutable-state внутри singleton оператора.
2. Делать entity side effects в `Plan()` и ломать детерминизм планировщика.
3. Не обрабатывать `Failed` состояние и получать бесконечный replan-thrash.
4. Удалять/менять ключи blackboard в неожиданных местах без контракта.
5. Подменять существующие боевые/steering системы ad-hoc кодом внутри оператора.
