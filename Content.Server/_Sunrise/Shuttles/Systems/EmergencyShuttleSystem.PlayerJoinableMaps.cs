using Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала.
namespace Content.Server.Shuttles.Systems;

public sealed partial class EmergencyShuttleSystem
{
    partial void ShouldSkipEmergencyShuttleStationPortal(EntityUid station, ref bool skip)
    {
        if (skip)
            return;

        skip = HasComp<PlayerJoinableMapComponent>(station);
    }
}
