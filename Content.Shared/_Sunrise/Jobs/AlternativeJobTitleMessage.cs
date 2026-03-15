using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Jobs;

/// <summary>
/// Отправляется клиентом на сервер перед присоединением к раунду,
/// чтобы выбрать альтернативное название должности.
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectAlternativeJobTitleMsg(ProtoId<JobPrototype> jobId, LocId alternativeTitle)
    : EntityEventArgs
{
    public ProtoId<JobPrototype> JobId = jobId;
    public LocId AlternativeTitle = alternativeTitle;
}
