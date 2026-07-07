using Content.Server._Sunrise.NewLife;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly NewLifeSystem _newLife = default!;

    partial void BeforePlayerSpawnProfilePortal(ICommonSession player)
    {
        _newLife.AddUsedCharactersForRespawn(player.UserId, _prefsManager.GetPreferences(player.UserId).SelectedCharacterIndex);
        _newLife.SetNextAllowRespawn(player.UserId, _gameTiming.CurTime + TimeSpan.FromMinutes(_newLife.NewLifeTimeout));
    }
}
