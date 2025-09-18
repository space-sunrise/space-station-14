using System.Numerics;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._Sunrise.BloodCult.Actions;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

public sealed class CultBloodSpearSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultBloodSpearComponent, ThrowDoHitEvent>(OnDoHit);
        SubscribeLocalEvent<CultBloodSpearComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BloodSpearOwnerComponent, CultReturnBloodSpearActionEvent>(OnReturnSpear);
        SubscribeLocalEvent<BloodSpearOwnerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, BloodSpearOwnerComponent component, ComponentStartup args)
    {
        _actionsSystem.AddAction(uid, component.ReturnSpearActionId);
    }

    private void OnReturnSpear(EntityUid uid, BloodSpearOwnerComponent component, CultReturnBloodSpearActionEvent args)
    {
        if (component.Spear == null)
            return;

        if (!_entityManager.EntityExists(component.Spear))
            return;

        _handsSystem.TryDrop(component.Spear.Value);

        var direction = CalculateDirection(component.Spear.Value, uid);

        if (direction == null)
        {
            _popupSystem.PopupEntity($"Копье не найдено", uid, uid, PopupType.Large);
            return;
        }

        if (direction.Value.Length() > component.MaxReturnDistance)
        {
            _popupSystem.PopupEntity($"Слишком далеко", uid, uid, PopupType.Large);
            return;
        }

        _throwingSystem.TryThrow(component.Spear.Value, direction.Value * 1.2f, 10f);
        args.Handled = true;
    }

    private Vector2? CalculateDirection(EntityUid pinUid, EntityUid trgUid)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        // check if entities have transform component
        if (!xformQuery.TryGetComponent(pinUid, out var pin))
            return null;
        if (!xformQuery.TryGetComponent(trgUid, out var trg))
            return null;

        // check if they are on same map
        if (pin.MapID != trg.MapID)
            return null;

        // get world direction vector
        var dir = _transform.GetWorldPosition(trg, xformQuery) - _transform.GetWorldPosition(pin, xformQuery);
        return dir;
    }

    private void OnShutdown(EntityUid uid, CultBloodSpearComponent component, ComponentShutdown args)
    {
        if (!_entityManager.TryGetComponent<ActionsComponent>(component.SpearOwner, out var actionsComponent))
            return;
        if (!_entityManager.TryGetComponent<BloodSpearOwnerComponent>(component.SpearOwner,
                out var spearOwnerComponent))
            return;
        foreach (var userAction in actionsComponent.Actions)
        {
            var entityPrototypeId = MetaData(userAction).EntityPrototype?.ID;
            if (entityPrototypeId == spearOwnerComponent.ReturnSpearActionId)
                _actionsSystem.RemoveAction(component.SpearOwner.Value, userAction);
        }

        if (HasComp<BloodSpearOwnerComponent>(component.SpearOwner.Value))
            RemComp<BloodSpearOwnerComponent>(component.SpearOwner.Value);
    }

    private void OnDoHit(EntityUid uid, CultBloodSpearComponent component, ThrowDoHitEvent args)
    {
        if (HasComp<ConstructComponent>(args.Target))
            return;

        if (HasComp<BloodCultistComponent>(args.Target))
        {
            _handsSystem.TryPickup(args.Target, uid, checkActionBlocker: false);
        }
        else
        {
            if (HasComp<MobStateComponent>(args.Target))
            {
                _stunSystem.TryAddParalyzeDuration(args.Target, TimeSpan.FromSeconds(component.StuhTime));
                _damageableSystem.TryChangeDamage(args.Target, component.Damage, origin: uid);
                _audio.PlayPvs(component.BreakSound, uid);
                QueueDel(uid);
            }
        }
    }
}
