using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Damage.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Server._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Systems;
using Content.Shared.Alert;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Antags.Vampires.Systems;

public sealed partial class VampireSystem : SharedVampireSystem
{
    // Инициализация и обновление вампиров.

    [Dependency] private readonly ActionsSystem _actions = null!;
    [Dependency] private readonly AntagSelectionSystem _antag = null!;
    [Dependency] private readonly AlertsSystem _alerts = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly PopupSystem _popup = null!;
    [Dependency] private readonly StunSystem _stun = null!;
    [Dependency] private readonly StaminaSystem _stamina = null!;
    [Dependency] private readonly EntityLookupSystem _lookup = null!;
    [Dependency] private readonly ILogManager _log = null!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = null!;
    [Dependency] private readonly InteractionSystem _interaction = null!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = null!;
    [Dependency] private readonly DamageableSystem _damageable = null!;

    private ISawmill _sawmill = null!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("Vampire");

        InitializeConversion();
        InitializeLifecycle();
        InitializeActions();
        InitializeFeeding();
        InitializeSleep();
        InitializeGlare();
        InitializeRejuvenation();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<VampireComponent, VampireFeedingComponent>();
        while (query.MoveNext(out var uid, out var vampire, out var feeding))
        {
            if (now <= feeding.NextUpdate)
                continue;

            var elapsed = feeding.LastUpdate == TimeSpan.Zero
                ? (float)feeding.UpdateDelay.TotalSeconds
                : MathF.Max(0f, (float)(now - feeding.LastUpdate).TotalSeconds);

            feeding.LastUpdate = now;
            feeding.NextUpdate = now + feeding.UpdateDelay;

            var ent = (uid, vampire);
            ProcessBloodDecay(ent, feeding, elapsed);
            HandleHolyWater(ent);
            HandleHolyPlace(ent);
        }

        ProcessActiveRejuvenation(now);
    }
}
