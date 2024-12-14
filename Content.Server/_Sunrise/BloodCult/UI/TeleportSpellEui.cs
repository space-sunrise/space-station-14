using Content.Server._Sunrise.BloodCult.Runes.Comps;
using Content.Server.EUI;
using Content.Server.Popups;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.UI;
using Content.Shared.Eui;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.BloodCult.UI;

public sealed class TeleportSpellEui : BaseEui
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private SharedTransformSystem _transformSystem;
    private PopupSystem _popupSystem;


    private EntityUid _performer;
    private EntityUid _target;

    private bool _used;


    public TeleportSpellEui(EntityUid performer, EntityUid target)
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _popupSystem = _entityManager.System<PopupSystem>();

        _performer = performer;
        _target = target;

        Timer.Spawn(TimeSpan.FromSeconds(10), Close );
    }

    public override EuiStateBase GetNewState()
    {
        var runes = _entityManager.EntityQuery<CultRuneTeleportComponent>();
        var state = new TeleportSpellEuiState();

        foreach (var rune in runes)
        {
            state.Runes.Add((int)rune.Owner, rune.Label!);
        }

        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if(_used) return;

        if (msg is not TeleportSpellTargetRuneSelected cast)
        {
            return;
        }

        if (!_entityManager.TryGetComponent<BloodCultistComponent>(_performer, out var cultistComponent))
        {
            return;
        }

        var performerPosition = _entityManager.GetComponent<TransformComponent>(_performer).Coordinates;
        var targetPosition = _entityManager.GetComponent<TransformComponent>(_target).Coordinates;;

        performerPosition.TryDistance(_entityManager, targetPosition, out var distance);

        if(distance > 1.5f)
        {
            _popupSystem.PopupEntity("Too far", _performer, PopupType.Medium);
            return;
        }

        TransformComponent? runeTransform = null!;

        foreach (var runeComponent in _entityManager.EntityQuery<CultRuneTeleportComponent>())
        {
            if (runeComponent.Owner == new EntityUid(cast.RuneUid))
            {
                runeTransform = _entityManager.GetComponent<TransformComponent>(runeComponent.Owner);
            }
        }

        if (runeTransform is null)
        {
            _popupSystem.PopupEntity("Rune is gone", _performer);
            DoStateUpdate();
            return;
        }

        _used = true;

        _transformSystem.SetCoordinates(_target, runeTransform.Coordinates);
        var ev = new TeleportSpellUserEvent();
        _entityManager.EventBus.RaiseLocalEvent(_performer, ev);
        Close();
    }
}
