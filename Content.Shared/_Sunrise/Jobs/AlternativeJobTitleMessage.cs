using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Jobs;

/// <summary>
/// Отправляется клиентом на сервер перед присоединением к раунду,
/// чтобы выбрать альтернативное название должности.
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectAlternativeJobTitleMsg : EntityEventArgs
{
    public ProtoId<JobPrototype> JobId;
    public LocId AlternativeTitle;

    public SelectAlternativeJobTitleMsg(ProtoId<JobPrototype> jobId, LocId alternativeTitle)
    {
        JobId = jobId;
        AlternativeTitle = alternativeTitle;
    }
}
