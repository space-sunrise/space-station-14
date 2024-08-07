using Content.Server.GameTicking.Events;
using Content.Server.Paper;
using Content.Shared.Fax.Components;
using Content.Shared.Paper;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.StationGoal
{
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        }

        private void OnRoundStarting(RoundStartingEvent ev)
        {
            var playerCount = _playerManager.PlayerCount;

            var query = EntityQueryEnumerator<StationGoalComponent>();
            while (query.MoveNext(out var uid, out var station))
            {
                var tempGoals = new List<ProtoId<StationGoalPrototype>>(station.Goals);
                StationGoalPrototype? selGoal = null;
                while (tempGoals.Count > 0)
                {
                    var goalId = _random.Pick(tempGoals);
                    var goalProto = _prototypeManager.Index(goalId);

                    if (playerCount > goalProto.MaxPlayers ||
                        playerCount < goalProto.MinPlayers)
                    {
                        tempGoals.Remove(goalId);
                        continue;
                    }

                    selGoal = goalProto;
                    break;
                }

                if (selGoal is null)
                    return;

                if (SendStationGoal(uid, selGoal))
                {
                    Log.Info($"Goal {selGoal.ID} has been sent to station {MetaData(uid).EntityName}");
                }
            }
        }

        public bool SendStationGoal(EntityUid? ent, ProtoId<StationGoalPrototype> goal)
        {
            return SendStationGoal(ent, _prototypeManager.Index(goal));
        }

        public bool SendStationGoal(EntityUid? ent, StationGoalPrototype goal)
        {
            if (ent is null)
                return false;

            var wasSent = false;
            var fleshTilesQuery = EntityQueryEnumerator<FaxMachineComponent>();
            while (fleshTilesQuery.MoveNext(out var faxId, out var fax))
            {
                if (!fax.ReceiveStationGoal)
                    continue;

                var printout = new FaxPrintout(
                    Loc.GetString(goal.Text, ("station", MetaData(ent.Value).EntityName)),
                    Loc.GetString("station-goal-fax-paper-name"),
                    null,
                    null,
                    "paper_stamp-centcom",
                    new List<StampDisplayInfo>
                    {
                        new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") },
                    });

                var xform = Transform(faxId);
                var paper = SpawnPaperGoal(xform.Coordinates, printout);
                var lockbox = Spawn(goal.LockBoxPrototypeId, xform.Coordinates);
                if (_containerSystem.TryGetContainer(lockbox, "storagebase", out var container))
                {
                    _containerSystem.Insert(paper, container);
                    if (goal.ExtraItems.Count != 0)
                    {
                        foreach (var goalExtraItem in goal.ExtraItems)
                        {
                            var item = Spawn(goalExtraItem);
                            _containerSystem.Insert(item, container);
                        }
                    }
                }

                wasSent = true;
            }

            return wasSent;
        }

        private EntityUid SpawnPaperGoal(EntityCoordinates coords, FaxPrintout printout)
        {
            var entityToSpawn = printout.PrototypeId.Length == 0 ? "Paper" : printout.PrototypeId;
            var printed = EntityManager.SpawnEntity(entityToSpawn, coords);
            if (!TryComp<PaperComponent>(printed, out var paper))
                return printed;

            _paperSystem.SetContent((printed, paper), printout.Content);

            if (printout.StampState == null)
                return printed;

            foreach (var stamp in printout.StampedBy)
            {
                _paperSystem.TryStamp((printed, paper), stamp, printout.StampState);
            }

            return printed;
        }
    }
}
