using Robust.Shared.Prototypes;
using Content.Shared.Access;

namespace Content.Shared._Sunrise.AlertAccess
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype("alertAccessesPrototype")]
    public sealed partial class AlertAccessesPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("access")]
        public List<ProtoId<AccessLevelPrototype>> Access = new();

    }
}
