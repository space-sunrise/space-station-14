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

        if (_mindSystem.TryGetMind(ent, out _, out var mindTarget) && mindTarget.Session != null)
        {
            if (IsDisabledByClient(mindTarget.Session, ent))
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

        if (IsDisabledByClient(mindOrigin.Session, ent))
            return;

        if (damageDelta > 0)
        {
            _popupSystem.PopupCoordinates($"-{damageDelta}", coords, mindOrigin.Session, ent.Comp.DamagePopupType);
        }
        else
        {
            _popupSystem.PopupCoordinates($"+{FixedPoint2.Abs(damageDelta)}", coords, mindOrigin.Session, ent.Comp.HealPopupType);
        }
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

    private sealed class DamageOverlaySettings
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
