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

// TODO: Рефактор попапов, с целью поддержки передачи цвета, размера и иконок в сам попап, не клепая 999 енумов
// Возможно стоит создать прототипы попапов

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private readonly List<ICommonSession> _disabledSessions = [];
    private readonly Dictionary<ICommonSession, DamageOverlaySettings> _playerSettings = new ();

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

        _playerSettings[args.SenderSession] = new DamageOverlaySettings(ev.SelfEnabled, ev.StructuresEnabled);
    }

    private void OnDamageChange(Entity<DamageOverlayComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var damageDelta = args.DamageDelta.GetTotal();
        var coords = GenerateRandomCoordinates(Transform(ent).Coordinates, ent.Comp.Radius);

        // Идея в том, что попапы должны разделяться на две большие категории: без отправителя и с ним
        // В итоге должен быть только один попап, либо тот, либо этот
        // Поэтому сначала проверяется, является ли попап из категории "без отправителя", и если да - отправляется он, а другой нет

        // Для урона, получаемого от окружения
        // Пример: Космос, огонь и т.д.


        if (_mindSystem.TryGetMind(ent, out _, out var mindTarget) && _player.TryGetSessionById(mindTarget.UserId, out var sessionTarget))
        {
            // Специально скрыл попапы с пассивной регенерацией, они скорее мешают
            TryCreatePopup(ent, damageDelta, coords, sessionTarget);

            return;
        }

        // Для урона, имеющего отправителя
        // Пример: Удары игрока, моба и т.д.

        if (args.Origin == null)
            return;

        if (!_mindSystem.TryGetMind(args.Origin.Value, out _, out var mindOrigin) || !_player.TryGetSessionById(mindOrigin.UserId, out var sessionOrigin))
            return;

        TryCreatePopup(ent, damageDelta, coords, sessionOrigin);
    }

    private EntityCoordinates GenerateRandomCoordinates(EntityCoordinates center, float radius)
    {
        var angle = _random.NextDouble() * 2 * Math.PI;

        var distance = _random.NextDouble() * radius;

        var offsetX = (float)(Math.Cos(angle) * distance);
        var offsetY = (float)(Math.Sin(angle) * distance);

        var newPosition = new Vector2(center.Position.X + offsetX, center.Position.Y + offsetY);

        return new EntityCoordinates(center.EntityId, newPosition);
    }

    private bool TryCreatePopup(Entity<DamageOverlayComponent> ent, FixedPoint2 damageDelta, EntityCoordinates coords, ICommonSession session, bool showHealPopup = true)
    {
        if (IsDisabledByClient(session, ent))
            return false;

        if (damageDelta > 0)
        {
            _popupSystem.PopupCoordinates($"-{damageDelta}", coords, session, ent.Comp.DamagePopupType);
        }
        else if (showHealPopup)
        {
            // Лечение меньше 1 это пасивный реген, его показывать не нужно.
            if (damageDelta < -1)
                _popupSystem.PopupCoordinates($"+{FixedPoint2.Abs(damageDelta)}", coords, session, ent.Comp.HealPopupType);
        }

        return true;
    }

    private bool IsDisabledByClient(ICommonSession session, Entity<DamageOverlayComponent> target)
    {
        if (_disabledSessions.Contains(session))
            return true;

        if (_playerSettings.TryGetValue(session, out var playerSettings))
        {
            if (target.Comp.IsStructure && !playerSettings.StructureDamage)
                return true;

            if (target == session.AttachedEntity && !playerSettings.SelfDamage)
                return true;
        }

        return false;
    }

    private struct DamageOverlaySettings
    {
        public readonly bool StructureDamage;

        public readonly bool SelfDamage;

        public DamageOverlaySettings(bool evSelfEnabled, bool evStructuresEnabled)
        {
            SelfDamage = evSelfEnabled;

            StructureDamage = evStructuresEnabled;
        }
    }
}
