using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.Animations;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.Animations;

public sealed class EmoteAnimationSystem : EntitySystem
{
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
        component.AnimationId = emoteId;
        Dirty(uid, component);
    }
}
