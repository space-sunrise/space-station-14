using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared._Sunrise.PlanetPrison;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.PlanetPrison.Voting;

/// <summary>
/// Система для управления голосованиями за удаление карт тюрьмы (Planet Prison).
/// Вынесена из VoteManager для улучшения модульности.
/// </summary>
public sealed class PlanetPrisonVoteSystem : EntitySystem
{
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    /// <summary>
    /// Создает голосование за завершение текущей тюремной карты для конкретного MapId.
    /// </summary>
    public void CreatePrisonEndVote(ICommonSession? initiator)
    {
        if (initiator?.AttachedEntity is not { Valid: true } ent)
            return;

        if (!_entityManager.TryGetComponent(ent, out PlanetPrisonSpawnedComponent? prison))
            return;

        var mapId = prison.MapId;

        var eligibleCount = _playerManager.Sessions.Count(session => CheckPrisonVoterEligibility(session, mapId));
        _logManager.GetSawmill("vote.prison").Info($"[PRISON-VOTE] CreatePrisonEndVote: initiator={initiator.Name}, mapId={mapId}, eligibleVoters={eligibleCount}, totalSessions={_playerManager.Sessions.Length}");

        var options = new VoteOptions
        {
            Title = Loc.GetString("ui-vote-prison-end-title"),
            Options =
            {
                (Loc.GetString("ui-vote-prison-end-finish"), "finish"),
                (Loc.GetString("ui-vote-prison-end-continue"), "continue"),
                (Loc.GetString("ui-vote-restart-abstain"), "abstain"),
            },
            Duration = TimeSpan.FromSeconds(10),
            VoterEligibility = VoteManager.VoterEligibility.PlanetPrisonSpawned,
            RequiredMapId = mapId,
            DisplayVotes = true,
            DisplayVotesAdmins = true,
        };

        WirePresetVoteInitiator(options, initiator);

        var vote = _voteManager.CreateVote(options);

        foreach (var player in _playerManager.Sessions)
        {
            if (!CheckPrisonVoterEligibility(player, mapId))
                continue;

            vote.CastVote(player, 2); // abstain
        }

        vote.OnFinished += (_, _) =>
        {
            var finish = vote.VotesPerOption["finish"];
            var cont = vote.VotesPerOption["continue"];
            var total = finish + cont;

            if (total == 0 || finish <= cont)
                return;

            var mindsToGhost = new List<Robust.Shared.GameObjects.EntityUid>();
            var mindSystem = _entityManager.System<MindSystem>();

            foreach (var session in _playerManager.Sessions)
            {
                if (session.AttachedEntity is not { Valid: true } attached)
                    continue;

                if (!_entityManager.TryGetComponent(attached, out TransformComponent? t))
                    continue;

                if (t.MapID != mapId)
                    continue;

                if (_entityManager.HasComponent<GhostComponent>(attached))
                    continue;

                if (!mindSystem.TryGetMind(session, out var mindId, out _))
                    continue;

                mindsToGhost.Add(mindId);
            }

            if (_mapManager.MapExists(mapId))
                _mapManager.DeleteMap(mapId);

            var query = _entityManager.EntityQueryEnumerator<PlanetPrisonSpawnedComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var spawned, out var t))
            {
                if (spawned.MapId != mapId && t.MapID != mapId)
                    continue;

                _entityManager.RemoveComponent<PlanetPrisonSpawnedComponent>(uid);
            }

            var ghostSystem = _entityManager.System<GhostSystem>();
            foreach (var mindId in mindsToGhost)
            {
                ghostSystem.OnGhostAttempt(mindId, canReturnGlobal: false, viaCommand: true, forced: true);
            }

