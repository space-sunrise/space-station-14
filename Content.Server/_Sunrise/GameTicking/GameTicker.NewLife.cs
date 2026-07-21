using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace не соответствует папке из-за partial-портала.
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    /// <summary>
    /// Commits the selected character and cooldown after a player has successfully joined the game.
    /// </summary>
    private void CommitSuccessfulNewLifeJoin(ICommonSession player)
    {
        var selectedCharacter = _prefsManager.GetPreferences(player.UserId).SelectedCharacterIndex;
        _newLife.AddUsedCharactersForRespawn(player.UserId, selectedCharacter);
        _newLife.SetNextAllowRespawn(
            player.UserId,
            _gameTiming.CurTime + TimeSpan.FromMinutes(_newLife.NewLifeTimeout));
    }
}
