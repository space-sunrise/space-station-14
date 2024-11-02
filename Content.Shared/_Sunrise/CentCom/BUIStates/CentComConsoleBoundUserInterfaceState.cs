using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CentCom.BUIStates;

[Serializable, NetSerializable]
public sealed class CentComConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsIdPresent = false;
    public readonly string IdName = string.Empty;
    public readonly LinkedStation? Station;

    public CentComConsoleBoundUserInterfaceState(bool isIdPresent, string idName, LinkedStation? station)
    {
        IsIdPresent = isIdPresent;
        IdName = idName;
        Station = station;
    }
}
