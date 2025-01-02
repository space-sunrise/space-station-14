using System.Linq;
using System.Numerics;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._Sunrise.DamageOverlay;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.DamageOverlay;

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly List<ICommonSession> _disabledSessions = new();
    private readonly Dictionary<ICommonSession, DamageOverlayPrototype> _playerSettings = new ();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOverlayComponent, DamageChangedEvent>(OnDamageChange);

        SubscribeNetworkEvent<DamageOverlayOptionEvent>(OnDamageOverlayOption);
        SubscribeNetworkEvent<DamageOverlayPresetChangedEvent>(OnDamageOverlayPresetChanged);
    }

    private async void OnDamageOverlayOption(DamageOverlayOptionEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Enabled)
            _disabledSessions.Remove(args.SenderSession);
        else
            _disabledSessions.Add(args.SenderSession);
    }

    private async void OnDamageOverlayPresetChanged(DamageOverlayPresetChangedEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
            return;

        if (!_prototype.TryIndex(ev.Preset, out var presetPrototype))
            return;

        _playerSettings[args.SenderSession] = presetPrototype;
    }

    private void OnDamageChange(Entity<DamageOverlayComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var damageDelta = args.DamageDelta.GetTotal();
        var coords = GenerateRandomCoordinates(Transform(ent).Coordinates, ent.Comp.Radius);

        // Проверка на игнорируемые типы урона
        if (args.DamageDelta.DamageDict.Keys.Any(item => ent.Comp.IgnoredDamageTypes.Contains(item)))
            return;

        if (_mindSystem.TryGetMind(ent, out _, out var mindTarget) && mindTarget.Session != null)
        {
            if (IsDisabledByClient(mindTarget.Session, ent, args.DamageDelta))
                return;

            if (damageDelta > 0)
            {
                _popupSystem.PopupCoordinates($"-{damageDelta}", coords, mindTarget.Session, ent.Comp.DamagePopupType);
            }
        }

        if (args.Origin == null)
            return;

        if (!_mindSystem.TryGetMind(args.Origin.Value, out _, out var mindOrigin) || mindOrigin.Session == null)
            return;

        if (IsDisabledByClient(mindOrigin.Session, ent, args.DamageDelta))
            return;

        if (damageDelta > 0)
        {
            // Ударили себя
            if (args.Origin == ent)
                return;

            _popupSystem.PopupCoordinates($"-{damageDelta}", coords, mindOrigin.Session, ent.Comp.DamagePopupType);
        }
        else
        {
            _popupSystem.PopupCoordinates($"+{FixedPoint2.Abs(damageDelta)}", coords, mindOrigin.Session, ent.Comp.HealPopupType);
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

    /// <summary>
    /// Проверка на то, включен ли у игрока данный урон для отображения
    /// </summary>
    private bool IsDisabledByClient(ICommonSession session, Entity<DamageOverlayComponent> target, DamageSpecifier damageDelta)
    {
        if (_disabledSessions.Contains(session))
            return true;

        if (_playerSettings.TryGetValue(session, out var playerPreset))
        {
            if (damageDelta.DamageDict.Keys.Any(item => playerPreset.Types.Contains(item)))
                return true;

            if (target.Comp.IsStructure && !playerPreset.StructureDamageEnabled)
                return true;

            if (target == session.AttachedEntity && !playerPreset.ToPlayerDamageEnabled)
                return true;
        }

        return false;
    }
}
