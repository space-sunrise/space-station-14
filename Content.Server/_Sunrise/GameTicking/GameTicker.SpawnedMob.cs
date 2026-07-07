using Content.Server._Sunrise.Station;
using Content.Server._Sunrise.TraitorTarget;
using Content.Server.Speech.Components;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    partial void AfterPlayerMobSpawnedPortal(ICommonSession player, EntityUid station, EntityUid mob)
    {
        if (HasComp<StationAntagsTargetsComponent>(station))
            EnsureComp<AntagTargetComponent>(mob);

        if (player.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            AddComp<OwOAccentComponent>(mob);
    }
}
