using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Smile;

/// <summary>
/// This handles...
/// </summary>
public sealed class SmileSlimeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private TimeSpan _nextTick = TimeSpan.Zero;
    private readonly TimeSpan _refreshCooldown = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageableComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid target, DamageableComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (!HasComp<SmileSlimeComponent>(args.User))
            return;
        Log.Debug($"OnGetVerbs {args.User} {args.Target}");
    }

    public override void Update(float delay)
    {
        if (_nextTick > _gameTiming.CurTime)
            return;

        _nextTick += _refreshCooldown;

        var query = AllEntityQuery<SmileSlimeComponent>();
        while (query.MoveNext(out var slimeUid, out var smileSlimeComponent))
        {
            foreach (var entity in _entityLookup.GetEntitiesInRange(slimeUid, 2f))
            {
                if (HasComp<SmileSlimeComponent>(entity))
                    continue;
                if (!TryComp<HumanoidAppearanceComponent>(entity, out var humanoidAppearanceComponent))
                    continue;
                _popupSystem.PopupEntity(Loc.GetString(_random.Pick(smileSlimeComponent.Messages)), entity, entity);
            }
        }
    }
}
