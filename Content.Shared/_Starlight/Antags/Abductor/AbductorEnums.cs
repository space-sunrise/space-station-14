﻿using Robust.Shared.Serialization;

namespace Content.Shared.Starlight.Antags.Abductor;

[Serializable, NetSerializable]
public enum AbductorExperimentatorVisuals : byte
{
    Full
}
[Serializable, NetSerializable]
public enum AbductorOrganType : byte
{
    None,
    Health,
    Plasma,
    Gravity,
    Egg,
    Spider
}
[Serializable, NetSerializable]
public enum AbductorCameraConsoleUIKey
{
    Key
}

[Serializable, NetSerializable]
public enum AbductorConsoleUIKey
{
    Key
}