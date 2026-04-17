---
name: ss14-eventbus
description: Architectural guide to EventBus in Space Station 14 - strict event taxonomy, subscription storage, dispatch logic and internal optimization mechanisms.
---

# 🚌 EventBus architecture in SS14

EventBus is the central nervous system of the Space Station 14 (RobustToolbox) engine. It orchestrates communication between systems and entities, implementing a highly optimized dispatch mechanism without unnecessary allocations. 🧠

## 🏗️ Basic Architecture

EventBus is implemented through the `IEventBus` interface and combines two different event paradigms:
1. **Broadcast Events**: Global events sent to all subscribers of a certain type. 🌍
2. **Directed Events**: Events targeting a specific entity are sent only to components on that entity. 🎯

### 💾 Internal Storage

To achieve high performance, EventBus supports several specialized data structures:

*   **Broadcast Subscriptions (`_eventData`)**:
    * Dictionary `Type` -> `EventData`.
    * Stores a list of all global subscribers for each event type.
    * Used for broadcast events and system notifications.

* **Directed Subscriptions (`_eventSubs` and `_compEventSubs`)**:
    * Array structure of arrays (jagged array), indexed by `CompIdx` (Component Index).
    * `_eventSubs[CompIdx]`: Maps `EventType` -> `Handler`.
    * Allows you to search for component handlers for a specific event in O(1). ⚡
    * Divided into general events and optimized Component Events (see below).

*   **Entity Event Tables (`_entEventTables`)**:
    * Dictionary stored for each active `EntityUid`.
    * Displays `EventType` -> `LinkedList<CompIdx>`.
    * Keeps track of which components *on a specific entity* are listening to a specific event.
    * This prevents iterating through all entity components to find listeners. 🚫🔄

## 🎭 Event Paradigms

### 1. Broadcast Events (`RaiseEvent`) 📢
* **Target**: No specific entity. Global scope.
* **Uses**: Communication between systems, global game state changes (for example, `GameRunLevelChangedEvent`), round notifications.
* **Dispatching**:
    * Looks up `EventData` for the event type.
    * Iterates over all registered `IEntityEventSubscriber` (mostly EntitySystems).
    * Supports `ByRef` events for performance with zero allocations.

### 2. Directed Events (`RaiseLocalEvent`) 🎯
* **Target**: Specific `EntityUid`.
* **Usage**: Interactions, damage, UI clicks, anything that happens *to* the entity.
* **Dispatching**:
    1. Finds `EventTable` for the target entity.
    2. Finds a list of component indices (`CompIdx`) subscribed to this event type.
    3. Gets the actual component instances from `EntityManager`.
    4. Calls the cached handler delegate for each component.
* **Performance**: Highly optimized. Uses `ref` structures and `Unsafe.As` to avoid boxing `struct` events. 🚀

### 3. Component Events (`RaiseComponentEvent`) 🧩
* **Target**: A specific instance of `IComponent` on an entity.
* **Application**: Network processing, specialized component updates.
* **Optimization**:
    * Bypasses the search in the `EventTable` entity.
    * Directly calls a handler for a specific component instance.
    * Used internally for `HandleComponentState` and network replication to minimize overhead.

## 🔗 Mechanics of Subscriptions

### Registration 📝
Subscriptions are usually registered during `EntitySystem.Initialize()`:

1. **Directed**: `SubscribeLocalEvent<TComp, TEvent>(Handler)` -> Registers `Handler` in `_eventSubs` for `TComp`.
2. **Broadcast**: `SubscribeEvent<TEvent>(Handler)` -> Registers `Handler` to `_eventData`.

###Ordering 🔢
Events/Subscriptions support explicit ordering using the `before` and `after` types.
* EventBus topologically sorts handlers based on these constraints.
* This ensures a deterministic order of execution (e.g. the armor system handles damage *before* the health system).
* **⚠️ Warning**: Circular dependencies in order will throw an exception on initialization.

## 🛠️ Optimization Techniques

EventBus uses aggressive optimization to support thousands of events per tick:

* **Ref Events (`ByRefEventAttribute`)**: Events can be marked for passing by reference. This avoids copying large structures. 📦➡️
* **Unit Structs**: Internally, the bus uses the `ref Unit` and `Unsafe.As` pointers to erase event argument types without boxing them into `object`. 🧙‍♂️
* **Frozen Collections**: After initialization, subscription dictionaries are "frozen" (`FrozenDictionary`) to optimize read performance. ❄️
* **Struct Events**: Most events are structures (`struct`) rather than classes (`class`) to eliminate pressure on the garbage collector (GC). 🗑️

## ⛔ Critical Limitations

1. **Lock Subscriptions**: You cannot subscribe/unsubscribe while dispatching events (unless you use options with `Queue`). The bus is blocked during iteration. 🔒
2. **Overhead of Generics**: The bus minimizes the use of generics in hot paths to avoid JIT overhead for each event type.
3. **Thread Safety**: EventBus **is not** thread safe. All events must be raised on the main thread. 🧵