            _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-prison-end-success"));
        };
    }

    /// <summary>
    /// Проверяет, может ли игрок голосовать за удаление конкретной тюремной карты.
    /// </summary>
    public bool CheckPrisonVoterEligibility(ICommonSession player, MapId requiredMapId)
    {
        if (player.AttachedEntity is not { Valid: true } attached)
        {
            _logManager.GetSawmill("vote.prison").Info($"[PRISON-VOTE] CheckPrisonVoterEligibility: player={player.Name}, result=false, reason=no attached entity");
            return false;
        }

        if (!_entityManager.TryGetComponent(attached, out PlanetPrisonSpawnedComponent? spawned))
        {
            _logManager.GetSawmill("vote.prison").Info($"[PRISON-VOTE] CheckPrisonVoterEligibility: player={player.Name}, result=false, reason=no PlanetPrisonSpawnedComponent, requiredMapId={requiredMapId}");
            return false;
        }

        var result = spawned.MapId == requiredMapId;
        _logManager.GetSawmill("vote.prison").Info($"[PRISON-VOTE] CheckPrisonVoterEligibility: player={player.Name}, result={result}, spawnedMapId={spawned.MapId}, requiredMapId={requiredMapId}");
        return result;
    }

    /// <summary>
    /// Создаёт голосование за исключение конкретного игрока с тюремной карты.
    /// args[0] — строковое представление NetUserId цели.
    /// </summary>
    public void CreatePrisonExcludeVote(ICommonSession? initiator, string[]? args)
    {
        if (initiator?.AttachedEntity is not { Valid: true } ent)
            return;

        if (!_entityManager.TryGetComponent(ent, out PlanetPrisonSpawnedComponent? prison))
            return;

        if (args == null || args.Length < 1)
            return;

        if (!Guid.TryParse(args[0], out var guid))
            return;

        var targetUserId = new NetUserId(guid);

        if (!_playerManager.TryGetSessionById(targetUserId, out var targetSession))
            return;

        var mapId = prison.MapId;

        // Определяем proto id карты для последующей пометки «исключён»
        var prisonSystem = _entityManager.System<PlanetPrisonStationSystem>();
        string? mapProtoId = null;
        foreach (var (proto, mId) in prisonSystem.GetLaunchedMaps())
        {
            if (mId == mapId)
            {
                mapProtoId = proto;
                break;
            }
        }

        var targetName = targetSession.AttachedEntity is { Valid: true } tEnt
            ? Comp<MetaDataComponent>(tEnt).EntityName
            : targetSession.Name;

        var eligibleCount = _playerManager.Sessions.Count(s => CheckPrisonVoterEligibility(s, mapId));
        _logManager.GetSawmill("vote.prison").Info(
            $"[PRISON-VOTE] CreatePrisonExcludeVote: initiator={initiator.Name}, target={targetSession.Name}, mapId={mapId}, eligibleVoters={eligibleCount}");

        var reason = args.Length >= 2 ? args[1].Trim() : "";
        if (reason.Length > 200)
            reason = reason[..200];

        var baseTitle = Loc.GetString("ui-vote-prison-exclude-title", ("target", targetName ?? ""));
        var title = string.IsNullOrWhiteSpace(reason) ? baseTitle : $"{baseTitle} — {reason}";

        var options = new VoteOptions
        {
            Title = title,
            Options =
            {
                (Loc.GetString("ui-vote-prison-exclude-yes"), "exclude"),
                (Loc.GetString("ui-vote-prison-exclude-no"), "keep"),
                (Loc.GetString("ui-vote-restart-abstain"), "abstain"),
            },
            Duration = TimeSpan.FromSeconds(5),
            VoterEligibility = VoteManager.VoterEligibility.PlanetPrisonSpawned,
            RequiredMapId = mapId,
            DisplayVotes = true,
            DisplayVotesAdmins = true,
        };

        WirePresetVoteInitiator(options, initiator);

        var vote = _voteManager.CreateVote(options);

        foreach (var player in _playerManager.Sessions)
        {
            if (!CheckPrisonVoterEligibility(player, mapId))
                continue;

            vote.CastVote(player, 2); // abstain
        }

        vote.OnFinished += (_, _) =>
        {
            var exclude = vote.VotesPerOption["exclude"];
            var keep = vote.VotesPerOption["keep"];
            var total = exclude + keep;

            if (total == 0 || exclude <= keep)
                return;

            // Делаем цель призраком
            if (targetSession.AttachedEntity is { Valid: true } targetEnt)
            {
                var mindSystem = _entityManager.System<MindSystem>();
                if (!_entityManager.HasComponent<GhostComponent>(targetEnt) &&
                    mindSystem.TryGetMind(targetSession, out var mindId, out _))
                {
                    var ghostSystem = _entityManager.System<GhostSystem>();
                    ghostSystem.OnGhostAttempt(mindId, canReturnGlobal: false, viaCommand: true, forced: true);
                }
            }

            // Помечаем игрока как исключённого с этой карты
            if (mapProtoId != null)
                prisonSystem.ExcludePlayerFromMap(targetUserId, mapProtoId);

            _chatManager.DispatchServerAnnouncement(
                Loc.GetString("ui-vote-prison-exclude-success", ("target", targetName ?? "")));
        };
    }

    private void WirePresetVoteInitiator(VoteOptions options, ICommonSession? player)
    {
        if (player != null)
        {
            options.SetInitiator(player);
        }
        else
        {
            options.InitiatorText = Loc.GetString("ui-vote-initiator-server");
        }
    }
}
