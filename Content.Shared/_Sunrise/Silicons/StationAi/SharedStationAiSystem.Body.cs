using Content.Shared._Sunrise.Silicons.StationAi;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Silicons.StationAi;
#pragma warning restore IDE0130

public abstract partial class SharedStationAiSystem
{
    /// <summary>
    /// Resolves a station AI actor, either the brain itself or a controlled body, back to its held brain and core.
    /// </summary>
    public bool TryGetCoreForAiActor(
        EntityUid actor,
        out Entity<StationAiCoreComponent?> core,
        out EntityUid stationAi)
    {
        stationAi = actor;

        if (HasComp<StationAiHeldComponent>(actor))
            return TryGetCore(actor, out core);

        if (TryComp<StationAiBodyComponent>(actor, out var body) &&
            body.LinkedAi is { } linkedAi &&
            TryGetCore(linkedAi, out core))
        {
            stationAi = linkedAi;
            return true;
        }

        core = (EntityUid.Invalid, null);
        stationAi = EntityUid.Invalid;
        return false;
    }

    /// <summary>
    /// Returns the controlled AI body when the station AI brain is currently piloting one.
    /// </summary>
    public EntityUid GetActiveAiActor(EntityUid stationAi)
    {
        return TryGetActiveAiActor(stationAi, out var activeActor)
            ? activeActor
            : stationAi;
    }

    /// <summary>
    /// Attempts to resolve a station AI brain to the entity currently receiving player-facing events.
    /// </summary>
    public bool TryGetActiveAiActor(EntityUid stationAi, out EntityUid activeActor)
    {
        activeActor = stationAi;

        if (TryComp<StationAiBodyControllerComponent>(stationAi, out var controller)
            && controller.CurrentBody is { } currentBody
            && TryComp<StationAiBodyComponent>(currentBody, out var body)
            && body.LinkedAi == stationAi)
        {
            activeActor = currentBody;
        }

        return Exists(activeActor);
    }

    /// <summary>
    /// Returns whether an entity is an active station AI actor that may use AI-only interactions.
    /// </summary>
    private bool ValidateAiActor(EntityUid actor)
    {
        return TryGetCoreForAiActor(actor, out _, out _) &&
               _blocker.CanComplexInteract(actor);
    }

    /// <summary>
    /// Keeps legacy station AI body appearance customization from overriding borg body visuals.
    /// </summary>
    private void UpdateStationAiBodyAppearance(Entity<StationAiCustomizationComponent> stationAi)
    {
        if (!TryComp<StationAiBodyControllerComponent>(stationAi.Owner, out var controller)
            || controller.CurrentBody is not { } currentBody
            || !TryComp<StationAiBodyComponent>(currentBody, out var body)
            || body.LinkedAi != stationAi.Owner)
        {
            return;
        }

        _appearance.RemoveData(currentBody, StationAiBodyVisuals.BodyAppearance);
    }
}
