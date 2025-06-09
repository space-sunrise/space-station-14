using System.Diagnostics.CodeAnalysis;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems
{
    /// <summary>
    ///     Handles NPCs running every tick.
    /// </summary>
    public sealed partial class NPCSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly HTNSystem _htn = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private readonly HashSet<EntityUid> _activeNPCs = new();
        private readonly HashSet<EntityUid> _sleepingNPCs = new();
        private readonly Dictionary<EntityUid, TimeSpan> _lastUpdateTime = new();
        private const float UpdateInterval = 0.1f;

        /// <summary>
        /// Whether any NPCs are allowed to run at all.
        /// </summary>
        public bool Enabled { get; set; } = true;

        private int _maxUpdates;

        private int _count;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            Subs.CVar(_configurationManager, CCVars.NPCEnabled, value => Enabled = value, true);
            Subs.CVar(_configurationManager, CCVars.NPCMaxUpdates, obj => _maxUpdates = obj, true);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            _activeNPCs.Clear();
            _sleepingNPCs.Clear();
            _lastUpdateTime.Clear();
        }

        public void OnPlayerNPCAttach(EntityUid uid, HTNComponent component, PlayerAttachedEvent args)
        {
            SleepNPC(uid, component);
        }

        public void OnPlayerNPCDetach(EntityUid uid, HTNComponent component, PlayerDetachedEvent args)
        {
            if (_mobState.IsIncapacitated(uid) || TerminatingOrDeleted(uid))
                return;

            // This NPC has an attached mind, so it should not wake up.
            if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
                return;

            WakeNPC(uid, component);
        }

        public void OnNPCMapInit(EntityUid uid, HTNComponent component, MapInitEvent args)
        {
            component.Blackboard.SetValue(NPCBlackboard.Owner, uid);
            WakeNPC(uid, component);
        }

        public void OnNPCShutdown(EntityUid uid, HTNComponent component, ComponentShutdown args)
        {
            SleepNPC(uid, component);
        }

        /// <summary>
        /// Is the NPC awake and updating?
        /// </summary>
        public bool IsAwake(EntityUid uid, HTNComponent component, ActiveNPCComponent? active = null)
        {
            return Resolve(uid, ref active, false);
        }

        public bool TryGetNpc(EntityUid uid, [NotNullWhen(true)] out NPCComponent? component)
        {
            // If you add your own NPC components then add them here.

            if (TryComp<HTNComponent>(uid, out var htn))
            {
                component = htn;
                return true;
            }

            component = null;
            return false;
        }

        /// <summary>
        /// Allows the NPC to actively be updated.
        /// </summary>
        public void WakeNPC(EntityUid uid, HTNComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }

            Log.Debug($"Waking {ToPrettyString(uid)}");
            EnsureComp<ActiveNPCComponent>(uid);
            _activeNPCs.Add(uid);
            _sleepingNPCs.Remove(uid);
        }

        public void SleepNPC(EntityUid uid, HTNComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
            {
                return;
            }

            // Don't bother with an event
            if (TryComp<HTNComponent>(uid, out var htn))
            {
                if (htn.Plan != null)
                {
                    var currentOperator = htn.Plan.CurrentOperator;
                    _htn.ShutdownTask(currentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
                    _htn.ShutdownPlan(htn);
                    htn.Plan = null;
                }
            }

            Log.Debug($"Sleeping {ToPrettyString(uid)}");
            RemComp<ActiveNPCComponent>(uid);
            _activeNPCs.Remove(uid);
            _sleepingNPCs.Add(uid);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!Enabled)
                return;

            var curTime = _timing.CurTime;
            var updateCount = 0;
            var activeNPCs = new List<(EntityUid, HTNComponent)>();

            foreach (var uid in _activeNPCs)
            {
                if (Deleted(uid) || !TryComp<HTNComponent>(uid, out var htn))
                {
                    _activeNPCs.Remove(uid);
                    continue;
                }

                if (!_lastUpdateTime.TryGetValue(uid, out var lastUpdate) ||
                    (curTime - lastUpdate).TotalSeconds >= UpdateInterval)
                {
                    activeNPCs.Add((uid, htn));
                    _lastUpdateTime[uid] = curTime;
                }
            }

            foreach (var (uid, htn) in activeNPCs)
            {
                if (updateCount >= _maxUpdates)
                    break;

                _htn.UpdateNPC(uid, htn, ref updateCount, _maxUpdates, frameTime);
            }
        }

        public void OnMobStateChange(EntityUid uid, HTNComponent component, MobStateChangedEvent args)
        {
            if (HasComp<ActorComponent>(uid))
                return;

            switch (args.NewMobState)
            {
                case MobState.Alive:
                    WakeNPC(uid, component);
                    break;
                case MobState.Critical:
                case MobState.Dead:
                    SleepNPC(uid, component);
                    break;
            }
        }
    }
}
