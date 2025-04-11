using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Medical.Surgery.Events;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
[ByRefEvent]
public record struct SurgeryOrganImplantationCompleted(EntityUid Body, EntityUid Part, EntityUid Organ);
[ByRefEvent]
public record struct SurgeryOrganExtracted(EntityUid Body, EntityUid Part, EntityUid Organ);
