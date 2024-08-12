using Content.Shared._Sunrise.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.Stunnable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTelescopicbatonSystem))]
public sealed partial class TelescopicbatonComponent : Component
{
	
}