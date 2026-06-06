---
trigger: always_on
---

# Rule: Architectural pattern OnEvent -> TryDo -> CanDo -> Do

This rule describes the mandatory architectural pattern for implementing actions and interactions in the Space Station 14 codebase. Following this pattern ensures predictability, reusability and cleanliness of the code.

Structure: `OnEvent()` -> `TryDoSomething()` -> (check) `if (!CanDoSomething()) return` -> `DoSomething()`

## 📝 General scheme

Logic is broken down into three levels of responsibility:

1. **Event Handler (`OnEvent`)**: Entry point. Receives the event, unpacks the data and calls the `Try` method.
2. **Public API (`TryDo`)**: “Public interface” of the action. Orchestrates verification (`CanDo`) and execution. Returns success/failure.
3. **Check (`CanDo`)**: Pure check of conditions. Determines whether an action *can* be performed, but *does not* perform it.

---

## 🔍 Pattern components

### 1. Event handler (`OnEvent`)
Method subscribed to the event (`SubscribeLocalEvent`).
* **Task**: Redirect execution flow to public API.
* **Logic**: Minimal. Only checking the validity of the event (for example, `args.Handled`) and calling `Try...`.
* **Name**: `On[EventName]`, `On[Action]`.

### 2. Action attempt (`TryDoSomething`)
A public method that can be called from other systems (API).
* **Signature**: `public bool TryAction(Entity<Component?> ent, ...)`
*   **Task**:
    1. Call `CanDoSomething`. If you returned `false`, return `false`.
    2. If the checks are passed, perform an action (change a component, trigger an event, play a sound, etc.).
    3. Return `true` on success.
* **Important**: If the action requires specific arguments (for example, `user`), they must be passed here.

### 3. Feasibility check (`CanDoSomething`)
A method containing execution conditions.
* **Signature**: `public bool CanAction(Entity<Component?> ent, ..., bool quiet = false)`
* **Task**: Check all conditions (distance, tool availability, component status).
*   **Side Effects**:
    * ❌ **It is PROHIBITED** to change the state of entities (components).
    * ✅ **ALLOWED** to send messages to the player (Popups) if the argument `quiet` is equal to `false`.

---

## ✅ Example (System of interaction with objects)

Pay attention to a clear division of responsibilities. This example shows how the Wielding processing system implements the pattern.

```csharp
// 1. Event Handler
// Receives the event of using an item in hand.
// If the event has already been processed, exit.
// Otherwise, calls the public method of attempting the action.
private void OnUseInHand(EntityUid uid, WieldableComponent component, UseInHandEvent args)
{
    if (args.Handled)
        return;

    // Call public API
    // The handler doesn't know the implementation details, it just "asks" to try to pick it up.
    if (TryWield(uid, component, args.User))
        args.Handled = true;
}

// 2. Public API (TryDo)
// A public method that can be called by other systems (for example, magic or the admin panel).
public bool TryWield(EntityUid used, WieldableComponent component, EntityUid user)
{
    // Step 1: Check (Early Return)
    // Strictly through calling the Can method.
    if (!CanWield(used, component, user))
        return false;

    // Step 2: Execution (Do)
    // Here we are already sure that the action is valid.

    // State change logic (component, visualization)
    SetWielded((used, component), true);

    // Visual and sound effects
    if (component.WieldSound != null)
        _audio.PlayPredicted(component.WieldSound, used, user);

    // Events (for reactions of other systems)
    var ev = new ItemWieldedEvent(user);
    RaiseLocalEvent(used, ref ev);

    // Popup about success for the player
    var message = Loc.GetString("wieldable-component-successful-wield", ("item", used));
    _popup.PopupPredicted(message, user, user);

    return true;
}

// 3. Check (CanDo)
// Pure verification function. Does not change the game state (except for sending error messages).
public bool CanWield(EntityUid uid, WieldableComponent component, EntityUid user, bool quiet = false)
{
    // Check 1: Are there any hands?
    // Uses TryComp to securely obtain dependencies.
    if (!TryComp<HandsComponent>(user, out var hands))
    {
        if (!quiet) // Popup only if not quiet
            _popup.PopupClient(Loc.GetString("wieldable-component-no-hands"), user, user);
        return false;
    }

    // Check 2: Is the item in your hands?
    if (!_hands.IsHolding((user, hands), uid, out _))
    {
        if (!quiet)
            _popup.PopupClient(Loc.GetString("wieldable-component-not-in-hands", ("item", uid)), user, user);
        return false;
    }

    // Check 3: Are there enough free hands?
    // Slot counting logic.
    if (_hands.CountFreeableHands((user, hands), except: uid) < component.FreeHandsRequired)
    {
        if (!quiet)
            _popup.PopupClient(Loc.GetString("wieldable-component-not-enough-free-hands"), user, user);
        return false;
    }

    // All checks passed
    return true;
}
```

---

## ❌ Anti-patterns (What to avoid)

### "Fat" Event Handler
All logic is inside `OnEvent`.
* **Problem**: The logic cannot be reused (for example, called from a console command or another `InteractionVerb` event).
*   **Badly**:
    ```csharp
    private void OnUse(EntityUid uid, Comp comp, UseEvent args) {
        if (!Condition) return; // Validation mixed with logic
        PerformAction();        // Direct Execution
    }
    ```

### Side-effects in `CanDo`
The `Can` method modifies the component data.
* **Problem**: Calling the check "just to find out" breaks the game state.
*   **Badly**:
    ```csharp
    public bool CanShoot(GunComponent gun) {
        gun.Ammo--; // ❌ NEVER do this when checking!
        return gun.Ammo >= 0;
    }
    ```

### "Blind" `TryDo`
The `Try` method does not call `Can`, but relies on the caller to have already checked everything.
* **Problem**: Encapsulation violation. The API becomes insecure. `Try` must always ensure that conditions are checked.

### Return a string instead of a bool in `CanDo`
Return an error code or string instead of `bool`.
* **Tip**: Use `out string? reason` if you need to return the failure reason, but the method itself should return `bool` for ease of use in `if`.
    ```csharp
    public bool CanDoWield(..., [NotNullWhen(false)] out string? reason)
    ```

---

## 🎯 Advantages of the scheme

1. **API for other systems**: `TryWield` can be called from anywhere (from verbs, from magic, from the admin panel), and it will work correctly with all checks.
2. **Prediction**: Splitting allows the client to easily predict the result of `CanWield` for the UI (for example, disable a button) without calling the action itself.
3. **Readability**: `OnEvent` becomes a trivial router, and the business logic is clearly structured.
