using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.HardsuitInjection.EntitySystems;

[Serializable, NetSerializable]
public sealed partial class AmpulaInsertDoAfterEvent : SimpleDoAfterEvent;