using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.Animations;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Gravity;
using Content.Shared.Standing;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.Animations;

public sealed class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private readonly SharedStandingStateSystem _sharedStanding = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

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

        component.AnimationId = emoteId;
        Dirty(uid, component);
    }
}
