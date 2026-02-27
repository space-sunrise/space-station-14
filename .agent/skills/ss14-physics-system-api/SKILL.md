---
name: SS14 Physics System API
description: Полный справочник публичного API SharedPhysicsSystem в Space Station 14: все семейства методов, перегрузки, ограничения, редкие методы и практические примеры использования на сервере и клиенте. Используй, когда нужно точно выбрать метод PhysicsSystem и не сломать контакты/коллизии/предикцию.
---

# PhysicsSystem: API

Этот skill — каталог публичного API `SharedPhysicsSystem` 🙂
Для общей архитектуры сначала прочитай `SS14 Physics System Core`.

## Как пользоваться этим каталогом

1. Сначала выбрать тип задачи:
- силы/скорости,
- тип тела/сон/коллизия,
- фикстуры/шейпы,
- контакты/рейкасты/дистанции,
- преобразования трансформов.

2. Затем выбрать уровень:
- безопасный gameplay-метод,
- или низкоуровневый engine-метод (редко нужен).

3. Если менялись фикстуры/коллизия:
- проверить, нужен ли `RegenerateContacts`.

## 1) Runtime и world-уровень

- `Initialize()`
- `Shutdown()`
- `Step(float frameTime, bool prediction)`
- `SetGravity(Vector2 value)`
- `UpdateIsPredicted(EntityUid? uid, PhysicsComponent? physics = null)` (virtual)

Публичные runtime-поля/состояние:
- `Gravity`
- `AwakeBodies`
- `EffectiveCurTime` (substep-aware время)

Когда использовать:
- gameplay-код обычно не вызывает `Initialize/Shutdown/Step` напрямую.
- полезны `SetGravity` и `UpdateIsPredicted`-flow.

## 2) Импульсы и силы

