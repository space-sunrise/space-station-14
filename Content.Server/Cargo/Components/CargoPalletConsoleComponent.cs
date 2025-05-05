using Content.Server.Cargo.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
[Access(typeof(CargoSystem))]
public sealed partial class CargoPalletConsoleComponent : Component
{
    [DataField]
    public SoundSpecifier ErrorSound = new SoundCollectionSpecifier("CargoError");
}
