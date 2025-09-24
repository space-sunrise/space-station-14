using Content.Shared._Sunrise.Biocode.Components;
using Content.Shared._Sunrise.Biocode.Systems;
using Content.Shared.Pinpointer;
using Content.Server.Popups;
using Content.Server.Pinpointer;

namespace Content.Server._Sunrise.Biocode.Systems;

/// <summary>
/// Server-side implementation of biocode deactivation system.
/// </summary>
public sealed class ServerBiocodeDeactivationSystem : BiocodeDeactivationSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PinpointerSystem _pinpointerSystem = default!;

    protected override void ShowAlert(EntityUid user, string alertText)
    {
        _popup.PopupEntity(Loc.GetString(alertText), user, user);
    }

    protected override void DeactivateItem(EntityUid uid)
    {
        // Handle pinpointer deactivation using the proper system method
        if (TryComp<PinpointerComponent>(uid, out var pinpointer))
        {
            if (pinpointer.IsActive)
            {
                _pinpointerSystem.SetActive(uid, false, pinpointer);
            }
        }

        // Add other item types here as needed
        // Example: if (TryComp<SomeOtherComponent>(uid, out var otherComponent))
        // {
        //     _someOtherSystem.Deactivate(uid, otherComponent);
        // }
    }
}
