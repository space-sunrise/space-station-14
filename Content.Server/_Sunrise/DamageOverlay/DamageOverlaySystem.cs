using System.Linq;
using System.Numerics;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._Sunrise.DamageOverlay;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.DamageOverlay;

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly List<ICommonSession> _disabledSessions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOverlayComponent, DamageChangedEvent>(OnDamageChange);
        SubscribeNetworkEvent<DamageOverlayOptionEvent>(OnDamageOverlayOption);
    }

    private async void OnDamageOverlayOption(DamageOverlayOptionEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Enabled)
            _disabledSessions.Remove(args.SenderSession);
        else
            _disabledSessions.Add(args.SenderSession);
    }

    private void OnDamageChange(EntityUid uid, DamageOverlayComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var damageDelta = args.DamageDelta.GetTotal();
        var coords = GenerateRandomCoordinates(Transform(uid).Coordinates, component.Radius);

        // Проверка на игнорируемые типы урона
        if (args.DamageDelta.DamageDict.Keys.Any(item => component.IgnoredDamageTypes.Contains(item)))
            return;

        if (_mindSystem.TryGetMind(uid, out _, out var mindTarget) && mindTarget.Session != null)
        {
            if (_disabledSessions.Contains(mindTarget.Session))
                return;

            if (damageDelta > 0)
            {
                _popupSystem.PopupCoordinates($"-{damageDelta}", coords, mindTarget.Session, component.DamagePopupType);
            }
        }

        if (args.Origin == null)
            return;

        if (!_mindSystem.TryGetMind(args.Origin.Value, out _, out var mindOrigin) || mindOrigin.Session == null)
            return;

        if (_disabledSessions.Contains(mindOrigin.Session))
            return;

        if (damageDelta > 0)
        {
            // Ударили себя
            if (args.Origin == uid)
                return;

            _popupSystem.PopupCoordinates($"-{damageDelta}", coords, mindOrigin.Session, component.DamagePopupType);
        }
        else
        {
            _popupSystem.PopupCoordinates($"+{FixedPoint2.Abs(damageDelta)}", coords, mindOrigin.Session, component.HealPopupType);
        }
    }

    private EntityCoordinates GenerateRandomCoordinates(EntityCoordinates center, float radius)
    {
        // Случайное направление в радианах.
        var angle = _random.NextDouble() * 2 * Math.PI;

        // Случайное расстояние от центра в пределах радиуса.
        var distance = _random.NextDouble() * radius;

        // Вычисление смещения.
        var offsetX = (float)(Math.Cos(angle) * distance);
        var offsetY = (float)(Math.Sin(angle) * distance);

        // Создание новых координат с учетом смещения.
        var newPosition = new Vector2(center.Position.X + offsetX, center.Position.Y + offsetY);

        return new EntityCoordinates(center.EntityId, newPosition);
    }
}
