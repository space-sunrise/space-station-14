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
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private static readonly ISawmill Sawmill = Logger.GetSawmill("biocode.deactivation");

    protected override void ShowAlert(EntityUid user, string alertText)
    {
        Sawmill.Info($"Showing alert '{alertText}' to user {user}");
        _popup.PopupEntity(Loc.GetString(alertText), user, user);
    }

    protected override void DeactivateItem(EntityUid uid)
    {
        Sawmill.Info($"Attempting to deactivate item {uid}");

        // Handle pinpointer deactivation using the proper system method
        if (TryComp<PinpointerComponent>(uid, out var pinpointer))
        {
            Sawmill.Info($"Pinpointer {uid} current state: IsActive={pinpointer.IsActive}");

            // Always ensure the pinpointer is deactivated
            _pinpointerSystem.SetActive(uid, false, pinpointer);

            // Force update appearance to ensure visual state is correct
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                Sawmill.Info($"Force updating appearance for pinpointer {uid}");
                _appearance.SetData(uid, PinpointerVisuals.IsActive, false, appearance);
                _appearance.SetData(uid, PinpointerVisuals.TargetDistance, Distance.Unknown, appearance);
            }
            else
            {
                Sawmill.Info($"No appearance component found for pinpointer {uid}");
            }
        }
        else
        {
            Sawmill.Info($"Item {uid} is not a pinpointer");
        }

        // Add other item types here as needed
        // Example: if (TryComp<SomeOtherComponent>(uid, out var otherComponent))
        // {
        //     _someOtherSystem.Deactivate(uid, otherComponent);
        // }
    }
}
