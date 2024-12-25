using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.Animations;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Animations;

public sealed class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private readonly SharedStandingStateSystem _sharedStanding = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly StaminaSystem _staminaSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    public static string JumpStatusEffectKey = "Jump";

    public override void Initialize()
    {
        SubscribeLocalEvent<EmoteAnimationComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<EmoteAnimationComponent, EmoteEvent>(OnEmote);
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
        if (emoteId == "EmoteLay")
        {
            if (_gravity.IsWeightless(uid))
                return;

            if (_sharedStanding.IsDown(uid))
                _sharedStanding.TryStandUp(uid);
            else
                _sharedStanding.TryLieDown(uid);

            return;
        }

        if (emoteId == "EmoteJump")
        {
            if (_gravity.IsWeightless(uid))
                return;

            // Мейби в будущем
            //_staminaSystem.TakeStaminaDamage(uid, 10);

            // Временная ржомба
            _audioSystem.PlayEntity("/Audio/_Sunrise/jump_mario.ogg", Filter.Pvs(uid), uid, true, AudioParams.Default);

            _statusEffects.TryAddStatusEffect<JumpComponent>(uid,
                JumpStatusEffectKey,
                TimeSpan.FromMilliseconds(500),
                false);
        }

        component.AnimationId = emoteId;
        Dirty(uid, component);
    }
}
