using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.AlertAccess;

namespace Content.Shared.Access
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype("accessLevel")]
    public sealed partial class AccessLevelPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The player-visible name of the access level, in the ID card console and such.
        /// </summary>
        [DataField("name")]
        public string? Name { get; set; }

        [DataField]
        public string CurrentAlertLevel;

        [DataField("redAlertAccesses")]
        public ProtoId<AlertAccessesPrototype> RedAlertAccesses;

        [DataField("yellowAlertAccesses")]
        public ProtoId<AlertAccessesPrototype> YellowAlertAccesses;

        [DataField("gammaAlertAccesses")]
        public ProtoId<AlertAccessesPrototype> GammaAlertAccesses;

        [DataField("blueAlertAccesses")]
        public ProtoId<AlertAccessesPrototype> BlueAlertAccesses;
        public string GetAccessLevelName()
        {
            if (Name is { } name)
                return Loc.GetString(name);

            return ID;
        }
    }
}
