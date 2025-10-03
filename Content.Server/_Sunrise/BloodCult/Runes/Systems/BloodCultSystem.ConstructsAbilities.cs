using Content.Server._Sunrise.BloodCult.GameRule;
using Content.Server._Sunrise.IncorporealSystem;
using Content.Shared._Sunrise.BloodCult;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;

namespace Content.Server._Sunrise.BloodCult.Runes.Systems
{
    public partial class BloodCultSystem
    {
        public void InitializeConstructsAbilities()
        {
            SubscribeLocalEvent<ArtificerCreateSoulStoneActionEvent>(OnArtificerCreateSoulStone);
            SubscribeLocalEvent<ArtificerCreateConstructShellActionEvent>(OnArtificerCreateConstructShell);
            SubscribeLocalEvent<ArtificerConvertCultistFloorActionEvent>(OnArtificerConvertCultistFloor);
            SubscribeLocalEvent<ArtificerCreateCultistWallActionEvent>(OnArtificerCreateCultistWall);
            SubscribeLocalEvent<ArtificerCreateCultistAirlockActionEvent>(OnArtificerCreateCultistAirlock);

            SubscribeLocalEvent<WraithPhaseActionEvent>(OnWraithPhase);
            SubscribeLocalEvent<IncorporealComponent, AttackAttemptEvent>(OnAttackAttempt);

            SubscribeLocalEvent<JuggernautCreateWallActionEvent>(OnJuggernautCreateWall);

            SubscribeLocalEvent<ConstructComponent, ComponentInit>(OnConstructInit);
        }

        private void OnConstructInit(EntityUid uid, ConstructComponent component, ComponentInit args)
        {
            var ev = new UpdateCultAppearance();
            RaiseLocalEvent(ev);

            foreach (var action in component.Actions)
            {
                _actionsSystem.AddAction(uid, action, uid);
            }
        }

        private void OnArtificerCreateSoulStone(ArtificerCreateSoulStoneActionEvent ev)
        {
            var transform = Transform(ev.Performer);
            Spawn(ev.SoulStonePrototypeId, transform.Coordinates);

            ev.Handled = true;
        }

        private void OnArtificerCreateConstructShell(ArtificerCreateConstructShellActionEvent ev)
        {
            var transform = Transform(ev.Performer);
            Spawn(ev.ShellPrototypeId, transform.Coordinates);

            ev.Handled = true;
        }

        private void OnArtificerConvertCultistFloor(ArtificerConvertCultistFloorActionEvent ev)
        {
            var transform = Transform(ev.Performer);
            var gridUid = transform.GridUid;

            if (!gridUid.HasValue)
            {
                _popupSystem.PopupEntity("Нельзя строить в космосе...", ev.Performer, ev.Performer);
                return;
            }

            var tileRef = _turf.GetTileRef(transform.Coordinates);

            if (!tileRef.HasValue)
            {
                _popupSystem.PopupEntity("Нельзя строить в космосе...", ev.Performer, ev.Performer);
                return;
            }

            var cultistTileDefinition = (ContentTileDefinition)_tileDefinition[ev.FloorTileId];
            _tileSystem.ReplaceTile(tileRef.Value, cultistTileDefinition);
            Spawn(CultTileEffectPrototypeId, transform.Coordinates);
            ev.Handled = true;
        }

        private void OnArtificerCreateCultistWall(ArtificerCreateCultistWallActionEvent ev)
        {
            if (!TrySpawnWall(ev.Performer, ev.WallPrototypeId))
            {
                return;
            }

            ev.Handled = true;
        }

        private void OnArtificerCreateCultistAirlock(ArtificerCreateCultistAirlockActionEvent ev)
        {
            if (!TrySpawnWall(ev.Performer, ev.AirlockPrototypeId))
            {
                return;
            }

            ev.Handled = true;
        }

        private void OnWraithPhase(WraithPhaseActionEvent ev)
        {
            if (_statusEffectsSystem.HasStatusEffect(ev.Performer, ev.StatusEffectId))
            {
                _popupSystem.PopupEntity("Вы уже в потустороннем мире", ev.Performer, ev.Performer);
                return;
            }

            _statusEffectsSystem.TryAddStatusEffect<IncorporealComponent>(ev.Performer,
                ev.StatusEffectId,
                TimeSpan.FromSeconds(ev.Duration),
                false);

            ev.Handled = true;
        }

        private void OnAttackAttempt(EntityUid uid, IncorporealComponent component, AttackAttemptEvent args)
        {
            if (_statusEffectsSystem.HasStatusEffect(args.Uid, "Incorporeal"))
            {
                _statusEffectsSystem.TryRemoveStatusEffect(args.Uid, "Incorporeal");
            }
        }

        private void OnJuggernautCreateWall(JuggernautCreateWallActionEvent ev)
        {
            if (!TrySpawnWall(ev.Performer, ev.WallPrototypeId))
            {
                return;
            }

            ev.Handled = true;
        }

        private bool TrySpawnWall(EntityUid performer, string wallPrototypeId)
        {
            var xform = Transform(performer);

            var offsetValue = xform.LocalRotation.ToWorldVec().Normalized();
            var coords = xform.Coordinates.Offset(offsetValue).SnapToGrid(_entityManager);
            var tile = _turf.GetTileRef(coords);
            if (tile == null)
                return false;

            // Check there are no walls there
            if (_turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable))
            {
                _popupSystem.PopupEntity(Robust.Shared.Localization.Loc.GetString("mime-invisible-wall-failed"),
                    performer,
                    performer);
                return false;
            }

            // Check there are no mobs there
            foreach (var entity in _lookup.GetLocalEntitiesIntersecting(tile.Value, 0f))
            {
                if (HasComp<MobStateComponent>(entity) && entity != performer)
                {
                    _popupSystem.PopupEntity(Robust.Shared.Localization.Loc.GetString("mime-invisible-wall-failed"),
                        performer,
                        performer);
                    return false;
                }
            }

            _popupSystem.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("mime-invisible-wall-popup", ("mime", performer)),
                performer);
            // Make sure we set the invisible wall to despawn properly
            Spawn(wallPrototypeId, coords);
            return true;
        }
    }
}
