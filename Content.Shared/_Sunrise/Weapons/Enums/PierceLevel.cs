// Sunrise-Edit

using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Weapons.Enums;

[Serializable, NetSerializable]
public enum PierceLevel : byte
{
    Flesh,
    Wood,
    Metal,
    HardenedMetal,
    Rock,
}
