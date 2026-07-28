using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.TTS;

/// <summary>
/// Adds a detailed examine verb that shows the TTS voice assigned to an entity.
/// </summary>
public sealed class TTSExamineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TTSComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamine);
    }

    private void OnGetExamine(Entity<TTSComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        if (ent.Comp.VoicePrototypeId is not { } voiceId ||
            !_prototype.TryIndex(voiceId, out TTSVoicePrototype? voice))
        {
            return;
        }

        var message = new FormattedMessage();
        var voiceName = Loc.GetString(voice.Name);
        message.AddMarkupOrThrow(Loc.GetString(
            "tts-examine",
            ("ent", ent.Owner),
            ("voice", FormattedMessage.EscapeText(voiceName))));

        _examine.AddDetailedExamineVerb(
            args,
            ent.Comp,
            message,
            Loc.GetString("tts-examinable-verb-text"),
            iconTexture: "/Textures/Interface/Actions/scream.png",
            hoverMessage: Loc.GetString("tts-examinable-verb-message"));
    }
}
