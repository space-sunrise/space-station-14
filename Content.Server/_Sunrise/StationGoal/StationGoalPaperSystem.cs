using System.Linq;
using Content.Server.Paper;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Robust.Server.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.StationGoal
{
    /// <summary>
    ///     System to spawn paper with station goal.
    /// </summary>
    public sealed class StationGoalPaperSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            SendRandomGoal();
        }

        public bool SendRandomGoal()
        {
            var availableGoals = _prototypeManager.EnumeratePrototypes<StationGoalPrototype>().ToList();
            var goal = _random.Pick(availableGoals);
            return SendStationGoal(goal);
        }

        /// <summary>
        ///     Send a station goal to all faxes which are authorized to receive it.
        /// </summary>
        /// <returns>True if at least one fax received paper</returns>
        public bool SendStationGoal(StationGoalPrototype goal)
        {
            var wasSent = false;
            var fleshTilesQuery = EntityQueryEnumerator<FaxMachineComponent>();
            while (fleshTilesQuery.MoveNext(out var faxId, out var fax))
            {
                if (!fax.ReceiveStationGoal)
                    continue;

                var printout = new FaxPrintout(
                    Loc.GetString(goal.Text),
                    Loc.GetString("station-goal-fax-paper-name"),
                    null,
                    null,
                    "paper_stamp-centcom",
                    new List<StampDisplayInfo>
                    {
                        new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") },
                    });
                // Sunrise-start
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
                // Sunrise-end

                wasSent = true;
            }

            return wasSent;
        }

        // Sunrise-start
        private EntityUid SpawnPaperGoal(EntityCoordinates coords, FaxPrintout printout)
        {
            var entityToSpawn = printout.PrototypeId.Length == 0 ? "Paper" : printout.PrototypeId;
            var printed = EntityManager.SpawnEntity(entityToSpawn, coords);
            if (!TryComp<PaperComponent>(printed, out var paper))
                return printed;

            _paperSystem.SetContent(printed, printout.Content);

            // Apply stamps
            if (printout.StampState == null)
                return printed;

            foreach (var stamp in printout.StampedBy)
            {
                _paperSystem.TryStamp(printed, stamp, printout.StampState);
            }

            return printed;
        }
        // Sunrise-end
    }
}
