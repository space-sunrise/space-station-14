// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server.Chat.Systems;
using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.TimeWindow;
using Content.Shared.Chat;
using Content.Shared._Nox.Disease.Symptoms;

namespace Content.Server._Nox.Disease.Symptoms;

[DiseaseSymptom("RashSymptom")]
public sealed class RashSymptom : DiseaseSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    private const string RashEmote = "чешется";

    public RashSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent virus)
    {
        base.OnAdded(host, virus);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent virus)
    {
        base.OnRemoved(host, virus);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent virus)
    {
        base.OnUpdate(host, virus);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent virus)
    {
        var chatSystem = _entityManager.System<ChatSystem>();

        chatSystem.TrySendInGameICMessage(host,
                            RashEmote,
                            InGameICChatType.Emote,
                            ChatTransmitRange.Normal);
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new RashSymptom(EffectTimedWindow.Clone());
    }
}