- `ApplyAngularImpulse(EntityUid uid, float impulse, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `ApplyForce(EntityUid uid, Vector2 force, Vector2 point, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `ApplyForce(EntityUid uid, Vector2 force, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `ApplyTorque(EntityUid uid, float torque, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `ApplyLinearImpulse(EntityUid uid, Vector2 impulse, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `ApplyLinearImpulse(EntityUid uid, Vector2 impulse, Vector2 point, FixturesComponent? manager = null, PhysicsComponent? body = null)`

Нюанс:
- методы сами пытаются разбудить тело; для немобильных тел эффекта не будет.

## 3) Динамика тела и состояние

### 3.1 Скорости, демпфинг, масса

- `DestroyContacts(PhysicsComponent body)`
- `DestroyContact(Contact contact)`
- `ResetDynamics(EntityUid uid, PhysicsComponent body, bool dirty = true)`
- `ResetMassData(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `SetAngularVelocity(EntityUid uid, float value, bool dirty = true, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `SetLinearVelocity(EntityUid uid, Vector2 velocity, bool dirty = true, bool wakeBody = true, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `SetAngularDamping(EntityUid uid, PhysicsComponent body, float value, bool dirty = true)`
- `SetLinearDamping(EntityUid uid, PhysicsComponent body, float value, bool dirty = true)`
- `SetFriction(EntityUid uid, PhysicsComponent body, float value, bool dirty = true)`
- `SetInertia(EntityUid uid, PhysicsComponent body, float value, bool dirty = true)`
- `SetLocalCenter(EntityUid uid, PhysicsComponent body, Vector2 value)`

### 3.2 Awake/sleep/prediction

- `[Obsolete] SetAwake(EntityUid uid, PhysicsComponent body, bool value, bool updateSleepTime = true)`
- `SetAwake(Entity<PhysicsComponent> ent, bool value, bool updateSleepTime = true)`
- `SetSleepingAllowed(EntityUid uid, PhysicsComponent body, bool value, bool dirty = true)`
- `SetSleepTime(PhysicsComponent body, float value)`
- `WakeBody(EntityUid uid, bool force = false, FixturesComponent? manager = null, PhysicsComponent? body = null)`

### 3.3 Режим и коллизия тела

- `TrySetBodyType(EntityUid uid, BodyType value, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `SetBodyType(EntityUid uid, BodyType value, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `SetBodyStatus(EntityUid uid, PhysicsComponent body, BodyStatus status, bool dirty = true)`
- `SetCanCollide(EntityUid uid, bool value, bool dirty = true, bool force = false, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `SetFixedRotation(EntityUid uid, bool value, bool dirty = true, FixturesComponent? manager = null, PhysicsComponent? body = null)`

Критично:
- `BodyType` влияет на solver.
- `BodyStatus` — игровой статус-флаг, не замена `BodyType` ⚠️

## 4) Скорости в map-терминах

- `GetLinearVelocity(EntityUid uid, Vector2 point, PhysicsComponent? component = null, TransformComponent? xform = null)`
- `GetMapLinearVelocity(EntityCoordinates coordinates)`
- `GetMapLinearVelocity(EntityUid uid, PhysicsComponent? component = null, TransformComponent? xform = null)`
- `GetMapAngularVelocity(EntityUid uid, PhysicsComponent? component = null, TransformComponent? xform = null)`
- `GetMapVelocities(EntityUid uid, PhysicsComponent? component = null, TransformComponent? xform = null)`

Когда использовать:
- любая логика в world/map-space, особенно при parent-иерархии.

## 5) Fixtures: материал и collision-профиль

- `SetDensity(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)`
- `SetFriction(EntityUid uid, string fixtureId, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)`
- `SetHard(EntityUid uid, Fixture fixture, bool value, FixturesComponent? manager = null)`
- `SetRestitution(EntityUid uid, Fixture fixture, float value, bool update = true, FixturesComponent? manager = null)`
- `ScaleFixtures(Entity<FixturesComponent?> ent, float factor)`
- `IsCurrentlyHardCollidable(Entity<FixturesComponent?, PhysicsComponent?> bodyA, Entity<FixturesComponent?, PhysicsComponent?> bodyB)`
- `IsHardCollidable(Entity<FixturesComponent?, PhysicsComponent?> bodyA, Entity<FixturesComponent?, PhysicsComponent?> bodyB)`
- `AddCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `SetCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `RemoveCollisionMask(EntityUid uid, string fixtureId, Fixture fixture, int mask, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `AddCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `SetCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `RemoveCollisionLayer(EntityUid uid, string fixtureId, Fixture fixture, int layer, FixturesComponent? manager = null, PhysicsComponent? body = null)`

Практика:
- после существенных collision/fixture-изменений часто нужен `RegenerateContacts`.

## 5.1) CollisionGroup: полная карта слоёв/масок 🙂

### Правило контакта (источник истины)

Две фикстуры считаются контактирующими, если выполняется хотя бы одно условие:
- `(A.mask & B.layer) != 0`
- `(B.mask & A.layer) != 0`

То есть это **не строгая двусторонняя фильтрация**, а логика "хотя бы одна сторона хочет контакт".

```csharp
// Базовая проверка контакта пары фикстур.
var shouldCollide =
    (fixtureA.CollisionMask & fixtureB.CollisionLayer) != 0 ||
    (fixtureB.CollisionMask & fixtureA.CollisionLayer) != 0;
```

Важно:
- `shouldCollide` отвечает за контакт в broadphase/narrowphase.
- Физическое "упирание" дополнительно требует `Hard = true` на обеих сторонах.

### Матрица базовых layer/mask-битов (все базовые флаги)

`✓` — слой будет пойман этой маской, `·` — не будет.

| layer \ mask | Opaque | Impassable | MidImpassable | HighImpassable | LowImpassable | GhostImpassable | BulletImpassable | InteractImpassable | DoorPassable |
|---|---|---|---|---|---|---|---|---|---|
| Opaque | ✓ | · | · | · | · | · | · | · | · |
| Impassable | · | ✓ | · | · | · | · | · | · | · |
| MidImpassable | · | · | ✓ | · | · | · | · | · | · |
| HighImpassable | · | · | · | ✓ | · | · | · | · | · |
| LowImpassable | · | · | · | · | ✓ | · | · | · | · |
| GhostImpassable | · | · | · | · | · | ✓ | · | · | · |
| BulletImpassable | · | · | · | · | · | · | ✓ | · | · |
| InteractImpassable | · | · | · | · | · | · | · | ✓ | · |
| DoorPassable | · | · | · | · | · | · | · | · | ✓ |

### Готовые группы CollisionGroup: что это и кому выдавать

Колонка `Кому выдавать` описывает практический intent (типовой контент-профиль), а не жесткое правило.

| Группа | Использовать как | Состав | Кому выдавать |
|---|---|---|---|
| `None` | layer/mask | `0` | временно отключенный фильтр, служебные кейсы |
| `Opaque` | layer | базовый бит | объекты, которые блокируют свет/часть лучей |
| `Impassable` | layer/mask бит | базовый бит | полноценные препятствия по земле |
| `MidImpassable` | layer/mask бит | базовый бит | "средняя высота": мобы, стойки, часть мебели |
| `HighImpassable` | layer/mask бит | базовый бит | верхняя часть препятствий (столы, высокие блокеры) |
| `LowImpassable` | layer/mask бит | базовый бит | низкие препятствия, важные для small/under-table логики |
| `GhostImpassable` | layer/mask бит | базовый бит | блокеры для призраков/наблюдателей |
| `BulletImpassable` | layer/mask бит | базовый бит | всё, что должно останавливать пули |
| `InteractImpassable` | layer/mask бит | базовый бит | блокировка `InRangeUnobstructed`/взаимодействия |
| `DoorPassable` | layer/mask бит | базовый бит | специальные "проходимые дверью" поверхности |
| `MapGrid` | layer | спецбит карты/грида | системный слой грида |
| `AllMask` | mask | `-1` | редкие force-кейсы (почти всегда избыточно) |
| `SingularityLayer` | layer+mask | `Opaque+Impassable+Mid+High+Low+Bullet+Interact+DoorPassable` | сингулярность/аналогичные "всепожирающие" сущности |
| `MobMask` | mask | `Impassable+High+Mid+Low` | гуманоиды/обычные наземные мобы |
| `MobLayer` | layer | `Opaque+BulletImpassable` | базовый слой обычных мобов |
| `SmallMobMask` | mask | `Impassable+Low` | маленькие мобы (мыши и т.п.) |
| `SmallMobLayer` | layer | `Opaque+BulletImpassable` | слой small mobs |
| `FlyingMobMask` | mask | `Impassable+High` | мелкие летающие сущности |
| `FlyingMobLayer` | layer | `Opaque+BulletImpassable` | слой flying mobs |
| `LargeMobMask` | mask | `Impassable+High+Mid+Low` | крупные сущности/транспорт/мехи |
| `LargeMobLayer` | layer | `Opaque+High+Mid+Low+BulletImpassable` | большие объекты, которые "занимают" больше высоты |
| `MachineMask` | mask | `Impassable+Mid+Low` | крупные машины/стационарные конструкции |
| `MachineLayer` | layer | `Opaque+Mid+Low+BulletImpassable` | машины, автоматы, каркасы |
| `ConveyorMask` | layer или mask по месту | `Impassable+Mid+Low+DoorPassable` | конвейеры и совместимость с дверями |
| `CrateMask` | mask | `Impassable+High+Low` | контейнеры/клетки, которым нужна "низкая проходимость" под флапами |
| `TableMask` | mask | `Impassable+Mid` | столы/поверхности, с которыми должны взаимодействовать другие столы |
| `TableLayer` | layer | `MidImpassable` | столы, перила, часть узких барьеров |
| `TabletopMachineMask` | mask | `Impassable+High` | настольные машины/виндуры |
| `TabletopMachineLayer` | layer | `Opaque+BulletImpassable` | небольшие настольные устройства |
| `GlassAirlockLayer` | layer | `High+Mid+Bullet+Interact` | стеклянные airlock/windoor-профили |
| `AirlockLayer` | layer | `Opaque+GlassAirlockLayer` | обычные airlock-профили |
| `HumanoidBlockLayer` | layer | `High+Mid` | сборки/промежуточные "блоки человека" |
| `SlipLayer` | layer | `Mid+Low` | не-hard триггеры скольжения/лужи/ловушки |
| `ItemMask` | mask | `Impassable+High` | предметы, гранаты, мелкий реквизит |
| `ThrownItem` | layer | `Impassable+High+Bullet` | специальные throw-профили (редкий точечный кейс) |
| `WallLayer` | layer | `Opaque+Impassable+High+Mid+Low+Bullet+Interact` | полноценные стены/барьеры |
| `GlassLayer` | layer | `Impassable+High+Mid+Low+Bullet+Interact` | окна/стеклянные препятствия |
| `HalfWallLayer` | layer | `Mid+Low` | "полувысокие" препятствия |
| `FlimsyLayer` | layer | `Opaque+High+Mid+Low+Interact` | "хрупкие" стены, которые не должны ловить пули как wall |
| `SpecialWallLayer` | layer+mask | `Opaque+High+Mid+Low+Bullet` | force-wall тип: блокирует движение/пули, но не блокирует interact так же, как `WallLayer` |
| `FullTileMask` | mask | `Impassable+High+Mid+Low+Interact` | полный тайл-блокер (стены, окна, двери, мемориалы и т.п.) |
| `FullTileLayer` | layer | `Opaque+High+Mid+Low+Bullet+Interact` | редкие не-hard/full-tile сенсоры и спец-фикстуры |
| `SubfloorMask` | mask | `Impassable+Low` | подпол/трубы/подпольные сети |

### Быстрые шаблоны выдачи (из актуальных конфигов)

- Гуманоид/большинство мобов: `mask = MobMask`, `layer = MobLayer`.
- Маленький моб: `mask = SmallMobMask`, `layer = SmallMobLayer`.
- Летающий моб: `mask = FlyingMobMask`, `layer = FlyingMobLayer`.
- Базовая структура/машина: `mask = MachineMask`, `layer = MachineLayer` или `Mid+Low` для generic-базы.
- Стол: `mask = TableMask`, `layer = TableLayer`.
- Настольная машина: `mask = TabletopMachineMask`, `layer = TabletopMachineLayer`.
- Окно: `mask = FullTileMask`, `layer = GlassLayer`.
- Обычная дверь: `mask = FullTileMask`, `layer = AirlockLayer` (со сваркой в `WallLayer`).
- Стеклянная дверь/виндур: `mask = FullTileMask` или `TabletopMachineMask` по типу двери, `layer = GlassAirlockLayer`.
- Стена: `mask = FullTileMask`, `layer = WallLayer`.
- Предмет: `mask = ItemMask`, `layer` часто `0` (если слой не нужен).
- Скользкий сенсор/ловушка: `hard = false`, `mask = ItemMask`, `layer = SlipLayer`.
- Подпольная труба: `mask = SubfloorMask`, обычно с выключенной коллизией до нужного anchored-state.
- Наблюдатель/инкорпорал: чаще `layer = GhostImpassable`, `mask = 0`.
- Сингулярность: симметричный профиль `layer = mask = SingularityLayer`.

### Кто с кем контактирует: матрица типовых профилей

`✓` — контакт возможен по `layer/mask`, `·` — нет.
Для `Airlock` здесь принят типовой профиль: `layer = AirlockLayer`, `mask = FullTileMask`.
Для `Item` и `SubfloorPipe` принят частый случай с нулевым `layer`.

| Профиль | Mob | SmallMob | FlyingMob | Machine | Table | Airlock | Wall | Item | SlipTrigger(non-hard) | SubfloorPipe |
|---|---|---|---|---|---|---|---|---|---|---|
| Mob | · | · | · | ✓ | ✓ | ✓ | ✓ | · | ✓ | · |
| SmallMob | · | · | · | ✓ | · | · | ✓ | · | ✓ | · |
| FlyingMob | · | · | · | · | · | ✓ | ✓ | · | · | · |
| Machine | ✓ | ✓ | · | ✓ | ✓ | ✓ | ✓ | · | ✓ | ✓ |
| Table | ✓ | · | · | ✓ | ✓ | ✓ | ✓ | · | ✓ | · |
| Airlock | ✓ | · | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | · |
| Wall | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Item | · | · | · | · | · | ✓ | ✓ | · | · | · |
| SlipTrigger(non-hard) | ✓ | ✓ | · | ✓ | ✓ | ✓ | ✓ | · | · | ✓ |
| SubfloorPipe | · | · | · | ✓ | · | · | ✓ | · | ✓ | · |

### Паттерны для настройки коллизии

- Используй готовые пары (`MobMask+MobLayer`, `TabletopMachineMask+TabletopMachineLayer`) вместо ручной сборки битов.
- Для full-tile блокеров держи `mask = FullTileMask`, а поведение отличай `layer` (`WallLayer`, `GlassLayer`, `AirlockLayer`).
- Для триггеров (лужи/скольжение/сенсоры) ставь `hard = false`, а фильтрацию делай через `layer/mask`.
- Для специальных пропусков (конвейер под дверями, подпол) используй `DoorPassable`/`SubfloorMask`, а не ad-hoc исключения.

### Анти-паттерны настройки коллизии

- Давать `AllMask` обычным gameplay-сущностям.
- Миксовать `WallLayer` и `SpecialWallLayer` без явной цели по interaction/blocking-поведению.
- Включать `InteractImpassable` там, где объект должен быть простреливаем/проходим для interaction-проверок.
- Менять `layer/mask` и не делать re-sync контактов (`RegenerateContacts`) при заметном изменении поведения.

### Примеры конфигураций

```yaml
# Типичный моб: стандартная наземная коллизия.
fixtures:
  fix1:
    shape: !type:PhysShapeCircle { radius: 0.35 }
    mask: [MobMask]
    layer: [MobLayer]
```

```yaml
# Полный тайл-блокер (стена): блокирует проход, взаимодействие и пули.
fixtures:
  fix1:
    shape: !type:PhysShapeAabb { bounds: "-0.5,-0.5,0.5,0.5" }
    mask: [FullTileMask]
    layer: [WallLayer]
```

```yaml
# Скользящая ловушка: не hard, но дает contact-событие по нужной маске.
fixtures:
  floortrap:
    hard: false
    shape: !type:PhysShapeAabb { bounds: "-0.4,-0.4,0.4,0.4" }
    mask: [ItemMask]
    layer: [SlipLayer]
```

## 6) Shapes API (геометрия фикстур)

- `SetRadius(EntityUid uid, string fixtureId, Fixture fixture, IPhysShape shape, float radius, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `SetPositionRadius(EntityUid uid, string fixtureId, Fixture fixture, PhysShapeCircle shape, Vector2 position, float radius, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `SetPosition(EntityUid uid, string fixtureId, Fixture fixture, PhysShapeCircle circle, Vector2 position, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `SetVertices(EntityUid uid, string fixtureId, Fixture fixture, EdgeShape edge, Vector2 vertex0, Vector2 vertex1, Vector2 vertex2, Vector2 vertex3, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `SetVertices(EntityUid uid, string fixtureId, Fixture fixture, PolygonShape poly, Vector2[] vertices, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`

## 7) Контакты и контактные выборки

- `RegenerateContacts(Entity<PhysicsComponent?> entity)`
- `GetTouchingContacts(Entity<FixturesComponent?> entity, string? ignoredFixtureId = null)`
- `GetContacts(Entity<FixturesComponent?> entity, bool includeDeleting = false)`
- `GetContactingEntities(Entity<PhysicsComponent?> ent, HashSet<EntityUid> contacting, bool approximate = false)`
- `GetContactingEntities(EntityUid uid, PhysicsComponent? body = null, bool approximate = false)`
- `IsInContact(PhysicsComponent body, bool approximate = false)`

`ContactEnumerator`:
- `MoveNext(out Contact? contact)`
- полезен для итерации без лишних аллокаций.

## 8) Трансформы физики и bounds

- `GetRelativePhysicsTransform(Transform worldTransform, Entity<TransformComponent?> relative)`
- `GetRelativePhysicsTransform(Entity<TransformComponent?> entity, Entity<TransformComponent?> relative)`
- `GetLocalPhysicsTransform(EntityUid uid, TransformComponent? xform = null)`
- `GetPhysicsTransform(EntityUid uid, TransformComponent? xform = null)`
- `GetWorldAABB(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `GetHardAABB(EntityUid uid, FixturesComponent? manager = null, PhysicsComponent? body = null, TransformComponent? xform = null)`
- `GetHardCollision(EntityUid uid, FixturesComponent? manager = null)`
- `GetHardCollision(FixturesComponent manager)` (static)

## 9) Пространственные query и ray API

- `TryCollideRect(Box2 collider, MapId mapId, bool approximate = true)`
- `GetEntitiesIntersectingBody(EntityUid uid, int collisionMask, bool approximate = true, PhysicsComponent? body = null, FixturesComponent? fixtureComp = null, TransformComponent? xform = null)`
- `[Obsolete] GetCollidingEntities(MapId mapId, in Box2 worldAABB)`
- `[Obsolete] GetCollidingEntities(MapId mapId, in Box2Rotated worldBounds)`
- `IntersectRayWithPredicate(MapId mapId, CollisionRay ray, float maxLength = 50F, Func<EntityUid, bool>? predicate = null, bool returnOnFirstHit = true)`
- `IntersectRayWithPredicate<TState>(MapId mapId, CollisionRay ray, TState state, Func<EntityUid, TState, bool> predicate, float maxLength = 50F, bool returnOnFirstHit = true)`
- `IntersectRay(MapId mapId, CollisionRay ray, float maxLength = 50, EntityUid? ignoredEnt = null, bool returnOnFirstHit = true)`
- `IntersectRayPenetration(MapId mapId, CollisionRay ray, float maxLength, EntityUid? ignoredEnt = null)`
- `TryGetDistance(EntityUid uidA, EntityUid uidB, out float distance, TransformComponent? xformA = null, TransformComponent? xformB = null, FixturesComponent? managerA = null, FixturesComponent? managerB = null, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)`
- `TryGetNearestPoints(EntityUid uidA, EntityUid uidB, out Vector2 pointA, out Vector2 pointB, TransformComponent? xformA = null, TransformComponent? xformB = null, FixturesComponent? managerA = null, FixturesComponent? managerB = null, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)`
- `TryGetNearest(EntityUid uidA, EntityUid uidB, out Vector2 pointA, out Vector2 pointB, out float distance, Transform xfA, Transform xfB, FixturesComponent? managerA = null, FixturesComponent? managerB = null, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)`
- `TryGetNearest(EntityUid uid, MapCoordinates coordinates, out Vector2 point, out float distance, TransformComponent? xformA = null, FixturesComponent? manager = null, PhysicsComponent? body = null)`
- `TryGetNearest(EntityUid uidA, EntityUid uidB, out Vector2 point, out Vector2 pointB, out float distance, TransformComponent? xformA = null, TransformComponent? xformB = null, FixturesComponent? managerA = null, FixturesComponent? managerB = null, PhysicsComponent? bodyA = null, PhysicsComponent? bodyB = null)`

## 10) События шага физики (полезно для контроллеров)

- `PhysicsUpdateBeforeSolveEvent`
- `PhysicsUpdateAfterSolveEvent`

Использование:
- подписываться для логики, которая должна идти до/после solve в рамках того же тика.

## 11) Паттерны выбора API 🙂

- Нужен управляемый толчок: `ApplyLinearImpulse`.
- Нужен устойчивый "движок"/тяга: `ApplyForce` + корректный `SetSleepingAllowed`.
- Нужно "поднять в воздух" gameplay-сущность: `SetBodyStatus(InAir)` + при необходимости скорректировать mask/layer.
- Нужно временно выключить столкновения: `SetCanCollide(false)` или `SetHard(false)` для конкретных фикстур.
- Нужна контактная реакция после резкой смены коллизии: `RegenerateContacts`.
- Нужна корректная дистанция между сложными формами: `TryGetNearest*`.
- Нужно преобразование между пространствами грида/карты: `GetRelativePhysicsTransform`/`GetLocalPhysicsTransform`.

## 12) Анти-паттерны

- Путать `BodyType` и `BodyStatus`.
- Править fixture/body поля напрямую, обходя API.
- Вставлять/вынимать из контейнера без гашения физики.
- Использовать `[Obsolete]` query-методы там, где уже есть более точные `TryGetNearest`/ray-методы.
- Вызывать редкие engine-методы (`Step`, `Initialize`, `DestroyContact`) из обычной gameplay-логики.

## 13) Примеры из кода

### Пример 1: shuttle/двигатель через силу и сон

```csharp
if (finalForce.Length() > 0f)
    PhysicsSystem.ApplyForce(shuttleUid, finalForce, body: body);

// Без входа можно разрешить сон, чтобы не держать тело "вечнозлым".
PhysicsSystem.SetSleepingAllowed(shuttleUid, body, true);
```

### Пример 2: контактный итератор как источник "кто рядом"

```csharp
var contacts = PhysicsSystem.GetContacts(conveyorUid);
while (contacts.MoveNext(out var contact))
{
    var other = contact.OtherEnt(conveyorUid);
    PhysicsSystem.WakeBody(other);
}
```

### Пример 3: re-check контактов после смены физрежима

```csharp
_physics.SetBodyType(uid, BodyType.Dynamic, fixtures, body, xform);
_physics.SetCanCollide(uid, true, manager: fixtures, body: body);
_broadphase.RegenerateContacts((uid, body, fixtures, xform));
```

### Пример 4: nearest-query для честной геометрической дистанции

```csharp
if (!_physics.TryGetNearest(uidA, uidB, out var pointA, out var pointB, out var distance, xfA, xfB))
    return;

if (distance <= interactionRange)
{
    // Объекты реально в радиусе с учетом форм фикстур.
}
```

### Пример 5: runtime переключение "в воздухе" с запретом сна

```csharp
_physics.SetBodyStatus(target, targetPhysics, BodyStatus.InAir, false);
_physics.SetSleepingAllowed(target, targetPhysics, false);
_physics.WakeBody(target, body: targetPhysics);
```

### Пример 6: точечный raycast с фильтром сущностей

```csharp
var ray = new CollisionRay(origin, direction, mask);
var results = _physics.IntersectRayWithPredicate(
    mapId,
    ray,
    maxDistance,
    uid => uid == source || uid == ignored,
    returnOnFirstHit: false);
```

### Пример 7: массовый scale физформ через системный API

```csharp
// Используется для согласованного масштабирования визуала и физики.
_physics.ScaleFixtures(entity, factor);
```

### Пример 8: выбор локального physics-transform (а не просто local transform)

```csharp
// Для некоторых проверок пересечений нужен именно physics-space текущего broadphase.
var physXf = PhysicsSystem.GetLocalPhysicsTransform(uid);
```

## 14) Мини-чеклист перед вызовом метода

- Выбран правильный слой API (high-level gameplay vs low-level engine)?
- Есть ли риск, что объект сейчас в контейнере/на другой карте?
- Нужен ли `WakeBody`?
- После изменения fixture/collision нужен ли `RegenerateContacts`?
- Не используешь ли `[Obsolete]` там, где есть актуальный метод?
