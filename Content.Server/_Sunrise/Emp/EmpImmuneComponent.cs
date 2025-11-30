using Content.Server._Sunrise.Emp;

namespace Content.Server.Emp;

/// <summary>
/// Upon being triggered will EMP area around it.
/// </summary>
[RegisterComponent]
[Access(typeof(EmpImmuneSystem))]
public sealed partial class EmpImmuneComponent : Component
{

}
