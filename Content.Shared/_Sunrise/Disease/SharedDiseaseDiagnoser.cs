// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Disease;

[Serializable, NetSerializable]
public enum DiseaseDiagnoserVisuals : byte
{
    Status
}

[Serializable, NetSerializable]
public enum DiseaseDiagnoserStatus : byte
{
    Off,
    On,
    Printing,
    Scanning,
    Denial,
    Successfully,
    GenerateDisease
}