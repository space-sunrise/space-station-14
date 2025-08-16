using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.Animations;
using Content.Shared._Sunrise.Flip;
using Content.Shared._Sunrise.Jump;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Emoting;
using Content.Shared.Gravity;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Animations;

public sealed class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedJumpSystem _jumpSystem = default!;
    [Dependency] private readonly SharedFlipSystem _flipSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmoteAnimationComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EmoteAnimationComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<EmoteAnimationComponent, PlayEmoteMessage>(OnPlayEmote);
    }

    private void OnPlayEmote(EntityUid uid, EmoteAnimationComponent component, PlayEmoteMessage args)
    {
        if (!_prototypeManager.TryIndex(args.ProtoId, out var proto))
            return;

        _chat.TryEmoteWithChat(uid, proto.ID);
    }

    private void OnGetState(EntityUid uid, EmoteAnimationComponent component, ref ComponentGetState args)
    {
        args.State = new EmoteAnimationComponent.EmoteAnimationComponentState(component.AnimationId);
    }

    private void OnEmote(EntityUid uid, EmoteAnimationComponent component, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Verb))
            return;

        PlayEmoteAnimation(uid, component, args.Emote.ID);
    }

    public void PlayEmoteAnimation(EntityUid uid, EmoteAnimationComponent component, string emoteId)
    {
        if (emoteId == "Lay")
        {
            if (_gravity.IsWeightless(uid))
                return;

            if (_standing.IsDown(uid))
                _stun.TryStanding(uid);
            else
                _stun.TryKnockdown(uid, TimeSpan.FromSeconds(0.5), true, false, false);

            return;
        }

        if (emoteId == "Jump")
        {
            _jumpSystem.TryJump(uid);
        }

        if (emoteId == "Flip")
        {
            _flipSystem.TryFlip(uid);
        }

        if (emoteId == "FallOnNeck")
        {
            var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 100);
            _damageableSystem.TryChangeDamage(uid, damage, true, useVariance: false, useModifier: false);
        }

        component.AnimationId = emoteId;
        Dirty(uid, component);
    }
}
