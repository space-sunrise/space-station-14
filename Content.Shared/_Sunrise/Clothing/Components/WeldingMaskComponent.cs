using Robust.Shared.GameStates;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Sunrise.Clothing.EntitySystems;
using Content.Shared.Sunrise.Clothing.Components;
using Content.Shared.Clothing;

namespace Content.Shared.Sunrise.Clothing.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(WeldingMaskSystem))]
public sealed partial class WeldingMaskComponent : Component
{
}