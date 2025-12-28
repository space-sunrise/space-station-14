using Content.Shared._Sunrise.Smile;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared._Sunrise.Smile;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Content.Shared.Zombies;
using Content.Shared.Damage.Systems;

namespace Content.Server._Sunrise.Smile;

public sealed class SmileSlimeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    private TimeSpan _nextTick = TimeSpan.Zero;
    private readonly TimeSpan _refreshCooldown = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        SubscribeLocalEvent<SmileSlimeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SmileSlimeComponent, SmileLoveActionEvent>(OnLoveAction);
        SubscribeLocalEvent<SmileSlimeComponent, ComponentShutdown>(OnCompShutdown);
        SubscribeLocalEvent<SmileSlimeComponent, SmileLoveDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SmileSlimeComponent, EntityZombifiedEvent>(OnZombified);
    }

    private void OnMapInit(EntityUid uid, SmileSlimeComponent comp, MapInitEvent args)
    {
        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
    }

    private void OnCompShutdown(EntityUid uid, SmileSlimeComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnZombified(EntityUid uid, SmileSlimeComponent comp, EntityZombifiedEvent args)
    {
        _actions.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnLoveAction(EntityUid smileUid, SmileSlimeComponent comp, SmileLoveActionEvent args)
    {
        var doAfter = new DoAfterArgs(EntityManager,
            smileUid,
            comp.ActionTime,
            new SmileLoveDoAfterEvent(),
            smileUid,
            args.Target)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        if (!TryComp<TransformComponent>(args.Target, out var targetXform))
            return;

        _audio.PlayPvs(comp.SoundSpecifier, targetXform.Coordinates);
        _entMan.SpawnEntity("EffectHearts", targetXform.Coordinates);
        _popupSystem.PopupEntity(
            Loc.GetString(comp.AffectionPopupText,
                ("slime", MetaData(smileUid).EntityName),
                ("target", MetaData(args.Target).EntityName)),
            args.Target,
            PopupType.Medium);
        _doAfterSystem.TryStartDoAfter(doAfter);

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, SmileSlimeComponent comp, SmileLoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target == null)
            return;

        if (!TryComp<TransformComponent>(args.Target.Value, out var targetXform))
            return;

        _entMan.SpawnEntity("EffectHearts", targetXform.Coordinates);
        _audio.PlayPvs(comp.SoundSpecifier, targetXform.Coordinates);
        _damageableSystem.TryChangeDamage(args.Target.Value, comp.DamageSpecifier, true, false);
        args.Handled = true;
    }

    public override void Update(float delay)
    {
        if (_nextTick > _gameTiming.CurTime)
            return;

        _nextTick += _refreshCooldown;

        var query = AllEntityQuery<SmileSlimeComponent>();
        while (query.MoveNext(out var slimeUid, out var smileSlimeComponent))
        {
            if (HasComp<ZombieComponent>(slimeUid))
                continue;
            foreach (var entity in _entityLookup.GetEntitiesInRange(slimeUid, 2f))
            {
                if (HasComp<SmileSlimeComponent>(entity))
                    continue;
                if (!TryComp<BodyComponent>(entity, out _))
                    continue;
                _popupSystem.PopupEntity(Loc.GetString(_random.Pick(smileSlimeComponent.Messages)), entity, entity);
            }
        }
    }
}
