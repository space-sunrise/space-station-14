using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._Sunrise.DamageOverlay;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.DamageOverlay;

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

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

        if (_mindSystem.TryGetMind(uid, out _, out var mindTarget) && mindTarget.Session != null)
        {
            if (_disabledSessions.Contains(mindTarget.Session))
                return;

            if (damageDelta > 0)
            {
                _popupSystem.PopupEntity($"-{damageDelta}", uid, mindTarget.Session, PopupType.LargeCaution);
            }
        }

        if (args.Origin == null)
            return;

        if (_mindSystem.TryGetMind(args.Origin.Value, out _, out var mindOrigin) && mindOrigin.Session != null)
        {
            if (_disabledSessions.Contains(mindOrigin.Session))
                return;

            if (damageDelta > 0)
            {
                if (args.Origin == uid)
                    return;

                _popupSystem.PopupEntity($"-{damageDelta}", uid, mindOrigin.Session, PopupType.LargeCaution);
            }
            else
            {
                _popupSystem.PopupEntity($"+{FixedPoint2.Abs(damageDelta)}", uid, mindOrigin.Session, PopupType.LargeGreen);
            }
        }
    }
}
