using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CentCom.BUIStates;

[Serializable, NetSerializable]
public sealed class CentComConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsIdPresent = false;
    public readonly bool IdEnoughPermissions = false;
    public readonly string IdName = string.Empty;
    public readonly LinkedStation? Station;
    public NetEntity Owner;
    public bool SentEvac;
    public TimeSpan? LeftBeforeEvac;

    public CentComConsoleBoundUserInterfaceState(bool isIdPresent, bool idEnoughPermissions, string idName, LinkedStation? station, NetEntity owner, bool sentEvac, TimeSpan? leftBeforeEvac = null)
    {
        IsIdPresent = isIdPresent;
        IdEnoughPermissions = idEnoughPermissions;
        IdName = idName;
        Station = station;
        Owner = owner;
        SentEvac = sentEvac;
        LeftBeforeEvac = leftBeforeEvac;
    }
}
