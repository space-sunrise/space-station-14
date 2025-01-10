using Content.Shared.Chat.Prototypes;

namespace Content.Shared.Emoting
{
    public sealed class EmoteAttemptEvent : CancellableEntityEventArgs
    {
        public EmoteAttemptEvent(EntityUid uid)
        {
            Uid = uid;
        }

        public EntityUid Uid { get; }
    }

    public sealed class AnimationEmoteAttemptEvent : CancellableEntityEventArgs
    {
        public AnimationEmoteAttemptEvent(EntityUid uid, EmotePrototype emote)
        {
            Uid = uid;
            Emote = emote;
        }

        public EntityUid Uid { get; }

        [ViewVariables, DataField("emote", readOnly: true, required: true)]
        public EmotePrototype Emote = default!;
    }
}
