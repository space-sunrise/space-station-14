using Content.Server.Popups;
using Content.Shared._Sunrise.DamageOverlay;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.DamageOverlay;

// TODO: Рефактор попапов, с целью поддержки передачи цвета, размера и иконок в сам попап, не клепая 999 енумов
// Возможно стоит создать прототипы попапов

public sealed class DamageOverlaySystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private static readonly HashSet<ICommonSession> DisabledSessions = [];
    private static readonly Dictionary<ICommonSession, DamageOverlaySettings> PlayerSettings = new ();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOverlayComponent, DamageChangedEvent>(OnDamageChange);

        SubscribeNetworkEvent<DamageOverlayOptionEvent>(OnDamageOverlayOption);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => CleanUp());
    }

    private static void CleanUp()
    {
        DisabledSessions.Clear();
        PlayerSettings.Clear();
    }

    private static async void OnDamageOverlayOption(DamageOverlayOptionEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Enabled)
            DisabledSessions.Remove(args.SenderSession);
        else
            DisabledSessions.Add(args.SenderSession);

        PlayerSettings.TryAdd(args.SenderSession, new DamageOverlaySettings(ev.SelfEnabled, ev.StructuresEnabled));
    }

    private void OnDamageChange(Entity<DamageOverlayComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var damageDelta = args.DamageDelta.GetTotal();
        var coords = Transform(ent).Coordinates.GetRandomInRadius(ent.Comp.Radius, _random);

        // Идея в том, что попапы должны разделяться на две большие категории: без отправителя и с ним
        // В итоге должен быть только один попап, либо тот, либо этот
        // Поэтому сначала проверяется, является ли попап из категории "без отправителя", и если да - отправляется он, а другой нет

        // Для урона, получаемого от окружения
        // Пример: Космос, огонь и т.д.


        if (_player.TryGetSessionByEntity(ent, out var targetSession))
        {
            // Специально скрыл попапы с пассивной регенерацией, они скорее мешают
            TryCreatePopup(ent, damageDelta, coords, targetSession);

            return;
        }

        // Для урона, имеющего отправителя
        // Пример: Удары игрока, моба и т.д.

        if (args.Origin == null)
            return;

        if (!_player.TryGetSessionByEntity(args.Origin.Value, out var originSession))
            return;

        TryCreatePopup(ent, damageDelta, coords, originSession);
    }

    private bool TryCreatePopup(Entity<DamageOverlayComponent> ent,
        FixedPoint2 damageDelta,
        EntityCoordinates coords,
        ICommonSession session,
        bool showHealPopup = true)
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

    private static bool IsDisabledByClient(ICommonSession session, Entity<DamageOverlayComponent> target)
    {
        if (DisabledSessions.Contains(session))
            return true;

        if (PlayerSettings.TryGetValue(session, out var playerSettings))
        {
            if (target.Comp.IsStructure && !playerSettings.StructureDamage)
                return true;

            if (target == session.AttachedEntity && !playerSettings.SelfDamage)
                return true;
        }

        return false;
    }

    private struct DamageOverlaySettings(bool selfEnabled, bool structuresEnabled)
    {
        public readonly bool SelfDamage = selfEnabled;
        public readonly bool StructureDamage = structuresEnabled;
    }
}
