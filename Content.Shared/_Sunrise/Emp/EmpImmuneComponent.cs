using Content.Shared._Sunrise.Emp;

namespace Content.Shared._Sunrise.Emp;

/// <summary>
/// Upon being triggered will EMP area around it.
/// </summary>
[RegisterComponent]
[Access(typeof(EmpImmuneSystem))]
public sealed partial class EmpImmuneComponent : Component
{
}
