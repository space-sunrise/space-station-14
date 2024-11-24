using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Explosion;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Shared.Localization;
using System.Threading.Tasks;

namespace Content.Server._Sunrise.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class BSAHitCommand : IConsoleCommand
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    private const string ExplosionPrototypeId = "MicroBomb";
    private const string SoundPath = "/Audio/_Sunrise/artillery.ogg";
    private const int ExplosionDelay = 500;

    public string Command => "bsahit";
    public string Description => Loc.GetString("bsa-hit-command-description");
    public string Help => Loc.GetString("bsa-hit-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!ValidatePlayer(shell, out var player))
            return;

        var coords = GetPlayerCoordinates(shell, player!);
        if (coords == null)
            return;

        PlayGlobalSound();
        SendGlobalAnnouncement();
        
        _ = DoHitAsync(coords.Value, player!);
    }

    private async Task DoHitAsync(MapCoordinates coords, ICommonSession player)
    {
        await Task.Delay(4000);
        QueueExplosion(coords);
        LogAction(player, coords);
    }

    private bool ValidatePlayer(IConsoleShell shell, out ICommonSession? player)
    {
        player = shell.Player;
        if (player?.AttachedEntity == null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return false;
        }
        return true;
    }

    private MapCoordinates? GetPlayerCoordinates(IConsoleShell shell, ICommonSession player)
    {
        if (!_entityManager.TryGetComponent(player.AttachedEntity, out TransformComponent? xform))
        {
            shell.WriteError(Loc.GetString("bsa-hit-coords-error"));
            return null;
        }
        return xform.MapPosition;
    }

    private void PlayGlobalSound()
    {
        var filter = Filter.Broadcast();
        var audioSystem = _entitySystemManager.GetEntitySystem<ServerGlobalSoundSystem>();
        audioSystem.PlayAdminGlobal(filter, SoundPath);
    }

    private void SendGlobalAnnouncement()
    {
        var chatSystem = _entitySystemManager.GetEntitySystem<ChatSystem>();
        chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("bsa-hit-announcement"),
            colorOverride: Color.Red);
    }

    private void QueueExplosion(MapCoordinates coords)
    {
        Timer.Spawn(ExplosionDelay, () =>
        {
            var explosionSystem = _entitySystemManager.GetEntitySystem<ExplosionSystem>();
            var entity = _entityManager.SpawnEntity(null, coords);

            explosionSystem.QueueExplosion(
                entity,
                ExplosionPrototypeId,
                20000,
                5,
                50);
        });
    }

    private void LogAction(ICommonSession player, MapCoordinates coords)
    {
        _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{player.Name} triggered BSA hit at {coords}");
    }
}