using Content.Server.Popups;
using Content.Server.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.Sunrise.Sandevistan
{
    public sealed class SandevistanSystem : EntitySystem
    {
        [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
        [Dependency] private readonly ProjectileSystem _projectiles = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IConsoleShell _shell = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SandevistanComponent, SandevistanActivateEvent>(OnActivate);
            SubscribeLocalEvent<SandevistanComponent, ProjectileCollideEvent>(OnProjectileHit);

            _shell.RegisterCommand(new SandevistanCommand(this));
        }

        private void OnActivate(EntityUid uid, SandevistanComponent comp, SandevistanActivateEvent args)
        {
            if (comp.IsActive) return;

            comp.IsActive = true;
            _movement.ChangeBaseSpeed(uid, comp.SpeedMultiplier);
            _popup.PopupEntity("САНДЕВИСТАН АКТИВИРОВАН!", uid, Filter.Pvs(uid));

            Timer.Spawn(TimeSpan.FromSeconds(comp.Duration), () =>
            {
                comp.IsActive = false;
                _movement.ResetBaseSpeed(uid);
                _popup.PopupEntity("Сандевистан деактивирован.", uid, Filter.Pvs(uid));
            });
        }

        private void OnProjectileHit(EntityUid uid, SandevistanComponent comp, ProjectileCollideEvent args)
        {
            if (!comp.IsActive || !_random.Prob(comp.ProjectileReflectChance))
                return;

            _projectiles.ReflectProjectile(args.Projectile, args.OtherEntity);
            _popup.PopupEntity("Снаряд отражён!", uid, Filter.Pvs(uid));
        }

        public void ActivateSandevistan(EntityUid uid)
        {
            RaiseLocalEvent(uid, new SandevistanActivateEvent());
        }
    }
}
