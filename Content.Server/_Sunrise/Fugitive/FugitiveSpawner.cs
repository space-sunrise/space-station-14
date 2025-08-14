using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Server.Stunnable;
using Content.Server.Traitor.Uplink;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Maps;
using Content.Shared.NPC.Systems;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Shared.Examine.ExamineSystemShared;

namespace Content.Server._Sunrise.Fugitive
{
    public sealed class FugitiveSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly TileSystem _tile = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly StoreSystem _store = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedSubdermalImplantSystem _subdermalImplant = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;
        [Dependency] private readonly UplinkSystem _uplinkSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FugitiveSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnPlayerAttached(EntityUid uid, FugitiveSpawnerComponent component, PlayerAttachedEvent args)
        {
            var xform = Transform(uid);
            var fugitive = Spawn(component.Prototype, xform.Coordinates);

            if (TryComp<FugitiveCountdownComponent>(fugitive, out var cd))
                cd.AnnounceTime = _timing.CurTime + cd.AnnounceCD;

            _popupSystem.PopupEntity(Loc.GetString("fugitive-spawn", ("name", fugitive)), fugitive,
                Filter.Pvs(fugitive).RemoveWhereAttachedEntity(entity => !_examine.InRangeUnOccluded(
                    fugitive, entity, ExamineRange, null)), true,
                Shared.Popups.PopupType.LargeCaution);

            _stun.TryAddParalyzeDuration(fugitive, TimeSpan.FromSeconds(2));
            _audioSystem.PlayPvs(component.SpawnSoundPath, uid, AudioParams.Default.WithVolume(-6f));

            if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
                return;
            var currentTile = map.GetTileRef(xform.Coordinates);
            _tile.PryTile(currentTile);

            if (!_mindSystem.TryGetMind(args.Player.UserId, out var mindId, out var mind))
                return;

            _mindSystem.TransferTo(mindId.Value, fugitive, ghostCheckOverride: true);

            _popupSystem.PopupEntity(Loc.GetString("fugitive-spawn", ("name", uid)), uid,
                Filter.Pvs(uid).RemoveWhereAttachedEntity(entity => !_examine.InRangeUnOccluded(uid, entity, ExamineRange, null)), true, Shared.Popups.PopupType.LargeCaution);

            foreach (var implantId in component.Implants)
            {
                var implantEnt = Spawn(implantId, xform.Coordinates);

                if (!TryComp<SubdermalImplantComponent>(implantEnt, out var implantComp))
                    return;

                _subdermalImplant.ForceImplant(fugitive, implantEnt, implantComp);
            }

            if (TryComp<ContainerManagerComponent>(fugitive, out var containerManagerComponent))
            {
                if (containerManagerComponent.Containers.TryGetValue("implant", out var container))
                {
                    foreach (var containedEntity in container.ContainedEntities)
                    {
                        if (!TryComp<StoreComponent>(containedEntity, out var storeComponent))
                            continue;
                        _uplinkSystem.SetUplink(fugitive, containedEntity, _random.Next(5, 10), true);
                        _tagSystem.AddTag(containedEntity, "FugitiveUplink");
                    }
                }
            }

            Del(uid);
        }
    }
}
